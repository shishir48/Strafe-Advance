using System;

namespace StrafAdvance
{
    /// <summary>
    /// Rewarded-ad abstraction so revive/2x-coin can use ads once an SDK is integrated.
    /// Same pluggable pattern as <c>ICrashUploader</c>. Default impl is a no-op until an
    /// IronSource/AppLovin adapter is supplied via <see cref="ReviveService.SetAdProvider"/>.
    /// </summary>
    public interface IRewardedAd
    {
        /// <summary>Show an ad. Invoke <paramref name="onComplete"/>(true) if the reward was earned.</summary>
        void Show(Action<bool> onComplete);
    }

    /// <summary>Stub: no ad available — always reports "not rewarded".</summary>
    public class NoOpRewardedAd : IRewardedAd
    {
        public void Show(Action<bool> onComplete) => onComplete?.Invoke(false);
    }
}
