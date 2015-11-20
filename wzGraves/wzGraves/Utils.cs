using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace wzGraves
{
    internal static class Utils
    {
        private static readonly float[] QDamageModifier = {60f, 80f, 100f, 120f, 140f};
        //private static readonly float[] QDamageModifier = {55f, 70f, 85f, 100f, 115f};   NOTE: Next patch 5.23
        private static readonly float[] RDamageModifier = {250f, 400f, 550f};

        //Copied from Fluxy's YasouBuddy - He is a god and so is his yasou addon.
        public static Vector3 ExtendVector3(this Vector3 vector, Vector3 direction, float distance)
        {
            if (vector.To2D().Distance(direction.To2D()) == 0)
            {
                return vector;
            }

            var edge = direction.To2D() - vector.To2D();
            edge.Normalize();

            var v = vector.To2D() + edge * distance;
            return new Vector3(v.X, v.Y, vector.Z);
        }

        public static float CalculateQDamage(this Spell.Skillshot skillshot, AIHeroClient target)
        {
            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical,
                QDamageModifier[skillshot.Level - 1] + Player.Instance.FlatPhysicalDamageMod * 0.75f);
        }

        public static float CalculateRDamage(this Spell.Skillshot skillshot, AIHeroClient target)
        {
            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical,
                RDamageModifier[skillshot.Level - 1] + Player.Instance.FlatPhysicalDamageMod * 1.50f);
        }

        public static float CalculateR1Damage(this Spell.Skillshot skillshot, AIHeroClient target)
        {
            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical, (RDamageModifier[skillshot.Level - 1] + (Player.Instance.FlatPhysicalDamageMod * 1.50f)) * 0.80f);
        }

        public static bool CheckWallCollison(this AIHeroClient player, Vector3 targetpos)
        {
            for (int i = 0; i < targetpos.Distance(player.Position); i += 30)
            {
                Vector3 wallPosition = targetpos.ExtendVector3(player.Position, targetpos.Distance(player.Position) - i);

                if (NavMesh.GetCollisionFlags(wallPosition).HasFlag(CollisionFlags.Wall) || NavMesh.GetCollisionFlags(wallPosition).HasFlag(CollisionFlags.Building))
                    return true;
            }

            return false;
        }

        public static bool IsMatureMonster(this Obj_AI_Base monster)
        {
            string[] array = { "SRU_Baron", "SRU_RiftHerald", "SRU_Dragon", "Sru_Crab", "SRU_Krug", "SRU_Red", "SRU_Blue", "SRU_Red", "SRU_Gromp", "SRU_Razorbeak", "SRU_Murkwolf" };

            if (array.Contains(monster.BaseSkinName))
                return true;

            return false;
        }
    }
}
