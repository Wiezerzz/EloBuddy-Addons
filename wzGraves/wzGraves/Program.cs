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
    public class Program
    {
        private static Menu _menu, _comboMenu, _harassMenu, _laneclearMenu, _jungleclearMenu, _drawingsMenu;

        private static readonly Dictionary<string, Spell.Skillshot> Spells = new Dictionary<string, Spell.Skillshot>
        {
            {"q", new Spell.Skillshot(SpellSlot.Q, 850, SkillShotType.Linear, 250, 2000, 60)},
            {"w", new Spell.Skillshot(SpellSlot.W, 950, SkillShotType.Circular, 250, 1650, 150)},
            {"e", new Spell.Skillshot(SpellSlot.E, 425, SkillShotType.Linear)},
            {"r", new Spell.Skillshot(SpellSlot.R, 1500, SkillShotType.Linear, 250, 2100, 100)}
        };

        private static readonly ColorBGRA DrawingsColour = new ColorBGRA(210, 100, 0, 255);

        //---------------------------------------------------------------------------------------------------------------//

        private static void Main()
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        #region Events

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.Hero != Champion.Graves)
                return;

            #region Main Menu

            _menu = MainMenu.AddMenu("wzGraves", "gravesmenu", "wzGraves");
            _menu.AddGroupLabel("wzGraves");
            _menu.AddLabel("Nothing to see here....");

            #endregion

            #region Combo Menu

            _comboMenu = _menu.AddSubMenu("Combo", "combomenu");
            _comboMenu.AddGroupLabel("Combo");

            _comboMenu.Add("comboq", new CheckBox("Use Q"));
            _comboMenu.Add("combowallq", new CheckBox("Use Wall Q"));
            _comboMenu.Add("combowimmobile", new CheckBox("Use W on immobile"));
            _comboMenu.Add("comboe", new CheckBox("Use E after AA"));
            _comboMenu.Add("combor", new CheckBox("Use R to execute"));

            #endregion

            #region Harass Menu

            _harassMenu = _menu.AddSubMenu("Harass", "harassmenu");
            _harassMenu.AddGroupLabel("Harass");

            _harassMenu.Add("harassq", new CheckBox("Use Q"));
            _harassMenu.Add("harasswallq", new CheckBox("Use Wall Q"));
            _harassMenu.Add("harassmana", new Slider("Minimum mana before using Q", 40));

            #endregion

            #region Laneclear Menu

            _laneclearMenu = _menu.AddSubMenu("Laneclear", "laneclearmenu");
            _laneclearMenu.AddGroupLabel("Laneclear");

            _laneclearMenu.Add("laneclearq", new CheckBox("Use Q", false));

            #endregion

            #region Jungleclear Menu

            _jungleclearMenu = _menu.AddSubMenu("Jungleclear", "jungleclearmenu");
            _jungleclearMenu.AddGroupLabel("Jungleclear");

            _jungleclearMenu.Add("jungleclearq", new CheckBox("Use Q"));
            _jungleclearMenu.Add("junglecleare", new CheckBox("Use E", false));

            #endregion

            #region Drawings Menu

            _drawingsMenu = _menu.AddSubMenu("Drawings", "drawingsmenu");
            _drawingsMenu.AddGroupLabel("Drawings");

            _drawingsMenu.Add("drawq", new CheckBox("Draw Q"));
            _drawingsMenu.Add("draww", new CheckBox("Draw W", false));
            _drawingsMenu.Add("drawe", new CheckBox("Draw E", false));
            _drawingsMenu.Add("drawr", new CheckBox("Draw R"));
            _drawingsMenu.AddSeparator();
            _drawingsMenu.Add("drawready", new CheckBox("Only draw when ready"));

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
            foreach (KeyValuePair<string, Spell.Skillshot> spell in Spells)
            {
                if (_drawingsMenu["draw" + spell.Key].Cast<CheckBox>().CurrentValue)
                {
                    if (_drawingsMenu["drawready"].Cast<CheckBox>().CurrentValue && spell.Value.IsReady() || !_drawingsMenu["drawready"].Cast<CheckBox>().CurrentValue)
                    {
                        Circle.Draw(DrawingsColour, spell.Value.Range, Player.Instance.Position);
                    }
                }
            }
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (Player.HasBuff("GravesBasicAttackAmmo2") || !Spells["e"].IsReady())
                return;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && _comboMenu["comboe"].Cast<CheckBox>().CurrentValue)
            {
                if (target.Type == GameObjectType.AIHeroClient)
                {
                    //Weird fix for BUG: Spells["e"].Cast(Game.CursorPos); not casting sometimes.
                    if (Player.CastSpell(SpellSlot.E, Game.CursorPos))
                        Orbwalker.ResetAutoAttack();
                }
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear) && _jungleclearMenu["junglecleare"].Cast<CheckBox>().CurrentValue)
            {
                Obj_AI_Minion jungleMob = (Obj_AI_Minion) target;

                if (jungleMob != null && jungleMob.IsMatureMonster())
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

            if (_comboMenu["combowimmobile"].Cast<CheckBox>().CurrentValue)
                AutoWImmobile();

            if (_comboMenu["combor"].Cast<CheckBox>().CurrentValue)
                HandleUlt();
        }

        private static void Harass()
        {
            if (!Spells["q"].IsReady() || Player.Instance.ManaPercent < _harassMenu["harassmana"].Cast<Slider>().CurrentValue ||
                !_harassMenu["harassq"].Cast<CheckBox>().CurrentValue && !_harassMenu["harasswallq"].Cast<CheckBox>().CurrentValue)
                return;

            AIHeroClient target = TargetSelector.GetTarget(Spells["q"].Range, DamageType.Physical);

            if (target != null)
            {
                if (_harassMenu["harasswallq"].Cast<CheckBox>().CurrentValue)
                {
                    if (CastWallQ(target))
                        return;
                }

                if (_harassMenu["harassq"].Cast<CheckBox>().CurrentValue)
                    CastBombQ(target);
            }
        }

        private static void LaneClear()
        {
            if (!_laneclearMenu["laneclearq"].Cast<CheckBox>().CurrentValue || !Spells["q"].IsReady())
                return;

            foreach (Obj_AI_Minion minion in EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, Player.Instance.Position, Spells["q"].Range))
            {
                if (minion.IsValidTarget(Spells["q"].Range) && Spells["q"].GetPrediction(minion).CollisionObjects.Count() >= 3 &&
                    !Player.Instance.CheckWallCollison(minion.Position))
                {
                    if (Spells["q"].Cast(minion.Position))
                        return;
                }
            }
        }

        private static void JungleClear()
        {
            if (!_jungleclearMenu["jungleclearq"].Cast<CheckBox>().CurrentValue || !Spells["q"].IsReady())
                return;

            Obj_AI_Minion jungleMob =
                EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, Spells["q"].Range).OrderByDescending(x => x.MaxHealth).FirstOrDefault();

            if (jungleMob != null && jungleMob.IsMatureMonster() && jungleMob.IsValidTarget(Spells["q"].Range) && !Player.Instance.CheckWallCollison(jungleMob.Position))
                CastWallQ(jungleMob);
        }

        #endregion

        #region Spell Methods

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

                if (_comboMenu["combowallq"].Cast<CheckBox>().CurrentValue)
                {
                    if (CastWallQ(target))
                        return;
                }

                if (_comboMenu["comboq"].Cast<CheckBox>().CurrentValue)
                    CastBombQ(target);
            }
        }

        private static bool CastWallQ(Obj_AI_Base target)
        {
            //400 castdelay instead of 250.
            PredictionResult pred = Prediction.Position.PredictLinearMissile(target, 850f, 60, 400, 2000f, 0, Player.Instance.Position, true);

            if (Player.Instance.CheckWallCollison(pred.CastPosition))
                return false;

            Vector3 qEndPosition = pred.CastPosition.ExtendVector3(Player.Instance.Position, -(Spells["q"].Range - Player.Instance.Distance(pred.CastPosition)));

            for (int i = 0; i < pred.CastPosition.Distance(qEndPosition); i += 30)
            {
                Vector3 wallPosition = pred.CastPosition.ExtendVector3(qEndPosition, pred.CastPosition.Distance(qEndPosition) - i);
                CollisionFlags collisionFlags = NavMesh.GetCollisionFlags(wallPosition);

                if (collisionFlags.HasFlag(CollisionFlags.Wall) || collisionFlags.HasFlag(CollisionFlags.Building))
                {
                    if (Spells["q"].Cast(pred.CastPosition))
                        return true;
                }
            }

            return false;
        }

        private static void CastBombQ(Obj_AI_Base target)
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

            foreach (
                AIHeroClient enemy in
                    EntityManager.Heroes.Enemies.Where(x => Player.Instance.Distance(x) > 600 && x.IsValidTarget(Spells["r"].Range) && x.Health < Spells["r"].CalculateRDamage(x)))
            {
                PredictionResult pred = Spells["r"].GetPrediction(enemy);

                if (Player.Instance.Distance(pred.CastPosition) > 1000 && enemy.Health > Spells["r"].CalculateR1Damage(enemy))
                    continue;

                if (pred.HitChance >= HitChance.Medium)
                {
                    if (Spells["r"].Cast(pred.CastPosition))
                        return;
                }
            }
        }

        #endregion

    }
}
 