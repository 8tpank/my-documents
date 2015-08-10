using System;
using System.Drawing;

namespace VayneHunter_Reborn.Utility
{
    #region
    using System.Collections.Generic;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.Common;
    #endregion

    internal class CustomTargetSelector
    {

        #region

        public static Obj_AI_Hero selectedHero { get; set; }
        public static Obj_AI_Hero scriptSelectedHero { get; set; }

        private static readonly List<PriorityClass> priorityList = new List<PriorityClass>()
        {
            new PriorityClass()
            {
                Name = "Highest",
                Champions = new []
                {
                    "Ahri", "Anivia", "Annie", "Ashe", "Azir", "Brand", "Caitlyn", "Cassiopeia", "Corki", "Draven",
                    "Ezreal", "Graves", "Jinx", "Kalista", "Karma", "Karthus", "Katarina", "Kennen", "KogMaw", "Leblanc",
                    "Lucian", "Lux", "Malzahar", "MasterYi", "MissFortune", "Orianna", "Quinn", "Sivir", "Syndra", "Talon",
                    "Teemo", "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne", "Veigar", "VelKoz", "Viktor", "Xerath",
                    "Zed", "Ziggs"
                },
                Priority = Priority.Highest
            },

            new PriorityClass()
            {
                Name = "High",
                Champions = new []
                {
                    "Akali", "Diana", "Ekko", "Fiddlesticks", "Fiora", "Fizz", "Heimerdinger", "Jayce", "Kassadin",
                    "Kayle", "Kha'Zix", "Lissandra", "Mordekaiser", "Nidalee", "Riven", "Shaco", "Vladimir", "Yasuo",
                    "Zilean"
                },
                Priority = Priority.Highest
            },

            new PriorityClass()
            {
                Name = "Medium",
                Champions = new []
                {
                    "Aatrox", "Darius", "Elise", "Evelynn", "Galio", "Gangplank", "Gragas", "Irelia", "Jax",
                    "Lee Sin", "Maokai", "Morgana", "Nocturne", "Pantheon", "Poppy", "Rengar", "Rumble", "Ryze", "Swain",
                    "Trundle", "Tryndamere", "Udyr", "Urgot", "Vi", "XinZhao", "RekSai"
                },
                Priority = Priority.Medium
            },

            new PriorityClass()
            {
                Name = "Low",
                Champions = new []
                {
                    "Alistar", "Amumu", "Bard", "Blitzcrank", "Braum", "Cho'Gath", "Dr. Mundo", "Garen", "Gnar",
                    "Hecarim", "Janna", "Jarvan IV", "Leona", "Lulu", "Malphite", "Nami", "Nasus", "Nautilus", "Nunu",
                    "Olaf", "Rammus", "Renekton", "Sejuani", "Shen", "Shyvana", "Singed", "Sion", "Skarner", "Sona",
                    "Soraka", "Taric", "Thresh", "Volibear", "Warwick", "MonkeyKing", "Yorick", "Zac", "Zyra"
                },
                Priority = Priority.Low
            }
        };
        #endregion

        internal static void OnLoad(Menu mainMenu)
        {
            var ctsMenu = new Menu("[VHR] Custom TargetSelector", "dz191.vhr.cts");
            {
                ctsMenu.AddItem(
                    new MenuItem("dz191.vhr.cts.selector", "Target Selector").SetValue(
                        new StringList(new[] { "VHR Custom", "Default" }, 1)));
                ctsMenu.AddItem(new MenuItem("dz191.vhr.cts.separator1", ""));
            }

            if (ctsMenu.Item("dz191.vhr.cts.selector").GetValue<StringList>().SelectedIndex == 1)
            {
                TargetSelector.AddToMenu(ctsMenu);
            }
            else
            {
                ConstructCustomMenu(ctsMenu);
                ctsMenu.AddItem(new MenuItem("dz191.vhr.cts.separator2", ""));
                ctsMenu.AddItem(new MenuItem("dz191.vhr.cts.forcetarget", "Focus Selected Target").SetValue(false));
                ctsMenu.AddItem(new MenuItem("dz191.vhr.cts.drawcircle", "Draw Circle").SetValue(true));
            }
            mainMenu.AddSubMenu(ctsMenu);
            
        }
        internal static void RegisterEvents()
        {
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (VayneHunterReborn.Menu.Item("dz191.vhr.cts.drawcircle").GetValue<bool>() && selectedHero.IsValidTarget())
            {
                Render.Circle.DrawCircle(selectedHero.Position, 160, Color.Red, 7, true);
            }
        }

