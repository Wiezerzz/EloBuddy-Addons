namespace wzShaco.Evade
{
    public class Skill
    {
        public string Name;
        public SkillType Type;
        public int Delay;

        public Skill(string name, SkillType type, int delay)
        {
            this.Name = name;
            this.Type = type;
            this.Delay = delay;
        }

        public enum SkillType
        {
            Targeted,
            Linear,
            Circular,
            GlobalAoE
        }
    }
}
