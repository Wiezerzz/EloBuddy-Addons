using EloBuddy;
using EloBuddy.SDK;
using SharpDX;

namespace wzShaco
{
    internal static class Utils
    {
        private static readonly int[] EDamageModifier = {50, 90, 130, 170, 210};

        //Credits to Fluxy.
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

        public static float CalculateEDamage(this Spell.SpellBase spell, AIHeroClient target)
        {
            if (!target.IsFacingFixed(Player.Instance))
            {
                return (Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical,
                    EDamageModifier[spell.Level - 1] + Player.Instance.TotalMagicalDamage, true, true)
                       +
                       Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical,
                           Player.Instance.FlatPhysicalDamageMod, false, true)) * 1.20f;
            }

            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical,
                EDamageModifier[spell.Level - 1] + Player.Instance.TotalMagicalDamage, true, true)
                   +
                   Player.Instance.CalculateDamageOnUnit(target, DamageType.Physical,
                       Player.Instance.FlatPhysicalDamageMod, false, true);
        }

        public static bool IsFacingFixed(this Obj_AI_Base source, Obj_AI_Base target)
        {
            if (source == null || target == null)
                return false;

            return source.Direction.To2D().Perpendicular().AngleBetween((target.Position - source.Position).To2D()) < 90;
        }
    }
}
