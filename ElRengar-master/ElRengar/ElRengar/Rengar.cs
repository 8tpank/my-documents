using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using LeagueSharp;
using LeagueSharp.Common;

using SharpDX;

using Color = System.Drawing.Color;

namespace ElRengar
{
    using ItemData = LeagueSharp.Common.Data.ItemData;

    internal enum Spells
    {
        Q,

        W,

        E,

        R
    }

    internal class Rengar
    {
        public static Obj_AI_Hero Player
        {
            get
            {
                return ObjectManager.Player;
            }
        }

        private static SpellSlot ignite;

        private static SpellSlot smite;

        private static Items.Item youmuu, cutlass, blade, tiamat, hydra;

        private static Notification notification;

        private static Obj_AI_Base SelectedTarget = null;

        public static Orbwalking.Orbwalker orbwalker;

        public static bool UsingLxOrbwalker;

        private static int lastSwitch;

        private static int lastNotification = 0;

        public static readonly int[] RedSmite = { 3715, 3718, 3717, 3716, 3714 };

        public static readonly int[] BlueSmite = { 3706, 3710, 3709, 3708, 3707 };

        public static String ScriptVersion
        {
            get
            {
                return typeof(Rengar).Assembly.GetName().Version.ToString();
            }
        }

        private static int sendTime = 0;

        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>()
        {
            { Spells.Q, new Spell(SpellSlot.Q, 250) },
            { Spells.W, new Spell(SpellSlot.W, 500) },
            { Spells.E, new Spell(SpellSlot.E, 1000) },
            { Spells.R, new Spell(SpellSlot.R, 2000) }
        };

        #region Gameloaded

        public static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Rengar")
            {
                return;
            }

            youmuu = new Items.Item(3142, 0f);
            cutlass = new Items.Item(3144, 450f);
            blade = new Items.Item(3153, 450f);

            tiamat = new Items.Item(3077, 400f);
            hydra = new Items.Item(3074, 400f);
            ignite = Player.GetSpellSlot("summonerdot");

            Notifications.AddNotification(String.Format("ElRengar by jQuery v{0}", ScriptVersion), 6000);
            spells[Spells.E].SetSkillshot(0.25f, 70f, 1500f, true, SkillshotType.SkillshotLine);

            ElRengarMenu.Initialize();
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += Drawings.Drawing_OnDraw;
            Drawing.OnEndScene += Drawings.OnDrawEndScene;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            CustomEvents.Unit.OnDash += Unit_OnDash;
        }

        #endregion
       
        
        #region Notifications 

        private static void ShowNotification(string message, Color color, int duration = -1, bool dispose = true)
        {
            Notifications.AddNotification(new Notification(message, duration, dispose).SetTextColor(color));
        }

        #endregion


        #region OnGameUpdate

        private static void OnGameUpdate(EventArgs args)
        {
            SwitchCombo();
            SmiteCombat();

            if (UsingLxOrbwalker)
            {
                switch (LXOrbwalker.CurrentMode)
                {
                    case LXOrbwalker.Mode.Combo:
                        OnCombo();
                        break;

                    case LXOrbwalker.Mode.LaneClear:
                        LaneClear();
                        JungleClear();
                        break;

                    case LXOrbwalker.Mode.Harass:
                        Harass();
                        break;
                }
            }
            else if (orbwalker != null)
            {
                switch (orbwalker.ActiveMode)
                {
                    case Orbwalking.OrbwalkingMode.Combo:
                        OnCombo();
                        break;

                    case Orbwalking.OrbwalkingMode.LaneClear:
                        LaneClear();
                        JungleClear();
                        break;

                    case Orbwalking.OrbwalkingMode.Mixed:
                        Harass();
                        break;
                }
            }

            SelfHealing();
            if (ElRengarMenu._menu.Item("ElRengar.Notifications.Active").GetValue<bool>())
            {
                Permashow();
            }
            else
            {
                return;
            }

            spells[Spells.R].Range = 0x3e8 + spells[Spells.R].Level * 0x3e8;

            /*if (ElRengarMenu._menu.Item("ElRengar.Combo.Triple.Q").GetValue<KeyBind>().Active)
            {
                TripleQ();
            }*/
        }

