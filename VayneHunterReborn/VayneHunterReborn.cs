namespace VayneHunter_Reborn
{
    #region References
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LeagueSharp;
    using LeagueSharp.Common;
    using SharpDX;
    using MapPosition;
    using Utility;

    using Color = System.Drawing.Color;
    using Geometry = LeagueSharp.Common.Geometry;
    using ActiveGapcloser = VayneHunter_Reborn.Utility.ActiveGapcloser;
    using System.Reflection;

    #endregion

    class VayneHunterReborn
    {
        #region Fields & Operators
        public static Menu Menu;
        public static Obj_AI_Hero Player = ObjectManager.Player;
        public static Orbwalking.Orbwalker Orbwalker;
        private static Vector3 _predictedPosition;
        private static Vector3 _predictedEndPosition;
        private static float _lastCheckTick;
        private static float _lastCheckTick2;
        private static float _lastCondemnCheck;
        private static bool goingToTumble;
        private static Vector3 TumblePosition = Vector3.Zero;

        private static readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q) },
            { SpellSlot.W, new Spell(SpellSlot.W) },
            { SpellSlot.E, new Spell(SpellSlot.E, 590f) },
            { SpellSlot.R, new Spell(SpellSlot.R) }
        };

        private static Spell trinketSpell;
        private static readonly Notification CondemnNotification = new Notification("Condemned",5500);
        #endregion

        /// <summary>
        /// Method Called when the Assembly is loaded.
        /// </summary>
        public static void OnLoad()
        {
            if (ObjectManager.Player.ChampionName != "Vayne")
            {
                return;
            }

            SetUpMenu();
            SetUpEvents();
            SetUpSkills();
        }

        #region Menu, Skills, Events
        /// <summary>
        /// Sets up the Menu
        /// </summary>
        private static void SetUpMenu()
        {
            Menu = new Menu("VayneHunter Reborn", "VHR", true);

            var owMenu = new Menu("[VHR] Orbwalker", "dz191.vhr.orbwalker");
            Orbwalker = new Orbwalking.Orbwalker(owMenu);
            Menu.AddSubMenu(owMenu);

           // var tgMenu = new Menu("[VHR] Target Selector", "dz191.vhr.targetselector");
            //TargetSelector.AddToMenu(tgMenu);
            //Menu.AddSubMenu(tgMenu);
            CustomTargetSelector.OnLoad(Menu);

            var comboMenu = new Menu("[VHR] Combo", "dz191.vhr.combo");
            comboMenu.AddModeMenu(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.E, SpellSlot.R }, new[] { true, true, false });
            comboMenu.AddManaManager(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.E, SpellSlot.R }, new[] { 0, 0, 0 });
            comboMenu.AddItem(new MenuItem("dz191.vhr.combo.r.minenemies", "Min R Enemies").SetValue(new Slider(2, 1, 5)));
            Menu.AddSubMenu(comboMenu);

            var harassMenu = new Menu("[VHR] Harass", "dz191.vhr.harass");
            harassMenu.AddModeMenu(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.E }, new[] { true, true });
            harassMenu.AddManaManager(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.E }, new[] { 25, 20 });
            Menu.AddSubMenu(harassMenu);

            var farmMenu = new Menu("[VHR] Farm", "dz191.vhr.farm");
            farmMenu.AddModeMenu(Mode.Farm, new[] { SpellSlot.Q }, new[] { true, true });
            farmMenu.AddManaManager(Mode.Farm, new[] { SpellSlot.Q }, new[] { 40 });
            Menu.AddSubMenu(farmMenu);

            var miscMenu = new Menu("[VHR] Misc", "dz191.vhr.misc");
            var miscQMenu = new Menu("Misc - Tumble", "dz191.vhr.misc.tumble");
            {
                miscQMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.qlogic", "Q Logic").SetValue(new StringList(new[] { "Normal", "Away from enemies" })));
                miscQMenu.AddItem(new MenuItem("dz191.vhr.misc.tumble.smartq", "Try to QE First").SetValue(false));
                miscQMenu.AddItem(new MenuItem("dz191.vhr.misc.tumble.noaastealth", "Don't AA while stealthed").SetValue(false));
                miscQMenu.AddItem(new MenuItem("dz191.vhr.misc.tumble.noqenemies", "Don't Q into enemies").SetValue(false));
                miscQMenu.AddItem(new MenuItem("dz191.vhr.misc.tumble.dynamicqsafety", "Dynamic Q Safety Distance").SetValue(false));
                miscQMenu.AddItem(new MenuItem("dz191.vhr.misc.tumble.qspam", "Ignore Q checks").SetValue(false));
                miscQMenu.AddItem(new MenuItem("dz191.vhr.misc.tumble.qinrange", "Q In Range if Enemy Health < Q+AA Dmg").SetValue(true));
                miscQMenu.AddItem(new MenuItem("dz191.vhr.misc.tumble.walltumble", "Tumble Over Wall (WallTumble)").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Press)));
                miscQMenu.AddItem(new MenuItem("dz191.vhr.misc.tumble.mirin", "Enable this if you're Mirin").SetValue(false));
            }

            var miscEMenu = new Menu("Misc - Condemn", "dz191.vhr.misc.condemn");
            {
                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.condemnmethod", "Condemn Method").SetValue(new StringList(new[] { "VH Revolution", "VH Reborn", "Marksman/Gosu", "VH Rework" })));
                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.pushdistance", "E Push Distance").SetValue(new Slider(420, 350, 500)));

                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.rev.predictionNumber", "Number of Predictions (Revolution Only)").SetValue(new Slider(10, 2, 15)));
                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.rev.accuracy", "Accuracy (Revolution Only)").SetValue(new Slider(60, 1)));
                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.rev.nextprediction", "Last Prediction (Rev. Only - Don't touch)").SetValue(new Slider(500, 1, 1000)));

                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.enextauto", "E Next Auto").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle)));
                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.onlystuncurrent", "Only stun current target").SetValue(false));
                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.autoe", "Auto E").SetValue(false));
                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.eks", "Smart E Ks").SetValue(false));
                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.noeaa", "Don't E if Target can be killed in X AA").SetValue(new Slider(1, 0, 4)));

                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.trinketbush", "Trinket Bush on Condemn").SetValue(true));
                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.ethird", "E 3rd proc in Harass").SetValue(false));
                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.lowlifepeel", "Peel with E when low").SetValue(false));

                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.condemnturret", "Try to Condemn to turret").SetValue(false));
                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.condemnflag", "Condemn to J4 flag").SetValue(true));
                miscEMenu.AddItem(new MenuItem("dz191.vhr.misc.condemn.noeturret", "No E Under enemy turret").SetValue(true));
            }

            var miscGeneralSubMenu = new Menu("Misc - General", "dz191.vhr.misc.general");
            {
                miscGeneralSubMenu.AddItem(new MenuItem("dz191.vhr.misc.general.antigp", "Anti Gapcloser")).SetValue(true);
                miscGeneralSubMenu.AddItem(new MenuItem("dz191.vhr.misc.general.interrupt", "Interrupter").SetValue(true));
                miscGeneralSubMenu.AddItem(new MenuItem("dz191.vhr.misc.general.antigpdelay", "Anti Gapcloser Delay (ms)")).SetValue(new Slider(0,0,1000));
                miscGeneralSubMenu.AddItem(new MenuItem("dz191.vhr.misc.general.specialfocus", "Focus targets with 2 W marks").SetValue(false));
                miscGeneralSubMenu.AddItem(new MenuItem("dz191.vhr.misc.general.reveal", "Stealth Reveal (Pink Ward)").SetValue(false));
                miscGeneralSubMenu.AddItem(new MenuItem("dz191.vhr.misc.general.disablemovement", "Disable Orbwalker Movement").SetValue(false));
                miscGeneralSubMenu.AddItem(new MenuItem("dz191.vhr.misc.general.permashow", "PermaShow").SetShared().SetValue(true)).ValueChanged += (s, args) =>
                {
                    if (args.GetNewValue<bool>())
                    {
                        Menu.Item("dz191.vhr.misc.condemn.enextauto").Permashow(true, "E Next Auto");
                    }
                    else
                    {
                        Menu.Item("dz191.vhr.misc.condemn.enextauto").Permashow(false);
                    }
                };
            }
            miscMenu.AddSubMenu(miscQMenu);
            miscMenu.AddSubMenu(miscEMenu);
            miscMenu.AddSubMenu(miscGeneralSubMenu);
            Menu.AddSubMenu(miscMenu);


            AntiGP.BuildMenu(Menu);


            var drawMenu = new Menu("[VHR] Drawing", "dz191.vhr.drawing");
            drawMenu.AddDrawMenu(_spells, Color.Red);
            drawMenu.AddItem(new MenuItem("dz191.vhr.drawing.drawstun", "Draw Stunnable").SetValue(true));
            drawMenu.AddItem(new MenuItem("dz191.vhr.drawing.drawspots", "Draw Spots").SetValue(true));
            Menu.AddSubMenu(drawMenu);

            Menu.AddItem(new MenuItem("dz191.vhr.info", "VHR by Asuna v." + Assembly.GetExecutingAssembly().GetName().Version));

            Menu.Item("dz191.vhr.misc.condemn.enextauto").Permashow(Menu.Item("dz191.vhr.misc.condemn.enextauto").GetValue<KeyBind>().Active, "E Next Auto");

            Menu.AddToMainMenu();
        }

        /// <summary>
        /// Sets up the Spell's Data
        /// </summary>
        private static void SetUpSkills()
        {
            _spells[SpellSlot.E].SetTargetted(0.25f,2000f);
            trinketSpell = new Spell(SpellSlot.Trinket);
        }

        /// <summary>
        /// Registers the Events.
        /// </summary>
        private static void SetUpEvents()
        {
            Cleanser.OnLoad();
            PotionManager.OnLoad(Menu);
            ItemManager.OnLoad(Menu);
            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += OrbwalkingAfterAttack;
            AntiGP.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Stealth.OnStealth += Stealth_OnStealth;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnPlayAnimation += Obj_AI_Hero_OnPlayAnimation;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            GameObject.OnCreate += GameObject_OnCreate;
            Game.OnWndProc += Game_OnWndProc;
            if (CustomTargetSelector.IsActive())
            {
                CustomTargetSelector.RegisterEvents();
            }
        }
        #endregion

        #region Delegate Methods.
        static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.WM_LBUTTONDOWN)
            {
                return;
            }
            var drakeWallQPos = new Vector2(11514, 4462);
            var midWallQPos = new Vector2(6962, 8952);
            var selected = Vector2.Zero;

            if (Game.CursorPos.Distance(new Vector2(12050, 4827).To3D()) <= 75)
            {
                selected = drakeWallQPos;
            }

            if (Game.CursorPos.Distance(midWallQPos.To3D()) <= 75)
            {
                selected = midWallQPos;
            }

            if (selected == drakeWallQPos)
            {        
                    goingToTumble = true;
                    TumblePosition = drakeWallQPos.To3D();
            }
            
        }

        #region Special AntiGP Methods
        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender is Obj_AI_Hero)
            {
                var s2 = (Obj_AI_Hero) sender;
                if (args.Target.IsMe && s2.ChampionName == "Pantheon" && s2.GetSpellSlot(args.SData.Name) == SpellSlot.W)
                {
                    if (MenuHelper.isMenuEnabled("dz191.vhr.misc.general.antigp"))
                    {
                           if (s2.IsValidTarget(_spells[SpellSlot.E].Range))
                           {
                               _spells[SpellSlot.E].Cast(s2);
                           }
                    }
                }
            }
            
        }


        /// <summary>
        /// Delegate called when a Game Object is created.
        /// </summary>
        /// <param name="sender">The Created Gameobject.</param>
        /// <param name="args">The event args'</param>
        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.general.antigp") && _spells[SpellSlot.E].IsReady())
            {
                if (sender.IsEnemy && sender.Name == "Rengar_LeapSound.troy")
                {
                    var rengarEntity = HeroManager.Enemies.Find(h => h.ChampionName.Equals("Rengar") && h.IsValidTarget(_spells[SpellSlot.E].Range));
                    if (rengarEntity != null)
                    {
                        _spells[SpellSlot.E].Cast(rengarEntity);
                    }
                }
            }
        }

        #endregion 

        #region
        static void Obj_AI_Hero_OnPlayAnimation(Obj_AI_Base sender, GameObjectPlayAnimationEventArgs args)
        {
            return;
            if (!sender.IsMe)
            {
                return;
            }
            if (args.Animation.Contains("Attack"))
            {
                LeagueSharp.Common.Utility.DelayAction.Add((25), () =>
                {
                    if (ObjectManager.Player.IsAttackingPlayer)
                    {
                        LeagueSharp.Common.Utility.DelayAction.Add((int)(ObjectManager.Player.AttackCastDelay * 1000 + 15), () => OrbwalkingAfterAttack(ObjectManager.Player, Orbwalker.GetTarget()));
                    }
                });
            }
        }
        #endregion

        /// <summary>
        /// Called when an unit goes on stealth.
        /// </summary>
        /// <param name="obj">The Event's arguments</param>
        private static void Stealth_OnStealth(Stealth.OnStealthEventArgs obj)
        {
            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.general.reveal"))
            {
                if (obj.Sender.Distance(ObjectManager.Player) <= 600f && obj.IsStealthed && !obj.Sender.IsMe)
                {
                    if (Items.HasItem(2043) && Items.CanUseItem(2043))
                    {
                        Items.UseItem(2043, obj.Sender.ServerPosition.Extend(ObjectManager.Player.ServerPosition, 400f));
                    }
                }
            }
        }

        /// <summary>
        /// Method called each tick.
        /// </summary>
        /// <param name="args">The Event's args</param>
        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            #region TS & WallTumble
            if (goingToTumble && TumblePosition != Vector3.Zero)
            {
                Vector2 drakeWallQPos = new Vector2(11514, 4462);
                Vector2 midWallQPos = new Vector2(6962, 8952);

                if (TumblePosition == drakeWallQPos.To3D())
                {
                    if (Player.Position.X < 12000 || Player.Position.X > 12070 || Player.Position.Y < 4800 ||
                    Player.Position.Y > 4872)
                    {
                        Helpers.MoveToLimited(new Vector2(12050, 4827).To3D());
                    }
                    else
                    {
                        Helpers.MoveToLimited(new Vector2(12050, 4827).To3D());
                        LeagueSharp.Common.Utility.DelayAction.Add((int)(106 + Game.Ping / 2f), () =>
                        {
                            _spells[SpellSlot.Q].Cast(drakeWallQPos);

                            goingToTumble = false;
                            TumblePosition = Vector3.Zero;
                        });
                    }
                }
                
            }

            if (CustomTargetSelector.IsActive())
            {
                if (CustomTargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(null)) != null)
                {
                    Orbwalker.ForceTarget(CustomTargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(null)));
                }
                else
                {
                    Orbwalker.ForceTarget(TargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(null), TargetSelector.DamageType.Physical));

                }
            }
            #endregion

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harrass();
                    break;
            }

            OnUpdateFunctions();
        }

        /// <summary>
        /// Called when an unit finishes the attack.
        /// </summary>
        /// <param name="unit">The Unit</param>
        /// <param name="target">The target of the attack</param>
        private static void OrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!(target is Obj_AI_Base) || !unit.IsMe)
            {
                return;
            }

            var tg = (Obj_AI_Base)target;

            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo))
                    {
                        CastQ(tg);
                    }
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Harrass) && (tg is Obj_AI_Hero))
                    {
                        CastQ(tg);
                    }
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    Farm();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Farm();
                    break;
            }

            if (MenuHelper.getKeybindValue("dz191.vhr.misc.condemn.enextauto") &&
                _spells[SpellSlot.E].CanCast(tg) && (tg is Obj_AI_Hero))
            {
                _spells[SpellSlot.E].Cast(tg);
                Menu.Item("dz191.vhr.misc.condemn.enextauto").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle));
            }
        }

        /// <summary>
        /// Delegate used for drawings.
        /// </summary>
        /// <param name="args">The event's args.</param>
        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawE = Menu.Item("VayneDrawE").GetValue<Circle>();
            var drakeWallQPos = new Vector2(11514, 4462);

            if (drawE.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _spells[SpellSlot.E].Range, drawE.Color);
            }

            if (MenuHelper.isMenuEnabled("dz191.vhr.drawing.drawstun"))
            {
                Obj_AI_Hero myTarget;
            }

            if (MenuHelper.isMenuEnabled("dz191.vhr.drawing.drawspots"))
            {
                if (ObjectManager.Player.Distance(drakeWallQPos) <= 1500f && Helpers.IsSummonersRift())
                {
                    Render.Circle.DrawCircle(new Vector2(12050, 4827).To3D(), 65f, Color.AliceBlue);
                }
            }
        }

        #region Gapcloser & Interrupter
        /// <summary>
        /// Called when an unit gapcloses onto the Player.
        /// </summary>
        /// <param name="gapcloser">The Event's args</param>
        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.general.antigp"))
            {
                LeagueSharp.Common.Utility.DelayAction.Add(MenuHelper.getSliderValue("dz191.vhr.misc.general.antigpdelay"),
                    () =>
                    {
                        if (gapcloser.Sender.IsValidTarget(_spells[SpellSlot.E].Range) 
                            && gapcloser.End.Distance(ObjectManager.Player.ServerPosition) <= 400f 
                            && (gapcloser.Sender is Obj_AI_Hero)
                            && MenuHelper.isMenuEnabled(
                            string.Format("dz191.vhr.agplist.{0}.{1}",gapcloser.Sender.ChampionName.ToLowerInvariant(), gapcloser.SpellName)
                            ))
                        {
                            _spells[SpellSlot.E].Cast(gapcloser.Sender);
                        }
                    });
            }
        }

        /// <summary>
        /// Called when an unit casts an interruptable spell.
        /// </summary>
        /// <param name="sender">The sender of the spell.</param>
        /// <param name="args">The event's args.</param>
        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.general.interrupt"))
            {
                if (args.DangerLevel == Interrupter2.DangerLevel.High && sender.IsValidTarget(_spells[SpellSlot.E].Range) && (sender is Obj_AI_Hero))
                {
                    _spells[SpellSlot.E].Cast(sender);
                }
            }
        }
        #endregion

        #endregion

        #region Modes
        /// <summary>
        /// The Combo Method.
        /// </summary>
        private static void Combo()
        {
            if (Environment.TickCount - _lastCheckTick2 < 80)
            {
                return;
            }

            _lastCheckTick2 = Environment.TickCount;

            if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo))
            {
                Obj_AI_Hero target;

                if (CondemnCheck(ObjectManager.Player.ServerPosition, out target))
                {
                    if (target.IsValidTarget(_spells[SpellSlot.E].Range) && (target is Obj_AI_Hero))
                    {
                        _spells[SpellSlot.E].Cast(target);
                    }
                }
            }

            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo))
            {
                CheckAndCastKSQ();
            }
        }

        /// <summary>
        /// The Harass Method
        /// </summary>
        private static void Harrass()
        {
            if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Harrass))
            {
                var possibleTarget = HeroManager.Enemies.Find(enemy => enemy.IsValidTarget(_spells[SpellSlot.E].Range) && enemy.Has2WStacks());
                if (possibleTarget != null && MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.ethird") && (possibleTarget is Obj_AI_Hero) && !ObjectManager.Player.HasBuff("vaynetumblebonus"))
                {
                    _spells[SpellSlot.E].Cast(possibleTarget);
                }

                Obj_AI_Hero target;
                if (CondemnCheck(ObjectManager.Player.ServerPosition, out target))
                {
                    if (target.IsValidTarget(_spells[SpellSlot.E].Range) && (target is Obj_AI_Hero))
                    {
                        _spells[SpellSlot.E].Cast(target);
                    }
                }
            }
        }

        /// <summary>
        /// The Farm Method.
        /// </summary>
        private static void Farm()
        {
            if (!_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Farm))
            {
                return;
            }

            var minionsInRange = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Player.AttackRange).FindAll(m => m.Health <= Player.GetAutoAttackDamage(m) + _spells[SpellSlot.Q].GetDamage(m)).ToList();
            if (!minionsInRange.Any())
            {
                return;
            }

            if (minionsInRange.Count > 1)
            {
                var firstMinion = minionsInRange.OrderBy(m => m.HealthPercent).First();
                CastTumble(firstMinion);
                Orbwalker.ForceTarget(firstMinion);
                
            }
        }

        /// <summary>
        /// Method called to execute the OnUpdate Functions (KS, AutoE, Etc).
        /// </summary>
        private static void OnUpdateFunctions()
        {
            if (Environment.TickCount - _lastCheckTick < 150)
            {
                return;
            }

            _lastCheckTick = Environment.TickCount;

            #region Auto E
            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.autoe") && _spells[SpellSlot.E].IsReady())
            {
                Obj_AI_Hero target;
                if (CondemnCheck(ObjectManager.Player.ServerPosition, out target))
                {
                    if (target.IsValidTarget(_spells[SpellSlot.E].Range) && (target is Obj_AI_Hero))
                    {
                        _spells[SpellSlot.E].Cast(target);
                    }
                }
            }
            #endregion

            #region Focus 2 W stacks
            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.general.specialfocus"))
            {
                var target = HeroManager.Enemies.Find(en => en.IsValidTarget(ObjectManager.Player.AttackRange) && en.Has2WStacks());
                if (target != null)
                {
                    Orbwalker.ForceTarget(target);
                    CustomTargetSelector.scriptSelectedHero = target;
                    Hud.SelectedUnit = target;
                }
            }
            #endregion

            #region Disable AA Stealth
            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.tumble.noaastealth") && ObjectManager.Player.CountEnemiesInRange(1000f) > 1)
            {
                Orbwalker.SetAttack(!Helpers.IsPlayerFaded());
            }
            #endregion

            #region Condemn KS
            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.eks") && _spells[SpellSlot.E].IsReady() && !ObjectManager.Player.HasBuff("vaynetumblebonus"))
            {
                var target = HeroManager.Enemies.Find(en => en.IsValidTarget(_spells[SpellSlot.E].Range) && en.Has2WStacks());
                if (target != null &&  target.IsValidTarget() && target.Health + 60 <= (_spells[SpellSlot.E].GetDamage(target) + _spells[SpellSlot.W].GetDamage(target)) && (target is Obj_AI_Hero))
                {
                    _spells[SpellSlot.E].Cast(target);
                }
            }
            #endregion

            #region WallTumble
            if (Menu.Item("dz191.vhr.misc.tumble.walltumble").GetValue<KeyBind>().Active && _spells[SpellSlot.Q].IsReady())
            {
                WallTumble();
            }
            #endregion

            #region Low Life Peel
            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.lowlifepeel") && ObjectManager.Player.HealthPercentage() <= 20 && _spells[SpellSlot.E].IsReady())
            {
                var meleeEnemies = ObjectManager.Player.GetEnemiesInRange(375f).FindAll(m => m.IsMelee());
                if (meleeEnemies.Any())
                {
                    var mostDangerous = meleeEnemies.OrderByDescending(m => m.GetAutoAttackDamage(ObjectManager.Player)).First();
                    if (mostDangerous is Obj_AI_Hero)
                    {
                        _spells[SpellSlot.E].Cast(mostDangerous);
                    }
                }
            }
            #endregion

            #region Disable Movement
            Orbwalker.SetMovement(!MenuHelper.isMenuEnabled("dz191.vhr.misc.general.disablemovement"));
            #endregion

        }

        #endregion

        #region Tumble Region

        private static void CheckAndCastKSQ()
        {
            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.tumble.qinrange") && _spells[SpellSlot.Q].IsReady())
            {
                var currentTarget = TargetSelector.GetTarget(Orbwalking.GetRealAutoAttackRange(null) + 240f, TargetSelector.DamageType.Physical);
                if (!currentTarget.IsValidTarget())
                {
                    return;
                }

                if (currentTarget.ServerPosition.Distance(ObjectManager.Player.ServerPosition) <=
                    Orbwalking.GetRealAutoAttackRange(null))
                {
                    return;
                }

                if (currentTarget.Health + 15 <
                    ObjectManager.Player.GetAutoAttackDamage(currentTarget) +
                    _spells[SpellSlot.Q].GetDamage(currentTarget))
                {
                    var extendedPosition = ObjectManager.Player.ServerPosition.Extend(
                        currentTarget.ServerPosition, 300f);
                    if (Helpers.OkToQ(extendedPosition))
                    {
                        _spells[SpellSlot.Q].Cast(extendedPosition);
                        Orbwalking.ResetAutoAttackTimer();
                        Orbwalker.ForceTarget(currentTarget);
                        CustomTargetSelector.scriptSelectedHero = currentTarget;
                    }
                }
            }
        }

        private static void CastQ(Obj_AI_Base target)
        {
            var myPosition = Game.CursorPos;
            Obj_AI_Hero myTarget = null;
            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.tumble.smartq") && _spells[SpellSlot.E].IsReady()) 
            {
                const int currentStep = 30;
                var direction = ObjectManager.Player.Direction.To2D().Perpendicular();
                for (var i = 0f; i < 360f; i += currentStep)
                {
                    var angleRad = Geometry.DegreeToRadian(i);
                    var rotatedPosition = ObjectManager.Player.Position.To2D() + (300f * direction.Rotated(angleRad));
                    if (CondemnCheck(rotatedPosition.To3D(), out myTarget) && Helpers.OkToQ(rotatedPosition.To3D()))
                    {
                        myPosition = rotatedPosition.To3D();
                        break;
                    }
                }
            }

            if (_spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo) && (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo) && ObjectManager.Player.CountEnemiesInRange(Orbwalking.GetRealAutoAttackRange(null) + 200f) >= MenuHelper.getSliderValue("dz191.vhr.combo.r.minenemies"))
            {
                _spells[SpellSlot.R].Cast();
            }

            CastTumble(myPosition,target);

            if (myPosition != Game.CursorPos && myTarget != null && myTarget.IsValidTarget(300f + _spells[SpellSlot.E].Range) && _spells[SpellSlot.E].IsReady())
            {
                    LeagueSharp.Common.Utility.DelayAction.Add((int)(Game.Ping / 2f + _spells[SpellSlot.Q].Delay * 1000 + 300f / 1500f + 50f),
                        () =>
                        {
                            if (!_spells[SpellSlot.Q].IsReady())
                            {
                                _spells[SpellSlot.E].Cast(myTarget);
                            }
                        });
            }
        }

        private static void CastTumble(Obj_AI_Base target)
        {
            if (!_spells[SpellSlot.Q].IsReady())
            {
                return;
            }

            var posAfterTumble =
                ObjectManager.Player.ServerPosition.To2D().Extend(Game.CursorPos.To2D(), 300f).To3D();
            var distanceAfterTumble = Vector3.DistanceSquared(posAfterTumble, target.ServerPosition);
            if ((distanceAfterTumble <= 550 * 550 && distanceAfterTumble >= 100 * 100) || (MenuHelper.isMenuEnabled("dz191.vhr.misc.tumble.qspam")))
            {
                if (!Helpers.OkToQ2(posAfterTumble) && MenuHelper.isMenuEnabled("dz191.vhr.misc.tumble.noqenemies"))
                {
                    if(!(MenuHelper.isMenuEnabled("dz191.vhr.misc.tumble.qspam")))
                    {
                          return;
                    }
                }
                _spells[SpellSlot.Q].Cast(Game.CursorPos);
            }
        }

        private static void CastTumble(Vector3 pos, Obj_AI_Base target)
        {
            if (!_spells[SpellSlot.Q].IsReady())
            {
                return;
            }
            var posAfterTumble =
                ObjectManager.Player.ServerPosition.To2D().Extend(pos.To2D(), 300f).To3D();
            var distanceAfterTumble = Vector3.DistanceSquared(posAfterTumble, target.ServerPosition);
            if ((distanceAfterTumble < 550 * 550 && distanceAfterTumble > 100 * 100) ||
                (MenuHelper.isMenuEnabled("dz191.vhr.misc.tumble.qspam")))
            {

                switch (Menu.Item("dz191.vhr.misc.condemn.qlogic").GetValue<StringList>().SelectedIndex)
                {
                    case 0:
                        if (!Helpers.OkToQ2(posAfterTumble) &&
                            MenuHelper.isMenuEnabled("dz191.vhr.misc.tumble.noqenemies"))
                        {
                            if (!(MenuHelper.isMenuEnabled("dz191.vhr.misc.tumble.qspam")))
                            {
                                return;
                            }
                        }
                        _spells[SpellSlot.Q].Cast(pos);
                        break;

                    case 1:
                        if (PositionalHelper.MeleeEnemiesTowardsMe.Any() &&
                            !PositionalHelper.MeleeEnemiesTowardsMe.All(m => m.HealthPercent <= 15))
                        {
                            var Closest =
                                PositionalHelper.MeleeEnemiesTowardsMe.OrderBy(m => m.Distance(ObjectManager.Player))
                                    .First();
                            var whereToQ = Closest.ServerPosition.Extend(
                                ObjectManager.Player.ServerPosition, Closest.Distance(ObjectManager.Player) + 300f);
                            if ((Helpers.OkToQ2(whereToQ) ||
                                 (!Helpers.OkToQ2(whereToQ) && MenuHelper.isMenuEnabled("dz191.vhr.misc.tumble.qspam"))) &&
                                !whereToQ.UnderTurret(true))
                            {
                                _spells[SpellSlot.Q].Cast(whereToQ);
                                return;
                            }
                        }

                        if (!Helpers.OkToQ2(posAfterTumble) &&
                            MenuHelper.isMenuEnabled("dz191.vhr.misc.tumble.noqenemies"))
                        {
                            if (!(MenuHelper.isMenuEnabled("dz191.vhr.misc.tumble.qspam")))
                            {
                                return;
                            }
                        }

                        _spells[SpellSlot.Q].Cast(pos);
                        break;
                }
            }
        }

        #endregion

        #region E Region
        /// <summary>
        /// The E logic checker method.
        /// </summary>
        /// <param name="fromPosition">The start position</param>
        /// <param name="tg">The target which can be condemned.</param>
        /// <returns>Whether the target is condemnable or not.</returns>
        private static bool CondemnCheck(Vector3 fromPosition, out Obj_AI_Hero tg)
        {
            if (Environment.TickCount - _lastCondemnCheck < 150)
            {
                tg = null;
                return false;
            }
            _lastCondemnCheck = Environment.TickCount;

            if ((fromPosition.UnderTurret(true) && MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.noeturret"))|| !_spells[SpellSlot.E].IsReady())
            {
                tg = null;
                return false;
            }
            if (
                !HeroManager.Enemies.Any(
                    h =>
                        h.IsValidTarget(_spells[SpellSlot.E].Range) && !h.HasBuffOfType(BuffType.SpellShield) &&
                        !h.HasBuffOfType(BuffType.SpellImmunity)))
            {
                tg = null;
                return false;
            }
            switch (Menu.Item("dz191.vhr.misc.condemn.condemnmethod").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    //VH revolution
                    var HeroList =
                                HeroManager.Enemies.Where(
                                    h =>
                                        h.IsValidTarget(_spells[SpellSlot.E].Range) &&
                                        !h.HasBuffOfType(BuffType.SpellShield) &&
                                        !h.HasBuffOfType(BuffType.SpellImmunity));
                    var NumberOfChecks = MenuHelper.getSliderValue("dz191.vhr.misc.condemn.rev.predictionNumber");
                    //dz191.vhr.misc.condemn.rev.accuracy
                    //dz191.vhr.misc.condemn.rev.nextprediction
                    var MinChecksPercent = MenuHelper.getSliderValue("dz191.vhr.misc.condemn.rev.accuracy");
                    var PushDistance = MenuHelper.getSliderValue("dz191.vhr.misc.condemn.pushdistance");
                    var NextPrediction = (MenuHelper.getSliderValue("dz191.vhr.misc.condemn.rev.nextprediction"));
                    var interval = NextPrediction / NumberOfChecks;
                    var currentInterval = interval;
                    var LastUnitPosition = Vector3.Zero;

                    if (ObjectManager.Player.ServerPosition.UnderTurret(true))
                    {
                        tg = null;
                        return false;
                    }

                    foreach (var Hero in HeroList)
                    {
                        var finalPosition = Hero.ServerPosition.Extend(fromPosition, -PushDistance);

                        if (MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.onlystuncurrent") &&
                                   Hero.NetworkId != Orbwalker.GetTarget().NetworkId)
                        {
                            continue;
                        }

                        if (Hero.Health + 10 <=
                                    ObjectManager.Player.GetAutoAttackDamage(Hero) *
                                    MenuHelper.getSliderValue("dz191.vhr.misc.condemn.noeaa"))
                        {
                            continue;
                        }

                        var PredictionsList = new List<Vector3>();

                        PredictionsList.Add(Hero.ServerPosition);

                        for (var i = 0; i < NumberOfChecks; i++)
                        {
                            var Prediction = LeagueSharp.Common.Prediction.GetPrediction(Hero, currentInterval);
                            var UnitPosition = Prediction.UnitPosition;
                            if (UnitPosition.Distance(LastUnitPosition, true) >=
                                (Hero.BoundingRadius / 2f) * (Hero.BoundingRadius / 2f))
                            {
                                PredictionsList.Add(UnitPosition);
                                LastUnitPosition = UnitPosition;
                                currentInterval += interval;
                            }
                        }

                        var ExtendedList = new List<Vector3>();

                        foreach (var position in PredictionsList)
                        {
                            ExtendedList.Add(position.Extend(fromPosition, -PushDistance / 4f));
                            ExtendedList.Add(position.Extend(fromPosition, -PushDistance / 2f));
                            ExtendedList.Add(position.Extend(fromPosition, -(PushDistance * 0.75f)));
                            ExtendedList.Add(position.Extend(fromPosition, -PushDistance));
                        }

                        var WallListCount = ExtendedList.Count(h => h.IsWall() || IsJ4Flag(h, Hero));
                        //Console.WriteLine("Actual Preds: {0} Walllist count: {1} TotalList: {2} Percent: {3}", PredictionsList.Count, WallListCount, ExtendedList.Count, ((float)WallListCount / (float)ExtendedList.Count));

                        if (((float)WallListCount / (float)ExtendedList.Count) >= MinChecksPercent / 100f)
                        {
                            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.trinketbush") &&
                                    NavMesh.IsWallOfGrass(finalPosition, 25) && trinketSpell != null)
                            {
                                var wardPosition = ObjectManager.Player.ServerPosition.Extend(
                                    finalPosition,
                                    ObjectManager.Player.ServerPosition.Distance(finalPosition) - 25f);
                                LeagueSharp.Common.Utility.DelayAction.Add(250, () => trinketSpell.Cast(wardPosition));
                            }

                            tg = Hero;
                            return true;
                        }
                    }
                    break;
                case 1:
                    //VHReborn Condemn Code
                    foreach (var target in HeroManager.Enemies.Where(h => h.IsValidTarget(_spells[SpellSlot.E].Range) && !h.HasBuffOfType(BuffType.SpellShield) && !h.HasBuffOfType(BuffType.SpellImmunity)))
                    {
                        var pushDistance = MenuHelper.getSliderValue("dz191.vhr.misc.condemn.pushdistance");
                        var targetPosition = target.ServerPosition;
                        var finalPosition = targetPosition.Extend(fromPosition, -pushDistance);
                        var numberOfChecks = (float)Math.Ceiling(pushDistance / 30f);

                        if (MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.onlystuncurrent") &&
                                    !target.Equals(Orbwalker.GetTarget()))
                        {
                            continue;
                        }

                        for (var i = 1; i <= 30; i++)
                        {
                            var v3 = (targetPosition - fromPosition).Normalized();
                            var extendedPosition = targetPosition + v3 * (numberOfChecks * i); 
                            //var underTurret = MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.condemnturret") && (Helpers.UnderAllyTurret(finalPosition) || Helpers.IsFountain(finalPosition));
                            var j4Flag = MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.condemnflag") && (Helpers.IsJ4FlagThere(extendedPosition, target));
                            if ((extendedPosition.IsWall() || j4Flag) && (target.Path.Count() < 2) && !target.IsDashing())
                            {
                                
                                if (target.Health + 10 <=
                                    ObjectManager.Player.GetAutoAttackDamage(target) *
                                    MenuHelper.getSliderValue("dz191.vhr.misc.condemn.noeaa"))
                                {
                                    tg = null;
                                    return false;
                                }

                                if (extendedPosition.UnderTurret(true))
                                {
                                    tg = null;
                                    return false;
                                }

                                if (MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.trinketbush") &&
                                    NavMesh.IsWallOfGrass(extendedPosition,25) && trinketSpell != null)
                                {
                                    var wardPosition = ObjectManager.Player.ServerPosition.Extend(
                                        extendedPosition,
                                        ObjectManager.Player.ServerPosition.Distance(extendedPosition) - 25f);
                                    LeagueSharp.Common.Utility.DelayAction.Add(250,() => trinketSpell.Cast(wardPosition));
                                }

                                CondemnNotification.Text = "Condemned " + target.ChampionName;
                                _predictedEndPosition = extendedPosition;
                                _predictedPosition = targetPosition;
                                
                                tg = target;
                                return true;
                            }
                        }
                    }
                    break;
                case 2:
                    //Marksman/Gosu Condemn Code
                    foreach (var target in HeroManager.Enemies.Where(h => h.IsValidTarget(_spells[SpellSlot.E].Range)))
                    {
                        var pushDistance = MenuHelper.getSliderValue("dz191.vhr.misc.condemn.pushdistance");
                        var targetPosition = _spells[SpellSlot.E].GetPrediction(target).UnitPosition;
                        var finalPosition = targetPosition.Extend(fromPosition, -pushDistance);
                        var finalPosition2 = targetPosition.Extend(fromPosition, -(pushDistance/2f));
                        var underTurret = MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.condemnturret") && (finalPosition.UnderTurret(false) || Helpers.IsFountain(finalPosition));
                        var j4Flag = MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.condemnflag") && (Helpers.IsJ4FlagThere(finalPosition, target) || Helpers.IsJ4FlagThere(finalPosition2, target));
                        if (finalPosition.IsWall() || finalPosition2.IsWall() || underTurret || j4Flag)
                        {
                            if (MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.onlystuncurrent") &&
                                    !target.Equals(Orbwalker.GetTarget()))
                            {
                                tg = null;
                                return false;
                            }

                            if (target.Health + 10 <=
                                    ObjectManager.Player.GetAutoAttackDamage(target) *
                                    MenuHelper.getSliderValue("dz191.vhr.misc.condemn.noeaa"))
                            {
                                tg = null;
                                return false;
                            }

                            tg = target;
                            return true;
                        }
                    }
                    break;
                case 3:
                    //Vayne Hunter Rework
                    foreach (var en in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy && hero.IsValidTarget() && hero.Distance(Player.Position)<= _spells[SpellSlot.E].Range))
                    {
                        var ePred = _spells[SpellSlot.E].GetPrediction(en);
                        int pushDist = Menu.Item("dz191.vhr.misc.condemn.pushdistance").GetValue<Slider>().Value;
                        for (int i = 0; i < pushDist; i += (int)en.BoundingRadius)
                        {
                            Vector3 loc3 = ePred.UnitPosition.To2D().Extend(fromPosition.To2D(), -i).To3D();
                            var orTurret = MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.condemnturret") && Helpers.UnderAllyTurret(loc3);
                            if (loc3.IsWall() || orTurret)
                            {
                                if (MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.onlystuncurrent") &&
                                    !en.Equals(Orbwalker.GetTarget()))
                                {
                                    tg = null;
                                    return false;
                                }

                                if (en.Health + 10 <=
                                    ObjectManager.Player.GetAutoAttackDamage(en) *
                                    MenuHelper.getSliderValue("dz191.vhr.misc.condemn.noeaa"))
                                {
                                    tg = null;
                                    return false;
                                }

                                if (MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.trinketbush") &&
                                    NavMesh.IsWallOfGrass(loc3, 25) && trinketSpell != null)
                                {
                                    var wardPosition = ObjectManager.Player.ServerPosition.Extend(
                                        loc3,
                                        ObjectManager.Player.ServerPosition.Distance(loc3) - 25f);
                                    LeagueSharp.Common.Utility.DelayAction.Add(250, () => trinketSpell.Cast(wardPosition));
                                }

                                tg = en;
                                return true; 
                            }
                        }
                    }
                    break;
            }
            tg = null;
            return false;
        }
        private static bool IsJ4Flag(Vector3 endPosition, Obj_AI_Base Target)
        {
            return MenuHelper.isMenuEnabled("dz191.vhr.misc.condemn.condemnflag") && Helpers.IsJ4FlagThere(endPosition, (Obj_AI_Hero)Target);
        }

        private static bool CondemnBeta(Vector3 fromPosition, out Obj_AI_Hero tg)
        {
            foreach (var target in from target in HeroManager.Enemies.Where(
                h =>
                    h.IsValidTarget(_spells[SpellSlot.E].Range) && !h.HasBuffOfType(BuffType.SpellShield) &&
                    !h.HasBuffOfType(BuffType.SpellImmunity)) let pushDistance = MenuHelper.getSliderValue("dz191.vhr.misc.condemn.pushdistance") let targetPosition = _spells[SpellSlot.E].GetPrediction(target).UnitPosition let finalPosition = targetPosition.Extend(fromPosition, -pushDistance) let condemnRectangle = new Polygon(Polygon.Rectangle(targetPosition.To2D(), finalPosition.To2D(), target.BoundingRadius)) where condemnRectangle.Points.Any(point => point.IsWall()) select target) {
                        tg = target;
                        return true;
                    }
            tg = null;
            return false;
        }
        #endregion

        #region WallTumble
        private static void WallTumble()
        { 
            Vector2 drakeWallQPos = new Vector2(11514, 4462);
            Vector2 midWallQPos = new Vector2(6962, 8952);

            if (Player.Distance(midWallQPos) >= Player.Distance(drakeWallQPos))
            {

                if (Player.Position.X < 12000 || Player.Position.X > 12070 || Player.Position.Y < 4800 ||
                    Player.Position.Y > 4872)
                {
                    Helpers.MoveToLimited(new Vector2(12050, 4827).To3D());
                }
                else
                {
                    Helpers.MoveToLimited(new Vector2(12050, 4827).To3D());
                    LeagueSharp.Common.Utility.DelayAction.Add((int)(106 + Game.Ping / 2f), () =>
                    {
                        _spells[SpellSlot.Q].Cast(drakeWallQPos);

                    });
                }
            }
            else
            {
                if (Player.Position.X < 6908 || Player.Position.X > 6978 || Player.Position.Y < 8917 ||
                    Player.Position.Y > 8989)
                {
                    Helpers.MoveToLimited(new Vector2(6958, 8944).To3D());
                }
                else
                {
                    Helpers.MoveToLimited(new Vector2(6958, 8944).To3D());
                    LeagueSharp.Common.Utility.DelayAction.Add((int)(106 + Game.Ping / 2f), () =>
                    {
                        _spells[SpellSlot.Q].Cast(midWallQPos);

                    });
                }
            }
            
        }

        #endregion

    }
}
