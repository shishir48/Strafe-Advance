using NUnit.Framework;
using StrafAdvance;

namespace StrafAdvance.Tests
{
    public class LocalizationTests
    {
        [SetUp]
        public void SetUp()
        {
            SaveSystem.Reset();
            Loc.Init();
        }

        [TearDown]
        public void TearDown()
        {
            Loc.SetLanguage("en");
            SaveSystem.Reset();
        }

        [Test]
        public void DefaultLanguage_IsEnglishWhenNoSave()
        {
            Assert.AreEqual("en", Loc.Current);
        }

        [Test]
        public void Tr_ReturnsEnglishForKnownKey()
        {
            Assert.AreEqual("PLAY", Loc.Tr("menu.play"));
        }

        [Test]
        public void Tr_FallsBackToEnglishForMissingTranslation()
        {
            Loc.SetLanguage("ja");
            // A key not present in ja must fall back to en, never to the raw key
            string s = Loc.Tr("menu.play");
            Assert.AreNotEqual("menu.play", s);
        }

        [Test]
        public void Tr_ReturnsKeyAsLastResortForUnknownKey()
        {
            Assert.AreEqual("nonexistent.key", Loc.Tr("nonexistent.key"));
        }

        [Test]
        public void SetLanguage_PersistsToSave()
        {
            Loc.SetLanguage("es");
            Assert.AreEqual("es", SaveSystem.Current.settings.language);
            var fresh = SaveSystem.Reload();
            Assert.AreEqual("es", fresh.settings.language);
        }

        [Test]
        public void SetLanguage_NormalisesUnknownToDefault()
        {
            Loc.SetLanguage("kr"); // unsupported
            Assert.AreEqual("en", Loc.Current);
        }

        [Test]
        public void SetLanguage_PublishesEvent()
        {
            string captured = null;
            void Handler(LanguageChanged e) => captured = e.Language;
            EventBus<LanguageChanged>.Subscribe(Handler);
            try
            {
                Loc.SetLanguage("ja");
                Assert.AreEqual("ja", captured);
            }
            finally { EventBus<LanguageChanged>.Unsubscribe(Handler); }
        }

        [Test]
        public void Tr_DiffersAcrossLanguages()
        {
            Loc.SetLanguage("en");
            string en = Loc.Tr("menu.play");
            Loc.SetLanguage("es");
            string es = Loc.Tr("menu.play");
            Loc.SetLanguage("ja");
            string ja = Loc.Tr("menu.play");
            Assert.AreNotEqual(en, es);
            Assert.AreNotEqual(en, ja);
            Assert.AreNotEqual(es, ja);
        }
    }
}
