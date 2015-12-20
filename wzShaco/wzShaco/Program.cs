using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Rendering;
using SharpDX;
using wzShaco.Evade;
using Color = System.Drawing.Color;

namespace wzShaco
{
    class Program
    {
        private static Item Tiamat;
        private static Item Hydra;
        private static bool isEvadingSkill = false;

        private static readonly Dictionary<string, Spell.SpellBase> Spells = new Dictionary<string, Spell.SpellBase>
        {
            {"q", new Spell.Targeted(SpellSlot.Q, 400)},
            {"w", new Spell.Targeted(SpellSlot.W, 425)},
            {"e", new Spell.Targeted(SpellSlot.E, 625)},
            {"r", new Spell.Active(SpellSlot.R)}
        };

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.Instance.Hero != Champion.Shaco)
                return;

            Bootstrap.Init(null);

            foreach (InventorySlot item in Player.Instance.InventoryItems)
            {
                switch (item.Id)
                {
                    case ItemId.Tiamat_Melee_Only:
                        Tiamat = new Item(ItemId.Tiamat_Melee_Only, 250f);
                        break;
                    case ItemId.Ravenous_Hydra_Melee_Only:
                        Hydra = new Item(ItemId.Ravenous_Hydra_Melee_Only, 250f);
                        break;
                }
            }
            Chat.Print(EntityManager.Heroes.Enemies[0].Spellbook.Spells[3].SData.Name);
            Evade.Evade.Initialize();

