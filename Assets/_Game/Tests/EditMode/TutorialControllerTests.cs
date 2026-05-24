using NUnit.Framework;
using UnityEngine;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class TutorialControllerTests
    {
        GameObject _go;
        TutorialController _ctrl;

        [SetUp]
        public void SetUp()
        {
            SaveSystem.Reset();
            _go = new GameObject("TutorialControllerTest");
            _ctrl = _go.AddComponent<TutorialController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            SaveSystem.Reset();
        }

        [Test]
        public void FreshSave_TutorialIsNotCompleted()
        {
            Assert.IsFalse(SaveSystem.Current.profile.tutorialCompleted);
        }

        [Test]
        public void Skip_MarksCompletedAndPersists()
        {
            _ctrl.Skip();
            Assert.IsTrue(SaveSystem.Current.profile.tutorialCompleted);
            var fresh = SaveSystem.Reload();
            Assert.IsTrue(fresh.profile.tutorialCompleted);
        }

        [Test]
        public void ResetAndArm_ClearsCompletedFlag()
        {
            SaveSystem.Current.profile.tutorialCompleted = true;
            SaveSystem.Save();

            _ctrl.ResetAndArm();
            Assert.IsFalse(SaveSystem.Current.profile.tutorialCompleted);
            var fresh = SaveSystem.Reload();
            Assert.IsFalse(fresh.profile.tutorialCompleted);
        }

    }
}