        #endregion

        #region TripleQ

        /*private static void TripleQ()
        {
            if (!spells[Spells.R].IsReady() || !spells[Spells.Q].IsReady() || Player.Mana != 0x5 || !Player.HasBuff("RengarR"))
            {
                if (Environment.TickCount - lastNotification > 5000)
                {
                    ShowNotification("Triple Q can't be activated", Color.White, 4000);
                    lastNotification = Environment.TickCount;
                }
            }

            Obj_AI_Hero target;
            if (UsingLxOrbwalker)
            {
                LXOrbwalker.Orbwalk(Game.CursorPos, null);
                target = TargetSelector.GetTarget(spells[Spells.R].Range, TargetSelector.DamageType.Physical);
                if (LXOrbwalker.ForcedTarget != null && LXOrbwalker.ForcedTarget is Obj_AI_Hero)
                {
                    target = (Obj_AI_Hero)LXOrbwalker.ForcedTarget;
                }

                if (target == null || !target.IsValid)
                {
                    return;
                }
            }
            else
            {
                Orbwalking.Orbwalk(null, Game.CursorPos);
                target = TargetSelector.GetSelectedTarget();
                if (target == null || !target.IsValidTarget())
                {
                    target = TargetSelector.GetTarget(spells[Spells.R].Range, TargetSelector.DamageType.Physical);
                }

                if (!target.IsValidTarget(spells[Spells.R].Range))
                {
                    return;
                }
            }

            if (UsingLxOrbwalker)
            {
                LXOrbwalker.Orbwalk(Game.CursorPos, target);
            }
            else
            {
                Orbwalking.Orbwalk(target, Game.CursorPos);
            }

            //Cast R on Player when selected target is in R range, Player needs to have 5 ferocity before ult is possible
            if (spells[Spells.R].IsReady() && spells[Spells.Q].IsReady()
                && Player.Distance(target) <= spells[Spells.R].Range && Player.Mana == 0x5)
            {
                spells[Spells.R].Cast();
            }

            //Cast youmuu when R buff
            if (youmuu.IsReady() && Player.HasBuff("RengarR"))
            {
                youmuu.Cast(Player);
            }

            //Check if Player is in R and if Player has 5 ferocity
            if (spells[Spells.Q].IsReady() && Player.Mana == 0x5 && Player.HasBuff("RengarR"))
            {
                spells[Spells.Q].Cast();
            }

            if (spells[Spells.Q].IsReady() && Player.Mana == 0x5 && !Player.HasBuff("RengarR"))
            {
                spells[Spells.Q].Cast();
            }

            //Q is casted, Player should have 3 Ferocity by now.
            if (Player.Mana <= 4)
            {
                if (spells[Spells.Q].IsReady())
                {
                    spells[Spells.Q].Cast();
                    if (target.IsValidTarget(0x190))
                    {
                        CastHydra();
                    }
                }

                if (spells[Spells.W].IsReady() && Vector3.Distance(Player.ServerPosition, target.ServerPosition) < spells[Spells.W].Range * 0x1 / 0x3)
                {
                    spells[Spells.W].Cast();
                }

                if (spells[Spells.E].IsReady() && Player.Distance(target) <= spells[Spells.E].Range)
                {
                    var prediction = spells[Spells.E].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.VeryHigh && prediction.CollisionObjects.Count == 0x0)
                    {
                        spells[Spells.E].Cast(target);
                    }
                }
            }

            if (smite != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(smite) == SpellState.Ready)
            {
                Player.Spellbook.CastSpell(smite, target);
            }

            if (Player.Distance(target) <= 0x258 && IgniteDamage(target) >= target.Health)
            {
                Player.Spellbook.CastSpell(ignite, target);
            }
        }*/

        #endregion

