using SharpDX;

namespace wzUtility.WardTracker
{
    internal class WardObject
    {
        public string Name { get; set; }
        public bool IsPink { get; set; }
        public int Mana { get; set; }
        public int MaxMana { get; set; }
        public float Expires { get; set; }
        public Vector3 Position { get; set; }

        public WardObject(string name, bool isPink, int mana, int maxMana, float expiresAt, Vector3 position)
        {
            Name = name;
            IsPink = isPink;
            Mana = mana;
            MaxMana = maxMana;
            Expires = expiresAt;
            Position = position;
        }
    }
}