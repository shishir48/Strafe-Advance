using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace StrafAdvance
{
    /// <summary>
    /// Scene-scoped DI root. Attach to a GameObject in GameScene. Drag the in-scene
    /// MonoBehaviours into the inspector slots — VContainer will resolve and inject
    /// them anywhere they're requested (constructor params, [Inject] fields, methods).
    ///
    /// Migration goal: drop FindAnyObjectByType + SetField reflection. New code
    /// requests dependencies through this container; legacy Find calls stay until
    /// they're refactored.
    /// </summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [Header("Scene Services (drag from hierarchy)")]
        [SerializeField] private GameManager       gameManager;
        [SerializeField] private WaveSpawner       waveSpawner;
        [SerializeField] private CorridorScroller  corridorScroller;
        [SerializeField] private AudioManager      audioManager;
        [SerializeField] private IAPManager        iapManager;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SaveSystemFacade>(Lifetime.Singleton).AsSelf();

            if (gameManager       != null) builder.RegisterComponent(gameManager);
            if (waveSpawner       != null) builder.RegisterComponent(waveSpawner);
            if (corridorScroller  != null) builder.RegisterComponent(corridorScroller);
            if (audioManager      != null) builder.RegisterComponent(audioManager);
            if (iapManager        != null) builder.RegisterComponent(iapManager);
        }
    }

    /// <summary>Thin wrapper so static SaveSystem can flow through DI.</summary>
    public class SaveSystemFacade
    {
        public SaveData Current => SaveSystem.Current;
        public void     Save()  => SaveSystem.Save();
        public SaveData Reload() => SaveSystem.Reload();
        public void     Reset()  => SaveSystem.Reset();
    }
}
