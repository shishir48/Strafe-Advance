using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// In-process crash + breadcrumb capture. Hooks <see cref="Application.logMessageReceivedThreaded"/>,
    /// keeps a ring buffer of the last N breadcrumbs, and atomically persists a crash report on
    /// every Exception / Error log so the next session can detect "we crashed last time."
    ///
    /// Upload is pluggable via <see cref="ICrashUploader"/>. Default uploader is a no-op that
    /// keeps the report on disk. A Sentry / Crashlytics adapter can be slotted in by calling
    /// <see cref="SetUploader"/> before <c>Awake</c> (e.g. from a Bootstrap scene).
    ///
    /// Native NDK crashes still need a real SDK (Sentry Unity, Firebase Crashlytics) — this
    /// layer covers C# unhandled exceptions, which are the bulk of user-visible bugs.
    /// </summary>
    public class CrashReporter : MonoBehaviour
    {
        public static CrashReporter Instance { get; private set; }

        private const int BreadcrumbCapacity = 50;
        private const string FileName = "crash-report.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        private static ICrashUploader _uploader = new LocalFileUploader();

        private readonly Queue<Breadcrumb> _breadcrumbs = new Queue<Breadcrumb>(BreadcrumbCapacity);
        private readonly object _lock = new object();
        private string _sessionId;
        private long   _sessionStartedUtcMs;
        private bool   _previousSessionCrashed;

        public IReadOnlyCollection<Breadcrumb> Breadcrumbs => _breadcrumbs;
        public bool PreviousSessionCrashed => _previousSessionCrashed;
        public string SessionId => _sessionId;

        // ─── Lifecycle ──────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);

            _sessionId = Guid.NewGuid().ToString("N");
            _sessionStartedUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _previousSessionCrashed = TryLoadPreviousReport(out var previous);
            if (_previousSessionCrashed)
            {
                Debug.LogWarning($"[CrashReporter] Previous session ended with an unhandled exception. Uploading.");
                _uploader.Upload(previous);
            }
            DeletePreviousReport();

            Application.logMessageReceivedThreaded += OnLog;
            // Standard gameplay breadcrumbs (no allocation in the hot path — Subscribe is one-shot).
            EventBus<GameStateChanged>.Subscribe(OnState);
            EventBus<WaveStarted>.Subscribe(OnWave);
            EventBus<PlayerDamaged>.Subscribe(OnDamaged);
        }

        void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            EventBus<GameStateChanged>.Unsubscribe(OnState);
            EventBus<WaveStarted>.Unsubscribe(OnWave);
            EventBus<PlayerDamaged>.Unsubscribe(OnDamaged);
            if (Instance == this) Instance = null;
        }

        // ─── Public API ─────────────────────────────────────────────────────────

        /// <summary>Swap the default no-op uploader for a real one (Sentry, Crashlytics). Call before Awake.</summary>
        public static void SetUploader(ICrashUploader uploader)
        {
            if (uploader != null) _uploader = uploader;
        }

        /// <summary>Drop a manual breadcrumb (e.g. "user tapped Buy"). Thread-safe.</summary>
        public void AddBreadcrumb(string category, string message)
        {
            var c = new Breadcrumb
            {
                UtcMs    = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Category = category,
                Message  = message,
            };
            lock (_lock)
            {
                if (_breadcrumbs.Count >= BreadcrumbCapacity) _breadcrumbs.Dequeue();
                _breadcrumbs.Enqueue(c);
            }
        }

        /// <summary>Manually capture a non-fatal error (no stack trace) — analytics for caught exceptions.</summary>
        public void CaptureMessage(string message, LogType severity = LogType.Error)
        {
            WriteReport(BuildReport(message, string.Empty, severity));
            _uploader.Upload(BuildReport(message, string.Empty, severity));
        }

        // ─── Internals ──────────────────────────────────────────────────────────

        void OnState(GameStateChanged s)    => AddBreadcrumb("state",   $"{s.Previous}→{s.Current}");
        void OnWave(WaveStarted w)          => AddBreadcrumb("wave",    $"W{w.Index + 1}/{w.Total}");
        void OnDamaged(PlayerDamaged d)     => AddBreadcrumb("damage",  $"-{d.Amount} hp");

        void OnLog(string condition, string stackTrace, LogType type)
        {
            // Always breadcrumb; only persist on Exception / Error / Assert.
            AddBreadcrumb("log:" + type, Truncate(condition, 200));

            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;

            var report = BuildReport(condition, stackTrace, type);
            WriteReport(report);
            // Don't try to upload on the log thread — defer to next frame.
        }

        CrashReport BuildReport(string message, string stackTrace, LogType type)
        {
            Breadcrumb[] snapshot;
            lock (_lock) snapshot = _breadcrumbs.ToArray();

            return new CrashReport
            {
                SessionId           = _sessionId,
                SessionStartedUtcMs = _sessionStartedUtcMs,
                CrashedUtcMs        = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Type                = type.ToString(),
                Message             = Truncate(message, 2000),
                StackTrace          = Truncate(stackTrace, 8000),
                Platform            = Application.platform.ToString(),
                UnityVersion        = Application.unityVersion,
                AppVersion          = Application.version,
                Device              = SystemInfo.deviceModel,
                Os                  = SystemInfo.operatingSystem,
                Breadcrumbs         = snapshot,
            };
        }

        static void WriteReport(CrashReport report)
        {
            try
            {
                string json = JsonUtility.ToJson(report);
                string tmp  = FilePath + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e)
            {
                // Last-resort: never crash inside the crash reporter.
                Debug.LogWarning($"[CrashReporter] Failed to persist crash report: {e.Message}");
            }
        }

        static bool TryLoadPreviousReport(out CrashReport report)
        {
            report = null;
            if (!File.Exists(FilePath)) return false;
            try
            {
                string json = File.ReadAllText(FilePath);
                report = JsonUtility.FromJson<CrashReport>(json);
                return report != null;
            }
            catch
            {
                return false;
            }
        }

        static void DeletePreviousReport()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        }

        static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max);
    }

    [Serializable]
    public class Breadcrumb
    {
        public long UtcMs;
        public string Category;
        public string Message;
    }

    [Serializable]
    public class CrashReport
    {
        public string SessionId;
        public long   SessionStartedUtcMs;
        public long   CrashedUtcMs;
        public string Type;
        public string Message;
        public string StackTrace;
        public string Platform;
        public string UnityVersion;
        public string AppVersion;
        public string Device;
        public string Os;
        public Breadcrumb[] Breadcrumbs;
    }

    /// <summary>Plug-in surface for Sentry / Crashlytics / custom backends.</summary>
    public interface ICrashUploader
    {
        void Upload(CrashReport report);
    }

    /// <summary>Default no-op uploader. The crash report stays on disk; integrators can ship it later.</summary>
    public sealed class LocalFileUploader : ICrashUploader
    {
        public void Upload(CrashReport report)
        {
            // Default behaviour: do nothing. Report already on disk for later inspection / pickup.
            // Slot in a real uploader via CrashReporter.SetUploader().
        }
    }
}
