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

namespace wzGraves
{
    class Program
    {
        private static Menu menu, comboMenu, harassMenu, laneclearMenu, jungleclearMenu, drawingsMenu;

        private static readonly Dictionary<string, Spell.Skillshot> Spells = new Dictionary<string, Spell.Skillshot>
        {
            {"q", new Spell.Skillshot(SpellSlot.Q, 850, SkillShotType.Linear, 250, 2000, 60)},
            {"w", new Spell.Skillshot(SpellSlot.W, 950, SkillShotType.Circular, 250, 1650, 150)},
            {"e", new Spell.Skillshot(SpellSlot.E, 425, SkillShotType.Linear)},
            {"r", new Spell.Skillshot(SpellSlot.R, 1500, SkillShotType.Linear, 250, 2100, 100)}
        };

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        #region Events
        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if(Player.Instance.Hero != Champion.Graves)
                return;

            Bootstrap.Init(null);

            #region Main Menu
            menu = MainMenu.AddMenu("wzGraves", "gravesmenu", "wzGraves");
            menu.AddGroupLabel("wzGraves");
            menu.AddLabel("Nothing to see here....");
            #endregion

            #region Combo Menu
            comboMenu = menu.AddSubMenu("Combo", "combomenu");
            comboMenu.AddGroupLabel("Combo");

            comboMenu.Add("comboq", new CheckBox("Use Q"));
            comboMenu.Add("combowallq", new CheckBox("Use Wall Q"));
            comboMenu.Add("combowimmobile", new CheckBox("Use W on immobile"));
            comboMenu.Add("comboe", new CheckBox("Use E after AA"));
            comboMenu.Add("combor", new CheckBox("Use R to execute"));
            #endregion

            #region Harass Menu
            harassMenu = menu.AddSubMenu("Harass", "harassmenu");
            harassMenu.AddGroupLabel("Harass");

            harassMenu.Add("harassq", new CheckBox("Use Q"));
            harassMenu.Add("harasswallq", new CheckBox("Use Wall Q"));
            harassMenu.Add("harassmana", new Slider("Minimum mana before using Q", 40));
            #endregion

            #region Laneclear Menu
            laneclearMenu = menu.AddSubMenu("Laneclear", "laneclearmenu");
            laneclearMenu.AddGroupLabel("Laneclear");

            laneclearMenu.Add("laneclearq", new CheckBox("Use Q", false));
            #endregion

            #region Jungleclear Menu
            jungleclearMenu = menu.AddSubMenu("Jungleclear", "jungleclearmenu");
            jungleclearMenu.AddGroupLabel("Jungleclear");

            jungleclearMenu.Add("jungleclearq", new CheckBox("Use Q"));
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

            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
        }

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
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                LaneClear();
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                JungleClear();
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            ColorBGRA color = new ColorBGRA(210, 100, 0, 255);

