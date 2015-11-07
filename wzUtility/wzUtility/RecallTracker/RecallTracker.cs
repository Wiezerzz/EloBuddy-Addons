using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;
using Menu = EloBuddy.SDK.Menu.Menu;

namespace wzUtility.RecallTracker
{
    class Tracker
    {
        private Menu menu, recallTrackerMenu;
        private Text Text;

        private List<Recall> Recalls = new List<Recall>();
        private float recallbarWidth = 250f;
        private float recallbarHeight = 15f;

        public Tracker(Menu mainMenu)
        {
            menu = mainMenu;

            recallTrackerMenu = menu.AddSubMenu("Recall Tracker", "recalltrackermenu");
            recallTrackerMenu.AddGroupLabel("Recall Tracker");

            recallTrackerMenu.Add("xposition", new Slider("X Position", (int)Math.Round(Screen.PrimaryScreen.Bounds.Width * 0.4083333333333333), 0, Screen.PrimaryScreen.Bounds.Width));
            recallTrackerMenu.Add("yposition", new Slider("Y Position", (int)Math.Round(Screen.PrimaryScreen.Bounds.Height * 0.6953703703703704d), 0, Screen.PrimaryScreen.Bounds.Height));

            Text = new Text("", new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold))
            {
                Color = Color.AntiqueWhite
            };

            Teleport.OnTeleport += Teleport_OnTeleport;
            Drawing.OnDraw += Drawing_OnDraw;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += OnDomainUnload;
        }

        private void Teleport_OnTeleport(Obj_AI_Base sender, Teleport.TeleportEventArgs args)
        {
            if (sender.Type != GameObjectType.AIHeroClient && args.Type != TeleportType.Recall)
                return;

            AIHeroClient player = sender as AIHeroClient;

            if (player == null || player.IsMe)
                return;

            switch (args.Status)
            {
                case TeleportStatus.Start:
                    Recalls.Add(new Recall(player.ChampionName, player.HealthPercent, Game.Time + (args.Duration / 1000f), args.Duration / 1000f));
                    break;
                case TeleportStatus.Abort:
                    Recall abortedRecall = Recalls.FirstOrDefault(x => x.Name == player.ChampionName);

                    if (abortedRecall != null)
                    {
                        abortedRecall.Abort();
                        Core.DelayAction(() => Recalls.Remove(abortedRecall), 2000);
                    }
                    break;
                case TeleportStatus.Finish:
                    //BUG: Procs when recall aborts with less than 1 second left.
                    Recall finishedRecall = Recalls.First(x => x.Name == player.ChampionName);

                    if (finishedRecall != null)
                        Recalls.Remove(finishedRecall);
                    break;
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            for (int i = 0; i < Recalls.Count; i++)
            {
                /*
                 * BUG: Procs when recall aborts with less than 1 second left.
                 * 
                if (!Recalls[i].IsAborted && 0 >= Recalls[i].Elapsed)
                {
                    if (Recalls[i] != null)
                    {
                        Chat.Print("removed");
                        Recalls.Remove(Recalls[i]);
                        return;
                    }
                }*/

                DrawSingleRecallBar(recallTrackerMenu["xposition"].Cast<Slider>().CurrentValue, recallTrackerMenu["yposition"].Cast<Slider>().CurrentValue - ((recallbarHeight + 2) * i), Recalls[i]);
            }
        }

        private void DrawSingleRecallBar(float X, float Y, Recall recall)
        {
            int opacity = (int)((60f/100f)*255);

            DrawingHelper.DrawRectangle(X, Y, recallbarWidth, recallbarHeight, Color.FromArgb(opacity, Color.Black));
            DrawingHelper.DrawRectangle(X + 1, Y + 1, recallbarWidth - 2, recallbarHeight - 2, Color.FromArgb(opacity, Color.Black));
            DrawingHelper.DrawRectangle(X + 2, Y + 2, recallbarWidth - 4, recallbarHeight - 4, Color.FromArgb(opacity, Color.Gray));

            if (!recall.IsAborted)
                DrawingHelper.DrawFilledRectangle(X + 2, Y + 2, (recallbarWidth - 4) * recall.Percent(), recallbarHeight - 4, Color.FromArgb(opacity, DrawingHelper.Interpolate(Color.Red, Color.LawnGreen, recall.Percent())));
            else
                DrawingHelper.DrawFilledRectangle(X + 2, Y + 2, (recallbarWidth - 4)*recall.Percent(), recallbarHeight - 4, Color.FromArgb(opacity, Color.Gray));

            Text.TextValue = recall.Name;
            Text.Position = new Vector2(X + recallbarWidth + 3, Y);
            Text.Draw();

            Text.Position = new Vector2(X + recallbarWidth + 6 + Text.Bounding.Width, Y);
            Text.TextValue = "(" + Math.Round(recall.HealthPercent) + "%)";
            Text.Color = DrawingHelper.Interpolate(Color.Red, Color.LawnGreen, recall.HealthPercent / 100f);
            Text.Draw();

            Text.Color = Color.AntiqueWhite;
        }

        private void OnDomainUnload(object sender, EventArgs e)
        {
            if (Text == null) return;

            Text.Dispose();
            Text = null;
        }
    }
}
