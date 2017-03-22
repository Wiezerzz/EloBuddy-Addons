using EloBuddy;

namespace wzUtility.RecallTracker
{
    internal class Recall
    {
        private float _lastPercent;

        public int NetworkId;
        public string Name;
        public float HealthPercent;
        public float StartTime { get { return EndTime - Duration; } }
        public float EndTime;
        public float Duration;
        public bool IsAborted;
        public float Elapsed { get { return EndTime - Game.Time; } }

        internal Recall(int networkid, string name, float healthPercent, float duration)
        {
            NetworkId = networkid;
            Name = name;
            HealthPercent = healthPercent;
            EndTime = Game.Time + duration;
            Duration = duration;
        }

        public float Percent()
        {
            if (IsAborted)
                return _lastPercent;
            
            float percent = (Elapsed > 0 && Duration > float.Epsilon) ? Elapsed / Duration : 0f;
            _lastPercent = percent;

            return percent;
        }
    }
}
