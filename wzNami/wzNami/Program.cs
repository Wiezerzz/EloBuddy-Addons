using System;
using System.Linq;
using System.Security.AccessControl;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Spells;
using SharpDX;

namespace wzNami
{
    public class Program
    {
        private static Menu _menu, _comboMenu, _harassMenu, _laneclearMenu, _interruptergapcloserMenu, _drawingsMenu;
        private static readonly string[] Skins = { "Classic", "Koi", "River Spirit", "Urf the Nami-tee", "Koi Gold Chroma", "Koi White Chroma", "Koi Purple Chroma", "Deep Sea" };
        private static readonly Spell.Skillshot Q = new Spell.Skillshot(SpellSlot.Q, 875, SkillShotType.Circular, 950, int.MaxValue, 160);
        private static readonly Spell.Targeted W = new Spell.Targeted(SpellSlot.W, 725);
        private static readonly Spell.Targeted E = new Spell.Targeted(SpellSlot.E, 800);
        private static readonly Spell.Skillshot R = new Spell.Skillshot(SpellSlot.R, 1850, SkillShotType.Linear, 500, 850, 250); //2750 maximum range

        private static void Main()
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.Hero != Champion.Nami)
                return;

            Bootstrap.Init(null);

            Q.MinimumHitChance = HitChance.High;
            Q.AllowedCollisionCount = int.MaxValue;
            R.MinimumHitChance = HitChance.High;
            R.AllowedCollisionCount = int.MaxValue;

            CreateMenu();

            Game.OnUpdate += Game_OnUpdate;
            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (!_interruptergapcloserMenu["gapcloserq"].Cast<CheckBox>().CurrentValue || !sender.IsEnemy || !Q.CanCast(sender))
                return;

