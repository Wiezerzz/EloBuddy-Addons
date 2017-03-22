using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Spells;
using SharpDX;

namespace wzMorgana
{
    public class Program
    {
        private static Menu _menu, _comboMenu, _harassMenu, _laneclearMenu, _interrupterMenu, _drawingsMenu;
        private static readonly string[] Skins = { "Classic", "Exiled", "Sinful Succulence", "Blade Mistress", "Blackthorn", "Ghost Bride", "Victorious", "Green Chroma", "Gray Chroma", "Blue Chroma", "Lunar Wraith" };
        private static readonly Spell.Skillshot Q = new Spell.Skillshot(SpellSlot.Q, 1200, SkillShotType.Linear, 250, 1200, 70);
        private static readonly Spell.Skillshot W = new Spell.Skillshot(SpellSlot.W, 900, SkillShotType.Circular, 250, 0, 275);
        private static readonly Spell.Targeted E = new Spell.Targeted(SpellSlot.E, 750);
        private static readonly Spell.Active R = new Spell.Active(SpellSlot.R, 625);
        private const int TetherRange = 1050;

        private static void Main()
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.Hero != Champion.Morgana)
                return;

            Bootstrap.Init(null);

            Q.MinimumHitChance = HitChance.High;
            W.MinimumHitChance = HitChance.High;

            CreateMenu();

            /*
            SpellSlot[] bla = {SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R};
            foreach (SpellSlot spellslot in bla)
            {
                Console.WriteLine(spellslot + " MissileSpeed: " + Player.GetSpell(spellslot).SData.MissileSpeed);
                Console.WriteLine(spellslot + " MissileMinSpeed: " + Player.GetSpell(spellslot).SData.MissileMinSpeed);
                Console.WriteLine(spellslot + " MissileMaxSpeed: " + Player.GetSpell(spellslot).SData.MissileMaxSpeed);
                Console.WriteLine(spellslot + " CastRange: " + Player.GetSpell(spellslot).SData.CastRange);
                Console.WriteLine(spellslot + " SpellCastTime: " + Player.GetSpell(spellslot).SData.SpellCastTime);
                Console.WriteLine(spellslot + " CastRadius: " + Player.GetSpell(spellslot).SData.CastRadius);
                Console.WriteLine(spellslot + " LineWidth: " + Player.GetSpell(spellslot).SData.LineWidth);
                Console.WriteLine("====================================");
            }
             */
            foreach (SpellInfo spellInfo in SpellDatabase.GetSpellInfoList(Player.Instance))
            {
                Console.WriteLine("=== SpellName: " + spellInfo.SpellName + " | Spellslot: " + spellInfo.Slot + " ===");
                Console.WriteLine("Range: " + spellInfo.Range);
                Console.WriteLine("Delay: " + spellInfo.Delay);
                Console.WriteLine("Radius: " + spellInfo.Radius);
                Console.WriteLine("Type: " + spellInfo.Type);
                Console.WriteLine("Acceleration: " + spellInfo.Acceleration);
                Console.WriteLine("MissileAccel: " + spellInfo.MissileAccel);
                Console.WriteLine("MissileFixedTravelTime: " + spellInfo.MissileFixedTravelTime);
                Console.WriteLine("MissileMaxSpeed: " + spellInfo.MissileMaxSpeed);
                Console.WriteLine("MissileMinSpeed: " + spellInfo.MissileMinSpeed);
                Console.WriteLine("MissileSpeed: " + spellInfo.MissileSpeed);
                Console.WriteLine("Collisions: " + string.Join(", ", spellInfo.Collisions));
                Console.WriteLine();
            }