        #region SwitchCombo
        private static void SwitchCombo()
        {         
            var switchTime = Environment.TickCount - lastSwitch;

            if (ElRengarMenu._menu.Item("ElRengar.Combo.Switch").GetValue<KeyBind>().Active && switchTime >= 350)
            {
                switch (ElRengarMenu._menu.Item("ElRengar.Combo.Prio").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        ElRengarMenu._menu.Item("ElRengar.Combo.Prio")
                            .SetValue(new StringList(new[] { "E", "W", "Q" }, 2));
                        lastSwitch = Environment.TickCount;
                        break;
                    case 1:
                        ElRengarMenu._menu.Item("ElRengar.Combo.Prio")
                            .SetValue(new StringList(new[] { "E", "W", "Q" }, 0));
                        lastSwitch = Environment.TickCount;
                        break;
                    case 2:
                        ElRengarMenu._menu.Item("ElRengar.Combo.Prio")
                            .SetValue(new StringList(new[] { "E", "W", "Q" }, 1));
                        lastSwitch = Environment.TickCount;
                        break;
                }
            }
        }
        #endregion

        #region PermaShow

        private static void Permashow()
        {
            var prioCombo = ElRengarMenu._menu.Item("ElRengar.Combo.Prio").GetValue<StringList>();

            var text = "";

            switch (prioCombo.SelectedIndex)
            {
                case 0:
                    text = "E";
                    break;
                case 1:
                    text = "W";
                    break;
                case 2:
                    text = "Q";
                    break;
            }

            if (notification == null)
            {
                notification = new Notification(text)
                {
                    TextColor = new ColorBGRA(red: 255, green: 255, blue: 255, alpha: 255)
                };

                Notifications.AddNotification(notification);
            }

            notification.Text = "Prioritized combo spell: " + text;
        }

        #endregion

        #region OnDash

        public static int TickCount
        {
            get
            {
                return (int)(Game.Time * 0x3e8);
            }
        }

