using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace wzShaco
{
    public class Program
    {
        private static readonly Spell.Targeted Q = new Spell.Targeted(SpellSlot.Q, 400);
        private static readonly Spell.Skillshot W = new Spell.Skillshot(SpellSlot.W, 425, SkillShotType.Circular, 2250, int.MaxValue, 300);
        private static readonly Spell.Targeted E = new Spell.Targeted(SpellSlot.E, 625);
        private static readonly Spell.Active R = new Spell.Active(SpellSlot.R);

        private static void Main()
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.Hero != Champion.Shaco)
                return;

            Bootstrap.Init(null);

            Player.Instance.SetSkinId(7);

            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalker.OverrideOrbwalkPosition += OverrideOrbwalkPosition;
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if (target == null || target.IsDead || target.Type != GameObjectType.AIHeroClient ||
                (!Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass) && !Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo)))
                return;

            if (CastActiveItem(ItemId.Tiamat_Melee_Only) || CastActiveItem(ItemId.Ravenous_Hydra_Melee_Only))
                return;
        }

        private static Vector3 bla;
        private static void Drawing_OnDraw(EventArgs args)
        {
            Circle.Draw(new ColorBGRA(50, 100, 150, 255), Q.Range, Player.Instance.Position);
            Circle.Draw(new ColorBGRA(80, 200, 150, 255), E.Range, Player.Instance.Position);
            Circle.Draw(new ColorBGRA(200, 200, 25, 255), 10, 20, bla);

            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget()))
            {
                int dmg = (int)((Player.Instance.GetAutoAttackDamage(enemy, true) + GetEDamage(enemy, true)) / enemy.Health * 100f);
                Drawing.DrawText(enemy.HPBarPosition + new Vector2(-70, 0), Color.AntiqueWhite, dmg + "%", 12);
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            KillstealE();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                AIHeroClient targetQ = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (targetQ != null && Q.CanCast(targetQ) && QBackstab(targetQ))
                    return;

                if (!Player.HasBuff("Deceive") && !Orbwalker.IsAutoAttacking)
                {
                    AIHeroClient targetE = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                    if (targetE != null && E.CanCast(targetE) && E.Cast(targetE))
                    {
                        Orbwalker.ResetAutoAttack();
                        return;
                    }

                    AIHeroClient targetW = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                    if (targetW != null && W.IsReady() && targetW.IsHPBarRendered)
                    {
                        if (W.CastMinimumHitchance(targetW, HitChance.Immobile) || WNearestTower(targetW))
                            return;
                    }
                }
            }
        }

        private static bool WNearestTower(Obj_AI_Base target)
        {
            Obj_AI_Turret tower = ObjectManager.Get<Obj_AI_Turret>().OrderBy(x => x.Distance(target)).FirstOrDefault(x => x.IsValidTarget(null, true));
            if (tower == null || !target.IsFacing(tower))
                return false;

            Vector3[] path = target.GetPath(tower.Position, true);
            float range = Player.Instance.Distance(target) + W.Range;
            Vector3 castPosition = Vector3.Zero;

            for (int index = 0; index < path.Length - 1; ++index)
            {
                Vector2 vector21 = path[index].To2D();
                Vector2 vector22 = path[index + 1].To2D();
                float num = vector21.Distance(vector22);
                if (num > range)
                {
                    castPosition = Player.Instance.Position.Extend(vector21.Extend(vector22, range), W.Range).To3D();
                    break;
                }
                range -= num;
            }

            bool returnvalue = castPosition != Vector3.Zero && W.Cast(castPosition);
            bla = castPosition;

            return returnvalue;
        }

        private static bool QBackstab(Obj_AI_Base target)
        {
            Vector3 castPosition = GetQPosition(target);

            if (!Player.Instance.IsInRange(castPosition, Q.Range))
                return false;

            if (!Q.Cast(castPosition))
                return false;

            //Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            return true;
        }

        private static Vector3? OverrideOrbwalkPosition()
        {
            Obj_AI_Base target;
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                target = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() - 50f, DamageType.Physical);
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear))
                target =
                    EntityManager.MinionsAndMonsters.GetJungleMonsters(Player.Instance.Position, Player.Instance.GetAutoAttackRange() + 50f)
                        .FirstOrDefault(x => x.IsValidTarget() && x.IsFacing(Player.Instance) && !x.BaseSkinName.Contains("Mini"));
            else
                return null;

            if (target == null || !target.IsFacing(Player.Instance))
                return null;

            return GetQPosition(target);
        }

        private static void KillstealE()
        {
            if (!E.IsReady())
                return;

            AIHeroClient target =
                EntityManager.Heroes.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.HasBuffOfType(BuffType.SpellShield) && x.Health < GetEDamage(x, false))
                    .OrderByDescending(TargetSelector.GetPriority)
                    .FirstOrDefault();

            if (target != null)
                E.Cast(target);
        }

        private static float GetEDamage(Obj_AI_Base target, bool ignorePassive)
        {
            float dmg;

            if (ignorePassive || target.IsFacing(Player.Instance))
                dmg = (10 + (40 * E.Level)) + Player.Instance.TotalMagicalDamage + Player.Instance.FlatPhysicalDamageMod;
            else
                dmg = (12 + (48 * E.Level)) + (Player.Instance.TotalMagicalDamage * 1.2f) + (Player.Instance.FlatPhysicalDamageMod * 1.2f);

            return Player.Instance.CalculateDamageOnUnit(target, DamageType.Magical, dmg, true, true);
        }

        private static Vector3 GetQPosition(Obj_AI_Base target)
        {
            return target.Position.Extend(target.Position + target.Direction.To2D().Perpendicular().To3D(), -(target.BoundingRadius)).To3D();
        }

        private static bool CastActiveItem(ItemId itemId)
        {
            InventorySlot inventorySlot = Player.Instance.InventoryItems.FirstOrDefault(x => x.Id == itemId);
            if (inventorySlot != null && Player.GetSpell(inventorySlot.SpellSlot).IsReady)
                return Player.CastSpell(inventorySlot.SpellSlot);

            return false;
        }

    }
}
