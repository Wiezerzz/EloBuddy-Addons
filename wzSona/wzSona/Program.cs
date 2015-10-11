using System;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;

namespace wzSona
{
    class Program
    {
        private static Menu menu;

        private static readonly Dictionary<SpellSlot, Spell.SpellBase> Spells = new Dictionary<SpellSlot, Spell.SpellBase>
        {
            {SpellSlot.Q, new Spell.Active(SpellSlot.Q, 850)},
            {SpellSlot.W, new Spell.Active(SpellSlot.W, 1000)},
            {SpellSlot.E, new Spell.Active(SpellSlot.E, 350)},
            {SpellSlot.R, new Spell.Skillshot(SpellSlot.R, 1000, SkillShotType.Linear, 500, 3000)}
        };

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.Hero != Champion.Sona)
                return;

            Bootstrap.Init(null);

            CreateMenu();

        }

        private static void CreateMenu()
        {
            throw new NotImplementedException();
        }
    }
}