            Interrupter.OnInterruptableSpell += Interrupter_OnInterruptableSpell;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Game.OnUpdate += Game_OnUpdate;
        }

        private static void CreateMenu()
        {
            #region Main Menu

            _menu = MainMenu.AddMenu("wzMorgana", "wzmorgana");

            ComboBox skinComboBox = new ComboBox("Change skin", Skins, Player.Instance.SkinId);
            _menu.Add("skinhack", skinComboBox);
            skinComboBox.OnValueChange += SkinComboBox_OnValueChange;

            #endregion

            #region Combo Menu

            _comboMenu = _menu.AddSubMenu("Combo", "combomenu");
            _comboMenu.AddGroupLabel("Combo");

            _comboMenu.Add("useq", new CheckBox("Use Q"));
            _comboMenu.Add("usew", new CheckBox("Use W"));
            _comboMenu.Add("user", new CheckBox("Use R"));
            _comboMenu.Add("countr", new Slider("Use R if it will hit X enemies", 2, 1, 5));

            #endregion

            #region Harass Menu

            _harassMenu = _menu.AddSubMenu("Harass", "harassmenu");
            _harassMenu.AddGroupLabel("Harass");

            _harassMenu.Add("useq", new CheckBox("Use Q"));
            _harassMenu.Add("usew", new CheckBox("Use W"));
            _harassMenu.Add("mana", new Slider("Don't harass when mana lower than x%", 40));

            #endregion

            #region Laneclear Menu

            _laneclearMenu = _menu.AddSubMenu("Laneclear", "laneclearmenu");
            _laneclearMenu.AddGroupLabel("Laneclear");

            _laneclearMenu.Add("usew", new CheckBox("Use W"));
            _laneclearMenu.Add("mana", new Slider("Don't laneclear when mana lower than x%", 40));

            #endregion

            #region Interrupter Menu

            _interrupterMenu = _menu.AddSubMenu("Interrupter", "interrupter");
            _interrupterMenu.AddGroupLabel("Interrupter");

            _interrupterMenu.Add("useq", new CheckBox("Auto Q to interrupt"));

            #endregion

            #region Drawings Menu

            _drawingsMenu = _menu.AddSubMenu("Drawings", "drawingsmenu");
            _drawingsMenu.AddGroupLabel("Drawings");

            _drawingsMenu.Add("drawq", new CheckBox("Draw Q"));
            _drawingsMenu.Add("draww", new CheckBox("Draw W", false));
            _drawingsMenu.AddSeparator(0);
            _drawingsMenu.Add("drawready", new CheckBox("Only draw when ready"));
            _drawingsMenu.Add("drawmodifier", new CheckBox("Take prediction range in account", false));

            #endregion
        }

        private static void SkinComboBox_OnValueChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            if (args.OldValue == args.NewValue || args.NewValue < 0)
                return;
            
            Player.Instance.SetSkinId(args.NewValue);
        }

        private static void Interrupter_OnInterruptableSpell(Obj_AI_Base sender, Interrupter.InterruptableSpellEventArgs e)
        {
            if (!Q.IsReady() || !_interrupterMenu["useq"].Cast<CheckBox>().CurrentValue || !sender.IsEnemy || e.DangerLevel != DangerLevel.High)
                return;

            Q.Cast(sender);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            //bool drawModifier = _drawingsMenu["drawmodifier"].Cast<CheckBox>().CurrentValue;
            bool drawReady = _drawingsMenu["drawready"].Cast<CheckBox>().CurrentValue;
            bool drawQ = _drawingsMenu["drawq"].Cast<CheckBox>().CurrentValue && (!drawReady || Q.IsReady());
            bool drawW = _drawingsMenu["draww"].Cast<CheckBox>().CurrentValue && (!drawReady || W.IsReady());

            if (drawQ)
                Circle.Draw(Color.Gray, Q.Range, Player.Instance);

            if (drawW)
                Circle.Draw(Color.Gray, W.Range, Player.Instance);
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender == null || !sender.IsEnemy || args.SData.TargettingType != SpellDataTargetType.Unit || (!args.Target.IsAlly && !args.Target.IsMe))
                return;

            Obj_AI_Base ally = args.Target as Obj_AI_Base;

            if (ally == null)
                return;

            if (E.CanCast(ally))
                E.Cast(ally);
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                Combo(Q.IsReady() && _comboMenu["useq"].Cast<CheckBox>().CurrentValue,
                      W.IsReady() && _comboMenu["usew"].Cast<CheckBox>().CurrentValue,
                      R.IsReady() && _comboMenu["user"].Cast<CheckBox>().CurrentValue);
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                Harass(Q.IsReady() && _harassMenu["useq"].Cast<CheckBox>().CurrentValue,
                       W.IsReady() && _harassMenu["usew"].Cast<CheckBox>().CurrentValue);
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                Laneclear(W.IsReady() && _laneclearMenu["usew"].Cast<CheckBox>().CurrentValue);
        }

        private static void Combo(bool useQ, bool useW, bool useR)
        {
            if (useR && Ult())
                return;

            if (useQ)
            {
                if (CastOnImmobile(Q) || CastOnTarget(Q))
                    return;
            }

            if (useW)
            {
                if (CastOnImmobile(W) || CastWPrediction())
                    return;
            }
        }

        private static void Harass(bool useQ, bool useW)
        {
            bool hasMana = Player.Instance.ManaPercent >= _harassMenu["mana"].Cast<Slider>().CurrentValue;

            if (useQ && hasMana && (CastOnImmobile(Q) || CastOnTarget(Q)))
                return;

            if (useW && hasMana && (CastOnImmobile(W) || CastWPrediction()))
                return;
        }

        private static void Laneclear(bool useW)
        {
            bool hasMana = Player.Instance.ManaPercent >= _laneclearMenu["mana"].Cast<Slider>().CurrentValue;

            if (useW && hasMana)
                W.CastOnBestFarmPosition();
        }

        private static bool CastOnTarget(Spell.Skillshot skillshot)
        {
            return EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget(skillshot.Range)).OrderByDescending(TargetSelector.GetPriority).Any(skillshot.Cast);
        }

        private static bool CastOnImmobile(Spell.Skillshot skillshot)
        {
            return EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget(skillshot.Range)).OrderByDescending(TargetSelector.GetPriority).Any(enemy => skillshot.CastMinimumHitchance(enemy, HitChance.Immobile));
        }

        private static bool CastWPrediction()
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(W.Range)))
            {
                PredictionResult predictionResult = W.GetPrediction(enemy);
                if (predictionResult.HitChance < W.MinimumHitChance)
                    continue;

                if (Prediction.Position.PredictUnitPosition(enemy, 2750).Distance(predictionResult.CastPosition) < W.Radius)
                    return W.Cast(predictionResult.CastPosition);
            }

            return false;
        }

        private static bool Ult()
        {
            bool useR = _comboMenu["user"].Cast<CheckBox>().CurrentValue;
            int countR = _comboMenu["countr"].Cast<Slider>().CurrentValue;

            if (!useR)
                return false;

            if (Player.Instance.CountEnemiesInRange(R.Range) < countR)
                return false;

            List<AIHeroClient> enemiesNearby = EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(R.Range)).ToList();
            int hits = enemiesNearby.Count(enemy => PredictUnitPositionWithMovementReduction(enemy, 2000, 20f).Distance(Player.Instance) <= TetherRange);
            
            if (hits >= countR)
                R.Cast();

            return false;
        }

        public static Vector2 PredictUnitPositionWithMovementReduction(Obj_AI_Base unit, int time, float percent)
        {
            float range = time / 1000f * (unit.MoveSpeed * ((100f - percent) / 100f));
            Vector3[] realPath = Prediction.Position.GetRealPath(unit);
            for (int index = 0; index < realPath.Length - 1; ++index)
            {
                Vector2 vector21 = realPath[index].To2D();
                Vector2 vector22 = realPath[index + 1].To2D();
                float num = vector21.Distance(vector22);
                if (num > (double)range)
                    return vector21.Extend(vector22, range);
                range -= num;
            }
            return (realPath.Length == 0 ? unit.ServerPosition : realPath.Last()).To2D();
        }
    }
}