        static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint) WindowsMessages.WM_LBUTTONDOWN)
            {
                return;
            }
            selectedHero =
                HeroManager.Enemies
                    .FindAll(hero => hero.IsValidTarget() && hero.Distance(Game.CursorPos, true) < 40000) // 200 * 200
                    .OrderBy(h => h.Distance(Game.CursorPos, true)).FirstOrDefault();
        }

        internal static void ConstructCustomMenu(Menu ctsMenu)
        {
            var Enemies = HeroManager.Enemies;
            var priorityDictionary = Enemies.ToDictionary(Enemy => Enemy, Enemy => GetPriorityByName(Enemy.ChampionName));
            foreach (var entry in priorityDictionary)
            {
                ctsMenu.AddItem(
                    new MenuItem(
                        "dz191.vhr.cts.heroes." + entry.Key.ChampionName.ToLowerInvariant(), entry.Key.ChampionName)
                        .SetValue(new Slider((int)entry.Value, 1, 5)));
            }
        }

        internal static Obj_AI_Hero GetTarget(float Range)
        {
            if (IsActive())
            {
                var EnemiesInRange = ObjectManager.Player.GetEnemiesInRange(Range);
                if (EnemiesInRange.Any())
                {
                    var priorityDictionary = EnemiesInRange.Where(en => en.IsValidTarget(Range)).ToDictionary(Enemy => Enemy, Enemy => GetPriorityByName(Enemy.ChampionName));
                    if (priorityDictionary.Any())
                    {
                        var HighestPriorityTarget = priorityDictionary.OrderByDescending(pair => pair.Value).First().Key;
                        if (HighestPriorityTarget != null && HighestPriorityTarget.IsValidTarget(Range))
                        {
                            var HighestPriority = VayneHunterReborn.Menu.Item("dz191.vhr.cts.heroes." + HighestPriorityTarget.ChampionName.ToLowerInvariant()).GetValue<Slider>().Value;

                            var numberOfAttacks = HighestPriorityTarget.Health / ObjectManager.Player.GetAutoAttackDamage(HighestPriorityTarget);
                        
                            foreach (var Item in priorityDictionary.Where(item => item.Key != HighestPriorityTarget))
                            {
                                var attacksNumber = HighestPriorityTarget.Health / ObjectManager.Player.GetAutoAttackDamage(Item.Key);
                                if ((attacksNumber <= 1 && Item.Key.IsValidTarget(Range)) || ((numberOfAttacks - attacksNumber) > 4 && Item.Key.IsValidTarget(Range)))
                                {
                                    return Item.Key;
                                }
                            
                                if ((int)Item.Value >= HighestPriority)
                                {
                                    if (attacksNumber < numberOfAttacks && Item.Key.IsValidTarget(Range))
                                    {
                                        numberOfAttacks = attacksNumber;
                                        HighestPriorityTarget = Item.Key;
                                        HighestPriority = (int) Item.Value;
                                    }
                                }

                                if (!priorityDictionary.Any(m => (int)m.Value >= HighestPriority))
                                {
                                    HighestPriority -= 1;
                                }
                            }
                            if (selectedHero != null && selectedHero.IsDead)
                            {
                                selectedHero = null;
                            }else if (selectedHero.IsValidTarget(Range))
                            {
                                return selectedHero;
                            }

                            if (scriptSelectedHero != null && scriptSelectedHero.IsDead)
                            {
                                scriptSelectedHero = null;
                            }else if (scriptSelectedHero.IsValidTarget(Range))
                            {
                                return scriptSelectedHero;
                            }
                        return HighestPriorityTarget;
                        }
                    }
                }
            }
            return TargetSelector.GetTarget(Range, TargetSelector.DamageType.Physical);
        }

        internal static bool IsActive()
        {
            return VayneHunterReborn.Menu.Item("dz191.vhr.cts.selector").GetValue<StringList>().SelectedIndex == 0;
        }

        internal static Priority GetPriorityByName(string name)
        {
            if (priorityList.Any(m => m.Champions.Contains(name)))
            {
                return priorityList.First(m => m.Champions.Contains(name)).Priority;
            }

            return Priority.Low;
        }
    }

    internal class PriorityClass
    {
        public string Name { get; set; }

        public Priority Priority { get; set; }

        public string[] Champions { get; set; } 
    }

    enum Priority
    {
        Highest = 4, 
        High = 3, 
        Medium = 2, 
        Low = 1
    }
}
