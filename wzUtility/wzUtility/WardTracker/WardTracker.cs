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
        private static Menu menu, wardTrackerMenu;
        private static Text Text;

        private static Dictionary<int, WardObject> wards = new Dictionary<int, WardObject>(); 
        private static List<WardObject> fovWards = new List<WardObject>(); 

        //TODO: FOW ward placement.
        //Text align not more needed

        public Tracker(Menu mainMenu)
        {
            menu = mainMenu;

            wardTrackerMenu = menu.AddSubMenu("Ward Tracker", "wardtrackermenu");
            wardTrackerMenu.AddGroupLabel("Ward Tracker");

            wardTrackerMenu.Add("showtime", new CheckBox("Show ward timer"));
            wardTrackerMenu.Add("wardrange", new Slider("Ward circle radius", 150, 100, 500));

            Text = new Text("", new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold))
            {
                Color = Color.AntiqueWhite,

                TextAlign = Text.Align.Center,
                TextOrientation = Text.Orientation.Center
            };

            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += OnDomainUnload;
        }

        private static void Drawing_OnDraw(EventArgs args)
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

            #region FOW wards

            foreach (WardObject ward in fovWards)
            {
                if(ward.Expires < Game.Time)
                {
                    if (fovWards.Contains(ward))
                        Core.DelayAction(() => fovWards.Remove(ward), 0);
                }

                if (!ward.Position.IsOnScreen())
                    continue;

                Circle.Draw(new ColorBGRA(0, 180, 0, 200), wardTrackerMenu["wardrange"].Cast<Slider>().CurrentValue, ward.Position);

                if (wardTrackerMenu["showtime"].Cast<CheckBox>().CurrentValue)
                {
                    Text.TextValue = string.Format("??{0:mm\\:ss}??", TimeSpan.FromSeconds(ward.Expires - Game.Time));
                    Text.Position = ward.Position.WorldToScreen() - new Vector2(Text.Bounding.Width / 2, -16);
                    Text.Draw();
                }
            }

            #endregion
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            #region Visible wards

            if (sender.Type == GameObjectType.obj_AI_Minion)
            {
                Obj_AI_Minion ward = sender as Obj_AI_Minion;

                if (ward == null || !ward.IsEnemy)
                    return;

                if (wards.ContainsKey(ward.NetworkId))
                {
                    wards[ward.NetworkId].Expires = Game.Time + ward.Mana;
                    wards[ward.NetworkId].Position = ward.ServerPosition;
                    return;
                }

                switch (ward.Name)
                {
                    case "SightWard":
                        wards.Add(ward.NetworkId,
                            new WardObject(ward.Name, false, (int) ward.Mana, (int) ward.MaxMana, Game.Time + ward.Mana,
                                ward.Position));
                        break;
                    case "VisionWard":
                        wards.Add(ward.NetworkId,
                            ward.BaseSkinName == "SightWard"
                                ? new WardObject(ward.Name, false, (int) ward.Mana, (int) ward.MaxMana,
                                    Game.Time + ward.Mana, ward.Position)
                                : new WardObject(ward.Name, true, (int) ward.Mana, (int) ward.MaxMana, 0, ward.Position));
                        break;
                }

                Core.DelayAction(() =>
                {
                    if (ward.IsValid && !ward.IsDead)
                    {
                        foreach (var fovward in fovWards)
                        {
                            if (fovward.Position.Distance(ward.Position) < 570)
                            {
                                Core.DelayAction(() => fovWards.Remove(fovward), 0);
                                break;
                            }
                        }
                    }
                }, 500);
            }

            #endregion

            #region FOW wards

            if (sender.Type == GameObjectType.MissileClient)
            {
                MissileClient missile = sender as MissileClient;

                if (missile != null && missile.SData.Name.ToLower() == "itemplacementmissile")
                {
                    if (!missile.SpellCaster.IsEnemy)
                        return;

                    Vector2 dir = (missile.EndPosition.To2D() - missile.StartPosition.To2D()).Normalized();
                    Vector3 pos = (missile.StartPosition.To2D() + dir * 570).To3DWorld();

                    //if visible ward placed in the last 2 seconds then return.
                    if (wards.Values.Any(ward => ward.Position.Distance(pos) < 570 && ward.Expires - Game.Time > ward.MaxMana - 2))
                        return;

                    fovWards.Add(new WardObject(missile.SData.Name.ToLower(), false, 180, 180, Game.Time + 180, pos));
                }
            }
            
            #endregion
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.Type != GameObjectType.obj_AI_Minion)
                return;

            Obj_AI_Minion ward = sender as Obj_AI_Minion;

            if (wards.ContainsKey(ward.NetworkId))
                Core.DelayAction(() => wards.Remove(ward.NetworkId), 0);
        }

        private static void OnDomainUnload(object sender, EventArgs e)
        {
            if (Text == null) return;

            Text.Dispose();
            Text = null;
        }
    }
}
