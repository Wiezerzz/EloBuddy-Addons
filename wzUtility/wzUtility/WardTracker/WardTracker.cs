using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace wzUtility.WardTracker
{
    class Tracker
    {
        private Menu menu, wardTrackerMenu;
        private Text Text;

        private Dictionary<int, WardObject> wards = new Dictionary<int, WardObject>(); 

        public Tracker(Menu mainMenu)
        {
            menu = mainMenu;

            wardTrackerMenu = menu.AddSubMenu("Ward Tracker", "wardtrackermenu");
            wardTrackerMenu.AddGroupLabel("Ward Tracker");

            wardTrackerMenu.Add("showtime", new CheckBox("Show ward timer"));
            wardTrackerMenu.Add("wardrange", new Slider("Ward circle radius", 150, 100, 500));

            Text = new Text("", new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold))
            {
                Color = Color.AntiqueWhite
            };

            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += OnDomainUnload;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            #region Visible wards

            foreach (KeyValuePair<int, WardObject> ward in wards)
            {
                if (!ward.Value.IsPink && ward.Value.Expires < Game.Time)
                {
                    if (wards.ContainsKey(ward.Key))
                        Core.DelayAction(() => wards.Remove(ward.Key), 0);
                }

                if(!ward.Value.Position.IsOnScreen())
                    continue;

                string time = "0";

                if (!ward.Value.IsPink)
                    time = string.Format("{0:mm\\:ss}", TimeSpan.FromSeconds(ward.Value.Expires - Game.Time));

                Circle.Draw(ward.Value.IsPink ? new ColorBGRA(240, 12, 147, 200) : new ColorBGRA(0, 180, 0, 200), wardTrackerMenu["wardrange"].Cast<Slider>().CurrentValue, ward.Value.Position);

                if (wardTrackerMenu["showtime"].Cast<CheckBox>().CurrentValue && !ward.Value.IsPink)
                {
                    Text.TextValue = time;
                    Text.Position = ward.Value.Position.WorldToScreen() - new Vector2(Text.Bounding.Width / 2, -16);
                    Text.Draw();
                }
            }

            #endregion
        }

        private void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            #region Visible wards

            if (sender.Type == GameObjectType.obj_AI_Minion)
            {
                Obj_AI_Minion ward = sender as Obj_AI_Minion;

                if (ward == null || !ward.IsEnemy)
                    return;
                
                if (wards.ContainsKey(ward.NetworkId))
                {
                    Core.DelayAction(() =>
                    {
                        Obj_AI_Minion tempWard = ObjectManager.GetUnitByNetworkId((uint)ward.NetworkId) as Obj_AI_Minion;

                        wards[ward.NetworkId].Expires = Game.Time + (tempWard.GetBuff("sharedwardbuff").EndTime - Game.Time);
                        wards[ward.NetworkId].Position = tempWard.ServerPosition;
                    }, 1);
                    return;
                }

                switch (ward.Name)
                {
                    case "SightWard":
                        Core.DelayAction(() =>
                        {
                            Obj_AI_Minion tempWard = ObjectManager.GetUnitByNetworkId((uint) ward.NetworkId) as Obj_AI_Minion;
                            BuffInstance buff = tempWard.GetBuff("sharedwardbuff");

                            if (buff != null)
                                wards.Add(ward.NetworkId, new WardObject(false, ((AIHeroClient)buff.Caster).ChampionName, Game.Time + (buff.EndTime - Game.Time), ward.Position));
                        }, 1);

                        break;
                    case "VisionWard":
                        Core.DelayAction(() =>
                        {
                            Obj_AI_Minion tempWard = ObjectManager.GetUnitByNetworkId((uint) ward.NetworkId) as Obj_AI_Minion;
                            BuffInstance buff = tempWard.GetBuff("sharedwardbuff");

                            if (buff != null)
                            {
                                wards.Add(ward.NetworkId, new WardObject(false, ((AIHeroClient)buff.Caster).ChampionName, Game.Time + (buff.EndTime - Game.Time), ward.Position));
                            }
                            else
                            {
                                buff = tempWard.GetBuff("sharedvisionwardbuff");
                                if (buff == null)
                                    return;

                                wards.Add(ward.NetworkId, new WardObject(true, ((AIHeroClient)buff.Caster).ChampionName, float.MaxValue, ward.Position));
                            }
                        }, 1);
                        break;
                }
            }

            #endregion
        }

        private void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Type != GameObjectType.obj_AI_Minion)
                return;

            Obj_AI_Minion ward = sender as Obj_AI_Minion;

            if (wards.ContainsKey(ward.NetworkId))
                Core.DelayAction(() => wards.Remove(ward.NetworkId), 0);
        }

        private void OnDomainUnload(object sender, EventArgs e)
        {
            if (Text == null) return;

            Text.Dispose();
            Text = null;
        }
    }
}
