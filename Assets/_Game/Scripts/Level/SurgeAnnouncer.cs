using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Watches wave starts during Endless runs and announces surge spikes (per
    /// <see cref="SurgeSchedule"/>) — publishes <see cref="SurgeEvent"/> and shows a HUD banner.
    /// The wave content itself is themed by <see cref="EndlessProvider"/>.
    /// </summary>
    public class SurgeAnnouncer : MonoBehaviour
    {
        void OnEnable()  => EventBus<WaveStarted>.Subscribe(OnWaveStarted);
        void OnDisable() => EventBus<WaveStarted>.Unsubscribe(OnWaveStarted);

        void OnWaveStarted(WaveStarted w)
        {
            if (GameManager.Instance == null || GameManager.Instance.Mode != GameManager.RunMode.Endless)
                return;

            SurgeType surge = SurgeSchedule.For(w.Index);
            if (surge == SurgeType.None) return;

            EventBus<SurgeEvent>.Publish(new SurgeEvent(surge));

            if (ToastNotifier.Instance == null) return;

            string title;
            Color accent;
            switch (surge)
            {
                case SurgeType.SwarmRush:
                    title = "⚠ SWARM INCOMING"; accent = new Color(1f, 0.55f, 0.1f); break;
                case SurgeType.EliteAmbush:
                    title = "⚠ ELITE AMBUSH";   accent = new Color(1f, 0.3f, 0.3f);  break;
                default:
                    title = "⚠ GAUNTLET";       accent = new Color(0.8f, 0.2f, 0.9f); break;
            }
            ToastNotifier.Instance.Enqueue(title, "Brace yourself", accent);
        }
    }
}
