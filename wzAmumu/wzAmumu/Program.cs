using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace wzAmumu
{
    public class Program
    {
        private static Menu _menu, _comboMenu, _harassMenu, _laneclearMenu, _jungleclearMenu, _drawingsMenu;
        private static readonly string[] Skins = { "Classic", "Pharaoh", "Vancouver", "Emumu", "Re-Gifted", "Almost Prom King", "Little Knight", "Sad Robot", "Suprise Party" };
        private static readonly Spell.Skillshot Q = new Spell.Skillshot(SpellSlot.Q, 1100, SkillShotType.Linear, 250, 2000, 80);
        private static readonly Spell.Active W = new Spell.Active(SpellSlot.W, 300);
        private static readonly Spell.Active E = new Spell.Active(SpellSlot.E, 350); //CastDelay: 500
        private static readonly Spell.Active R = new Spell.Active(SpellSlot.R, 550); //CastDelay: 250

        private static void Main()
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.Hero != Champion.Amumu)
                return;

            Bootstrap.Init(null);

            CreateMenu();

            Q.AllowedCollisionCount = 0;

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void CreateMenu()
        {
            #region Main Menu

            _menu = MainMenu.AddMenu("wzAmumu", "wzamumu");
            _menu.AddLabel("This is my little retarded baby addon, so no hate plz.");
            _menu.AddSeparator(0);
            ComboBox skinComboBox = new ComboBox("Change skin", Skins, Player.Instance.SkinId);
            _menu.Add("skinhack", skinComboBox);
            skinComboBox.OnValueChange += SkinComboBox_OnValueChange;

            #endregion

            #region Combo Menu

            _comboMenu = _menu.AddSubMenu("Combo", "combomenu");

            _comboMenu.Add("useq", new CheckBox("Use Q"));
            _comboMenu.Add("usee", new CheckBox("Use E"));
            _comboMenu.Add("useqsmite", new CheckBox("Use Q + Smite", false));
            _comboMenu.Add("user", new CheckBox("Use R"));
            _comboMenu.Add("countr", new Slider("Minimum enemies required to use R", 2, 1, 5));

            #endregion

            #region Harass Menu

            _harassMenu = _menu.AddSubMenu("Harass", "harassmenu");

            _harassMenu.Add("useq", new CheckBox("Use Q"));
            _harassMenu.Add("usee", new CheckBox("Use E"));
            _harassMenu.Add("mana", new Slider("Don't harass when mana lower than x%", 40));

            #endregion

            #region Laneclear Menu

            _laneclearMenu = _menu.AddSubMenu("Laneclear", "laneclearmenu");

            _laneclearMenu.Add("usee", new CheckBox("Use E"));
            _laneclearMenu.Add("counte", new Slider("Minimum minions required to use E", 3, 1, 7));
            _laneclearMenu.Add("mana", new Slider("Don't laneclear when mana lower than x%", 40));

            #endregion

            #region Drawings Menu

            _drawingsMenu = _menu.AddSubMenu("Drawings", "drawingsmenu");

            _drawingsMenu.Add("drawq", new CheckBox("Draw Q"));
            _drawingsMenu.Add("draww", new CheckBox("Draw W", false));
            _drawingsMenu.Add("drawe", new CheckBox("Draw E", false));
            _drawingsMenu.Add("drawr", new CheckBox("Draw R"));
            _drawingsMenu.AddSeparator(0);
            _drawingsMenu.Add("drawready", new CheckBox("Only draw when ready"));

            #endregion
        }

        private static void SkinComboBox_OnValueChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            if (args.OldValue == args.NewValue || args.NewValue < 0)
                return;

            Player.Instance.SetSkinId(args.NewValue);
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                Combo(Q.IsReady() && _comboMenu["useq"].Cast<CheckBox>().CurrentValue,
                      E.IsReady() && _comboMenu["usee"].Cast<CheckBox>().CurrentValue,
                      R.IsReady() && _comboMenu["user"].Cast<CheckBox>().CurrentValue);
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                Harass(Q.IsReady() && _harassMenu["useq"].Cast<CheckBox>().CurrentValue,
                       E.IsReady() && _harassMenu["usee"].Cast<CheckBox>().CurrentValue);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            bool drawReady = _drawingsMenu["drawready"].Cast<CheckBox>().CurrentValue;
            bool drawQ = _drawingsMenu["drawq"].Cast<CheckBox>().CurrentValue && (!drawReady || Q.IsReady());
            bool drawW = _drawingsMenu["draww"].Cast<CheckBox>().CurrentValue && (!drawReady || W.IsReady());
            bool drawE = _drawingsMenu["drawe"].Cast<CheckBox>().CurrentValue && (!drawReady || E.IsReady());
            bool drawR = _drawingsMenu["drawr"].Cast<CheckBox>().CurrentValue && (!drawReady || R.IsReady());

            if (drawQ)
                Circle.Draw(Color.Gray, Q.Range, Player.Instance);

            if (drawW)
                Circle.Draw(Color.Gray, W.Range, Player.Instance);

            if (drawE)
                Circle.Draw(Color.Gray, E.Range, Player.Instance);

            if (drawR)
                Circle.Draw(Color.Gray, R.Range, Player.Instance);
        }

        private static void Combo(bool useQ, bool useE, bool useR)
        {
            if (useR)
            {
                int countR = _comboMenu["countr"].Cast<Slider>().CurrentValue;
                int hits =
                    EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget())
                        .Select(enemy => Prediction.Position.PredictCircularMissile(enemy, 0, (int) R.Range, 250, float.MaxValue, Player.Instance.ServerPosition, true))
                        .Count(predictionResult => predictionResult.HitChance >= HitChance.High);

                if (hits >= countR)
                    R.Cast();
            }

            if (useQ)
            {
                Obj_AI_Base targetQ = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (targetQ != null)
                    Q.CastMinimumHitchance(targetQ, HitChance.High);
            }
            
            if (useE)
            {
                if (EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget())
                    .Select(enemy => Prediction.Position.PredictCircularMissile(enemy, 0, (int) E.Range, 500, float.MaxValue, Player.Instance.ServerPosition, true))
                    .Any(predictionResult => predictionResult.HitChance >= HitChance.High))
                    E.Cast();
            }
        }

        private static void Harass(bool useQ, bool useE)
        {
            bool hasMana = Player.Instance.ManaPercent >= _harassMenu["mana"].Cast<Slider>().CurrentValue;

            if (!hasMana)
                return;

            if (useQ)
            {
                Obj_AI_Base targetQ = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (targetQ != null)
                    Q.CastMinimumHitchance(targetQ, HitChance.High);
            }

            if (useE)
            {
                if (EntityManager.Heroes.Enemies.Where(enemy => enemy.IsValidTarget())
                    .Select(enemy => Prediction.Position.PredictCircularMissile(enemy, 0, (int) E.Range, 500, float.MaxValue, Player.Instance.ServerPosition, true))
                    .Any(predictionResult => predictionResult.HitChance >= HitChance.High))
                    E.Cast();
            }
        }
    }
}
