using EloBuddy;
using EloBuddy.SDK;

namespace wzAmumu
{
    public static class Extensions
    {
        public static bool ToggleState(this Spell.Active spell)
        {
            if (spell.Slot != SpellSlot.W)
                return false;

            return spell.Cast() && spell.Handle.ToggleState == 2;
        }

        public static bool IsToggled(this Spell.Active spell)
        {
            if (spell.Slot != SpellSlot.W)
                return false;

            return spell.Handle.ToggleState == 2;
        }
    }
}
