using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
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

        private static  Dictionary<int, WardObject> wards = new Dictionary<int, WardObject>(); 

        //TODO: FOW ward placement.
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
            foreach (KeyValuePair<int, WardObject> ward in wards)
            {
                if (ward.Value.Expires < Game.Time && !ward.Value.IsPink)
                {
                    if (wards.ContainsKey(ward.Key))
                        Core.DelayAction(() => wards.Remove(ward.Key), 1);
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
                    Text.Position = ward.Value.Position.WorldToScreen();
                    Text.Draw();
                }

            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            Obj_AI_Minion ward = sender as Obj_AI_Minion;

            if (ward != null)// && ward.IsEnemy)
            {
                if (wards.ContainsKey(ward.NetworkId))
                {
                    wards[ward.NetworkId].Expires = Game.Time + ward.Mana;
                    wards[ward.NetworkId].Position = ward.Position;
                    return;
                }

                switch (ward.Name)
                {
                    case "SightWard":
                        wards.Add(ward.NetworkId, new WardObject(ward.Name, false, Game.Time + ward.Mana, ward.Position));
                        break;
                    case "VisionWard":
                        wards.Add(ward.NetworkId, new WardObject(ward.Name, true, 0, ward.Position));
                        break;
                }
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            Obj_AI_Minion ward = sender as Obj_AI_Minion;

            if (ward == null) return;

            if (wards.ContainsKey(ward.NetworkId))
                Core.DelayAction(() => wards.Remove(ward.NetworkId), 1);
        }

        private static void OnDomainUnload(object sender, EventArgs e)
        {
            if (Text == null) return;

            Text.Dispose();
            Text = null;
        }
    }
}
