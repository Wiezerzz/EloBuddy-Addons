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

using CheckBox = EloBuddy.SDK.Menu.Values.CheckBox;
using Color = System.Drawing.Color;
using Menu = EloBuddy.SDK.Menu.Menu;

namespace wzUtility.RecallTracker
{
    public class Tracker
    {
        private readonly Menu _recallTrackerMenu;
        private readonly Text _text;

        private readonly List<Recall> _recalls = new List<Recall>();
        private readonly List<RecallPrediction> _recallPredictions = new List<RecallPrediction>(); 
        private float _recallbarWidth = 250f;
        private float _recallbarHeight = 15f;

        public Tracker(Menu mainMenu)
        {
            #region Menu
            _recallTrackerMenu = mainMenu.AddSubMenu("Recall Tracker", "recalltrackermenu");
            _recallTrackerMenu.AddGroupLabel("Recall Tracker");

            _recallTrackerMenu.Add("xposition", new Slider("X Position", (int)Math.Round(Screen.PrimaryScreen.Bounds.Width * 0.4083333333333333d), 0, Screen.PrimaryScreen.Bounds.Width));
            _recallTrackerMenu.Add("yposition", new Slider("Y Position", (int)Math.Round(Screen.PrimaryScreen.Bounds.Height * 0.6953703703703704d), 0, Screen.PrimaryScreen.Bounds.Height));
            _recallTrackerMenu.Add("opacity", new Slider("Opacity", 70));
            _recallTrackerMenu.AddSeparator(1);
            CheckBox reset = _recallTrackerMenu.Add("resetxy", new CheckBox("Reset X/Y to 100", false));
            reset.OnValueChange += (sender, args) =>
            {
                if (args.NewValue)
                {
                    _recallTrackerMenu["xposition"].Cast<Slider>().CurrentValue = 100;
                    _recallTrackerMenu["yposition"].Cast<Slider>().CurrentValue = 100;
                    Core.DelayAction(() => reset.CurrentValue = false, 200);
                }
            };

            CheckBox reset2 = _recallTrackerMenu.Add("resetoriginal", new CheckBox("Reset X/Y to default", false));
            reset2.OnValueChange += (sender, args) =>
            {
                if (args.NewValue)
                {
                    _recallTrackerMenu["xposition"].Cast<Slider>().CurrentValue = (int)Math.Round(Screen.PrimaryScreen.Bounds.Width * 0.4083333333333333d);
                    _recallTrackerMenu["yposition"].Cast<Slider>().CurrentValue = (int)Math.Round(Screen.PrimaryScreen.Bounds.Height * 0.6953703703703704d);
                    Core.DelayAction(() => reset2.CurrentValue = false, 200);
                }
            };
            #endregion

            _text = new Text("", new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold))
            {
                Color = Color.AntiqueWhite
            };

            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                _recallPredictions.Add(new RecallPrediction(enemy.NetworkId, Game.Time, enemy.RealPath()));
            }

            Game.OnUpdate += Game_OnUpdate;
            Teleport.OnTeleport += Teleport_OnTeleport;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                if (enemy.IsHPBarRendered)
                    continue;

                RecallPrediction recallPrediction = _recallPredictions.FirstOrDefault(x => x.NetworkId == enemy.NetworkId);
                if (recallPrediction == null)
                    continue;

                Recall recall = _recalls.FirstOrDefault(x => x.NetworkId == enemy.NetworkId);
                if (recall == null)
                    continue;

                Vector2 predictedPosition = Vector2.Zero;

                float range = (Math.Abs(recallPrediction.LastSeen - recall.StartTime) - 0.5f) * enemy.MoveSpeed;
                Vector3[] realPath = recallPrediction.Path;
                for (int index = 0; index < realPath.Length - 1; ++index)
                {
                    Vector2 vector21 = realPath[index].To2D();
                    Vector2 vector22 = realPath[index + 1].To2D();
                    float num = vector21.Distance(vector22);
                    if (num > range)
                    {
                        predictedPosition = vector21.Extend(vector22, range);
                        break;
                    }
                    range -= num;
                }

                if (predictedPosition == Vector2.Zero)
                    predictedPosition = realPath.Last().To2D();

