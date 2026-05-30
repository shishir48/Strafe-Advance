using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using StrafAdvance;

namespace StrafAdvance.Tests.PlayMode
{
    /// <summary>
    /// Sprint 7.6 — verifies the crash pipeline end-to-end: when an unhandled exception / error is
    /// logged while a CrashReporter is live, it must atomically persist crash-report.json with the
    /// message + breadcrumbs intact, so the next session can detect and upload it.
    ///
    /// PlayMode (not EditMode): CrashReporter.Awake registers the log hook, and Awake only runs at
    /// runtime — an EditMode AddComponent never wires the pipeline.
    /// </summary>
    public class CrashReporterPipelineTests
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "crash-report.json");

        GameObject _go;
        CrashReporter _cr;

        [SetUp]
        public void SetUp()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            _go = new GameObject("CrashReporterPipelineTest");
            _cr = _go.AddComponent<CrashReporter>();
        }

        [TearDown]
        public void TearDown()
        {
            // DestroyImmediate (not Destroy) so the singleton's OnDestroy clears Instance synchronously —
            // otherwise the next test's CrashReporter.Awake self-destroys against the lingering instance.
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }

        [UnityTest]
        public IEnumerator LoggedException_WritesCrashReportToDisk()
        {
            yield return null; // let Awake register the log hook
            _cr.AddBreadcrumb("test", "before-crash");

            const string msg = "Synthetic NRE for pipeline test";
            // Unity logs an exception as "Exception: <msg>" — match by regex on the message body.
            LogAssert.Expect(LogType.Exception, new Regex(Regex.Escape(msg)));
            Debug.LogException(new Exception(msg));

            yield return WaitForFile();
            Assert.IsTrue(File.Exists(FilePath), "CrashReporter did not persist crash-report.json on exception.");

            var report = JsonUtility.FromJson<CrashReport>(File.ReadAllText(FilePath));
            Assert.IsNotNull(report, "crash-report.json did not parse into a CrashReport.");
            Assert.AreEqual(LogType.Exception.ToString(), report.Type);
            StringAssert.Contains(msg, report.Message);
            Assert.IsNotNull(report.Breadcrumbs);
            Assert.Greater(report.Breadcrumbs.Length, 0, "Breadcrumb trail should be captured in the report.");
        }

        [UnityTest]
        public IEnumerator LoggedError_AlsoPersistsReport()
        {
            yield return null;

            const string msg = "Synthetic error for pipeline test";
            LogAssert.Expect(LogType.Error, msg);
            Debug.LogError(msg);

            yield return WaitForFile();
            Assert.IsTrue(File.Exists(FilePath), "CrashReporter did not persist a report on LogType.Error.");
            var report = JsonUtility.FromJson<CrashReport>(File.ReadAllText(FilePath));
            Assert.AreEqual(LogType.Error.ToString(), report.Type);
        }

        // logMessageReceivedThreaded fires on the logging thread — poll a few frames for the write.
        static IEnumerator WaitForFile(int maxFrames = 30)
        {
            for (int i = 0; i < maxFrames && !File.Exists(FilePath); i++)
                yield return null;
        }
    }
}
