using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using Color = System.Drawing.Color;

namespace wzUtility.CooldownTracker
{
    class Tracker
    {

        private static Menu menu, spellTrackerMenu;
        private static SpellSlot[] summonerSpellSlots = { SpellSlot.Summoner1, SpellSlot.Summoner2 };
        private static SpellSlot[] spellSlots = { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };

        public Tracker(Menu mainMenu)
        {
            menu = mainMenu;

            spellTrackerMenu = menu.AddSubMenu("Cooldown Tracker", "cooldowntracker");
            spellTrackerMenu.AddGroupLabel("Cooldown Tracker");

            spellTrackerMenu.Add("trackallies", new CheckBox("Track Allies"));
            spellTrackerMenu.Add("trackenemies", new CheckBox("Track Enemies"));

            AIHeroClient.OnProcessSpellCast += AIHeroClient_OnProcessSpellCast;
            Drawing.OnEndScene += Drawing_OnDraw;
        }

        void AIHeroClient_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            AIHeroClient hero = sender as AIHeroClient;
            if (hero != null)
            {

            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            foreach (AIHeroClient hero in EntityManager.Heroes.AllHeroes)
            {
                if (!hero.IsHPBarRendered || hero.IsMe)
                    continue;
                
                Vector2 startVector2 = new Vector2(hero.HPBarPosition.X - 12, hero.HPBarPosition.Y + (hero.IsAlly ? 13 : 13.2f));
                
                #region SpellBar Block
                Drawing.DrawLine(startVector2.X, startVector2.Y + 18, startVector2.X, startVector2.Y + 29, 1, Color.FromArgb(115, 113, 115)); //left light gray outer line
                Drawing.DrawLine(startVector2.X, startVector2.Y + 28, startVector2.X + 130, startVector2.Y + 28, 1, Color.FromArgb(115, 113, 115)); //bottom light gray outer line
                Drawing.DrawLine(startVector2.X + 130, startVector2.Y + 28, startVector2.X + 134, startVector2.Y + 24, 1, Color.FromArgb(115, 113, 115)); //right diagonal light gray outer line
                Drawing.DrawLine(startVector2.X + 133, startVector2.Y + 24, startVector2.X + 133, startVector2.Y + 17, 1, Color.FromArgb(115, 113, 115)); //right up light gray outer line

                Drawing.DrawLine(startVector2.X + 1, startVector2.Y + 18, startVector2.X + 1, startVector2.Y + 27, 1, Color.FromArgb(74, 69, 74)); //left dark gray middle line
                Drawing.DrawLine(startVector2.X + 1, startVector2.Y + 27, startVector2.X + 130, startVector2.Y + 27, 1, Color.FromArgb(74, 69, 74)); //bottom dark gray middle line
                Drawing.DrawLine(startVector2.X + 130, startVector2.Y + 27, startVector2.X + 133, startVector2.Y + 24, 1, Color.FromArgb(74, 69, 74)); //right diagonal dark gray middle line

                Drawing.DrawLine(startVector2.X + 2, startVector2.Y + 17, startVector2.X + 2, startVector2.Y + 26, 1, Color.Black); //left black inner line
                Drawing.DrawLine(startVector2.X + 2, startVector2.Y + 26, startVector2.X + 107, startVector2.Y + 26, 1, Color.Black); //bottom black inner line

                Drawing.DrawLine(startVector2.X + 2, startVector2.Y + 17, startVector2.X + 2, startVector2.Y + 18, 1, Color.FromArgb(115, 113, 115)); //Pixel fix light
                Drawing.DrawLine(startVector2.X + 2, startVector2.Y + 18, startVector2.X + 2, startVector2.Y + 19, 1, Color.FromArgb(49, 48, 49)); //Pixel fix dark
                #endregion
                
                DrawingHelper.DrawRectangle(startVector2.X + 2, startVector2.Y + 19, 106, 8, Color.Black); //SpellBar container

                foreach (SpellSlot slot in spellSlots)
                {
                    SpellDataInst spell = hero.Spellbook.GetSpell(slot);
                    float time = spell.CooldownExpires - Game.Time;
                    float totalCooldown = spell.Cooldown;

                    int Xoffset = 0;
                    float length = 25;
                    switch (slot)
                    {
                        case SpellSlot.Q:
                            Xoffset = 3;
                            break;
                        case SpellSlot.W:
                            Xoffset = 29;
                            break;
                        case SpellSlot.E:
                            Xoffset = 55;
                            break;
                        case SpellSlot.R:
                            Xoffset = 81;
                            length = 26;
                            break;
                    }

                    SpellState spellState = hero.Spellbook.CanUseSpell(slot);
                    float percent = (time > 0 && Math.Abs(totalCooldown) > float.Epsilon) ? 1f - (time / totalCooldown) : 1f;

                    if (spellState != SpellState.NotLearned)
                    {
                        if (percent == 1f)
                            DrawingHelper.DrawFilledRectangle(startVector2.X + Xoffset, startVector2.Y + 20, length, 6, Color.Green);
                        else
                        {
                            DrawingHelper.DrawFilledRectangle(startVector2.X + Xoffset, startVector2.Y + 20, length, 6, Color.DimGray);
                            DrawingHelper.DrawFilledRectangle(startVector2.X + Xoffset, startVector2.Y + 20, length * percent, 6, Color.Orange);
                            Drawing.DrawText(startVector2.X + Xoffset + 9, startVector2.Y + 30, Color.Black, Math.Round(time).ToString());
                            Drawing.DrawText(startVector2.X + Xoffset + 8, startVector2.Y + 29, Color.White, Math.Round(time).ToString());
                        }

                    }
                    else
                    {
                        DrawingHelper.DrawFilledRectangle(startVector2.X + Xoffset, startVector2.Y + 20, length, 6, Color.Gray);
                    }
                }

                Drawing.DrawLine(startVector2.X + 28, startVector2.Y + 20, startVector2.X + 28, startVector2.Y + 26, 1, Color.Black); //First Line
                Drawing.DrawLine(startVector2.X + 54, startVector2.Y + 20, startVector2.X + 54, startVector2.Y + 26, 1, Color.Black); //Second Line
                Drawing.DrawLine(startVector2.X + 80, startVector2.Y + 20, startVector2.X + 80, startVector2.Y + 26, 1, Color.Black); // Third Line
            }
        }
    }
}