        private static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (sender.IsMe)
            {
                sendTime = TickCount;
            }
        }

        #endregion

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (RengarQ)
                {
                    if (UsingLxOrbwalker)
                    {
                        LXOrbwalker.ResetAutoAttackTimer();
                    }
                    else
                    {
                        Orbwalking.ResetAutoAttackTimer();
                    }
                }

                if (args.SData.Name == "RengarR" && Items.CanUseItem(3142) && ElRengarMenu._menu.Item("ElRengar.Combo.Youmuu").GetValue<bool>())
                    Utility.DelayAction.Add(0x5dc, () => Items.UseItem(3142));
            }
        }

        #region OnCombo

        private static void OnCombo()
        {

            Obj_AI_Hero target;
            if (UsingLxOrbwalker)
            {
                target = TargetSelector.GetTarget(spells[Spells.R].Range, TargetSelector.DamageType.Physical);

                if (LXOrbwalker.ForcedTarget != null && LXOrbwalker.ForcedTarget is Obj_AI_Hero)
                {
                    target = (Obj_AI_Hero)LXOrbwalker.ForcedTarget;
                }

                if (target != null && LXOrbwalker.GetPossibleTarget() != null)
                {
                    if (target.Distance(Player.ServerPosition) < spells[Spells.R].Range)
                    {
                        target = (Obj_AI_Hero)LXOrbwalker.GetPossibleTarget();
                    }
                }
                
                if (ElRengarMenu._menu.Item("ElRengar.Notifications.selected").GetValue<bool>()  && Environment.TickCount - lastNotification > 5000)
                {
                    if (target != null)
                    {
                        ShowNotification(target.ChampionName + ": is selected", Color.White, 4000);
                        lastNotification = Environment.TickCount;
                    }
                }

                target = (Obj_AI_Hero) LXOrbwalker.GetPossibleTarget();

                if (target == null || !target.IsValid)
                    return;
            }
            else
            {
                target = TargetSelector.GetSelectedTarget();
                if (target == null || !target.IsValidTarget())
                {
                    target = TargetSelector.GetTarget(spells[Spells.R].Range, TargetSelector.DamageType.Physical);
                }

                if (TargetSelector.GetSelectedTarget() != null)
                {
                    if (Vector3.Distance(Player.ServerPosition, target.ServerPosition) < spells[Spells.R].Range)
                    {
                        target = TargetSelector.GetSelectedTarget();
                    }
                    //Drawing.DrawText(target.HPBarPosition.X + 40, target.HPBarPosition.Y - 5, Color.LimeGreen, "Selected");
                    if (ElRengarMenu._menu.Item("ElRengar.Notifications.selected").GetValue<bool>() && Environment.TickCount - lastNotification > 5000)
                    {
                        ShowNotification(target.ChampionName + ": is selected", Color.White, 4000);
                        lastNotification = Environment.TickCount;
                    }
                }
                else
                {
                    if (ElRengarMenu._menu.Item("ElRengar.Notifications.selected").GetValue<bool>() && Environment.TickCount - lastNotification > 5000)
                    {
                        ShowNotification("No target selected, auto-prio", Color.White, 4000);
                        lastNotification = Environment.TickCount;
                    }
                }

                target = TargetSelector.GetTarget(spells[Spells.R].Range, TargetSelector.DamageType.Physical);

               
                if (!target.IsValidTarget(spells[Spells.R].Range) || target == null || !target.IsValid)
                    return;
            }

            var etarget = TargetSelector.GetTarget(spells[Spells.W].Range, TargetSelector.DamageType.Physical);

            if (etarget == null || !etarget.IsValid)
                return;

            var useQ = ElRengarMenu._menu.Item("ElRengar.Combo.Q").GetValue<bool>();
            var useW = ElRengarMenu._menu.Item("ElRengar.Combo.W").GetValue<bool>();
            var useE = ElRengarMenu._menu.Item("ElRengar.Combo.E").GetValue<bool>();
            var eComboOor = ElRengarMenu._menu.Item("ElRengar.Combo.EOOR").GetValue<bool>();
            var useCutlass = ElRengarMenu._menu.Item("ElRengar.Combo.Cutlass").GetValue<bool>();
            var useYoumuu = ElRengarMenu._menu.Item("ElRengar.Combo.Youmuu").GetValue<bool>();
            var bladeItem = ElRengarMenu._menu.Item("ElRengar.Combo.Blade").GetValue<bool>();
            var prioCombo = ElRengarMenu._menu.Item("ElRengar.Combo.Prio").GetValue<StringList>();
            var useIgnite = ElRengarMenu._menu.Item("ElRengar.Combo.Ignite").GetValue<bool>();
            var useSmite = ElRengarMenu._menu.Item("ElRengar.Combo.Smite").GetValue<bool>();

            if (useYoumuu && youmuu.IsReady() && InAutoAttackRange(target))
            {
                youmuu.Cast(Player);
            }

            if (Player.Mana == 5)
            {
                switch (prioCombo.SelectedIndex)
                {
                    case 0:
                        var prediction = spells[Spells.E].GetPrediction(target);

                        if (target.IsValidTarget(spells[Spells.E].Range) && spells[Spells.E].IsReady())
                        {
                            if (prediction.Hitchance >= HitChance.VeryHigh && prediction.CollisionObjects.Count == 0x0)
                            {
                                spells[Spells.E].Cast(target);
                            }
                        }

                         if (spells[Spells.E].IsReady() && target.IsValidTarget(spells[Spells.E].Range) && !Player.HasBuff("RengarR"))
                         {
                             if (sendTime + Game.Ping + 0x2bc - TickCount > 0x0)
                             {
                                 if (prediction.CollisionObjects.Count == 0x0)
                                 {
                                     spells[Spells.E].Cast(target);
                                 }
                             }
                             else
                             {
                                 if (target.IsValidTarget(spells[Spells.E].Range))
                                 {
                                     if (prediction.CollisionObjects.Count == 0x0)
                                     {
                                         spells[Spells.E].Cast(target);
                                     }
                                 }
                             }
                         }

                        break;

                    case 1:
                        if (spells[Spells.W].IsReady() && !Player.HasBuff("RengarR") && Vector3.Distance(Player.ServerPosition, target.ServerPosition) < spells[Spells.W].Range * 0x1 / 0x3)
                        {
                            spells[Spells.W].Cast();
                        }
                        break;

                    case 2:
                        if (spells[Spells.Q].IsReady())
                        {
                            if (sendTime + Game.Ping + 0x2bc - TickCount > 0x0)
                            {
                                spells[Spells.Q].Cast();
                            }
                            else if (Vector3.Distance(Player.ServerPosition, target.ServerPosition) <= spells[Spells.Q].Range)
                            {
                                spells[Spells.Q].Cast();
                            }
                        }
                        break;
                }

                if (eComboOor && Player.Distance(target) > Player.AttackRange + 0x64 && !Player.HasBuff("RengarR"))
                {
                    var prediction = spells[Spells.E].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.VeryHigh && prediction.CollisionObjects.Count == 0x0)
                    {
                        spells[Spells.E].Cast(target);
                    }
                }
            }

            if (Player.Mana == 0x3)
            {
                if (useQ && spells[Spells.Q].IsReady())
                {
                    spells[Spells.Q].Cast();
                }

                if (HasPassive)
                {
                    if (!Player.IsDashing() && useE && spells[Spells.E].IsReady()
                    && Vector3.Distance(Player.ServerPosition, etarget.ServerPosition) <= spells[Spells.E].Range)
                    {
                        var prediction = spells[Spells.E].GetPrediction(etarget);
                        if (prediction.Hitchance >= HitChance.High && prediction.CollisionObjects.Count == 0x0)
                        {
                            spells[Spells.E].Cast(target);
                        }
                    }
                }
                else
                {
                    if (!Player.IsDashing() && useE && spells[Spells.E].IsReady()
                         && Vector3.Distance(Player.ServerPosition, target.ServerPosition) <= spells[Spells.E].Range)
                    {
                        var prediction = spells[Spells.E].GetPrediction(target);
                        if (prediction.Hitchance >= HitChance.High && prediction.CollisionObjects.Count == 0x0)
                        {
                            spells[Spells.E].Cast(target);
                        }
                    }
                }
                
                if (InAutoAttackRange(target))
                {
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }

                if (useW && spells[Spells.W].IsReady() && Vector3.Distance(Player.ServerPosition, target.ServerPosition) < spells[Spells.W].Range * 0x1 / 0x3)
                {
                    spells[Spells.W].Cast();

                    if (target.IsValidTarget(0x190))
                    {
                        CastHydra();
                    }

                    if (InAutoAttackRange(target))
                    {
                        Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    }
                }
            }

            if (Player.Mana <= 0x4)
            {
                if (useQ && spells[Spells.Q].IsReady() && Player.HasBuff("RengarR"))
                {
                    spells[Spells.Q].Cast();
                }
                else if (useQ && spells[Spells.Q].IsReady()
                    && InAutoAttackRange(target))
                {
                    spells[Spells.Q].Cast();
                }

                if (Player.HasBuff("RengarR"))
                {
                    return;
                }

                if (InAutoAttackRange(target))
                {
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                }

                if (HasPassive)
                {
                    if (!Player.IsDashing() && useE && spells[Spells.E].IsReady() && Vector3.Distance(Player.ServerPosition, etarget.ServerPosition) <= spells[Spells.E].Range)
                    {
                        var prediction = spells[Spells.E].GetPrediction(etarget);
                        if (prediction.Hitchance >= HitChance.High && prediction.CollisionObjects.Count == 0x0)
                        {
                            spells[Spells.E].Cast(target);
                        }
                    }
                }
                else
                {
                    if (!Player.IsDashing() && useE && spells[Spells.E].IsReady() && Vector3.Distance(Player.ServerPosition, target.ServerPosition) <= spells[Spells.E].Range)
                    {
                        var prediction = spells[Spells.E].GetPrediction(target);
                        if (prediction.Hitchance >= HitChance.High && prediction.CollisionObjects.Count == 0x0)
                        {
                            spells[Spells.E].Cast(target);
                        }
                    }
                }

                if (useW && spells[Spells.W].IsReady() && Vector3.Distance(Player.ServerPosition, target.ServerPosition) < spells[Spells.W].Range * 0x1 / 0x3 && !Player.HasBuff("RengarR"))
                {
                    spells[Spells.W].Cast();

                    if (target.IsValidTarget(0x190))
                    {
                        CastHydra();
                    }

                    if (InAutoAttackRange(target))
                    {
                        Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    }
                }

                if (Player.Mana == 0x4)
                {
                    if (!spells[Spells.Q].IsReady() && !spells[Spells.W].IsReady() && spells[Spells.E].IsReady())
                    {
                        spells[Spells.E].Cast(target);
                    }
                }

                if (useE && spells[Spells.E].IsReady() && Vector3.Distance(Player.ServerPosition, target.ServerPosition) <= spells[Spells.E].Range && !Player.HasBuff("RengarR"))
                {
                    var prediction = spells[Spells.E].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.VeryHigh && prediction.CollisionObjects.Count == 0x0)
                    {
                        spells[Spells.E].Cast(target);
                    }
                }

               /* if (useE && spells[Spells.E].IsReady() && !Player.HasBuff("RengarR"))
                {
                    var prediction = spells[Spells.E].GetPrediction(etarget);
                    if (prediction.CollisionObjects.Count == 0x0)
                    {
                        spells[Spells.E].Cast(prediction.CastPosition);
                    }
                }*/
            }

            if (smite != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(smite) == SpellState.Ready && useSmite)
            {
                Player.Spellbook.CastSpell(smite, target);
            }

            if (useCutlass && Player.Distance(target) <= 0x1c2 && cutlass.IsReady())
            {
                cutlass.Cast(target);
            }

            if (bladeItem && Player.Distance(target) <= 0x1c2 && blade.IsReady())
            {
                blade.Cast(target);
            }

            if (Player.Distance(target) <= 0x258 && IgniteDamage(target) >= target.Health && useIgnite)
            {
                Player.Spellbook.CastSpell(ignite, target);
            }
        }

        #endregion

        public static float GetAutoAttackRange(Obj_AI_Base source = null, Obj_AI_Base target = null)
        {
            if (source == null)
                source = Player;

            var ret = source.AttackRange + Player.BoundingRadius;
            if (target != null)

                ret += target.BoundingRadius;

            return ret;
        }


        public static bool InAutoAttackRange(Obj_AI_Base target)
        {
            if (target == null)
                return false;

            var myRange = GetAutoAttackRange(Player, target);
            return Vector2.DistanceSquared(target.ServerPosition.To2D(), Player.ServerPosition.To2D()) <= myRange * myRange;
        }

        #region harass

        private static void Harass()
        {
            var target = TargetSelector.GetSelectedTarget();
            if (target == null || !target.IsValidTarget())
            {
                target = TargetSelector.GetTarget(spells[Spells.E].Range, TargetSelector.DamageType.Physical);
            }

            if (!target.IsValidTarget(spells[Spells.E].Range))
            {
                return;
            }

            var qHarass = ElRengarMenu._menu.Item("ElRengar.Harass.Q").GetValue<bool>();
            var wHarass = ElRengarMenu._menu.Item("ElRengar.Harass.W").GetValue<bool>();
            var eHarass = ElRengarMenu._menu.Item("ElRengar.Harass.E").GetValue<bool>();
            var prioHarass = ElRengarMenu._menu.Item("ElRengar.Harass.Prio").GetValue<StringList>();

            if (Player.Mana <= 0x4)
            {
                if (qHarass && Player.Distance(target) <= Player.AttackRange && spells[Spells.Q].IsReady())
                {
                    spells[Spells.Q].Cast();
                }

                if (wHarass
                    && Vector3.Distance(Player.ServerPosition, target.ServerPosition)
                    <= spells[Spells.W].Range * 0x1 / 0x3 && spells[Spells.W].IsReady())
                {
                    spells[Spells.W].Cast();
                }

                if (eHarass && spells[Spells.E].IsReady() && target.IsValidTarget(spells[Spells.E].Range))
                {
                    var prediction = spells[Spells.E].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.VeryHigh && prediction.CollisionObjects.Count == 0x0)
                    {
                        spells[Spells.E].Cast(target);
                    }
                }
            }

            if (Player.Mana >= 0x5)
            {
                if (qHarass && prioHarass.SelectedIndex == 0x2 && Player.Distance(target) <= Player.AttackRange
                    && spells[Spells.Q].IsReady())
                {
                    spells[Spells.Q].Cast();
                }

                if (wHarass && prioHarass.SelectedIndex == 0x1 && target.IsValidTarget(spells[Spells.W].Range)
                    && spells[Spells.W].IsReady())
                {
                    spells[Spells.W].Cast();
                }

                if (eHarass && prioHarass.SelectedIndex == 0x0 && Player.Distance(target) <= spells[Spells.E].Range
                    && spells[Spells.E].IsReady())
                {
                    var prediction = spells[Spells.E].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.VeryHigh && prediction.CollisionObjects.Count == 0x0)
                    {
                        spells[Spells.E].Cast(target);
                    }
                }
            }
        }

        #endregion

        private static void CastHydra()
        {
            if (Player.IsWindingUp) return;

            if (!ItemData.Tiamat_Melee_Only.GetItem().IsReady() &&
                !ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady())
            {
                return;
            }

            ItemData.Tiamat_Melee_Only.GetItem().Cast();
            ItemData.Ravenous_Hydra_Melee_Only.GetItem().Cast();
        }

        #region jungle

        private static void JungleClear()
        {
            var qWaveClear = ElRengarMenu._menu.Item("ElRengar.Clear.Q").GetValue<bool>();
            var wWaveClear = ElRengarMenu._menu.Item("ElRengar.Clear.W").GetValue<bool>();
            var eWaveClear = ElRengarMenu._menu.Item("ElRengar.Clear.E").GetValue<bool>();
            var hydraClear = ElRengarMenu._menu.Item("ElRengar.LaneClear.Hydra").GetValue<bool>();
            var saveClear = ElRengarMenu._menu.Item("ElRengar.Clear.Save").GetValue<bool>();
            var prioClear = ElRengarMenu._menu.Item("ElRengar.Clear.Prio").GetValue<StringList>();

            var minions = MinionManager.GetMinions(
                Player.Position,
                700,
                MinionTypes.All,
                MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            if (minions.Count <= 0)
            {
                return;
            }

            if (Player.Mana <= 0x4)
            {
                if (qWaveClear && spells[Spells.Q].IsReady()
                    )
                {
                    spells[Spells.Q].Cast();
                }
                if (wWaveClear && spells[Spells.W].IsReady() && minions.Count >= 0x1 && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.W].Range)
                {
                    spells[Spells.W].Cast();
                    if (hydraClear && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < 0x190)
                    {
                        CastHydra();
                    }
                }
                if (eWaveClear && spells[Spells.E].IsReady() && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast(minions[0]);
                }
            }

            if (Player.Mana == 0x5)
            {
                if (saveClear)
                {
                    return;
                }

                if (prioClear.SelectedIndex == 0x2 && qWaveClear && spells[Spells.Q].IsReady()
                    && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.Q].Range)
                {
                    spells[Spells.Q].Cast();
                }
                if (prioClear.SelectedIndex == 0x1 && wWaveClear && spells[Spells.W].IsReady()
                    && minions.Count >= 0x2 && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.W].Range * 0x1 / 0x3)
                {
                    spells[Spells.W].Cast();
                    if (hydraClear && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < 0x190)
                    {
                        CastHydra();
                    }
                }
                if (prioClear.SelectedIndex == 0x0 && eWaveClear && spells[Spells.E].IsReady()
                    && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast(minions[0]);
                }
            }
        }

        #endregion

        #region laneclear

        private static void LaneClear()
        {
            var useQ = ElRengarMenu._menu.Item("ElRengar.LaneClear.Q").GetValue<bool>();
            var useW = ElRengarMenu._menu.Item("ElRengar.LaneClear.W").GetValue<bool>();
            var useE = ElRengarMenu._menu.Item("ElRengar.LaneClear.E").GetValue<bool>();
            var hydraClear = ElRengarMenu._menu.Item("ElRengar.LaneClear.Hydra").GetValue<bool>();
            var saveClear = ElRengarMenu._menu.Item("ElRengar.LaneClear.Save").GetValue<bool>();
            var prioClear = ElRengarMenu._menu.Item("ElRengar.LaneClear.Prio").GetValue<StringList>();

            var minions = MinionManager.GetMinions(Player.ServerPosition, spells[Spells.W].Range);
            if (minions.Count <= 0)
            {
                return;
            }

            if (Player.Mana <= 0x4)
            {
                if (useW && spells[Spells.W].IsReady() && minions.Count >= 0x2
                    && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition)
                    < spells[Spells.W].Range * 0x1 / 0x3)
                {
                    spells[Spells.W].Cast();
                    if (hydraClear && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < 300f)
                    {
                        CastHydra();
                    }
                }

                if (useQ && spells[Spells.Q].IsReady() && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.Q].Range)
                {
                    spells[Spells.Q].Cast();
                }

                if (useE && spells[Spells.E].IsReady() && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast(minions[0]);
                }
            }

            if (Player.Mana == 0x5)
            {
                if (saveClear)
                {
                    return;
                }

                if (prioClear.SelectedIndex == 0x1 && useW && minions.Count > 0x2 && spells[Spells.W].IsReady()
                    && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition)
                    < spells[Spells.W].Range * 0x1 / 0x3)
                {
                    spells[Spells.W].Cast();
                    if (hydraClear && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < 300f)
                    {
                        CastHydra();
                    }
                }

                if (prioClear.SelectedIndex == 0x2 && useQ && spells[Spells.Q].IsReady() && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.Q].Range)
                {
                    spells[Spells.Q].Cast();
                }

                if (prioClear.SelectedIndex == 0x0 && useE && spells[Spells.E].IsReady() && Vector3.Distance(Player.ServerPosition, minions[0].ServerPosition) < spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast(minions[0]);
                }
            }
        }

        #endregion

        #region selfheal

        private static void SelfHealing()
        {
            if (Player.IsRecalling() || Player.InFountain() || Player.Mana <= 0x4)
            {
                return;
            }

            var useHeal = ElRengarMenu._menu.Item("ElRengar.Heal.AutoHeal").GetValue<bool>();
            var healPercentage = ElRengarMenu._menu.Item("ElRengar.Heal.HP").GetValue<Slider>().Value;

            if (useHeal && (Player.Health / Player.MaxHealth) * 0x64 <= healPercentage && spells[Spells.W].IsReady())
            {
                spells[Spells.W].Cast(Player);
            }
        }

        #endregion

        #region Ignitecombo

        private static bool RengarQ
        {
            get
            {
                return Player.Buffs.Any(x => x.Name.Contains("rengarq"));
            }
        }

        public static bool HasPassive
        {
            get
            {
                return Player.HasBuff("rengarpassivebuff");
            }
        }

        public static bool Rengartrophyicon6 //riot is retarded lmao kappahd 
        {
            get
            {
                return Player.HasBuff("rengarbushspeedbuff");
            }
        }

        public static int LeapRange
        {
            get
            {
                if (HasPassive && Rengartrophyicon6)
                {
                    return 725;
                }

                return 600;
            }
        }

        private static void SmiteCombat()
        {
            if (BlueSmite.Any(id => Items.HasItem(id)))
            {
                smite = Player.GetSpellSlot("s5_summonersmiteplayerganker");
                return;
            }

            if (RedSmite.Any(id => Items.HasItem(id)))
            {
                smite = Player.GetSpellSlot("s5_summonersmiteduel");
                return;
            }

            smite = Player.GetSpellSlot("summonersmite");
        }

        private static float IgniteDamage(Obj_AI_Hero target)
        {
            if (ignite == SpellSlot.Unknown || Player.Spellbook.CanUseSpell(ignite) != SpellState.Ready)
            {
                return 0f;
            }
            return (float)Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        #endregion
    }
}
