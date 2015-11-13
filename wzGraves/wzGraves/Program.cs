using System;
using System.Collections.Generic;
using System.Diagnostics;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Rendering;
using SharpDX;

using Color = System.Drawing.Color;

namespace wzGraves
{
    class Program
    {

        //static Spell.Skillshot spellQ = new Spell.Skillshot(SpellSlot.Q, 850, SkillShotType.Circular, 900, 2000, 80);
        static Spell.Skillshot spellQ = new Spell.Skillshot(SpellSlot.Q, 850, SkillShotType.Linear, 250, 2000, 80);
        private static Vector3 pos2;
        private static Vector3 pos;
        private static Stopwatch stopwatch = new Stopwatch();
        private static List<Vector3> vecs = new List<Vector3>();
        private static List<Vector3> vecs2 = new List<Vector3>();

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
        }

        static void Loading_OnLoadingComplete(EventArgs args)
        {
            if(Player.Instance.Hero != Champion.Graves)
                return;

            Bootstrap.Init(null);
            
            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if(sender.Name == "Graves_Base_Q_Bomb.troy")
            {
                stopwatch.Stop();
                Chat.Print(stopwatch.ElapsedMilliseconds + "ms");
            }
        }

        static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Graves_Base_Q_Bomb.troy")
            {
                stopwatch.Restart();
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            Circle.Draw(new ColorBGRA(210, 100, 0, 255), 850f, Player.Instance.Position);
            Circle.Draw(new ColorBGRA(255, 255, 0, 255), 80, pos2);
            Circle.Draw(new ColorBGRA(52, 100, 10, 255), 50, pos);

            foreach (Vector3 vector3 in vecs)
            {
                Circle.Draw(new ColorBGRA(255, 255, 255, 255), 15, vector3);
            }

            foreach (Vector3 vector3 in vecs2)
            {
                Circle.Draw(new ColorBGRA(255, 100, 255, 255), 15, vector3);
            }
        }

        static void Game_OnTick(EventArgs args)
        {
            switch (Orbwalker.ActiveModesFlags)
            {
                case Orbwalker.ActiveModes.Harass:
                    AIHeroClient target = TargetSelector.GetTarget(spellQ.Range, DamageType.Physical);

                    if (target != null)
                    {
                        PredictionResult pred = spellQ.GetPrediction(target);
                        pos2 = pred.CastPosition;

                        pos = pred.CastPosition.ExtendVector3(Player.Instance.Position, -(spellQ.Range - Player.Instance.Distance(pred.CastPosition)));

                        vecs.Clear();
                        vecs2.Clear();
                        for (int i = 0; i < pred.CastPosition.Distance(pos); i += 30)
                        {
                            Vector3 cPos = pred.CastPosition.ExtendVector3(pos, pred.CastPosition.Distance(pos) - i);
                            
                            if (cPos.ToNavMeshCell().CollFlags.HasFlag(CollisionFlags.Wall) || cPos.ToNavMeshCell().CollFlags.HasFlag(CollisionFlags.Building))
                            {
                                vecs.Add(cPos);
                                spellQ.Cast(pred.CastPosition);
                            }
                            else
                            {
                                vecs2.Add(cPos);
                            }
                        }
                        
                    }
                    break;
            }
        }
    }
}
 