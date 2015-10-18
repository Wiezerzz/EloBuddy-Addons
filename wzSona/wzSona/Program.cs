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
        private static Menu menu, comboMenu, harassMenu, healMenu, interrupterMenu, drawingsMenu;
        private static Spell.Targeted exhaust;
        private static string lastCastedSpellName;

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

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.Hero != Champion.Sona)
                return;

            Bootstrap.Init(null);

            CreateMenu();

            if (Player.Spells.FirstOrDefault(o => o.SData.Name.Contains("summonerexhaust")) != null)
                exhaust = new Spell.Targeted(Player.Instance.GetSpellSlotFromName("summonerexhaust"), 650);

            Game.OnTick += Game_OnTick;
            Orbwalker.OnPreAttack += Orbwalker_OnPreAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void CreateMenu()
        {
            #region Main Menu
            menu = MainMenu.AddMenu("wzSona", "wzsona");
            menu.AddGroupLabel("wzSona");
            menu.Add("supportmode", new CheckBox("Support Mode"));
            menu.AddLabel("When Support Mode is checked the orbwalker will only attack minions when no allies are near.");
            #endregion
            
            #region Combo Menu
            comboMenu = menu.AddSubMenu("Combo", "combomenu");
            comboMenu.AddGroupLabel("Combo");

            comboMenu.Add("combouseq", new CheckBox("Use Q"));
            comboMenu.Add("combousee", new CheckBox("Use E"));
            comboMenu.Add("combocountq", new Slider("Enemies in range to use Q", 1, 1, 2));
            comboMenu.AddSeparator();
            comboMenu.Add("combouser", new CheckBox("Auto R"));
            comboMenu.Add("combocountr", new Slider("Use R if it will hit X enemies", 2, 1, 5));
            #endregion

            #region Harass Menu
            harassMenu = menu.AddSubMenu("Harass", "harassmenu");
            harassMenu.AddGroupLabel("Harass");

            harassMenu.Add("useq", new CheckBox("Use Q"));
            harassMenu.Add("countq", new Slider("Enemies in range to use Q", 1, 1, 2));
            harassMenu.Add("manaq", new Slider("Don't use Q when mana lower than x%", 30, 1));
            #endregion

            #region Heal Menu
            healMenu = menu.AddSubMenu("Auto Heal", "healmenu");
            healMenu.AddGroupLabel("Auto Heal");

            healMenu.Add("useheal", new CheckBox("Enable Auto Heal"));
            healMenu.Add("healself", new Slider("Heal yourself when health is lower than", 40, 1));
            healMenu.Add("allyhp", new Slider("Heal only when ally has less than x% health", 40, 1));
            healMenu.Add("allycount", new Slider("Heal only when x allies are near", 1, 1, 5));
            #endregion

            #region Interrupter Menu
            interrupterMenu = menu.AddSubMenu("Interrupter", "interrupter");
            interrupterMenu.AddGroupLabel("Interrupter");

            interrupterMenu.Add("user", new CheckBox("Use R to interrupt", false));
            interrupterMenu.AddSeparator(0);
            interrupterMenu.Add("useexhaust", new CheckBox("Use Exhaust to reduce damage (40% reduced damage)"));
            interrupterMenu.AddSeparator(0);
            interrupterMenu.Add("usewpassive", new CheckBox("Use W passive to reduce damage (20% reduced damage)"));
            #endregion

            #region Drawings Menu
            drawingsMenu = menu.AddSubMenu("Drawings", "drawingsmenu");
            drawingsMenu.AddGroupLabel("Drawings");

            drawingsMenu.Add("drawq", new CheckBox("Draw Q"));
            drawingsMenu.Add("draww", new CheckBox("Draw W", false));
            drawingsMenu.Add("drawe", new CheckBox("Draw E", false));
            drawingsMenu.Add("drawr", new CheckBox("Draw R"));
            drawingsMenu.AddSeparator(0);
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
                case Orbwalker.ActiveModes.Harass:
                    Harass();
                    break;
                case Orbwalker.ActiveModes.Flee:
                    Flee();
                    break;
            }

            AutoHeal();
        }

        private static void Orbwalker_OnPreAttack(AttackableUnit target, Orbwalker.PreAttackArgs args)
        {
            if (args.Target.Type == GameObjectType.obj_AI_Minion && menu["supportmode"].Cast<CheckBox>().CurrentValue && CountAlliesInRange(800) > 0)
                args.Process = false;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            lastCastedSpellName = args.SData.Name;
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (!sender.IsValid || sender.IsDead || !sender.IsTargetable || sender.IsStunned || e.DangerLevel < DangerLevel.High || !Player.Instance.IsInRange(sender, Spells[SpellSlot.R].Range))
                return;

            Chat.Print("wzSona| InterruptableSpell: " + sender.Name);

            if (interrupterMenu["user"].Cast<CheckBox>().CurrentValue && Spells[SpellSlot.R].IsReady())
                Spells[SpellSlot.R].Cast(sender);

            if (interrupterMenu["useexhaust"].Cast<CheckBox>().CurrentValue && exhaust.IsReady() && Player.Instance.IsInRange(sender, exhaust.Range))
                exhaust.Cast(sender);

            if (interrupterMenu["usewpassive"].Cast<CheckBox>().CurrentValue)
                UseWPassive(sender);
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

        private static void Combo()
        {
            AutoR();

            if (comboMenu["combouseq"].Cast<CheckBox>().CurrentValue && Spells[SpellSlot.Q].IsReady())
            {
                int cnt = EntityManager.Heroes.Enemies.Count(enemy => enemy.IsValid && !enemy.IsDead && Player.Instance.IsInRange(enemy, Spells[SpellSlot.Q].Range));

                if (cnt >= comboMenu["combocountq"].Cast<Slider>().CurrentValue)
                    Spells[SpellSlot.Q].Cast();
            }

            if (comboMenu["combousee"].Cast<CheckBox>().CurrentValue && Spells[SpellSlot.E].IsReady())
            {
                AIHeroClient target = TargetSelector.GetTarget(1700, DamageType.Magical);
                if (target != null)
                    UseE(target);
            }
        }

        private static void Harass()
        {
            if (harassMenu["useq"].Cast<CheckBox>().CurrentValue && Spells[SpellSlot.Q].IsReady())
            {
                int cnt = EntityManager.Heroes.Enemies.Count(enemy => enemy.IsValid && !enemy.IsDead && Player.Instance.IsInRange(enemy, Spells[SpellSlot.Q].Range));

                if (cnt >= harassMenu["countq"].Cast<Slider>().CurrentValue && Player.Instance.ManaPercent >= harassMenu["manaq"].Cast<Slider>().CurrentValue)
                    Spells[SpellSlot.Q].Cast();
            }
        }

        private static void Flee()
        {
            if (Spells[SpellSlot.E].IsReady())
            {
                Spells[SpellSlot.E].Cast();
            }

            if (Player.HasBuff("sonapassiveattack") && lastCastedSpellName == "SonaE")
            {
                AIHeroClient target = TargetSelector.GetTarget(Player.Instance.AttackRange, DamageType.Magical);

                if (target != null && !Player.Instance.IsInRange(target, Orbwalker.HoldRadius))
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
        }

        private static void AutoHeal()
        {
            if (healMenu["useheal"].Cast<CheckBox>().CurrentValue)
            {
                if (Player.Instance.HealthPercent < healMenu["healself"].Cast<Slider>().CurrentValue)
                {
                    Spells[SpellSlot.W].Cast();
                    return;
                }

                if (EntityManager.Heroes.Allies.Count(ally => !ally.IsMe && !ally.IsDead && Player.Instance.IsInRange(ally, Spells[SpellSlot.W].Range) && ally.HealthPercent < healMenu["allyhp"].Cast<Slider>().CurrentValue) >= healMenu["allycount"].Cast<Slider>().CurrentValue)
                {
                    Spells[SpellSlot.W].Cast();
                }
            }
        }

        private static void UseWPassive(Obj_AI_Base target)
        {
            if (!Player.Instance.IsInAutoAttackRange(target) || target.Type != GameObjectType.AIHeroClient)
                return;

            if ((PowerChordCount() == 2))
            {
                if (Spells[SpellSlot.W].IsReady())
                {
                    Spells[SpellSlot.W].Cast();
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    return;
                }
            }

            if (!Player.HasBuff("sonapassiveattack"))
                return;

            if (lastCastedSpellName == "SonaW")
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
            else
            {
                if (Spells[SpellSlot.W].IsReady())
                {
                    Spells[SpellSlot.W].Cast();
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
            }
        }

        private static int PowerChordCount()
        {
            if (Player.HasBuff("sonapassivecount"))
                return Player.GetBuff("sonapassivecount").Count;

            return 0;
        }

        private static void UseE(Obj_AI_Base target)
        {
            //Credits for this method goes to DETUKS from L$
            //TODO: Do it better.

            if (target.Path.Length == 0 || !target.IsMoving)
                return;

            Vector2 nextEnemPath = target.Path[0].To2D();
            float dist = Player.Instance.Position.To2D().Distance(target.Position.To2D());
            float distToNext = nextEnemPath.Distance(Player.Instance.Position.To2D());
            if (distToNext <= dist)
                return;
            float msDif = Player.Instance.MoveSpeed - target.MoveSpeed;
            if (msDif <= 0 && !Player.Instance.IsInAutoAttackRange(target))
                Spells[SpellSlot.E].Cast();

            float reachIn = dist / msDif;
            if (reachIn > 4)
                Spells[SpellSlot.E].Cast();
        }

        private static void AutoR()
        {
            if (comboMenu["combouser"].Cast<CheckBox>().CurrentValue && Spells[SpellSlot.R].IsReady())
            {
                List<AIHeroClient> enemies = EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValid && !enemy.IsDead && Player.Instance.IsInRange(enemy, Spells[SpellSlot.R].Range)).ToList();

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

        private static int CountAlliesInRange(int range)
        {
            return EntityManager.Heroes.Allies.Count(ally => !ally.IsMe && !ally.IsDead && Player.Instance.IsInRange(ally, range));
        }
    }
}
