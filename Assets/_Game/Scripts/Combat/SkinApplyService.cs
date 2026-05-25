using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Applies equipped cosmetic skin tints to in-scene renderers. Placeholder visuals until real
    /// art lands — but it proves the unlock loop end-to-end (purchase a skin → see the player tint
    /// change next run). Subscribes to <see cref="SkinEquipped"/> for live mid-run swaps.
    /// </summary>
    public class SkinApplyService : MonoBehaviour
    {
        public static SkinApplyService Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            EventBus<SkinEquipped>.Subscribe(OnEquipped);
            EventBus<GameStateChanged>.Subscribe(OnState);
        }

        void OnDestroy()
        {
            EventBus<SkinEquipped>.Unsubscribe(OnEquipped);
            EventBus<GameStateChanged>.Unsubscribe(OnState);
            if (Instance == this) Instance = null;
        }

        void OnState(GameStateChanged s)
        {
            if (s.Current == GameState.Playing) ApplyAll();
        }

        void OnEquipped(SkinEquipped _) => ApplyAll();

        public void ApplyAll()
        {
            ApplyPlayer(SaveSystem.Current.progress.equippedPlayerSkinId);
            // Bullet + trail tints would apply at bullet spawn time — readers should query the
            // service via TintFor(slot) when they materialise visuals (kept here for future use).
        }

        public Color TintFor(CosmeticSlot slot)
        {
            string id = slot switch
            {
                CosmeticSlot.Player => SaveSystem.Current.progress.equippedPlayerSkinId,
                CosmeticSlot.Bullet => SaveSystem.Current.progress.equippedBulletSkinId,
                CosmeticSlot.Trail  => SaveSystem.Current.progress.equippedTrailSkinId,
                _                    => "",
            };
            var skin = CosmeticCatalog.Find(id);
            return skin != null ? skin.TintColor : Color.white;
        }

        static void ApplyPlayer(string skinId)
        {
            var skin = CosmeticCatalog.Find(skinId);
            if (skin == null) return;
            var player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            foreach (var r in player.GetComponentsInChildren<Renderer>())
            {
                foreach (var m in r.materials) // instance materials so we don't mutate the shared asset
                {
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", skin.TintColor);
                    else if (m.HasProperty("_Color")) m.SetColor("_Color",    skin.TintColor);
                }
            }
        }
    }

    public readonly struct SkinEquipped
    {
        public readonly CosmeticSlot Slot;
        public readonly string SkinId;
        public SkinEquipped(CosmeticSlot slot, string skinId) { Slot = slot; SkinId = skinId; }
    }
}
