using System;
using System.Drawing;
using System.Globalization;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Utils;
using SharpDX;

using Color = System.Drawing.Color;

namespace wzUtility.CooldownTracker
{
    public class Tracker
    {
        private readonly Menu _cooldownTrackerMenu;
        private readonly SpellSlot[] _summonerSpellSlots = { SpellSlot.Summoner1, SpellSlot.Summoner2 };
        private readonly SpellSlot[] _spellSlots = { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
        private Text _text;

        public Tracker(Menu mainMenu)
        {
            _cooldownTrackerMenu = mainMenu.AddSubMenu("Cooldown Tracker", "cooldowntrackermenu");
            _cooldownTrackerMenu.AddGroupLabel("Cooldown Tracker");

            _cooldownTrackerMenu.Add("trackallies", new CheckBox("Track Allies"));
            _cooldownTrackerMenu.Add("trackenemies", new CheckBox("Track Enemies"));

            _text = new Text("", new Font(FontFamily.GenericSansSerif, 8, FontStyle.Bold))
            {
                Color = Color.AntiqueWhite,
                
                TextAlign = Text.Align.Center,
                TextOrientation = Text.Orientation.Center
            };

            Drawing.OnEndScene += Drawing_OnEndScene;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += OnDomainUnload;
        }

        private void Drawing_OnEndScene(EventArgs args)
        {
            foreach (AIHeroClient hero in EntityManager.Heroes.AllHeroes)
            {
                if (!hero.IsHPBarRendered || !hero.HPBarPosition.IsOnScreen() || hero.IsMe || hero.IsDead || (hero.IsAlly && !_cooldownTrackerMenu["trackallies"].Cast<CheckBox>().CurrentValue) ||
                    (hero.IsEnemy && !_cooldownTrackerMenu["trackenemies"].Cast<CheckBox>().CurrentValue))
                    continue;

                Vector2 startVector2 = new Vector2((int) hero.HPBarPosition.X - 1, (int) hero.HPBarPosition.Y + 2);

                #region SummmonersBar Block

                DrawingHelper.DrawFilledRectangle(startVector2.X - 14, startVector2.Y, 14, 29, Color.FromArgb(115, 113, 115)); // light gray box
                DrawingHelper.DrawRectangle(startVector2.X - 13, startVector2.Y + 1, 13, 13, Color.Black); // 1st outer box
                DrawingHelper.DrawRectangle(startVector2.X - 13, startVector2.Y + 15, 13, 13, Color.Black); // 2nd outer box

                #endregion

                foreach (SpellSlot slot in _summonerSpellSlots)
                {
                    SpellDataInst spell = hero.Spellbook.GetSpell(slot);
                    float cooldown = Math.Max(0, spell.CooldownExpires - Game.Time);
                    int cooldownLvl = Math.Max(0, spell.Level - 1);
                    float percent = 1 - Math.Min(1, cooldown/spell.SData.CooldownArray[cooldownLvl]);

                    if (slot == SpellSlot.Summoner1)
                    {
                        DrawingHelper.DrawFilledRectangle(startVector2.X - 12, startVector2.Y + 2, 11, 11, GetSummonerColor(spell.Name));
                        if (percent < 1f)
                        {
                            Drawing.DrawLine(startVector2.X - 12, startVector2.Y + 2, startVector2.X - 1, startVector2.Y + 13, 1f, Color.Black);
                            Drawing.DrawLine(startVector2.X - 1, startVector2.Y + 1, startVector2.X - 13, startVector2.Y + 13, 1f, Color.Black);

                            _text.TextValue = Math.Ceiling(cooldown).ToString(CultureInfo.InvariantCulture);
                            _text.Position = new Vector2(startVector2.X - 21 - _text.TextValue.Length*5, startVector2.Y - 1);
                            _text.Draw();
                        }
                    }
                    else
                    {
                        DrawingHelper.DrawFilledRectangle(startVector2.X - 12, startVector2.Y + 16, 11, 11, GetSummonerColor(spell.Name));
                        if (percent < 1f)
                        {
                            Drawing.DrawLine(startVector2.X - 12, startVector2.Y + 16, startVector2.X - 1, startVector2.Y + 27, 1f, Color.Black);
                            Drawing.DrawLine(startVector2.X - 1, startVector2.Y + 15, startVector2.X - 13, startVector2.Y + 27, 1f, Color.Black);

                            _text.TextValue = Math.Ceiling(cooldown).ToString(CultureInfo.InvariantCulture);
                            _text.Position = new Vector2(startVector2.X - 21 - _text.TextValue.Length*5, startVector2.Y + 15);
                            _text.Draw();
                        }
                    }
                }

                #region SpellBar Block

                Drawing.DrawLine(startVector2.X, startVector2.Y + 18, startVector2.X, startVector2.Y + 29, 1, Color.FromArgb(115, 113, 115)); //left light gray outer line
                Drawing.DrawLine(startVector2.X, startVector2.Y + 28, startVector2.X + 130, startVector2.Y + 28, 1, Color.FromArgb(115, 113, 115)); //bottom light gray outer line
                Drawing.DrawLine(startVector2.X + 130, startVector2.Y + 28, startVector2.X + 134, startVector2.Y + 24, 1, Color.FromArgb(115, 113, 115));
                    //right diagonal light gray outer line
                Drawing.DrawLine(startVector2.X + 133, startVector2.Y + 24, startVector2.X + 133, startVector2.Y + 17, 1, Color.FromArgb(115, 113, 115));
                    //right up light gray outer line

                Drawing.DrawLine(startVector2.X + 1, startVector2.Y + 18, startVector2.X + 1, startVector2.Y + 27, 1, Color.FromArgb(74, 69, 74)); //left dark gray middle line
                Drawing.DrawLine(startVector2.X + 1, startVector2.Y + 27, startVector2.X + 130, startVector2.Y + 27, 1, Color.FromArgb(74, 69, 74)); //bottom dark gray middle line
                Drawing.DrawLine(startVector2.X + 130, startVector2.Y + 27, startVector2.X + 133, startVector2.Y + 24, 1, Color.FromArgb(74, 69, 74));
                    //right diagonal dark gray middle line

                Drawing.DrawLine(startVector2.X + 2, startVector2.Y + 17, startVector2.X + 2, startVector2.Y + 26, 1, Color.Black); //left black inner line
                Drawing.DrawLine(startVector2.X + 2, startVector2.Y + 26, startVector2.X + 107, startVector2.Y + 26, 1, Color.Black); //bottom black inner line

                Drawing.DrawLine(startVector2.X + 2, startVector2.Y + 17, startVector2.X + 2, startVector2.Y + 18, 1, Color.FromArgb(115, 113, 115)); //Pixel fix light
                Drawing.DrawLine(startVector2.X + 2, startVector2.Y + 18, startVector2.X + 2, startVector2.Y + 19, 1, Color.FromArgb(49, 48, 49)); //Pixel fix dark

                DrawingHelper.DrawRectangle(startVector2.X + 2, startVector2.Y + 19, 106, 8, Color.Black); //SpellBar container

                #endregion

                foreach (SpellSlot slot in _spellSlots)
                {
                    SpellDataInst spell = hero.Spellbook.GetSpell(slot);
                    float cooldown = Math.Max(0, spell.CooldownExpires - Game.Time);
                    int cooldownLvl = Math.Max(0, spell.Level - 1);
                    float percent = 1 - Math.Min(1, cooldown/spell.SData.CooldownArray[cooldownLvl]);

                    int xoffset = 0;
                    float length = 25;
                    switch (slot)
                    {
                        case SpellSlot.Q:
                            xoffset = 3;
                            break;
                        case SpellSlot.W:
                            xoffset = 29;
                            break;
                        case SpellSlot.E:
                            xoffset = 55;
                            break;
                        case SpellSlot.R:
                            xoffset = 81;
                            length = 26;
                            break;
                    }

                    if (hero.Spellbook.CanUseSpell(slot) == SpellState.NotLearned)
                        DrawingHelper.DrawFilledRectangle(startVector2.X + xoffset, startVector2.Y + 20, length, 6, Color.Gray);
                    else
                    {
                        if (percent >= 1f)
                        {
                            DrawingHelper.DrawFilledRectangle(startVector2.X + xoffset, startVector2.Y + 20, length, 6, Color.Green);
                        }
                        else
                        {
                            DrawingHelper.DrawFilledRectangle(startVector2.X + xoffset, startVector2.Y + 20, length, 6, Color.DarkGray);
                            DrawingHelper.DrawFilledRectangle(startVector2.X + xoffset, startVector2.Y + 20, length*percent, 6, Color.Orange);
                            _text.TextValue = Math.Ceiling(cooldown).ToString(CultureInfo.InvariantCulture);
                            _text.Position = new Vector2(startVector2.X + xoffset + 13 - _text.TextValue.Length*3, startVector2.Y + 29);
                            _text.Draw();
                        }
                    }
                }

                Drawing.DrawLine(startVector2.X + 28, startVector2.Y + 20, startVector2.X + 28, startVector2.Y + 26, 1, Color.Black); //First Line
                Drawing.DrawLine(startVector2.X + 54, startVector2.Y + 20, startVector2.X + 54, startVector2.Y + 26, 1, Color.Black); //Second Line
                Drawing.DrawLine(startVector2.X + 80, startVector2.Y + 20, startVector2.X + 80, startVector2.Y + 26, 1, Color.Black); // Third Line
            }
        }

        private static Color GetSummonerColor(string name)
        {
            Color color;

            switch (name.ToLower())
            {
                case "summonerbarrier":
                    color = Color.FromArgb(255, 200, 153, 0);
                    break;
                case "summonersnowball":
                    color = Color.White;
                    break;
                case "summonerodingarrison":
                    color = Color.Green;
                    break;
                case "summonerclairvoyance":
                    color = Color.Blue;
                    break;
                case "summonerboost": //cleanse
                    color = Color.LightBlue;
                    break;
                case "summonermana":
                    color = Color.DarkBlue;
                    break;
                case "summonerteleport":
                    color = Color.Purple;
                    break;
                case "summonerheal":
                    color = Color.GreenYellow;
                    break;
                case "summonerexhaust":
                    color = Color.FromArgb(255, 255, 150, 0);
                    break;
                case "summonerdot":
                    color = Color.Red;
                    break;
                case "summonerhaste":
                    color = Color.SkyBlue;
                    break;
                case "summonerflash":
                    color = Color.Yellow;
                    break;
                case "summonersmite":
                case "s5_summonersmiteduel":
                case "s5_summonersmiteplayerganker":
                case "s5_summonersmitequick":
                case "itemsmiteaoe":
                    color = Color.FromArgb(255, 148, 77, 16);
                    break;
                default:
                    color = Color.Black;
                    break;
            }

            return color;
        }

        //Not in use atm.
/*
        private string GetSummonerSpellName(string name)
        {
            string text = "";

            switch (name.ToLower())
            {
                case "summonerbarrier":
                    text = "Barrier";
                    break;
                case "summonersnowball":
                    text = "Snowball";
                    break;
                case "summonerodingarrison":
                    text = "Garrison";
                    break;
                case "summonerclairvoyance":
                    text = "Clairvoyance";
                    break;
                case "summonerboost": //cleanse
                    text = "Cleanse";
                    break;
                case "summonermana":
                    text = "Clarity";
                    break;
                case "summonerteleport":
                    text = "Teleport";
                    break;
                case "summonerheal":
                    text = "Heal";
                    break;
                case "summonerexhaust":
                    text = "Exhausht";
                    break;
                case "summonerdot":
                    text = "Ignite";
                    break;
                case "summonerhaste":
                    text = "Ghost";
                    break;
                case "summonerflash":
                    text = "Flash";
                    break;
                case "summonersmite":
                case "s5_summonersmiteduel":
                case "s5_summonersmiteplayerganker":
                case "s5_summonersmitequick":
                case "itemsmiteaoe":
                    text = "Smite";
                    break;
            }

            return text;
        }
*/

        private void OnDomainUnload(object sender, EventArgs e)
        {
            if (_text == null) return;

            _text.Dispose();
            _text = null;
        }
    }
}
