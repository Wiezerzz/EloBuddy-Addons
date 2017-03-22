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

namespace wzUtility.GankAlerter
{
    public class Alerter
    {
        private Menu menu, gankAlerterMenu;
        private List<ChampionAlertInfo> Alerts = new List<ChampionAlertInfo>();
        private Text _text;
        private Vector3 enemyFountain = ObjectManager.Get<Obj_SpawnPoint>().First(o => o.IsEnemy).Position;

        public Alerter(Menu mainMenu)
        {
            menu = mainMenu;

            gankAlerterMenu = menu.AddSubMenu("Gank Alerter", "gankalertermenu");
            gankAlerterMenu.AddGroupLabel("Gank Alerter Tracker");

            gankAlerterMenu.Add("triggerrange", new Slider("Trigger Range", 3000, 1000, 10000));
            gankAlerterMenu.Add("triggercooldown", new Slider("Trigger Cooldown (seconds)", 10, 0, 60));
            gankAlerterMenu.Add("lineduration", new Slider("Line Duration (seconds)", 10, 1, 60));

            _text = new Text("", new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold))
            {
                Color = System.Drawing.Color.AntiqueWhite,
                TextAlign = Text.Align.Center,
                TextOrientation = Text.Orientation.Center
            };

            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                Alerts.Add(new ChampionAlertInfo {NetworkId = enemy.NetworkId, PlayerName = enemy.Name, ChampionName = enemy.ChampionName, LastPosition = enemy.ServerPosition});
            }

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (Player.Instance.IsDead)
                return;

            foreach (ChampionAlertInfo championAlert in Alerts)
            {
                if (!championAlert.ShouldDraw)
                    continue;

                Line.DrawLine(System.Drawing.Color.Black, 6f, Player.Instance.Position, championAlert.LastPosition);
                Line.DrawLine(System.Drawing.Color.DarkRed, 5f, Player.Instance.Position, championAlert.LastPosition);

                _text.TextValue = championAlert.ChampionName;
                _text.Position = Drawing.WorldToScreen(Player.Instance.Position).Extend(Drawing.WorldToScreen(championAlert.LastPosition), 200 + (Alerts.FindIndex(x => x == championAlert) * 40)) -
                                 new Vector2(_text.Bounding.Width/2f, _text.Bounding.Height/2f);
                _text.Draw();
            }
        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (Player.Instance.IsDead)
                return;

            foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
            {
                ChampionAlertInfo championAlert = Alerts.FirstOrDefault(x => x.NetworkId == enemy.NetworkId);
                if (championAlert == null)
                    continue;

                championAlert.LastPosition = enemy.IsDead ? enemyFountain : enemy.ServerPosition;

                if (enemy.IsDead)
                    championAlert.DrawUntil = 0f;

                if (!enemy.IsDead && enemy.IsHPBarRendered && Player.Instance.Distance(championAlert.LastPosition) <= gankAlerterMenu["triggerrange"].Cast<Slider>().CurrentValue && ShouldUpdate(championAlert))
                    Update(championAlert);
            }
        }

        private bool ShouldUpdate(ChampionAlertInfo championAlertInfo)
        {
            return championAlertInfo.DrawUntil + gankAlerterMenu["triggercooldown"].Cast<Slider>().CurrentValue <= Game.Time;
        }

        private void Update(ChampionAlertInfo championAlertInfo)
        {
            championAlertInfo.DrawUntil = Game.Time + gankAlerterMenu["lineduration"].Cast<Slider>().CurrentValue;
        }

        internal class ChampionAlertInfo
        {
            public int NetworkId;
            public string PlayerName;
            public string ChampionName;
            public Vector3 LastPosition = Vector3.Zero;
            public float DrawUntil;

            public bool ShouldDraw
            {
                get { return DrawUntil >= Game.Time; }
            }
        }
    }
}
