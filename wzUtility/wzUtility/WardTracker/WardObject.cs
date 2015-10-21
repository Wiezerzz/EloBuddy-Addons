using SharpDX;

namespace wzUtility.WardTracker
{
    internal class WardObject
    {
        public string Name { get; set; }
        public bool IsPink { get; set; }
        public float Expires { get; set; }
        public Vector3 Position { get; set; }

        public WardObject(string name, bool isPink, float expiresAt, Vector3 position)
        {
            Name = name;
            IsPink = isPink;
            Expires = expiresAt;
            Position = position;
        }
    }
}


