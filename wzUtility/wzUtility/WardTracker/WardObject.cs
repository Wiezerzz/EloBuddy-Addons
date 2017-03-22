using SharpDX;

namespace wzUtility.WardTracker
{
    internal class WardObject
    {
        public WardEnum WardType;
        public string Caster;
        public float Expires;
        public Vector3 Position;

        public WardObject(WardEnum wardType, string caster, float expiresAt, Vector3 position)
        {
            WardType = wardType;
            Caster = caster;
            Expires = expiresAt;
            Position = position;
        }

        public enum WardEnum
        {
            SightWard,
            VisionWard,
            BlueTrinket
        }
    }
}