            foreach (KeyValuePair<string, Spell.Skillshot> spell in Spells)
            {
                if (drawingsMenu["draw" + spell.Key].Cast<CheckBox>().CurrentValue)
                {
                    if (drawingsMenu["drawready"].Cast<CheckBox>().CurrentValue && spell.Value.IsReady() || !drawingsMenu["drawready"].Cast<CheckBox>().CurrentValue)
                    {
                        Circle.Draw(color, spell.Value.Range, Player.Instance.Position);
                    }
                }
            }
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo && comboMenu["comboe"].Cast<CheckBox>().CurrentValue && !Player.HasBuff("GravesBasicAttackAmmo2"))
            {
                if (Spells["e"].IsReady())
                {
                    //Weird fix for BUG: Spells["e"].Cast(Game.CursorPos); not casting sometimes.
                    if (Player.CastSpell(SpellSlot.E, Game.CursorPos))
                        Orbwalker.ResetAutoAttack();
                }
            }
        }
        #endregion

        #region Modes
        private static void Combo()
        {
            HandleQ();

            if (comboMenu["combowimmobile"].Cast<CheckBox>().CurrentValue)
                AutoWImmobile();

            if (comboMenu["combor"].Cast<CheckBox>().CurrentValue)
                HandleUlt();
        }

        private static void Harass()
        {
            if (!Spells["q"].IsReady() || Player.Instance.ManaPercent < harassMenu["harassmana"].Cast<Slider>().CurrentValue || !harassMenu["harassq"].Cast<CheckBox>().CurrentValue && !harassMenu["harasswallq"].Cast<CheckBox>().CurrentValue)
                return;

            AIHeroClient target = TargetSelector.GetTarget(Spells["q"].Range, DamageType.Physical);

            if (target != null)
            {
                if (harassMenu["harasswallq"].Cast<CheckBox>().CurrentValue)
                {
                    if (CastWallQ(target))
                        return;
                }

                if (harassMenu["harassq"].Cast<CheckBox>().CurrentValue)
                    CastBombQ(target);
            }
        }

        private static void LaneClear()
        {
            if (!laneclearMenu["laneclearq"].Cast<CheckBox>().CurrentValue || !Spells["q"].IsReady())
                return;

            foreach (Obj_AI_Minion minion in EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position, Spells["q"].Range))
            {
                if (Spells["q"].GetPrediction(minion).CollisionObjects.Count() >= 3 && !Player.Instance.CheckWallCollison(minion.Position))
                {
                    if (Spells["q"].Cast(minion.Position))
                        return;
                }
            }
        }

        private static void JungleClear()
        {
            if(!jungleclearMenu["jungleclearq"].Cast<CheckBox>().CurrentValue || !Spells["q"].IsReady())
                return;

            Obj_AI_Minion jungleMob = EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, Spells["q"].Range).OrderByDescending(x => x.MaxHealth).FirstOrDefault();

            if (jungleMob != null && !Player.Instance.CheckWallCollison(jungleMob.Position))
                Spells["q"].Cast(jungleMob);
        }
        #endregion

        private static void HandleQ()
        {
            if (!Spells["q"].IsReady())
                return;

            AIHeroClient target = TargetSelector.GetTarget(Spells["q"].Range, DamageType.Physical);

            if (target != null)
            {
                if (!Player.Instance.CheckWallCollison(target.Position) && target.Health < Spells["q"].CalculateQDamage(target))
                {
                    if (Spells["q"].Cast(target))
                        return;
                }

                if (comboMenu["combowallq"].Cast<CheckBox>().CurrentValue)
                {
                    if (CastWallQ(target))
                        return;
                }

                if (comboMenu["comboq"].Cast<CheckBox>().CurrentValue)
                    CastBombQ(target);
            }
        }

        private static bool CastWallQ(AIHeroClient target)
        {
            //400 castdelay instead of 250.
            PredictionResult pred = Prediction.Position.PredictLinearMissile(target, 850f, 60, 400, 2000f, 0, Player.Instance.Position, true);

            if (Player.Instance.CheckWallCollison(pred.CastPosition))
                return false;

            Vector3 QEndPosition = pred.CastPosition.ExtendVector3(Player.Instance.Position, -(Spells["q"].Range - Player.Instance.Distance(pred.CastPosition)));

            for (int i = 0; i < pred.CastPosition.Distance(QEndPosition); i += 30)
            {
                Vector3 wallPosition = pred.CastPosition.ExtendVector3(QEndPosition, pred.CastPosition.Distance(QEndPosition) - i);

                if (NavMesh.GetCollisionFlags(wallPosition).HasFlag(CollisionFlags.Wall) || NavMesh.GetCollisionFlags(wallPosition).HasFlag(CollisionFlags.Building))
                {
                    if (Spells["q"].Cast(pred.CastPosition))
                        return true;
                }
            }

            return false;
        }

        private static void CastBombQ(AIHeroClient target)
        {
            PredictionResult pred = Prediction.Position.PredictLinearMissile(target, 850f, 80, 1250, 2000f, 0, Player.Instance.Position, true);

            if (Player.Instance.CheckWallCollison(pred.CastPosition))
                return;

            if (pred.HitChance == HitChance.High || pred.HitChance == HitChance.Immobile)
            {
                if (Player.Instance.Position.Distance(pred.CastPosition) > 750 && Player.Instance.Position.Distance(pred.CastPosition) < 850)
                    Spells["q"].Cast(pred.CastPosition);
            }
        }

        private static void AutoWImmobile()
        {
            if (!Spells["w"].IsReady())
                return;

            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(Spells["w"].Range)))
            {
                PredictionResult pred = Spells["w"].GetPrediction(enemy);

                if (pred.HitChance == HitChance.Immobile)
                {
                    if (Spells["w"].Cast(pred.CastPosition))
                        return;
                }
            }
        }

        private static void HandleUlt()
        {
            if (!Spells["r"].IsReady())
                return;

            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(x => Player.Instance.Distance(x) > 700 && x.IsValidTarget(Spells["r"].Range) && x.Health < Spells["r"].CalculateRDamage(x)))
            {
                PredictionResult pred = Spells["r"].GetPrediction(enemy);

                if (Player.Instance.Distance(pred.CastPosition) > 1000 && enemy.Health > Spells["r"].CalculateR1Damage(enemy))
                    continue;

                if (pred.HitChance >= HitChance.Medium)
                {
                    if(Spells["r"].Cast(pred.CastPosition))
                        return;
                }
            }
        }

    }
}
 