                if (!predictedPosition.IsValid())
                    continue;

                Circle.Draw(SharpDX.Color.HotPink, enemy.BoundingRadius, predictedPosition.To3D());
            }
        }

        private void Game_OnUpdate(EventArgs args)
        {
            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                if (!enemy.IsHPBarRendered)
                    continue;

                RecallPrediction recallPrediction = _recallPredictions.FirstOrDefault(x => x.NetworkId == enemy.NetworkId);
                if (recallPrediction == null)
                    continue;

                recallPrediction.LastSeen = Game.Time;

                recallPrediction.Path = enemy.RealPath();


            }
        }

        private void Teleport_OnTeleport(Obj_AI_Base sender, Teleport.TeleportEventArgs args)
        {
            if (sender.Type != GameObjectType.AIHeroClient || args.Type != TeleportType.Recall)
                return;

            AIHeroClient player = sender as AIHeroClient;

            if (player == null || player.IsMe || player.IsAlly)
                return;

            switch (args.Status)
            {
                case TeleportStatus.Start:
                    Recall startedRecall = _recalls.FirstOrDefault(x => x.NetworkId == player.NetworkId);

                    if (startedRecall != null)
                        _recalls.Remove(startedRecall);

                    _recalls.Add(new Recall(player.NetworkId, player.ChampionName, player.HealthPercent, args.Duration / 1000f));
                    break;
                case TeleportStatus.Abort:
                    Recall abortedRecall = _recalls.FirstOrDefault(x => x.NetworkId == player.NetworkId);

                    if (abortedRecall != null)
                    {
                        abortedRecall.IsAborted = true;
                        Core.DelayAction(() => _recalls.Remove(abortedRecall), 2000);
                    }
                    break;
                case TeleportStatus.Finish:
                    //BUG: Procs when recall aborts with less than 0.3 second left.
                    Recall finishedRecall = _recalls.FirstOrDefault(x => x.NetworkId == player.NetworkId);

                    if (finishedRecall != null)
                        _recalls.Remove(finishedRecall);
                    break;
            }
        }

        private void Drawing_OnEndScene(EventArgs args)
        {
            for (int i = 0; i < _recalls.Count; i++)
            {
                /*
                 * BUG: Procs when recall aborts with less than 0.3 second left.
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

                DrawSingleRecallBar(_recallTrackerMenu["xposition"].Cast<Slider>().CurrentValue, _recallTrackerMenu["yposition"].Cast<Slider>().CurrentValue - ((_recallbarHeight + 2) * i), _recalls[i]);
            }
        }

        private void DrawSingleRecallBar(float x, float y, Recall recall)
        {
            int opacity = (int)((_recallTrackerMenu["opacity"].Cast<Slider>().CurrentValue/100f)*255);

            DrawingHelper.DrawRectangle(x, y, _recallbarWidth, _recallbarHeight, Color.FromArgb(opacity, Color.Black));
            DrawingHelper.DrawRectangle(x + 1, y + 1, _recallbarWidth - 2, _recallbarHeight - 2, Color.FromArgb(opacity, Color.Black));
            DrawingHelper.DrawRectangle(x + 2, y + 2, _recallbarWidth - 4, _recallbarHeight - 4, Color.FromArgb(opacity, Color.Gray));

            DrawingHelper.DrawFilledRectangle(x + 2, y + 2, (_recallbarWidth - 4)*recall.Percent(), _recallbarHeight - 4,
                !recall.IsAborted ? Color.FromArgb(opacity, DrawingHelper.Interpolate(Color.Red, Color.LawnGreen, recall.Percent())) : Color.FromArgb(opacity, Color.SlateGray));

            _text.TextValue = recall.Name;
            _text.Position = new Vector2(x + _recallbarWidth + 3, y);
            _text.Draw();

            _text.Position = new Vector2(x + _recallbarWidth + 6 + _text.Bounding.Width, y);
            _text.TextValue = "(" + Math.Round(recall.HealthPercent) + "%)";
            _text.Color = DrawingHelper.Interpolate(Color.Red, Color.LawnGreen, recall.HealthPercent / 100f);
            _text.Draw();

            _text.Color = Color.AntiqueWhite;
        }
    }
}
