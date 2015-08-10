using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using LeagueSharp;
using LeagueSharp.Common;
using MagicalGirlLux.Helpers;
using SharpDX;

namespace MagicalGirlLux
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }
        public const string ChampName = "Lux";
        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q, W, E, R;
        public static Vector3 castpos;
        public static SpellSlot Ignite;
        public static SpellSlot Flash;
        public static GameObject LuxEGameObject;
        public static readonly Obj_AI_Hero player = ObjectManager.Player;
        public static SoundPlayer welcome = new SoundPlayer(Properties.Resources.Welcome);

        private static void OnLoad(EventArgs args)
        {
            if (player.ChampionName != ChampName)
                return;

            Notifications.AddNotification("Magical Girl Lux ~ ", 10000);

            Q = new Spell(SpellSlot.Q, 1175);

            W = new Spell(SpellSlot.W, 1075);

            E = new Spell(SpellSlot.E, 1075);

            R = new Spell(SpellSlot.R, 3300);

            Q.SetSkillshot(0.25f, 110f, 1300f, false, SkillshotType.SkillshotLine);

            W.SetSkillshot(0.25f, 110f, 1200f, false, SkillshotType.SkillshotLine);

            E.SetSkillshot(0.25f, 280f, 1300f, false, SkillshotType.SkillshotCircle);

            R.SetSkillshot(1f, 190f, float.MaxValue, false, SkillshotType.SkillshotLine);

            Config = new Menu("Magical Girl Lux", "MLGLux", true);
            Orbwalker = new Orbwalking.Orbwalker(Config.AddSubMenu(new Menu("[Magical]: Orbwalker", "Orbwalker")));
            TargetSelector.AddToMenu(Config.AddSubMenu(new Menu("[Magical]: Target Selector", "Target Selector")));

            var combo = Config.AddSubMenu(new Menu("[Magical]: Combo Settings", "Combo Settings"));
            var harass = Config.AddSubMenu(new Menu("[Magical]: Harass Settings", "Harass Settings"));
            var killsteal = Config.AddSubMenu(new Menu("[Magical]: Killsteal Settings", "Killsteal Settings"));
            var laneclear = Config.AddSubMenu(new Menu("[Magical]: Laneclear Settings", "Laneclear Settings"));
            var jungleclear = Config.AddSubMenu(new Menu("[Magical]: Jungle Settings", "Jungle Settings"));
            var misc = Config.AddSubMenu(new Menu("[Magical]: Misc Settings", "Misc Settings"));
            var drawing = Config.AddSubMenu(new Menu("[Magical]: Draw Settings", "Draw Settings"));
            var debug = Config.AddSubMenu(new Menu("[Magical]: Debug Settings", "debug"));
            Config.AddItem(new MenuItem("Science", "Made By ScienceARK"));

            combo.SubMenu("[SBTW]ManaManager")
                .AddItem(new MenuItem("qmana", "[Q] Mana %").SetValue(new Slider(10, 100, 0)));
            combo.SubMenu("[SBTW]ManaManager")
                .AddItem(new MenuItem("wmana", "[W] Mana %").SetValue(new Slider(20, 100, 0)));
            combo.SubMenu("[SBTW]ManaManager")
                .AddItem(new MenuItem("emana", "[E] Mana %").SetValue(new Slider(5, 100, 0)));
            combo.SubMenu("[SBTW]ManaManager")
                .AddItem(new MenuItem("rmana", "[R] Mana %").SetValue(new Slider(10, 100, 0)));

            combo.SubMenu("[Q] Settings").AddItem(new MenuItem("UseQ", "Use Q - Light Binding").SetValue(true));
            combo.SubMenu("[Q] Settings").AddItem(new MenuItem("AutoQ", "Auto Q on CC'd targets").SetValue(true));

            combo.SubMenu("[W] Settings").AddItem(new MenuItem("UseW", "Use W - Super Magical Barrier").SetValue(true));
            combo.SubMenu("[W] Settings")
                .AddItem(new MenuItem("autow", "Auto [W] on Turrets/Targetted Spells").SetValue(true));

            combo.SubMenu("[E] Settings").AddItem(new MenuItem("UseE", "Use E - Lucent Singularity").SetValue(true));

            combo.SubMenu("[R] Settings")
                .AddItem(new MenuItem("semir", "Semi-Manual [R]").SetValue(new KeyBind('M', KeyBindType.Press)));
            combo.SubMenu("[R] Settings").AddItem(new MenuItem("UseR", "Use R - Finales Funkeln").SetValue(true));
            combo.SubMenu("[R] Settings").AddItem(new MenuItem("UseRA", "Use R [AOE]").SetValue(false));
            combo.SubMenu("[R] Settings").AddItem(new MenuItem("RA", "Enemy Hit Count").SetValue(new Slider(3, 5, 1)));
            combo.SubMenu("[R] Settings").AddItem(new MenuItem("RQ", "If Q hits target auto R").SetValue(false));

            combo.SubMenu("Summoner Settings").AddItem(new MenuItem("UseIgnite", "Use Ignite").SetValue(true));

            killsteal.AddItem(new MenuItem("SmartKS", "Use SmartKS").SetValue(true));
            killsteal.AddItem(new MenuItem("KSI", "Use Ignite").SetValue(true));
            killsteal.AddItem(new MenuItem("KSQ", "Use Q").SetValue(true));
            killsteal.AddItem(new MenuItem("KSE", "Use E").SetValue(true));
            killsteal.AddItem(new MenuItem("RKS", "Use R").SetValue(true));

            drawing.AddItem(new MenuItem("Draw_Disabled", "Disable All Drawings").SetValue(false));
            drawing.AddItem(new MenuItem("Qdraw", "Draw Q Range").SetValue(new Circle(true, System.Drawing.Color.Orange)));
            drawing.AddItem(
                new MenuItem("Wdraw", "Draw W Range").SetValue(new Circle(true, System.Drawing.Color.DarkOrange)));
            drawing.AddItem(
                new MenuItem("Edraw", "Draw E Range").SetValue(new Circle(true, System.Drawing.Color.AntiqueWhite)));
            drawing.AddItem(
                new MenuItem("Rdraw", "Draw R Range").SetValue(new Circle(true, System.Drawing.Color.CornflowerBlue)));

            drawing.SubMenu("Misc Drawings")
                .AddItem(
                    new MenuItem("dmgdrawer", "[Damage Indicator]:").SetValue(new StringList(new[] {"Custom", "Common"})));
            drawing.SubMenu("Misc Drawings")
                .AddItem(
                    new MenuItem("dmgcolor", "Damage Draw Color").SetValue(new Circle(true, System.Drawing.Color.Orange)));

            drawing.SubMenu("Misc Drawings").AddItem(new MenuItem("drawMinimapR", "Minimap [R] Range Indicator").SetValue(true));
            drawing.SubMenu("Misc Drawings").AddItem(new MenuItem("HUD", "Hud Display").SetValue(true));
            drawing.SubMenu("Misc Drawings").AddItem(new MenuItem("sliderY", "WIDTH [Default : 40]").SetValue(new Slider(40, 250, 0)));
            drawing.SubMenu("Misc Drawings").AddItem(new MenuItem("sliderX", "HEIGHT [Default : 40]").SetValue(new Slider(40, 250, 0)));
            drawing.SubMenu("Misc Drawings").AddItem(new MenuItem("indicator", "Enemy Indicator").SetValue(true));
            drawing.SubMenu("Misc Drawings").AddItem(new MenuItem("jg", "Junglesteal Indicator").SetValue(true));
            drawing.SubMenu("Misc Drawings").AddItem(new MenuItem("ah", "AutoHarass Indicator").SetValue(true));
            //misc.AddItem(new MenuItem("skinhax", "Skin Manager").SetValue(true));
           // misc.AddItem(new MenuItem("luxskins", "Skin Name").SetValue(new StringList(new[] 
           // { "Classic Lux", "Sorceress Lux", "Spellthief Lux", "Imperial Lux", "Commando Lux", "Steel Legion Lux", "Star Guardian Lux" }, 7)));

            harass.AddItem(new MenuItem("autoharass", "AutoHarass Toggle").SetValue(new KeyBind('L', KeyBindType.Toggle)));
            harass.AddItem(new MenuItem("Qharass", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("Qharassslowed", "Only use Q if target is slowed/stunned/rooted").SetValue(false));
            harass.AddItem(new MenuItem("Eharass", "Use E").SetValue(true));
            harass.AddItem(new MenuItem("harassmana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));

            misc
                .AddItem(new MenuItem("Welcome", "Welcome Sound Effect").SetValue(true));
            misc
                .AddItem(new MenuItem("AntiGapQ", "AntiGapCloser [Q]").SetValue(true));
            misc
                .AddItem(new MenuItem("AntiGapE", "AntiGapCloser [E]").SetValue(false));



            laneclear
                .AddItem(new MenuItem("laneQ", "Use Q").SetValue(true));
            laneclear
                .AddItem(new MenuItem("laneE", "Use E").SetValue(true));
            laneclear
                .AddItem(new MenuItem("lanemana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));


            jungleclear
                .AddItem(new MenuItem("jungleQ", "Use Q").SetValue(true));
            jungleclear
                .AddItem(new MenuItem("jungleE", "Use E").SetValue(true));
            jungleclear
                .AddItem(new MenuItem("junglemana", "Mana Percentage").SetValue(new Slider(30, 100, 0)));
            jungleclear
                .AddItem(new MenuItem("blank", "                                        "));

            jungleclear.AddItem(
                new MenuItem("jungleks", "Junglesteal [TOGGLE] ").SetValue(new KeyBind('K', KeyBindType.Toggle)));

            jungleclear
                .AddItem(new MenuItem("Blue", "Steal Bluebuff").SetValue(true));
            jungleclear
                .AddItem(new MenuItem("Red", "Steal Redbuff").SetValue(true));
            jungleclear
                .AddItem(new MenuItem("Dragon", "Steal Dragon").SetValue(true));
            jungleclear
                .AddItem(new MenuItem("Baron", "Steal Baron").SetValue(true));

            debug.AddItem(new MenuItem("Debug", "Draw Prediction || Cast Position").SetValue(false));
            debug.AddItem(new MenuItem("qdebug", "[Q] debug").SetValue(new Circle(true, System.Drawing.Color.Orange)));
            debug.AddItem(new MenuItem("edebug", "[E] debug").SetValue(new Circle(true, System.Drawing.Color.Purple)));
            debug.AddItem(new MenuItem("rdebug", "[R] debug").SetValue(new Circle(true, System.Drawing.Color.CadetBlue)));

            Config.AddToMainMenu();
            if (Config.Item("Welcome").GetValue<bool>()) Soundplayer.PlaySound(welcome);

            Game.OnUpdate += Game_OnGameUpdate;
            GameObject.OnDelete += GameObject_OnDelete;
            GameObject.OnCreate += GameObject_OnCreate;
            Drawing.OnDraw += OnDraw;

            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawings.DrawEvent();

            Obj_AI_Turret.OnAggro += Turretaggro;
            Obj_AI_Base.OnProcessSpellCast += TurretOnProcessSpellCast;


        }

        private static void TurretOnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!W.IsReady())
                return;

            if (!args.Target.IsMe || sender.IsAlly || sender.IsMinion)
            {
                return;
            }

            if (W.IsReady() && Config.Item("autow").GetValue<bool>())
                W.Cast(Game.CursorPos);
        }

        private static void Turretaggro(Obj_AI_Base sender, GameObjectAggroEventArgs args)
        {
            if (!W.IsReady())
                return;
            if (sender.Target.IsMe && W.IsReady() && Config.Item("autow").GetValue<bool>())
                W.Cast(Game.CursorPos);
        }

        public static float CalcDamage(Obj_AI_Base target)
        {
            //Calculate Combo Damage
            var aa = player.GetAutoAttackDamage(target);
            var damage = aa;
            Ignite = player.GetSpellSlot("summonerdot");

            if (Ignite.IsReady())
                damage += player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);

            if (Config.Item("UseE").GetValue<bool>()) // edamage
            {
                if (E.IsReady())
                {
                    damage += E.GetDamage(target);
                }
            }
            if (target.HasBuff("luxilluminatingfraulein"))
            {
                damage += aa +  player.CalcDamage(target, Damage.DamageType.Magical,
                    10 + (8*player.Level) + 0.2*player.FlatMagicDamageMod);
            }
            if (player.HasBuff("lichbane"))
            {
                damage += player.CalcDamage(target, Damage.DamageType.Magical,
                    (player.BaseAttackDamage*0.75) + ((player.BaseAbilityDamage + player.FlatMagicDamageMod)*0.5));
            }
            if (R.IsReady() && Config.Item("UseR").GetValue<bool>()) // rdamage
            {
                damage += R.GetDamage(target);
            }

            if (Q.IsReady() && Config.Item("UseQ").GetValue<bool>())
            {
                damage += Q.GetDamage(target);
            }
            return (float) damage;
        }

        private static void Autospells()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target == null)
                return;
            if (target.IsInvulnerable)
                return;
            if (Q.IsReady() && target.IsValidTarget(Q.Range) && Q.GetPrediction(target).Hitchance >= HitChance.VeryHigh)
            {
                if (target.HasBuffOfType(BuffType.Snare)
                    || target.HasBuffOfType(BuffType.Suppression)
                    || target.HasBuffOfType(BuffType.Taunt)
                    || target.HasBuffOfType(BuffType.Stun)
                    || target.HasBuffOfType(BuffType.Charm)
                    || target.HasBuffOfType(BuffType.Fear))

                    Q.Cast(target);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (player.IsDead || gapcloser.Sender.IsInvulnerable)
                return;

            var targetpos = Drawing.WorldToScreen(gapcloser.Sender.Position);
            if (gapcloser.Sender.IsValidTarget(Q.Range) && Config.Item("AntiGap").GetValue<bool>())
            {
                Render.Circle.DrawCircle(gapcloser.Sender.Position, gapcloser.Sender.BoundingRadius,
                    System.Drawing.Color.DeepPink);
                Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, System.Drawing.Color.MediumPurple, "GAPCLOSER!");
            }

            if (Q.IsReady() && gapcloser.Sender.IsValidTarget(Q.Range) &&
                Config.Item("AntiGapQ").GetValue<bool>())
                Q.Cast(gapcloser.Sender);

            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range) &&
                Config.Item("AntiGapE").GetValue<bool>())
                E.Cast(gapcloser.End);
        }

        public static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            //Lux E has detonated :S
            if (sender.Name.Contains("Lux_Base_E") && sender.IsAlly)
            {
                LuxEGameObject = null;
            }
        }

        public static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            //Lux E spell position (detonation check/enemy check)
            if (sender.Name.Contains("Lux_Base_E") && sender.IsAlly)
            {
                LuxEGameObject = sender;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Config.Item("Draw_Disabled").GetValue<bool>())
                return;


            var pos = Drawing.WorldToScreen(ObjectManager.Player.Position);

            if (Config.Item("ah").GetValue<bool>())
            Drawing.DrawText(pos.X - 50, pos.Y + 30, System.Drawing.Color.Gold, "AutoHarass:");
            if (Config.Item("jg").GetValue<bool>())
            Drawing.DrawText(pos.X - 50, pos.Y + 50, System.Drawing.Color.Gold, "Junglesteal:");


            if (Config.Item("jungleks").GetValue<KeyBind>().Active && Config.Item("jg").GetValue<bool>())
            {
                Drawing.DrawText(pos.X + 41, pos.Y + 50, System.Drawing.Color.LawnGreen, "On");
            }
            else if (Config.Item("jg").GetValue<bool>())
            {
                Drawing.DrawText(pos.X + 41, pos.Y + 50, System.Drawing.Color.Tomato, "Off");
            }
            if (Config.Item("autoharass").GetValue<KeyBind>().Active && Config.Item("ah").GetValue<bool>())
            {
                Drawing.DrawText(pos.X + 43, pos.Y + 30, System.Drawing.Color.LawnGreen, "On");
            }
            else if (Config.Item("ah").GetValue<bool>())
            {
                Drawing.DrawText(pos.X + 43, pos.Y + 30, System.Drawing.Color.Tomato, "Off");
            }
            {
                if (Config.Item("Qdraw").GetValue<Circle>().Active)
                    if (Q.Level > 0)
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range,
                            Q.IsReady() ? Config.Item("Qdraw").GetValue<Circle>().Color : System.Drawing.Color.Red);

                if (Config.Item("Wdraw").GetValue<Circle>().Active)
                    if (W.Level > 0)
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range,
                            W.IsReady() ? Config.Item("Wdraw").GetValue<Circle>().Color : System.Drawing.Color.Red);

                if (Config.Item("Edraw").GetValue<Circle>().Active)
                    if (E.Level > 0)
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range - 1,
                            E.IsReady() ? Config.Item("Edraw").GetValue<Circle>().Color : System.Drawing.Color.Red);

                if (Config.Item("Rdraw").GetValue<Circle>().Active)
                    if (R.Level > 0)
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range - 2,
                            R.IsReady() ? Config.Item("Rdraw").GetValue<Circle>().Color : System.Drawing.Color.Red);

                {
                    var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                    if (target == null)
                        return;
                    if (target.IsInvulnerable)
                        return;

                    var orbwalkert = Orbwalker.GetTarget();
                    if (orbwalkert.IsValidTarget(R.Range))
                        Render.Circle.DrawCircle(orbwalkert.Position, 80, System.Drawing.Color.DeepSkyBlue, 7);


                    var rdebugp = R.GetPrediction(target);
                    var qdebugp = Q.GetPrediction(target);
                    var edebugp = E.GetPrediction(target);
                    var rdebug = new Geometry.Polygon.Line(player.Position, rdebugp.CastPosition, R.Range);
                    var qdebug = new Geometry.Polygon.Line(player.Position, qdebugp.CastPosition, Q.Range + 200);
                    var edebug = new Geometry.Polygon.Line(player.Position, edebugp.CastPosition, E.Range + 200);

                    if (!Config.Item("Debug").GetValue<bool>())
                        return;

                    if (qdebugp.Hitchance >= HitChance.VeryHigh && Config.Item("qdebug").GetValue<Circle>().Active)
                        qdebug.Draw(Config.Item("qdebug").GetValue<Circle>().Color, 2);
                    if (edebugp.Hitchance >= HitChance.VeryHigh && Config.Item("edebug").GetValue<Circle>().Active)
                        edebug.Draw(Config.Item("edebug").GetValue<Circle>().Color, 3);
                    if (rdebugp.Hitchance >= HitChance.VeryHigh && Config.Item("rdebug").GetValue<Circle>().Active)
                        rdebug.Draw(Config.Item("rdebug").GetValue<Circle>().Color, 4);
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (target == null)
                return;
            if (target.IsInvulnerable)
                return;
            var harassmana = Config.Item("harassmana").GetValue<Slider>().Value;
            var qpred = Q.GetPrediction(target);
            var qcollision = Q.GetCollision(player.ServerPosition.To2D(), new List<Vector2> {qpred.CastPosition.To2D()});
            var minioncol = qcollision.Where(x => !(x is Obj_AI_Hero)).Count(x => x.IsMinion);

            if (E.IsReady() && target.IsValidTarget(R.Range) && Config.Item("Eharass").GetValue<bool>() &&
                player.ManaPercent >= harassmana)
                Elogic();

            if (target.IsValidTarget(Q.Range)
                && minioncol <= 1
                && Config.Item("Qharass").GetValue<bool>()
                && qpred.Hitchance >= HitChance.VeryHigh && player.ManaPercent >= harassmana
                && target.HasBuffOfType(BuffType.Slow) || target.IsValidTarget(Q.Range)
                && minioncol <= 1
                && Config.Item("Qharass").GetValue<bool>()
                && qpred.Hitchance >= HitChance.VeryHigh && player.ManaPercent >= harassmana
                && target.HasBuffOfType(BuffType.Stun) || target.IsValidTarget(Q.Range)
                && minioncol <= 1
                && Config.Item("Qharass").GetValue<bool>()
                && qpred.Hitchance >= HitChance.VeryHigh && player.ManaPercent >= harassmana
                && target.HasBuffOfType(BuffType.Snare))

                Q.Cast(target);

            if (Config.Item("Qharassslowed").GetValue<bool>())
                return;

            if (target.IsValidTarget(Q.Range)
                && minioncol <= 1
                && Config.Item("Qharass").GetValue<bool>()
                && qpred.Hitchance >= HitChance.VeryHigh && player.ManaPercent >= harassmana)
                Q.Cast(target);
        }

        private static float IgniteDamage(Obj_AI_Hero target)
        {
            if (Ignite == SpellSlot.Unknown || player.Spellbook.CanUseSpell(Ignite) != SpellState.Ready)
                return 0f;
            return (float) player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
        }

        private static void Killsteal()
        {
            foreach (var enemy in
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(x => x.IsValidTarget(R.Range))
                    .Where(x => !x.IsZombie)
                    .Where(x => !x.IsDead))
            {
                Ignite = player.GetSpellSlot("summonerdot");
                var edmg = E.GetDamage(enemy);
                var qdmg = Q.GetDamage(enemy);
                var rdmg = R.GetDamage(enemy);
                var passivedmg1 = player.CalcDamage(enemy, Damage.DamageType.Magical,
                    10 + (8 * player.Level) + 0.2 * player.FlatMagicDamageMod);                
                var rpdmg = R.GetDamage(enemy) + passivedmg1;
                var rpred = R.GetPrediction(enemy);
                var qpred = Q.GetPrediction(enemy);
                var epred = E.GetPrediction(enemy);

                var qcollision = Q.GetCollision(player.ServerPosition.To2D(),
                    new List<Vector2> {qpred.CastPosition.To2D()});
                var minioncol = qcollision.Where(x => !(x is Obj_AI_Hero)).Count(x => x.IsMinion);

                if (player.Distance(enemy.Position) <= 600 && IgniteDamage(enemy) >= enemy.Health &&
                    player.HealthPercent <= 25 &&
                    Config.Item("KSI").GetValue<bool>())
                    player.Spellbook.CastSpell(Ignite, enemy);


                if (enemy.Health < edmg && E.IsReady() && epred.Hitchance >= HitChance.VeryHigh)
                    E.Cast(epred.CastPosition);
                else if (enemy.Health < edmg && E.IsReady() && epred.Hitchance >= HitChance.VeryHigh)
                    E.Cast(epred.CastPosition);

                if (enemy.Health < qdmg && qpred.Hitchance >= HitChance.VeryHigh && minioncol <= 1 && Q.IsReady() &&
                    Config.Item("KSQ").GetValue<bool>())
                    Q.Cast(enemy);

                var ripdmg = R.GetDamage(enemy) + passivedmg1;

                var passivedmg = player.CalcDamage(enemy, Damage.DamageType.Magical,
                    10 + (8*player.Level) + 0.2*player.FlatMagicDamageMod);
                var passiveaa = player.GetAutoAttackDamage(player) + passivedmg;
                var lichdmg = player.CalcDamage(enemy, Damage.DamageType.Magical,
                    (player.BaseAttackDamage*0.75) + ((player.BaseAbilityDamage + player.FlatMagicDamageMod)*0.5));

                if (enemy.IsValidTarget(Q.Range) && Q.GetPrediction(enemy).Hitchance >= HitChance.VeryHigh &&
                    Q.IsReady() &&
                    E.IsReady() && enemy.Health < E.GetDamage(enemy) + Q.GetDamage(enemy))
                    return;

                if (player.HasBuff("lichbane") && enemy.Health < lichdmg &&
                    player.Distance(enemy.Position) < Orbwalking.GetRealAutoAttackRange(player))
                    return;


                if (player.HasBuff("lichbane") && enemy.HasBuff("luxilluminatingfraulein") &&
                    enemy.Health < lichdmg + passivedmg && //LIchbane AA PASSIVE check
                    player.Distance(enemy.Position) < Orbwalking.GetRealAutoAttackRange(player))
                    return;

                if (player.Distance(enemy.Position) < E.Range - 200 && E.GetDamage(enemy) > enemy.Health && E.IsReady() ||
                    LuxEGameObject != null && enemy.Distance(LuxEGameObject.Position) <= E.Width &&
                    enemy.Health < E.GetDamage(enemy) ||
                    player.Distance(enemy.Position) < E.Range - 200 && Q.GetDamage(enemy) > enemy.Health && Q.IsReady() &&
                    Q.GetPrediction(enemy).Hitchance >= HitChance.VeryHigh ||
                    player.Distance(enemy.Position) < Orbwalking.GetRealAutoAttackRange(player) &&
                    player.GetAutoAttackDamage(enemy)*2 > enemy.Health)
                    return;

                if (LuxEGameObject != null && enemy.Distance(LuxEGameObject.Position) <= E.Width &&
                    enemy.Health < E.GetDamage(enemy) ||
                    enemy.HasBuff("LuxLightBindingMis")
                    && enemy.Health < passiveaa &&
                    player.Distance(enemy.Position) <= Orbwalking.GetRealAutoAttackRange(player) &&
                    enemy.HasBuff("luxilluminatingfraulein"))
                    return;

                if (enemy.Health < rdmg - 100 + (3*player.Level) && enemy.Position.CountAlliesInRange(650) >= 1)
                    return;

                if (enemy.Health <= passiveaa && enemy.HasBuff("luxilluminatingfraulein") &&
                    player.Distance(enemy.Position) <= Orbwalking.GetRealAutoAttackRange(player))
                    return;

                if (enemy.Health <= Q.GetDamage(enemy) && Q.GetPrediction(enemy).Hitchance >= HitChance.VeryHigh &&
                    Q.IsReady())
                    return;

                if (enemy.HasBuff("caitlynaceinthehole"))
                    return;

                if (enemy.Health < rdmg - 100 + (3*player.Level) && enemy.Position.CountAlliesInRange(650) >= 1)
                    return;

                if (enemy.IsValidTarget(R.Range)
                    && rpred.Hitchance >= HitChance.VeryHigh
                    && Config.Item("RKS").GetValue<bool>()
                    && enemy.HasBuff("luxilluminatingfraulein")
                    && enemy.Health < rpdmg
                    && player.Distance(enemy.Position) >= 100
                    || enemy.IsValidTarget(R.Range)
                    && rpred.Hitchance >= HitChance.VeryHigh
                    && Config.Item("RKS").GetValue<bool>()
                    && enemy.HasBuff("luxilluminatingfraulein")
                    && enemy.Health < rpdmg
                    && enemy.HasBuff("LuxLightBindingMis"))
                    R.Cast(enemy);

                if (enemy.IsValidTarget(R.Range)
                    && Config.Item("RKS").GetValue<bool>()
                    && rpred.Hitchance >= HitChance.VeryHigh
                    && enemy.Health < rdmg
                    && player.Distance(enemy.Position) >= 100
                    || enemy.IsValidTarget(R.Range)
                    && Config.Item("RKS").GetValue<bool>()
                    && rpred.Hitchance >= HitChance.VeryHigh
                    && enemy.Health < rdmg
                    && enemy.HasBuff("LuxLightBindingMis"))
                    R.Cast(enemy);

                if (player.Distance(enemy.Position) < 600 && E.GetDamage(enemy) > enemy.Health && E.IsReady()
                    ||
                    LuxEGameObject != null && enemy.Distance(LuxEGameObject.Position) <= E.Width &&
                    enemy.Health < E.GetDamage(enemy) ||
                    player.Distance(enemy.Position) < 600 && Q.GetDamage(enemy) > enemy.Health && Q.IsReady() ||
                    player.Distance(enemy.Position) < 600 && enemy.Health < player.GetAutoAttackDamage(enemy)*2)
                    return;

                if (player.Distance(enemy.Position) <= 600 && ripdmg >= enemy.Health &&
                    Config.Item("UseIgnite").GetValue<bool>() && R.IsReady() && Ignite.IsReady())
                    player.Spellbook.CastSpell(Ignite, enemy);
            }
        }

        private static void Junglesteal()
        {
            if (!R.IsReady())
                return;

            if (Config.Item("Blue").GetValue<bool>()) //
            {
                var blueBuff =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(x => x.BaseSkinName == "SRU_Blue")
                        .Where(x => player.GetSpellDamage(x, SpellSlot.R) > x.Health)
                        .FirstOrDefault(x => (x.IsAlly) || (x.IsEnemy));

                if (blueBuff != null)
                    R.Cast(blueBuff);
            }

            if (Config.Item("Red").GetValue<bool>()) //
            {
                var redBuff =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(x => x.BaseSkinName == "SRU_Red")
                        .Where(x => player.GetSpellDamage(x, SpellSlot.R) > x.Health)
                        .FirstOrDefault(x => (x.IsAlly) || (x.IsEnemy));

                if (redBuff != null)
                    R.Cast(redBuff);
            }

            if (Config.Item("Baron").GetValue<bool>()) //
            {
                var Baron =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(x => x.BaseSkinName == "SRU_Baron")
                        .Where(x => player.GetSpellDamage(x, SpellSlot.R) > x.Health)
                        .FirstOrDefault(x => (x.IsAlly) || (x.IsEnemy));

                if (Baron != null)
                    R.Cast(Baron);
            }

            if (Config.Item("Dragon").GetValue<bool>()) //
            {
                var Dragon =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(x => x.BaseSkinName == "SRU_Dragon")
                        .Where(x => player.GetSpellDamage(x, SpellSlot.R) > x.Health)
                        .FirstOrDefault(x => (x.IsAlly) || (x.IsEnemy));

                if (Dragon != null)
                    R.Cast(Dragon);
            }
        }


        private static void Jungleclear()
        {
            var junglem = Config.Item("junglemana").GetValue<Slider>().Value;
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width,
                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width,
                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            var Qfarmpos = W.GetLineFarmLocation(allMinionsQ, Q.Width);
            var Efarmpos = E.GetCircularFarmLocation(allMinionsE, E.Width);
            if (Qfarmpos.MinionsHit >= 1 && Config.Item("jungleQ").GetValue<bool>() &&
                player.ManaPercent >= junglem)
            {
                Q.Cast(Qfarmpos.Position);
            }
            if (Efarmpos.MinionsHit >= 1 && allMinionsE.Count >= 1 && Config.Item("jungleE").GetValue<bool>() &&
                player.ManaPercent >= junglem)
                E.Cast(Efarmpos.Position);

            if (LuxEGameObject != null)
                E.Cast();
        }

        private static void Lasthit()
        {
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width);

             foreach (var minion in allMinionsE.Where(a => a.HasBuff("luxilluminatingfraulein")))
            {
                var passivedmg = player.CalcDamage(minion, Damage.DamageType.Magical,
                10 + (8 * player.Level) + 0.2 * player.FlatMagicDamageMod) + player.GetAutoAttackDamage(minion);
                if (minion.Health < passivedmg)
                {
                    Orbwalker.ForceTarget(minion);
                    player.IssueOrder(GameObjectOrder.AutoAttack, minion);
                }
            }
        }
        private static void Laneclear()
        {
            var lanem = Config.Item("lanemana").GetValue<Slider>().Value;
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range + Q.Width);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width);


            var Qfarmpos = W.GetLineFarmLocation(allMinionsQ, Q.Width);
            var Efarmpos = E.GetCircularFarmLocation(allMinionsE, E.Width);
            if (Qfarmpos.MinionsHit >= 1 && Config.Item("laneQ").GetValue<bool>() && player.ManaPercent >= lanem)
            {
                Q.Cast(Qfarmpos.Position);
            }
            foreach (var minion in allMinionsE.Where(a => a.HasBuff("luxilluminatingfraulein")))
            {
                var passivedmg = player.CalcDamage(minion, Damage.DamageType.Magical,
                10 + (8 * player.Level) + 0.2 * player.FlatMagicDamageMod);
                if (minion.Health < passivedmg && !player.IsWindingUp)
                {
                    Orbwalker.ForceTarget(minion);
                    player.IssueOrder(GameObjectOrder.AutoAttack, minion);
                }
            }
            if (Efarmpos.MinionsHit >= 3 && allMinionsE.Count >= 2 && Config.Item("laneE").GetValue<bool>() &&
                player.ManaPercent >= lanem)
                E.Cast(Efarmpos.Position);

            if (LuxEGameObject != null)
                E.Cast();
        }

        private static void forceR()
        {
            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            if (target == null)
                return;
            if (R.IsReady() && target.IsValidTarget(R.Range))
                R.Cast(target);
        }

        public static bool qcasted { get; set; }

        private static void Rlogic()
        {
            {
                if (!Q.IsReady())
                    qcasted = true;
                Utility.DelayAction.Add(1000, () => qcasted = false);
            }

            Ignite = player.GetSpellSlot("summonerdot");
            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            if (target == null)
                return;


            var rdmg = R.GetDamage(target);
            var rpdmg = R.GetDamage(target) + player.CalcDamage(target, Damage.DamageType.Magical,
                    10 + (8*player.Level) + 0.2*player.FlatMagicDamageMod);
            var rpred = R.GetPrediction(target);
            var ripdmg = R.GetDamage(target) + player.CalcDamage(target, Damage.DamageType.Magical,
                    10 + (8 * player.Level) + 0.2 * player.FlatMagicDamageMod) +
                         IgniteDamage(target);
            var lichdmg = player.CalcDamage(target, Damage.DamageType.Magical,
                (player.BaseAttackDamage*0.75) + ((player.BaseAbilityDamage + player.FlatMagicDamageMod)*0.5));

            var cdmg = R.GetDamage(target) + E.GetDamage(target);
            var passivedmg = player.CalcDamage(target, Damage.DamageType.Magical,
                10 + (8 * player.Level) + 0.2 * player.FlatMagicDamageMod);
            var passiveaa = player.GetAutoAttackDamage(player) + passivedmg;

            if (target.IsInvulnerable)
                return;

            if (target.HasBuff("caitlynaceinthehole"))
                return;



            if (target.Health <= passiveaa && target.HasBuff("luxilluminatingfraulein") &&
                player.Distance(target.Position) <= Orbwalking.GetRealAutoAttackRange(player))
                return;

            if (target.Health <= Q.GetDamage(target) && Q.GetPrediction(target).Hitchance >= HitChance.VeryHigh &&
                Q.IsReady())
                return;

            if (target.Health <= Q.GetDamage(target) && Q.GetPrediction(target).Hitchance >= HitChance.VeryHigh &&
                qcasted)
                return;

            if (LuxEGameObject != null && target.IsValidTarget(R.Range) &&
                target.Position.Distance(LuxEGameObject.Position) <= E.Width && R.IsReady() //EPR combo
                && target.IsValidTarget(R.Range) && rpred.Hitchance >= HitChance.VeryHigh &&
                target.Health < cdmg + (passivedmg*1) && target.HasBuff("LuxLightBindingMis")
                ||
                LuxEGameObject != null && target.IsValidTarget(R.Range) &&
                target.Position.Distance(LuxEGameObject.Position) <= E.Width && R.IsReady() && Ignite.IsReady()
                    //ERQPI combo
                && target.IsValidTarget(R.Range) && rpred.Hitchance >= HitChance.VeryHigh &&
                target.Health < cdmg + (passivedmg*1) + IgniteDamage(target) && target.HasBuff("LuxLightBindingMis")
                ||
                LuxEGameObject != null && target.IsValidTarget(R.Range) && player.HasBuff("lichbane") &&
                player.Distance(target.Position) <= Orbwalking.GetRealAutoAttackRange(player) && //ERPB combo
                target.Position.Distance(LuxEGameObject.Position) <= E.Width && R.IsReady()
                && target.IsValidTarget(R.Range) && rpred.Hitchance >= HitChance.VeryHigh &&
                target.Health < cdmg + (passivedmg*1) + lichdmg && target.HasBuff("LuxLightBindingMis")
                ||
                LuxEGameObject != null && target.IsValidTarget(R.Range) && player.HasBuff("lichbane") &&
                player.Distance(target.Position) <= Orbwalking.GetRealAutoAttackRange(player) && //ERLIP combo
                target.Position.Distance(LuxEGameObject.Position) <= E.Width && R.IsReady() && Ignite.IsReady()
                && target.IsValidTarget(R.Range) && rpred.Hitchance >= HitChance.VeryHigh &&
                target.Health < cdmg + (passivedmg*1) + IgniteDamage(target) + lichdmg &&
                target.HasBuff("LuxLightBindingMis"))
                R.Cast(target);


            if (target.HasBuff("LuxLightBindingMis") && Config.Item("RQ").GetValue<bool>() && //RQ config
                rpred.Hitchance >= HitChance.VeryHigh)
                R.Cast(target);

            if (player.HasBuff("lichbane") && target.Health < lichdmg && //LIchbane AA check
                player.Distance(target.Position) < Orbwalking.GetRealAutoAttackRange(player))
                return;

            if (player.HasBuff("lichbane") && target.HasBuff("luxilluminatingfraulein") &&
                target.Health < lichdmg + passivedmg && //LIchbane AA PASSIVE check
                player.Distance(target.Position) < Orbwalking.GetRealAutoAttackRange(player))
                return;

            if (target.IsValidTarget(Q.Range) && Q.GetPrediction(target).Hitchance >= HitChance.VeryHigh && Q.IsReady() &&
                //QE overkill check
                E.IsReady() && target.Health < E.GetDamage(target) + Q.GetDamage(target))
                return;

            if (player.Distance(target.Position) < E.Range - 200 && E.GetDamage(target) > target.Health && E.IsReady() ||
                LuxEGameObject != null && target.Distance(LuxEGameObject.Position) <= E.Width &&
                target.Health < E.GetDamage(target) || //ECHECK
                player.Distance(target.Position) < E.Range - 200 && Q.GetDamage(target) > target.Health && Q.IsReady() &&
                Q.GetPrediction(target).Hitchance >= HitChance.VeryHigh ||
                player.Distance(target.Position) < Orbwalking.GetRealAutoAttackRange(player) &&
                player.GetAutoAttackDamage(target)*2 > target.Health)
                return;

            if (LuxEGameObject != null && target.Distance(LuxEGameObject.Position) <= E.Width &&
                target.Health < E.GetDamage(target) ||
                target.HasBuff("LuxLightBindingMis")
                && target.Health < passiveaa &&
                player.Distance(target.Position) <= Orbwalking.GetRealAutoAttackRange(player) &&
                target.HasBuff("luxilluminatingfraulein"))
                return;

            if (target.IsValidTarget(R.Range)
                && R.IsReady()
                && Config.Item("UseRA").GetValue<bool>()
                && rpred.Hitchance >= HitChance.VeryHigh && player.Distance(target.Position) <= E.Range &&
                target.IsValidTarget(R.Range) && !E.IsReady())

                R.CastIfWillHit(target, Config.Item("RA").GetValue<Slider>().Value,
                    Config.Item("packetcast").GetValue<bool>());

            else if (target.IsValidTarget(R.Range)
                     && R.IsReady()
                     && Config.Item("UseRA").GetValue<bool>()
                     && rpred.Hitchance >= HitChance.VeryHigh && player.Distance(target.Position) >= E.Range &&
                     target.IsValidTarget(R.Range))

                R.CastIfWillHit(target, Config.Item("RA").GetValue<Slider>().Value,
                    Config.Item("packetcast").GetValue<bool>());

            if (target.Health < rdmg - 100 + (3*player.Level) && target.Position.CountAlliesInRange(650) >= 1)
                return;

            if (target.IsValidTarget(R.Range)
                && rpred.Hitchance >= HitChance.VeryHigh
                && Config.Item("UseR").GetValue<bool>()
                && target.HasBuff("luxilluminatingfraulein")
                && target.Health < rpdmg
                && player.Distance(target.Position) >= 100
                || target.IsValidTarget(R.Range)
                && rpred.Hitchance >= HitChance.VeryHigh
                && Config.Item("UseR").GetValue<bool>()
                && target.HasBuff("luxilluminatingfraulein")
                && target.Health < rpdmg
                && target.HasBuff("LuxLightBindingMis"))
                R.Cast(target);

            if (target.IsValidTarget(R.Range)
                && Config.Item("UseR").GetValue<bool>()
                && rpred.Hitchance >= HitChance.VeryHigh
                && target.Health < rdmg
                && player.Distance(target.Position) >= 100
                || target.IsValidTarget(R.Range)
                && Config.Item("UseR").GetValue<bool>()
                && rpred.Hitchance >= HitChance.VeryHigh
                && target.Health < rdmg
                && target.HasBuff("LuxLightBindingMis"))
                R.Cast(target);

            if (player.Distance(target.Position) < 600 && E.GetDamage(target) > target.Health && E.IsReady()
                ||
                LuxEGameObject != null && target.Distance(LuxEGameObject.Position) <= E.Width &&
                target.Health < E.GetDamage(target) ||
                player.Distance(target.Position) < 600 && Q.GetDamage(target) > target.Health && Q.IsReady() ||
                player.Distance(target.Position) < 600 && target.Health < player.GetAutoAttackDamage(target)*2)
                return;

            if (player.Distance(target.Position) <= 600 && ripdmg >= target.Health &&
                Config.Item("UseIgnite").GetValue<bool>() && R.IsReady() && Ignite.IsReady())
                player.Spellbook.CastSpell(Ignite, target);

        }


        private static void Edetonate()
        {
            var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            if (target == null)
                return;

            if (LuxEGameObject != null && LuxEGameObject.Position.CountEnemiesInRange(E.Width) >= 2)
                E.Cast();

            if (LuxEGameObject != null && LuxEGameObject.Position.CountEnemiesInRange(E.Width) >= 1 &&
                target.Position.CountEnemiesInRange(500) >= 2)
                E.Cast();

            if (LuxEGameObject != null && LuxEGameObject.Position.CountEnemiesInRange(E.Width) >= 1 &&
                player.Distance(target.Position) >= Orbwalking.GetRealAutoAttackRange(player))
                E.Cast();

            if (target.HasBuff("luxilluminatingfraulein") && target.HasBuff("LuxLightBindingMis") &&
                player.Distance(target.Position) <= Orbwalking.GetRealAutoAttackRange(player))
                return;

            if (LuxEGameObject != null && LuxEGameObject.Position.CountEnemiesInRange(E.Width) >= 1)
                E.Cast();
        }

        private static void Elogic()
        {
            var target = TargetSelector.GetTarget(E.Range + E.Width, TargetSelector.DamageType.Magical);
            var epred = E.GetPrediction(target);
            var emana = Config.Item("emana").GetValue<Slider>().Value;

            if (LuxEGameObject != null && E.IsReady() && LuxEGameObject.Position.CountEnemiesInRange(E.Width) < 1)
                Utility.DelayAction.Add(2000, () => E.Cast());

            if (target.IsInvulnerable)
                return;

            if (target.HasBuff("luxilluminatingfraulein") && target.HasBuff("LuxLightBindingMis") &&
                player.Distance(target.Position) <= Orbwalking.GetRealAutoAttackRange(player))
                return;

            if (LuxEGameObject != null
                && LuxEGameObject.Position.CountEnemiesInRange(300) >= 1)
                E.Cast();

            if (LuxEGameObject != null
                && target.HasBuffOfType(BuffType.Slow))
                E.Cast();

            if (LuxEGameObject != null)
                return;

            if (player.ManaPercent >= emana && epred.Hitchance >= HitChance.VeryHigh)
                E.Cast(epred.CastPosition);
            else if (player.ManaPercent >= emana && epred.Hitchance >= HitChance.VeryHigh)
                E.Cast(epred.CastPosition);
        }

        private static void Combo()
        {
            Ignite = player.GetSpellSlot("summonerdot");
            var qmana = Config.Item("qmana").GetValue<Slider>().Value;
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (target == null)
                return;

            if (target.IsInvulnerable)
                return;

            var qpred = Q.GetPrediction(target);
            var qcollision = Q.GetCollision(player.ServerPosition.To2D(), new List<Vector2> { qpred.CastPosition.To2D() });
            var minioncol = qcollision.Where(x => !(x is Obj_AI_Hero)).Count(x => x.IsMinion);
            var lichdmg = player.CalcDamage(target, Damage.DamageType.Magical,
                (player.BaseAttackDamage * 0.75) + ((player.BaseAbilityDamage + player.FlatMagicDamageMod) * 0.5));

            if (target.IsValidTarget(Q.Range)
                && minioncol <= 1
                && Q.IsReady()
                && qpred.Hitchance >= HitChance.VeryHigh && player.ManaPercent >= qmana &&
                Config.Item("UseQ").GetValue<bool>())
                Q.Cast(target);

            var passivedmg = player.CalcDamage(target, Damage.DamageType.Magical,
                10 + (8 * player.Level) + 0.2 * player.FlatMagicDamageMod);
            var passiveaa = player.GetAutoAttackDamage(player) + passivedmg;

            if (E.IsReady() && Config.Item("UseE").GetValue<bool>() && target.IsValidTarget(E.Range))
                Elogic();

            if (player.Distance(target.Position) <= 600 && IgniteDamage(target) >= target.Health &&
                player.HealthPercent <= 25 &&
                Config.Item("UseIgnite").GetValue<bool>())
                player.Spellbook.CastSpell(Ignite, target);

            if (player.Distance(target.Position) <= 600 && IgniteDamage(target) + E.GetDamage(target) >= target.Health &&
            player.HealthPercent <= 25 && E.IsReady() && LuxEGameObject == null &&
            Config.Item("UseIgnite").GetValue<bool>())
                player.Spellbook.CastSpell(Ignite, target);


            if (player.Distance(target.Position) <= 600 && IgniteDamage(target) + E.GetDamage(target) >= target.Health &&
            player.HealthPercent <= 25 && E.IsReady() && LuxEGameObject != null && target.Distance(LuxEGameObject.Position) <= LuxEGameObject.BoundingRadius &&
            Config.Item("UseIgnite").GetValue<bool>())
                player.Spellbook.CastSpell(Ignite, target);

            if (player.Distance(target.Position) <= 600 && IgniteDamage(target) + E.GetDamage(target) >= target.Health &&
            player.HealthPercent <= 25 && Q.IsReady() && Q.GetPrediction(target).Hitchance >= HitChance.High &&
            Config.Item("UseIgnite").GetValue<bool>())
                player.Spellbook.CastSpell(Ignite, target);



            if (player.HasBuff("lichbane") && target.Health < lichdmg &&
                player.Distance(target.Position) < Orbwalking.GetRealAutoAttackRange(player))
                return;

            if (LuxEGameObject != null && target.Distance(LuxEGameObject.Position) <= E.Width &&
                target.Health < E.GetDamage(target) ||
                target.HasBuff("LuxLightBindingMis")
                && target.Health < passiveaa && player.Distance(target.Position) <= Orbwalking.GetRealAutoAttackRange(player) &&
                target.HasBuff("luxilluminatingfraulein"))
                return;

            if (player.Distance(target.Position) <= 600 && IgniteDamage(target) >= target.Health &&
                Config.Item("UseIgnite").GetValue<bool>())
                player.Spellbook.CastSpell(Ignite, target);
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    Rlogic();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Laneclear();
                    Jungleclear();
                    break;
                case  Orbwalking.OrbwalkingMode.LastHit:
                    Lasthit();
                    break;
            }
            if (Config.Item("jungleks").GetValue<KeyBind>().Active)
            {
                Junglesteal();
            }
            if (E.IsReady())
            {
                Edetonate();
            }
            if (Config.Item("SmartKS").GetValue<bool>())
            {
                Killsteal();
            }
            if (Config.Item("autoharass").GetValue<KeyBind>().Active)
            {
                Harass();
            }
            if (Config.Item("semir").GetValue<KeyBind>().Active)
            {
                forceR();
            }
            if (Config.Item("AutoQ").GetValue<bool>())
            {
                Autospells();
            }
           // player.SetSkin(player.BaseSkinName, Config.Item("skinhax").GetValue<bool>()
            //? Config.Item("luxskins").GetValue<StringList>().SelectedIndex : player.BaseSkinId);
        }
    }
}
