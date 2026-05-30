using UnityEngine;

namespace StrafAdvance
{
    /// <summary>
    /// Tracks crossings of evenly-spaced milestones (every <c>step</c>). <see cref="Check"/> returns
    /// the milestone value the first time a new band is reached, else 0. <see cref="Reset"/> clears
    /// state for a new run.
    /// </summary>
    public class MilestoneTracker
    {
        private readonly int _step;
        private int _last;

        public MilestoneTracker(int step)
        {
            _step = Mathf.Max(1, step);
        }

        public int Check(int value)
        {
            int m = (value / _step) * _step;
            if (m > _last)
            {
                _last = m;
                return m;
            }
            return 0;
        }

        public void Reset() => _last = 0;
    }
}
