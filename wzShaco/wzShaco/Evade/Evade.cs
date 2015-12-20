using System.Collections.Generic;

namespace wzShaco.Evade
{
    public class Evade
    {
        public static List<Skill> skillList = new List<Skill>();

        public static void Initialize()
        {
            skillList.Add(new Skill("DariusExecute", Skill.SkillType.Targeted, 250));
            skillList.Add(new Skill("GarenR", Skill.SkillType.Targeted, 250));
            skillList.Add(new Skill("SyndraR", Skill.SkillType.Targeted, 300));
            skillList.Add(new Skill("BrandWildfire", Skill.SkillType.Targeted, 100));
            skillList.Add(new Skill("VeigarPrimordialBurst", Skill.SkillType.Targeted, 200));

            skillList.Add(new Skill("KarthusFallenOne", Skill.SkillType.GlobalAoE, 2900));
        }
    }
}