            Shop.OnBuyItem += Shop_OnBuyItem;
            Shop.OnSellItem += Shop_OnSellItem;
            AIHeroClient.OnProcessSpellCast += AIHeroClient_OnProcessSpellCast;
            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void AIHeroClient_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsEnemy || sender.Type != GameObjectType.AIHeroClient || isEvadingSkill)
                return;

            if (args.Slot == SpellSlot.R)
            {
                //Targeted
                if (args.Target != null && args.Target.IsMe)
                {
                    Skill evadeSkill = Evade.Evade.skillList.FirstOrDefault(x => x.Name == args.SData.Name && x.Type == Skill.SkillType.Targeted);

                    if (evadeSkill != null)
                    {
                        Chat.Print(evadeSkill.Name + "||" + Game.Time + evadeSkill.Delay + "||" + args.Time +
                                   evadeSkill.Delay);

                        isEvadingSkill = true;
                        Core.DelayAction(() =>
                        {
                            isEvadingSkill = false;
                            if (Spells["r"].IsReady() && Player.Instance.Pet == null)
                            {
                                Chat.Print("DODGE2");
                                Spells["r"].Cast();
                            }
                        }, evadeSkill.Delay);
                        return;
                    }
                }

                Skill evadeGlobalAoE = Evade.Evade.skillList.FirstOrDefault(x => x.Name == args.SData.Name && x.Type == Skill.SkillType.GlobalAoE);

                if (evadeGlobalAoE != null)
                {
                    Chat.Print(evadeGlobalAoE.Name + "||" + Game.Time + evadeGlobalAoE.Delay + "||" + args.Time +
                               evadeGlobalAoE.Delay);

                    isEvadingSkill = true;
                    Core.DelayAction(() =>
                    {
                        isEvadingSkill = false;
                        if (Spells["r"].IsReady() && Player.Instance.Pet == null)
                        {
                            Chat.Print("DODGE3");
                            Spells["r"].Cast();
                        }
                    }, evadeGlobalAoE.Delay);
                }
            }
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit target, EventArgs args)
        {
            if(target == null || target.IsDead)
                return;

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && target.Type == GameObjectType.AIHeroClient)
            {
                if(CastTiamat())
                    return;

                CastHydra();
            }
        }
        
        private static void Shop_OnBuyItem(AIHeroClient sender, ShopActionEventArgs args)
        {
 	        if(!sender.IsMe)
                return;

            switch (args.Id)
            {
                case (int)ItemId.Tiamat_Melee_Only:
                    Tiamat = new Item(ItemId.Tiamat_Melee_Only, 250f);
                    break;
                case (int)ItemId.Ravenous_Hydra_Melee_Only:
                    Tiamat = null;
                    Hydra = new Item(ItemId.Ravenous_Hydra_Melee_Only, 250f);
                    break;
            }
        }

        private static void Shop_OnSellItem(AIHeroClient sender, ShopActionEventArgs args)
        {
            if (!sender.IsMe)
                return;

            switch (args.Id)
            {
                case (int)ItemId.Tiamat_Melee_Only:
                    Tiamat = null;
                    break;
                case (int)ItemId.Ravenous_Hydra_Melee_Only:
                    Hydra = null;
                    break;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            Circle.Draw(new ColorBGRA(50, 100, 150, 255), Spells["q"].Range, Player.Instance.Position);
            Circle.Draw(new ColorBGRA(80, 200, 150, 255), Spells["e"].Range, Player.Instance.Position);

            //Circle.Draw(new ColorBGRA(200, 108, 100, 255), 25, Player.Instance.Position.ExtendVector3(Player.Instance.Position + Player.Instance.Direction, 100f));

           // Circle.Draw(new ColorBGRA(200, 50, 150, 255), 25, EntityManager.Heroes.Enemies[0].Position.ExtendVector3(EntityManager.Heroes.Enemies[0].Position + EntityManager.Heroes.Enemies[0].Direction, -EntityManager.Heroes.Enemies[0].BoundingRadius));
           // Circle.Draw(new ColorBGRA(200, 50, 150, 255), 17, EntityManager.Heroes.Enemies[0].ServerPosition.ExtendVector3(EntityManager.Heroes.Enemies[0].ServerPosition + EntityManager.Heroes.Enemies[0].Direction, -EntityManager.Heroes.Enemies[0].BoundingRadius));
        }

        private static void Game_OnTick(EventArgs args)
        {
            KillstealE();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                TryBackstab();

                AIHeroClient targetQ = TargetSelector.GetTarget(Spells["q"].Range, DamageType.Physical);
                CastBackstabQ(targetQ);

                if (!Player.HasBuff("Deceive"))
                {
                    AIHeroClient targetE = TargetSelector.GetTarget(Spells["e"].Range, DamageType.Mixed);
                    CastE(targetE);
                }
            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                AIHeroClient targetE = TargetSelector.GetTarget(Spells["e"].Range, DamageType.Mixed);
                CastE(targetE);
            }
        }

        private static bool CastE(AIHeroClient target)
        {
            if (!Spells["e"].IsReady())
                return false;

            if (target != null && target.IsValidTarget(Spells["e"].Range) && !target.HasBuffOfType(BuffType.SpellShield))
            {
                if (Spells["e"].Cast(target))
                    return true;
            }

            return false;
        }

        private static bool CastBackstabQ(AIHeroClient target)
        {
            if (!Spells["q"].IsReady())
                return false;

            if (target != null)
            {
                Vector3 QCastPosition = target.Position.ExtendVector3(target.Position + target.Direction, -target.BoundingRadius);

                if (!Player.Instance.IsInRange(QCastPosition, Spells["q"].Range))
                    return false;

                if (Spells["q"].Cast(QCastPosition))
                {
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }
            }

            return false;
        }

        private static void TryBackstab()
        {
            AIHeroClient target = TargetSelector.GetTarget(Player.Instance.GetAutoAttackRange() + 50f, DamageType.Physical);

            if (target != null && target.IsFacingFixed(Player.Instance) && !Orbwalker.IsAutoAttacking && Orbwalker.CanMove)
            {
                if (!Orbwalker.DisableMovement)
                    Orbwalker.DisableMovement = true;

                Player.IssueOrder(GameObjectOrder.MoveTo, target.Position.ExtendVector3(target.Position + target.Direction, -target.BoundingRadius));

                if (Orbwalker.DisableMovement)
                    Orbwalker.DisableMovement = false;
            }
        }

        private static void KillstealE()
        {
            if (!Spells["e"].IsReady())
                return;

            AIHeroClient target = EntityManager.Heroes.Enemies.FirstOrDefault(x => x.IsValidTarget(Spells["e"].Range) && !x.HasBuffOfType(BuffType.SpellShield) && x.Health < Spells["e"].CalculateEDamage(x));

            if (target != null)
                Spells["e"].Cast(target);
        }

        private static bool CastTiamat()
        {
            if (Tiamat != null)
            {
                InventorySlot inventorySlot = Player.Instance.InventoryItems.FirstOrDefault(x => x.Id == Tiamat.Id);
                if (inventorySlot != null && Player.GetSpell(inventorySlot.SpellSlot).IsReady)
                {
                    if (Player.CastSpell(inventorySlot.SpellSlot))
                        return true;

                }
                return false;
            }
            return false;
        }

        private static bool CastHydra()
        {
            if (Hydra != null)
            {
                InventorySlot inventorySlot = Player.Instance.InventoryItems.FirstOrDefault(x => x.Id == Hydra.Id);
                if (inventorySlot != null && Player.GetSpell(inventorySlot.SpellSlot).IsReady)
                {
                    if (Player.CastSpell(inventorySlot.SpellSlot))
                        return true;

                }
                return false;
            }
            return false;
        }

    }
}
