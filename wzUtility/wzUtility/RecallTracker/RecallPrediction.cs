using SharpDX;

namespace wzUtility.RecallTracker
{
    internal class RecallPrediction
    {
        public int NetworkId;
        public float LastSeen;
        public Vector3[] Path;

        internal RecallPrediction(int networkid, float lastseen, Vector3[] path)
        {
            NetworkId = networkid;
            LastSeen = lastseen;
            Path = path;
        }
    }
}
