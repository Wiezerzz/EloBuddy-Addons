using SharpDX;

namespace wzUtility.WardTracker
{
    internal class WardObject
    {
        public bool IsPink { get; set; }
        public string Caster { get; set; }
        public float Expires { get; set; }
        public Vector3 Position { get; set; }

        public WardObject(bool isPink, string caster, float expiresAt, Vector3 position)
        {
            IsPink = isPink;
            Caster = caster;
            Expires = expiresAt;
            Position = position;
        }
    }
}