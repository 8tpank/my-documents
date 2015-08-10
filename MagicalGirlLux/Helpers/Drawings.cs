using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace MagicalGirlLux.Helpers
{
    internal class Drawings : Program
    {
        public static void DrawEvent()
        {
            Drawing.OnDraw += DamageIndicator;
            Drawing.OnDraw += HuDisplay;
            Drawing.OnDraw += Indicator;
            Drawing.OnEndScene += MiniMapDraw;
        }
        static void MiniMapDraw (EventArgs args)
        {
            bool drawMinimapR = Config.Item("drawMinimapR").GetValue<bool>();
            if (ObjectManager.Player.Level >= 6 && drawMinimapR)
                Utility.DrawCircle(ObjectManager.Player.Position, R.Range, Color.DeepSkyBlue, 2, 30, true);
        }

        public static void Indicator(EventArgs args)
        {

            var enemies1 = HeroManager.Enemies.Where(e => !e.IsDead).ToList();
            var enemies2 = HeroManager.Enemies.Where(e => !e.IsDead && player.Distance(e.Position) < 3000).ToList();

            foreach (var enemy in enemies1.Where(enemy => enemy.Team != player.Team))
            {
                if (enemy.IsVisible && !enemy.IsDead)
                {
                    if (Config.Item("indicator").GetValue<bool>())
                    {
                        var pos = player.Position +
                                  Vector3.Normalize(enemy.Position - player.Position)*200;
                        var myPos = Drawing.WorldToScreen(pos);
                        pos = player.Position + Vector3.Normalize(enemy.Position - player.Position)*450;
                        var ePos = Drawing.WorldToScreen(pos);

                        var linecolor = Color.LawnGreen;
                        var linecolor2 = Color.LawnGreen;
                        if (enemy.Position.Distance(player.Position) > 3000)
                        {
                            linecolor = Color.LawnGreen;
                        }
                        else if (enemy.Position.Distance(player.Position) < 3000) 
                            linecolor = Color.Red;
                        if (enemies2.Count > 1)
                            linecolor2 = Color.Red;

                        if (Config.Item("indicator").GetValue<bool>())
                            Drawing.DrawLine(myPos.X, myPos.Y, ePos.X, ePos.Y, 2, linecolor);
                            Render.Circle.DrawCircle(player.Position, 200, linecolor2);
                        }
                    }
                }
            }
        


        public static void HuDisplay(EventArgs args)
        {
            if (Config.Item("Draw_Disabled").GetValue<bool>())
                return;

            if (Config.Item("HUD").GetValue<bool>())
            {
                var moveY = Config.Item("sliderY").GetValue<Slider>().Value*10;
                var moveX = Config.Item("sliderX").GetValue<Slider>().Value*10;
                    
                //if you see this, you might think I'm crazy. You're probably right. 
                if (Config.Item("HUD").GetValue<bool>())
                    Drawing.DrawText(Drawing.Width - moveY - 190 + 400, Drawing.Height - moveX - 296 + 400, System.Drawing.Color.Yellow, "Minions Killed: " + player.MinionsKilled);

                if (Config.Item("HUD").GetValue<bool>())
                    Drawing.DrawText(Drawing.Width - moveY - 190 + 400, Drawing.Height - moveX - 278 + 400, System.Drawing.Color.Yellow, "Total Kills: " + player.ChampionsKilled);

                if (Config.Item("HUD").GetValue<bool>())
                    Drawing.DrawText(Drawing.Width - moveY - 190 + 400, Drawing.Height - moveX - 336 + 400, System.Drawing.Color.Pink, "     [Magical Girl Lux]");

                if (Config.Item("HUD").GetValue<bool>())
                    Drawing.DrawText(Drawing.Width - moveY - 190 + 400, Drawing.Height - moveX - 260 + 400, System.Drawing.Color.Yellow, "Total Gold: " + player.GoldTotal.ToString("#.##"));


                if (Config.Item("SmartKS").GetValue<bool>())
                    Drawing.DrawText(Drawing.Width - moveY - 190 + 400, Drawing.Height - moveX - 314 + 400, System.Drawing.Color.Yellow,
                        "Smart Killsteal: On");
                else
                    Drawing.DrawText(Drawing.Width - moveY - 190 + 40, Drawing.Height - moveX - 314 + 400, System.Drawing.Color.Tomato,
                        "Smart Killsteal: Off");
            }

        }

        public static void DamageIndicator(EventArgs args)
        {
            if (Config.Item("Draw_Disabled").GetValue<bool>())
                return;

            var mode = Config.Item("dmgdrawer").GetValue<StringList>().SelectedIndex;
            if (mode == 0)
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != player.Team))
                {
                    if (enemy.IsVisible && !enemy.IsDead)                                     
                        {

                            var combodamage = (Program.CalcDamage(enemy));

                            var PercentHPleftAfterCombo = (enemy.Health - combodamage)/enemy.MaxHealth;
                            var PercentHPleft = enemy.Health/enemy.MaxHealth;
                            if (PercentHPleftAfterCombo < 0)
                                PercentHPleftAfterCombo = 0;

                            var hpBarPos = enemy.HPBarPosition;
                            hpBarPos.X += 45;
                            hpBarPos.Y += 18;
                            double comboXPos = hpBarPos.X - 36 + (107*PercentHPleftAfterCombo);
                            double currentHpxPos = hpBarPos.X - 36 + (107*PercentHPleft);
                            var diff = currentHpxPos - comboXPos;
                            for (var i = 0; i < diff; i++)
                            {
                                Drawing.DrawLine(
                                    (float) comboXPos + i, hpBarPos.Y + 2, (float) comboXPos + i,
                                    hpBarPos.Y + 10, 1, Config.Item("dmgcolor").GetValue<Circle>().Color);
                                Utility.HpBarDamageIndicator.Enabled = false;
                            }

                        }
                    }
                }
            if (mode == 1)
            {
                    Utility.HpBarDamageIndicator.DamageToUnit = Program.CalcDamage;
                    Utility.HpBarDamageIndicator.Color = Config.Item("dmgcolor").GetValue<Circle>().Color;
                    Utility.HpBarDamageIndicator.Enabled = true;                
            }
            }        
        }
    }



