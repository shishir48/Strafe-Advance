using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class CrashReporterTests
    {
        GameObject _go;
        CrashReporter _cr;

        [SetUp]
        public void SetUp()
        {
            // Wipe any leftover report from a previous test run to prevent "previous session crashed" false positives.
            string path = System.IO.Path.Combine(Application.persistentDataPath, "crash-report.json");
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            _go = new GameObject("CrashReporterTest");
            _cr = _go.AddComponent<CrashReporter>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            string path = System.IO.Path.Combine(Application.persistentDataPath, "crash-report.json");
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        [Test]
        public void AddBreadcrumb_AppendsToRing()
        {
            _cr.AddBreadcrumb("test", "alpha");
            _cr.AddBreadcrumb("test", "bravo");
            Assert.AreEqual(2, _cr.Breadcrumbs.Count);
        }

        [Test]
        public void BreadcrumbRing_CapsAtFifty()
        {
            for (int i = 0; i < 75; i++) _cr.AddBreadcrumb("flood", $"msg_{i}");
            Assert.AreEqual(50, _cr.Breadcrumbs.Count);
        }

        [Test]
        public void CrashReport_RoundtripsThroughJson()
        {
            var report = new CrashReport
            {
                SessionId = "abc123",
                Type = "Exception",
                Message = "NullRef in Update",
                StackTrace = "at PlayerController.Update()",
                Platform = "Android",
                Breadcrumbs = new[]
                {
                    new Breadcrumb { Category = "state", Message = "Menu->Playing", UtcMs = 1000 },
                    new Breadcrumb { Category = "wave",  Message = "W1/10",       UtcMs = 1500 },
                },
            };
            string json = JsonUtility.ToJson(report);
            var roundtrip = JsonUtility.FromJson<CrashReport>(json);
            Assert.AreEqual(report.SessionId, roundtrip.SessionId);
            Assert.AreEqual(report.Message,   roundtrip.Message);
            Assert.AreEqual(2, roundtrip.Breadcrumbs.Length);
            Assert.AreEqual("state", roundtrip.Breadcrumbs[0].Category);
        }

        [Test]
        public void SetUploader_ReplacesDefault()
        {
            var probe = new ProbeUploader();
            CrashReporter.SetUploader(probe);
            _cr.CaptureMessage("manual capture", LogType.Error);
            Assert.AreEqual(1, probe.UploadCount);
            // Restore default to avoid bleed-through to other tests.
            CrashReporter.SetUploader(new LocalFileUploader());
        }

        sealed class ProbeUploader : ICrashUploader
        {
            public int UploadCount;
            public void Upload(CrashReport report) { UploadCount++; }
        }
    }
}