            Q.CastMinimumHitchance(sender, HitChance.Dashing);
        }

        private static void CreateMenu()
        {
            #region Main Menu

            _menu = MainMenu.AddMenu("wzNami", "wznami");

            ComboBox skinComboBox = new ComboBox("Change skin", Skins, Player.Instance.SkinId);
            _menu.Add("skinhack", skinComboBox);
            skinComboBox.OnValueChange += (sender, args) =>
            {
                if (args.OldValue == args.NewValue || args.NewValue < 0)
                    return;

                Player.Instance.SetSkinId(args.NewValue);
            };

            #endregion

            #region Combo Menu

            _comboMenu = _menu.AddSubMenu("Combo", "combomenu");
            _comboMenu.AddGroupLabel("Combo");

            _comboMenu.Add("useq", new CheckBox("Use Q"));
            _comboMenu.Add("usew", new CheckBox("Use W"));
            _comboMenu.Add("usee", new CheckBox("Use E"));
            _comboMenu.Add("user", new CheckBox("Use R"));

            Slider comboCountR = new Slider("Use R if it will hit 2 enemies", 2, 1, 5);
            _comboMenu.Add("countr", comboCountR);
            comboCountR.OnValueChange += (sender, args) =>
            {
                if (args.OldValue == args.NewValue || args.NewValue < 0)
                    return;

                sender.DisplayName = "Use R if it will hit " + args.NewValue + " enemies";
            };
            comboCountR.DisplayName = "Use R if it will hit " + comboCountR.CurrentValue + " enemies";

            Slider comboRangeR = new Slider("Maximum range to R", 1850, 400, 2750);
            _comboMenu.Add("range", comboRangeR);
            comboRangeR.OnValueChange += (sender, args) =>
            {
                if (args.OldValue == args.NewValue || args.NewValue < 0)
                    return;

                R.Range = (uint)args.NewValue;
            };
            R.Range = (uint)comboRangeR.CurrentValue;

            #endregion

            #region Harass Menu

            _harassMenu = _menu.AddSubMenu("Harass", "harassmenu");
            _harassMenu.AddGroupLabel("Harass");

            _harassMenu.Add("useq", new CheckBox("Use Q"));
            _harassMenu.Add("usew", new CheckBox("Use W"));
            _harassMenu.Add("usee", new CheckBox("Use E"));

            Slider harassMana = new Slider("Don't harass when mana is lower than 40%", 40);
            _harassMenu.Add("mana", harassMana);
            harassMana.OnValueChange += (sender, args) =>
            {
                if (args.OldValue == args.NewValue || args.NewValue < 0)
                    return;

                sender.DisplayName = "Don't harass when mana is lower than " + args.NewValue + "%";
            };
            harassMana.DisplayName = "Don't harass when mana is lower than " + harassMana.CurrentValue + "%";

            #endregion

            #region Laneclear Menu

            _laneclearMenu = _menu.AddSubMenu("Laneclear", "laneclearmenu");
            _laneclearMenu.AddGroupLabel("Laneclear");

            _laneclearMenu.Add("useq", new CheckBox("Use Q"));

            Slider laneclearCountQ = new Slider("Minimum 3 minions required to use Q", 3, 1, 7);
            _laneclearMenu.Add("countq", laneclearCountQ);
            laneclearCountQ.OnValueChange += (sender, args) =>
            {
                if (args.OldValue == args.NewValue || args.NewValue < 0)
                    return;

                sender.DisplayName = "Minimum " + args.NewValue + " minions required to use Q";
            };
            laneclearCountQ.DisplayName = "Minimum " + laneclearCountQ.CurrentValue + " minions required to use Q";

            Slider laneclearMana = new Slider("Don't laneclear when mana is lower than 30%", 30);
            _laneclearMenu.Add("mana", laneclearMana);
            laneclearMana.OnValueChange += (sender, args) =>
            {
                if (args.OldValue == args.NewValue || args.NewValue < 0)
                    return;

                sender.DisplayName = "Don't laneclear when mana is lower than " + args.NewValue + "%";
            };
            laneclearMana.DisplayName = "Don't laneclear when mana is lower than " + laneclearMana.CurrentValue + "%";

            #endregion

            #region Interrupter & Gapcloser Menu

            _interruptergapcloserMenu = _menu.AddSubMenu("Interrupter & Gapcloser", "interruptergapcloser");
            _interruptergapcloserMenu.AddGroupLabel("Interrupter & Gapcloser");

            _interruptergapcloserMenu.Add("interrupterq", new CheckBox("Auto Q to interrupt"));
            _interruptergapcloserMenu.Add("gapcloserq", new CheckBox("Auto Q on gapcloser"));

            #endregion

            #region Drawings Menu

            _drawingsMenu = _menu.AddSubMenu("Drawings", "drawingsmenu");
            _drawingsMenu.AddGroupLabel("Drawings");

            _drawingsMenu.Add("drawq", new CheckBox("Draw Q"));
            _drawingsMenu.Add("draww", new CheckBox("Draw W", false));
            _drawingsMenu.Add("drawe", new CheckBox("Draw E", false));
            _drawingsMenu.Add("drawr", new CheckBox("Draw R"));
            _drawingsMenu.AddSeparator(0);
            _drawingsMenu.Add("drawready", new CheckBox("Only draw when ready"));

            #endregion
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (!_interruptergapcloserMenu["interrupterq"].Cast<CheckBox>().CurrentValue || !sender.IsEnemy || e.DangerLevel != DangerLevel.High || !Q.CanCast(sender))
                return;

            Q.Cast(sender);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            bool drawReady = _drawingsMenu["drawready"].Cast<CheckBox>().CurrentValue;
            bool drawQ = _drawingsMenu["drawq"].Cast<CheckBox>().CurrentValue && (!drawReady || Q.IsReady());
            bool drawW = _drawingsMenu["draww"].Cast<CheckBox>().CurrentValue && (!drawReady || W.IsReady());
            bool drawE = _drawingsMenu["drawe"].Cast<CheckBox>().CurrentValue && (!drawReady || E.IsReady());
            bool drawR = _drawingsMenu["drawr"].Cast<CheckBox>().CurrentValue && (!drawReady || R.IsReady());

            if (drawQ)
                Circle.Draw(Color.Aqua, Q.Range, Player.Instance);

            if (drawW)
                Circle.Draw(Color.Gray, W.Range, Player.Instance);

            if (drawE)
                Circle.Draw(Color.Gray, E.Range, Player.Instance);

            if (drawR)
                Circle.Draw(Color.Aqua, R.Range, Player.Instance);
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                Combo(Q.IsReady() && _comboMenu["useq"].Cast<CheckBox>().CurrentValue,
                      W.IsReady() && _comboMenu["usew"].Cast<CheckBox>().CurrentValue,
                      E.IsReady() && _comboMenu["usee"].Cast<CheckBox>().CurrentValue,
                      R.IsReady() && _comboMenu["user"].Cast<CheckBox>().CurrentValue);
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                Harass(Q.IsReady() && _harassMenu["useq"].Cast<CheckBox>().CurrentValue,
                       W.IsReady() && _harassMenu["usew"].Cast<CheckBox>().CurrentValue,
                       E.IsReady() && _harassMenu["usee"].Cast<CheckBox>().CurrentValue);
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                Laneclear(Q.IsReady() && _laneclearMenu["useq"].Cast<CheckBox>().CurrentValue);
        }

        private static void Combo(bool useQ, bool useW, bool useE, bool useR)
        {
            if (useR)
                RLogic();

            if (useE)
                ELogic();

            if (useQ)
                QLogic();

            if (useW)
                WLogic();
        }

        private static void Harass(bool useQ, bool useW, bool useE)
        {
            int mana = _harassMenu["mana"].Cast<Slider>().CurrentValue;

            if (Player.Instance.ManaPercent < mana)
                return;

            if (useE)
                ELogic();

            if (useQ)
                QLogic();

            if (useW)
                WLogic();
        }

        private static void Laneclear(bool useQ)
        {
            if (!useQ)
                return;

            int mana = _laneclearMenu["mana"].Cast<Slider>().CurrentValue;
            int countQ = _laneclearMenu["countq"].Cast<Slider>().CurrentValue;

            if (Player.Instance.ManaPercent < mana)
                return;

            Q.CastOnBestFarmPosition(countQ, 70);
        }

        private static void QLogic()
        {
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget(Q.Range + 200)).OrderByDescending(TargetSelector.GetPriority))
            {
                if (Q.Cast(enemy))
                    break;
            }
        }

        private static void WLogic()
        {
            /* NOTE: This could be improved by adding movement prediction but that would be hell so noty.
             * 
             * After 200AP W damage increases per bounce instead of decrease.
             * bool increasedBounce = Player.Instance.TotalMagicalDamage >= 200;
             */
             

            //W ally, W bounces to enemy
            foreach (AIHeroClient ally in EntityManager.Heroes.Allies.Where(ally => ally.IsValidTarget(W.Range) && ally.HealthPercent <= 95).OrderBy(ally => ally.HealthPercent))
            {
                AIHeroClient allyCopy = ally;
                if (!EntityManager.Heroes.Enemies.Any(enemy => enemy.IsValidTarget(W.Range, true, allyCopy.ServerPosition)))
                    continue;

                if (W.Cast(allyCopy))
                    return;
            }

            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget(W.Range)).OrderByDescending(TargetSelector.GetPriority))
            {
                //W self, W bounces to enemy, W bounces to ally
                if (enemy.CountAlliesInRange(W.Range) > 2)
                    if (W.Cast(Player.Instance))
                        return;

                //W enemy, W bounces to self, W bounces to other enemy
                if (Player.Instance.CountEnemiesInRange(W.Range) > 2)
                    if (W.Cast(enemy))
                        return;
            }

            //If we can't do special shit, just hit somebody
            AIHeroClient targetW = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            if (targetW != null)
                W.Cast(targetW);
        }

        private static void ELogic()
        {
            foreach (AIHeroClient ally in EntityManager.Heroes.Allies.Where(ally => ally.IsValidTarget(E.Range)).OrderByDescending(ally => ally.TotalAttackDamage))
            {
                if (!EntityManager.Heroes.Enemies.Any(enemy => enemy.IsValidTarget() && (ally.IsInAutoAttackRange(enemy) || (ally.IsMelee && ally.Distance(enemy) < 350))))
                    continue;

                if (E.Cast(ally))
                    break;
            }
        }

        private static void RLogic()
        {
            int countR = _comboMenu["countr"].Cast<Slider>().CurrentValue;

            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget(R.Range)).OrderByDescending(TargetSelector.GetPriority))
            {
                PredictionResult predictionResult = R.GetPrediction(enemy);
                if (predictionResult.HitChance < R.MinimumHitChance)
                    continue;

                Geometry.Polygon.Rectangle ultiRectangle = new Geometry.Polygon.Rectangle(Player.Instance.ServerPosition.To2D(), Player.Instance.ServerPosition.Extend(predictionResult.CastPosition, R.Range), R.Width);

                int hits = 1 +
                           EntityManager.Heroes.Enemies.Count(
                               x =>
                                   enemy.NetworkId != x.NetworkId && x.IsValidTarget() && R.GetPrediction(x).HitChance >= R.MinimumHitChance &&
                                   ultiRectangle.IsInside(R.GetPrediction(x).CastPosition));

                if (hits < countR)
                    continue;

                if (R.Cast(predictionResult.CastPosition))
                    return;
            }
        }
    }
}
