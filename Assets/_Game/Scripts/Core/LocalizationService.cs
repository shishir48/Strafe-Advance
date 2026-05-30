using System.Collections.Generic;
using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Lightweight in-house localization. Hardcoded multi-lang catalog keyed by short string IDs.
    /// English is always the fallback when a key/lang is missing. Live language switch publishes
    /// <see cref="LanguageChanged"/> so subscribed UI can re-render without scene reload.
    ///
    /// Bigger projects swap this for `com.unity.localization`; for a paid mobile shooter with
    /// &lt;200 strings this is faster to author and ship.
    /// </summary>
    public static class Loc
    {
        public const string DefaultLanguage = "en";
        public static readonly string[] SupportedLanguages = { "en", "es", "ja", "zh-CN" };

        static string _current = DefaultLanguage;

        /// <summary>Current ISO language code (e.g. "en", "es", "ja", "zh-CN").</summary>
        public static string Current => _current;

        /// <summary>Initialize from saved preference (auto-detects system language if save is blank).</summary>
        public static void Init()
        {
            string saved = SaveSystem.Current.settings.language;
            if (string.IsNullOrEmpty(saved))
            {
                saved = DetectSystemLanguage();
                SaveSystem.Current.settings.language = saved;
                SaveSystem.Save();
            }
            _current = NormaliseOrDefault(saved);
        }

        /// <summary>Translate a key into the current language, falling back to English then the key itself.</summary>
        public static string Tr(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            if (LocalizationCatalog.TryGet(_current, key, out var v)) return v;
            if (LocalizationCatalog.TryGet(DefaultLanguage, key, out var en)) return en;
            return key; // last-resort: surface the raw key so missing entries are visible
        }

        /// <summary>Switch language. Persists + publishes <see cref="LanguageChanged"/>.</summary>
        public static void SetLanguage(string lang)
        {
            string next = NormaliseOrDefault(lang);
            if (next == _current) return;
            _current = next;
            SaveSystem.Current.settings.language = next;
            SaveSystem.Save();
            EventBus<LanguageChanged>.Publish(new LanguageChanged(next));
        }

        public static int LanguageIndex => System.Array.IndexOf(SupportedLanguages, _current);

        static string NormaliseOrDefault(string code)
        {
            if (string.IsNullOrEmpty(code)) return DefaultLanguage;
            foreach (var s in SupportedLanguages)
                if (string.Equals(s, code, System.StringComparison.OrdinalIgnoreCase)) return s;
            return DefaultLanguage;
        }

        static string DetectSystemLanguage() => Application.systemLanguage switch
        {
            SystemLanguage.Spanish        => "es",
            SystemLanguage.Japanese       => "ja",
            SystemLanguage.ChineseSimplified => "zh-CN",
            SystemLanguage.Chinese          => "zh-CN",
            _                              => "en",
        };
    }

    public readonly struct LanguageChanged
    {
        public readonly string Language;
        public LanguageChanged(string language) { Language = language; }
    }

    /// <summary>
    /// Hardcoded multi-language string table. Adding a new language: add an entry to <see cref="Strings"/>.
    /// Adding a key: add the key to every language map (English is required, others fall back to English).
    /// </summary>
    public static class LocalizationCatalog
    {
        public static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
        {
            ["en"] = new Dictionary<string, string>
            {
                ["menu.play"]          = "PLAY",
                ["menu.arcade"]        = "ARCADE",
                ["menu.loadout"]       = "LOADOUT",
                ["menu.shop"]          = "SHOP",
                ["menu.battle_pass"]   = "BATTLE PASS",
                ["menu.settings"]      = "SETTINGS",
                ["menu.quit"]          = "QUIT",
                ["menu.subtitle"]      = "CORRIDOR SHOOTER  ·  v0.1",
                ["menu.best_score"]    = "Best: {0}   ·   Lvl {1}",
                ["pause.title"]        = "PAUSED",
                ["pause.resume"]       = "RESUME",
                ["pause.perks"]        = "PERKS",
                ["pause.settings"]     = "SETTINGS",
                ["pause.restart"]      = "RESTART",
                ["pause.quit"]         = "QUIT",
                ["settings.title"]            = "SETTINGS",
                ["settings.music_volume"]     = "Music Volume",
                ["settings.sfx_volume"]       = "SFX Volume",
                ["settings.ui_volume"]        = "UI Volume",
                ["settings.aim_sensitivity"]  = "Aim Sensitivity",
                ["settings.vibration"]        = "Vibration",
                ["settings.invert_y"]         = "Invert Y",
                ["settings.colorblind"]       = "Colorblind Mode",
                ["settings.quality"]          = "Quality",
                ["settings.language"]         = "Language",
                ["settings.reset_profile"]    = "RESET PROFILE",
                ["settings.reset_tutorial"]   = "RESET TUTORIAL",
                ["settings.close"]            = "CLOSE",
                ["run_summary.win"]           = "LEVEL COMPLETE",
                ["run_summary.loss"]          = "GAME OVER",
                ["run_summary.restart"]       = "RESTART",
                ["run_summary.menu"]          = "MENU",
                ["toast.achievement"]         = "ACHIEVEMENT: {0}",
                ["toast.daily_login"]         = "DAY {0} LOGIN",
                ["toast.tier_reached"]        = "BATTLE PASS TIER {0}",
                ["toast.reward_credits"]      = "+{0} credits",
            },
            ["es"] = new Dictionary<string, string>
            {
                ["menu.play"]          = "JUGAR",
                ["menu.arcade"]        = "ARCADE",
                ["menu.loadout"]       = "EQUIPO",
                ["menu.shop"]          = "TIENDA",
                ["menu.battle_pass"]   = "PASE DE BATALLA",
                ["menu.settings"]      = "AJUSTES",
                ["menu.quit"]          = "SALIR",
                ["menu.subtitle"]      = "TIRADOR DE CORREDOR  ·  v0.1",
                ["menu.best_score"]    = "Mejor: {0}   ·   Nv {1}",
                ["pause.title"]        = "PAUSA",
                ["pause.resume"]       = "REANUDAR",
                ["pause.perks"]        = "VENTAJAS",
                ["pause.settings"]     = "AJUSTES",
                ["pause.restart"]      = "REINICIAR",
                ["pause.quit"]         = "SALIR",
                ["settings.title"]            = "AJUSTES",
                ["settings.music_volume"]     = "Volumen Música",
                ["settings.sfx_volume"]       = "Volumen Efectos",
                ["settings.ui_volume"]        = "Volumen Interfaz",
                ["settings.aim_sensitivity"]  = "Sensibilidad",
                ["settings.vibration"]        = "Vibración",
                ["settings.invert_y"]         = "Invertir Y",
                ["settings.colorblind"]       = "Modo Daltónico",
                ["settings.quality"]          = "Calidad",
                ["settings.language"]         = "Idioma",
                ["settings.reset_profile"]    = "REINICIAR PERFIL",
                ["settings.reset_tutorial"]   = "REINICIAR TUTORIAL",
                ["settings.close"]            = "CERRAR",
                ["run_summary.win"]           = "NIVEL COMPLETADO",
                ["run_summary.loss"]          = "FIN DEL JUEGO",
                ["run_summary.restart"]       = "REINICIAR",
                ["run_summary.menu"]          = "MENÚ",
                ["toast.achievement"]         = "LOGRO: {0}",
                ["toast.daily_login"]         = "DÍA {0} INICIO",
                ["toast.tier_reached"]        = "PASE NIVEL {0}",
                ["toast.reward_credits"]      = "+{0} créditos",
            },
            ["ja"] = new Dictionary<string, string>
            {
                ["menu.play"]          = "プレイ",
                ["menu.arcade"]        = "アーケード",
                ["menu.loadout"]       = "装備",
                ["menu.shop"]          = "ショップ",
                ["menu.battle_pass"]   = "バトルパス",
                ["menu.settings"]      = "設定",
                ["menu.quit"]          = "終了",
                ["menu.subtitle"]      = "コリドーシューター  ·  v0.1",
                ["menu.best_score"]    = "ベスト: {0}   ·   Lv {1}",
                ["pause.title"]        = "一時停止",
                ["pause.resume"]       = "再開",
                ["pause.perks"]        = "パーク",
                ["pause.settings"]     = "設定",
                ["pause.restart"]      = "再開始",
                ["pause.quit"]         = "終了",
                ["settings.title"]            = "設定",
                ["settings.music_volume"]     = "音楽音量",
                ["settings.sfx_volume"]       = "効果音音量",
                ["settings.ui_volume"]        = "UI音量",
                ["settings.aim_sensitivity"]  = "感度",
                ["settings.vibration"]        = "振動",
                ["settings.invert_y"]         = "Y反転",
                ["settings.colorblind"]       = "色覚モード",
                ["settings.quality"]          = "画質",
                ["settings.language"]         = "言語",
                ["settings.reset_profile"]    = "プロファイルリセット",
                ["settings.reset_tutorial"]   = "チュートリアルリセット",
                ["settings.close"]            = "閉じる",
                ["run_summary.win"]           = "レベルクリア",
                ["run_summary.loss"]          = "ゲームオーバー",
                ["run_summary.restart"]       = "リスタート",
                ["run_summary.menu"]          = "メニュー",
                ["toast.achievement"]         = "達成: {0}",
                ["toast.daily_login"]         = "{0}日目ログイン",
                ["toast.tier_reached"]        = "バトルパス Tier {0}",
                ["toast.reward_credits"]      = "+{0} クレジット",
            },
            ["zh-CN"] = new Dictionary<string, string>
            {
                ["menu.play"]          = "开始",
                ["menu.arcade"]        = "街机",
                ["menu.loadout"]       = "装备",
                ["menu.shop"]          = "商店",
                ["menu.battle_pass"]   = "战令",
                ["menu.settings"]      = "设置",
                ["menu.quit"]          = "退出",
                ["menu.subtitle"]      = "走廊射手  ·  v0.1",
                ["menu.best_score"]    = "最佳: {0}   ·   等级 {1}",
                ["pause.title"]        = "暂停",
                ["pause.resume"]       = "继续",
                ["pause.perks"]        = "天赋",
                ["pause.settings"]     = "设置",
                ["pause.restart"]      = "重新开始",
                ["pause.quit"]         = "退出",
                ["settings.title"]            = "设置",
                ["settings.music_volume"]     = "音乐音量",
                ["settings.sfx_volume"]       = "音效音量",
                ["settings.ui_volume"]        = "界面音量",
                ["settings.aim_sensitivity"]  = "灵敏度",
                ["settings.vibration"]        = "振动",
                ["settings.invert_y"]         = "反转Y轴",
                ["settings.colorblind"]       = "色盲模式",
                ["settings.quality"]          = "画质",
                ["settings.language"]         = "语言",
                ["settings.reset_profile"]    = "重置存档",
                ["settings.reset_tutorial"]   = "重置教程",
                ["settings.close"]            = "关闭",
                ["run_summary.win"]           = "关卡完成",
                ["run_summary.loss"]          = "游戏结束",
                ["run_summary.restart"]       = "重新开始",
                ["run_summary.menu"]          = "菜单",
                ["toast.achievement"]         = "成就: {0}",
                ["toast.daily_login"]         = "第{0}日登录",
                ["toast.tier_reached"]        = "战令等级 {0}",
                ["toast.reward_credits"]      = "+{0} 点券",
            },
        };

        public static bool TryGet(string lang, string key, out string value)
        {
            value = null;
            if (!Strings.TryGetValue(lang, out var table)) return false;
            return table.TryGetValue(key, out value);
        }
    }
}
