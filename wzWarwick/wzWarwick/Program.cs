using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Utils;
using SharpDX;

namespace wzWarwick
{
    public static class Program
    {
        private static Spell.Targeted Q = new Spell.Targeted(SpellSlot.Q, 375, DamageType.Magical);

        private static void Main()
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                AIHeroClient target = (Orbwalker.ForcedTarget ?? Q.GetTarget()) as AIHeroClient;
                if (target != null && target.IsValidTarget())
                    Q.Cast(target);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            //Warwick_Base_W_Tar.troy
            foreach (GameObject particle in ObjectManager.Get<GameObject>().Where(x => x.Name == "Warwick_Base_W_Tar.troy"))
            {
                if (!particle.Position.WorldToScreen().IsOnScreen())
                    continue;

                Circle.Draw(new ColorBGRA(Color.Blue.ToBgra()), 50f, particle.Position);
                //Drawing.DrawText(particle.Position.WorldToScreen(), System.Drawing.Color.AliceBlue, particle.Name, 12);
            }

            Q.DrawRange(Color.DarkRed);
        }
    }
}

