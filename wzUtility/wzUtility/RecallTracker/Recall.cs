using EloBuddy;

namespace wzUtility.RecallTracker
{
    class Recall
    {
        public Recall(string name, float healthPercent, float endTime, float duration)
        {
            Name = name;
            HealthPercent = healthPercent;
            EndTime = endTime;
            Duration = duration;
        }

        public string Name;
        public float HealthPercent;
        public float EndTime;
        public float Duration;
        public float Elapsed { get { return EndTime - Game.Time; } }

        public bool IsAborted;
        public void Abort()
        {
            IsAborted = true;
        }

        private float _lastPercent;

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
