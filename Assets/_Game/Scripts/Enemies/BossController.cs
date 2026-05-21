using System;
using UnityEngine;

namespace StrafAdvance
{
    public class BossController : EnemyBase
    {
        public int Phase { get; private set; } = 1;
        public event Action<int> OnPhaseChanged;

        protected override void OnDamageTaken()
        {
            if (Phase != 1) return;
            if (CurrentHp <= Config.maxHp / 2)
            {
                Phase = 2;
                OnPhaseChanged?.Invoke(2);
            }
        }
    }
}
