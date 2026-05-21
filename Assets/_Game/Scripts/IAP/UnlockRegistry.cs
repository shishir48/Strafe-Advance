using System;
using UnityEngine;

namespace StrafAdvance
{
    public class UnlockRegistry
    {
        private const string Prefix = "unlock_";

        public event Action<string> OnUnlocked;

        public void Unlock(string productId)
        {
            PlayerPrefs.SetInt(Prefix + productId, 1);
            PlayerPrefs.Save();
            OnUnlocked?.Invoke(productId);
        }

        public bool IsUnlocked(string productId)
            => PlayerPrefs.GetInt(Prefix + productId, 0) == 1;
    }
}
