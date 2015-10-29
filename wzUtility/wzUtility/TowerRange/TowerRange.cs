using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;

namespace wzUtility.TowerRange
{
    class TowerRange
    {
        private static Menu menu, towerRangeMenu;
        private static Dictionary<int, Obj_AI_Turret> turrets = new Dictionary<int, Obj_AI_Turret>();

        public TowerRange(Menu mainMenu)
        {
            menu = mainMenu;

            towerRangeMenu = menu.AddSubMenu("Tower Range Indicator", "towerrange");
            towerRangeMenu.AddGroupLabel("Tower Range Indicator");

            towerRangeMenu.Add("drawallyrange", new CheckBox("Draw enemy tower range"));
            towerRangeMenu.Add("drawenemyrange", new CheckBox("Draw enemy tower range"));
            towerRangeMenu.AddSeparator(0);
            towerRangeMenu.Add("rangetodraw", new Slider("Distance from tower to start drawing", 600, 300, 1000));


            Core.DelayAction(() =>
            {
                foreach (Obj_AI_Turret obj in EntityManager.Turrets.AllTurrets.Where(x => x.TotalAttackDamage < 800).Where(obj => !turrets.ContainsKey(obj.NetworkId)))
                {
                    turrets.Add(obj.NetworkId, obj);
                }
            }, 100);

            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            float turretRange = 800 + Player.Instance.BoundingRadius;
            int drawRange = towerRangeMenu["rangetodraw"].Cast<Slider>().CurrentValue;

            foreach (KeyValuePair<int, Obj_AI_Turret> entry in turrets)
            {
                Obj_AI_Turret turret = entry.Value;

                if (turret == null || !turret.IsValid || turret.IsDead)
                {
                    Core.DelayAction(() => turrets.Remove(entry.Key), 1);
                    continue;
                }

                if (turret.IsAlly && !towerRangeMenu["drawallyrange"].Cast<CheckBox>().CurrentValue || turret.IsEnemy && !towerRangeMenu["drawenemyrange"].Cast<CheckBox>().CurrentValue || Player.Instance.IsDead)
                {
                    continue;
                }

                float distToTurret = Player.Instance.ServerPosition.Distance(turret.Position);
                if (distToTurret < turretRange + drawRange)
                {
                    if (distToTurret < turretRange && turret.IsEnemy)
                    {
                        Circle.Draw(new ColorBGRA(255, 0, 0, 255), turretRange, 5, turret.Position);
                        continue;
                    }

                    float alpha = distToTurret > turretRange ? ((turretRange + drawRange - distToTurret)/2) / (drawRange / 2) : 1f;
                    Circle.Draw(new ColorBGRA(0, 255, 0, alpha), turretRange, 5, turret.Position);
                }
            }
        }
    }
}
