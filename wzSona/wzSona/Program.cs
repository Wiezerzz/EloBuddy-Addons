using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace wzSona
{
    class Program
    {
        private static Menu menu, comboMenu, drawingsMenu;

        private static readonly Dictionary<SpellSlot, Spell.SpellBase> Spells = new Dictionary<SpellSlot, Spell.SpellBase>
        {
            {SpellSlot.Q, new Spell.Active(SpellSlot.Q, 850)},
            {SpellSlot.W, new Spell.Active(SpellSlot.W, 1000)},
            {SpellSlot.E, new Spell.Active(SpellSlot.E, 350)},
            {SpellSlot.R, new Spell.Skillshot(SpellSlot.R, 1000, SkillShotType.Linear, 500, 3000, 125)}
        };

        private static readonly List<KeyValuePair<string, SpellSlot>> DrawSpellsList = new List<KeyValuePair<string, SpellSlot>>
        {
            new KeyValuePair<string, SpellSlot>("q", SpellSlot.Q),
            new KeyValuePair<string, SpellSlot>("w", SpellSlot.W),
            new KeyValuePair<string, SpellSlot>("e", SpellSlot.E),
            new KeyValuePair<string, SpellSlot>("r", SpellSlot.R)
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

            Game.OnTick += Game_OnTick;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void CreateMenu()
        {
            #region Main Menu
            menu = MainMenu.AddMenu("wzSona", "wzsona");
            menu.AddGroupLabel("wzSona");
            menu.Add("supportmode", new CheckBox("Support mode"));
            menu.AddLabel("When Support mode is on the orbwalker will only attack minions when no allies are near.");
            #endregion

            #region Combo Menu
            comboMenu = menu.AddSubMenu("Combo", "combomenu");
            comboMenu.AddGroupLabel("Combo");

            comboMenu.Add("combouseq", new CheckBox("Use Q"));
            comboMenu.Add("combocountq", new Slider("Enemies in range to use Q", 1, 1, 2));
            comboMenu.AddSeparator();
            comboMenu.Add("combouser", new CheckBox("Auto R"));
            comboMenu.Add("combocountr", new Slider("Enemies in range to use R", 2, 1, 5));
            #endregion

            #region Drawings Menu
            drawingsMenu = menu.AddSubMenu("Drawings", "drawingsmenu");
            drawingsMenu.AddGroupLabel("Drawings");

            drawingsMenu.Add("drawq", new CheckBox("Draw Q"));
            drawingsMenu.Add("draww", new CheckBox("Draw W", false));
            drawingsMenu.Add("drawe", new CheckBox("Draw E", false));
            drawingsMenu.Add("drawr", new CheckBox("Draw R"));
            drawingsMenu.AddSeparator();
            drawingsMenu.Add("drawready", new CheckBox("Only draw when ready"));
            #endregion
        }

        #region Events
        private static void Game_OnTick(EventArgs args)
        {
            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Combo:
                    Combo();
                    break;
            }
        }

        static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (args.Target.Type == GameObjectType.obj_AI_Minion && menu["supportmode"].Cast<CheckBox>().CurrentValue && CountAlliesInRange(800) > 0)
                args.Process = false;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            //Resharper can make this look even harder than it already is with Linq but I'm just gonna let it be this way.
            foreach (KeyValuePair<string, SpellSlot> pair in DrawSpellsList)
            {
                if (drawingsMenu["draw" + pair.Key].Cast<CheckBox>().CurrentValue)
                {
                    if (drawingsMenu["drawready"].Cast<CheckBox>().CurrentValue && Spells[pair.Value].IsReady() || !drawingsMenu["drawready"].Cast<CheckBox>().CurrentValue)
                    {
                        Circle.Draw(Color.DarkGray, Spells[pair.Value].Range, Player.Instance.Position);
                    }
                }
            }
        }
        #endregion

        private static void AutoR()
        {
            if (comboMenu["combouser"].Cast<CheckBox>().CurrentValue && Spells[SpellSlot.R].IsReady())
            {
                IEnumerable<AIHeroClient> enemies = EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValid && !enemy.IsDead && enemy.Distance(Player.Instance.ServerPosition) <= Spells[SpellSlot.R].Range);

                if (enemies.Count() >= comboMenu["combocountr"].Cast<Slider>().CurrentValue)
                {
                    foreach (AIHeroClient enemy in enemies)
                    {
                        PredictionResult predictionResult = Prediction.Position.PredictLinearMissile(enemy, Spells[SpellSlot.R].Range, 125, 500, 3000, 0, Player.Instance.ServerPosition);
                        if (predictionResult.CollisionObjects.Count(x => x.IsValid() && !enemy.IsDead && x.Type == GameObjectType.AIHeroClient) + 1 >= comboMenu["combocountr"].Cast<Slider>().CurrentValue)
                        {
                            Spells[SpellSlot.R].Cast(predictionResult.CastPosition);
                            return;
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            AutoR();

            if (comboMenu["combouseq"].Cast<CheckBox>().CurrentValue && Spells[SpellSlot.Q].IsReady())
            {
                int cnt = EntityManager.Heroes.Enemies.Count(enemy => enemy.IsValid && !enemy.IsDead && enemy.Distance(Player.Instance.ServerPosition) <= Spells[SpellSlot.Q].Range);

                if (cnt >= comboMenu["combocountq"].Cast<Slider>().CurrentValue)
                    Spells[SpellSlot.Q].Cast();
            }
        }

        private static int CountAlliesInRange(int range)
        {
            return EntityManager.Heroes.Allies.Count(ally => !ally.IsMe && !ally.IsDead && ally.Distance(Player.Instance.ServerPosition) <= range);
        }
    }
}
