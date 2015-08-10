using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;


namespace ElRengar
{

    public class ElRengarMenu
    {
        public static Menu _menu;
       

        public static void Initialize()
        {
            _menu = new Menu("ElRengar", "menu", true);

            _menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            _menu.SubMenu("Orbwalker").AddItem(new MenuItem("SelectedOrbwalker", "Orbwalker").SetValue(new StringList(new[] { "Custom Orbwalker", "Default common orbwalker" })));

            if (_menu.SubMenu("Orbwalker").Item("SelectedOrbwalker").GetValue<StringList>().SelectedIndex == 1)
            {
                Rengar.orbwalker = new Orbwalking.Orbwalker(_menu.SubMenu("Orbwalker"));
                Rengar.UsingLxOrbwalker = false;
            }
            else if (_menu.SubMenu("Orbwalker").Item("SelectedOrbwalker").GetValue<StringList>().SelectedIndex == 0)
            {
                Rengar.orbwalker = null;
                LXOrbwalker.AddToMenu(_menu.SubMenu("Orbwalker"));
                Rengar.UsingLxOrbwalker = true;
            }

            //ElRengar.Orbwalker
            /* var orbwalkerMenu = new Menu("Orbwalker", "orbwalker");
             Rengar.Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            _menu.AddSubMenu(orbwalkerMenu);*/

            /*var orbwalkerMenu = new Menu("LX Orbwalker", "my_Orbwalker");
            LXOrbwalker.AddToMenu(orbwalkerMenu);
            _menu.AddSubMenu(orbwalkerMenu);*/

            //ElRengar.TargetSelector
            var targetSelector = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelector);
            _menu.AddSubMenu(targetSelector);

            //ElRengar.Menu
            var comboMenu = _menu.AddSubMenu(new LeagueSharp.Common.Menu("Combo", "Combo"));
            comboMenu.AddItem(new MenuItem("ElRengar.Combo.Q", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("ElRengar.Combo.W", "Use W").SetValue(true));
            comboMenu.AddItem(new MenuItem("ElRengar.Combo.E", "Use E").SetValue(true));

            comboMenu.AddItem(new MenuItem("ElRengar.Combo.EOOR", "Use E when out of range").SetValue(true));
            //comboMenu.AddItem(new MenuItem("ElRengar.Combo.focusSelected", "Focus selected target").SetValue(false));
            comboMenu.AddItem(new MenuItem("ElRengar.Combo.separator", ""));
            comboMenu.AddItem(new MenuItem("ElRengar.Combo.Ignite", "Use Ignite").SetValue(true));
            comboMenu.AddItem(new MenuItem("ElRengar.Combo.Smite", "Use Smite").SetValue(true));
            comboMenu.AddItem(new MenuItem("ElRengar.Combo.separator.one", ""));
            comboMenu.AddItem(new MenuItem("ElRengar.Combo.Switch", "Switch prioritize").SetValue(new KeyBind("L".ToCharArray()[0], KeyBindType.Press)));
            //comboMenu.AddItem(new MenuItem("ElRengar.Combo.Triple.Q", "Triple Q").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));

            comboMenu.AddItem(new MenuItem("ElRengar.Combo.Prio", "Prioritize").SetValue(new StringList(new[] { "E", "W", "Q" }, 2)));

            //ElRengar.Harass
            var harassMenu = _menu.AddSubMenu(new Menu("Harass", "Harass"));
            harassMenu.AddItem(new MenuItem("ElRengar.Harass.Q", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("ElRengar.Harass.W", "Use W").SetValue(true));
            harassMenu.AddItem(new MenuItem("ElRengar.Harass.E", "Use E").SetValue(true));
            harassMenu.AddItem(new MenuItem("ElRengar.Harass.Prio", "Prioritize").SetValue(new StringList(new[] { "E", "W", "Q" }, 2)));

            //ElRengar.Items
            comboMenu.SubMenu("Items").AddItem(new MenuItem("ElRengar.Combo.Tiamat", "Use Tiamat").SetValue(true));
            comboMenu.SubMenu("Items").AddItem(new MenuItem("ElRengar.Combo.Hydra", "Use Ravenous Hydra").SetValue(true));
            comboMenu.SubMenu("Items").AddItem(new MenuItem("ElRengar.Combo.Youmuu", "Use Youmuu's Ghostblade").SetValue(true));
            comboMenu.SubMenu("Items").AddItem(new MenuItem("ElRengar.Combo.Cutlass", "Use Bilgewater Cutlass").SetValue(true));
            comboMenu.SubMenu("Items").AddItem(new MenuItem("ElRengar.Combo.Blade", "Use Blade of the Ruined King").SetValue(true));

            //ElRengar.Jungleclear
            var clearMenu = _menu.AddSubMenu(new Menu("Clear", "JC"));

            clearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("ElRengar.Clear.Prio", "Prioritize").SetValue(new StringList(new[] { "E", "W", "Q" }, 2)));
            clearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("ElRengar.Clear.Q", "Use Q").SetValue(true));
            clearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("ElRengar.Clear.W", "Use W").SetValue(true));
            clearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("ElRengar.Clear.E", "Use E").SetValue(true));
            clearMenu.SubMenu("Jungleclear").AddItem(new MenuItem("ElRengar.Clear.Save", "Save Ferocity").SetValue(false));

            //ElRengar.Laneclear
            clearMenu.SubMenu("Laneclear").AddItem(new MenuItem("ElRengar.LaneClear.Prio", "Prioritize").SetValue(new StringList(new[] { "E", "W", "Q" }, 2)));
            clearMenu.SubMenu("Laneclear").AddItem(new MenuItem("ElRengar.LaneClear.Q", "Use Q").SetValue(true));
            clearMenu.SubMenu("Laneclear").AddItem(new MenuItem("ElRengar.LaneClear.W", "Use W").SetValue(true));
            clearMenu.SubMenu("Laneclear").AddItem(new MenuItem("ElRengar.LaneClear.E", "Use E").SetValue(true));
            clearMenu.SubMenu("Laneclear").AddItem(new MenuItem("ElRengar.LaneClear.Save", "Save Ferocity").SetValue(false));

            //ElRengar.Clear.Item
            clearMenu.SubMenu("Items").AddItem(new MenuItem("ElRengar.LaneClear.Hydra", "Use Ravenous Hydra").SetValue(true));

            //ElRengar.Healing
            var healMenu = _menu.AddSubMenu(new Menu("Heal", "SH"));
            healMenu.AddItem(new MenuItem("ElRengar.Heal.AutoHeal", "Auto heal yourself").SetValue(true));
            healMenu.AddItem(new MenuItem("ElRengar.Heal.HP", "Self heal at >= ").SetValue(new Slider(25, 1, 100)));

            var notificationsMenu = _menu.AddSubMenu(new Menu("Notification settings", "Notifications"));
            notificationsMenu.AddItem(new MenuItem("ElRengar.Notifications.Active", "Permashow prioritized spell").SetValue(true));
            notificationsMenu.AddItem(new MenuItem("ElRengar.Notifications.selected", "Notifications for selected target").SetValue(true));


            //ElRengar.Misc
            var miscMenu = _menu.AddSubMenu(new Menu("Misc", "Misc"));
            miscMenu.AddItem(new MenuItem("ElRengar.Draw.off", "Turn all drawings off").SetValue(false));
            miscMenu.AddItem(new MenuItem("ElRengar.Draw.W", "Draw W").SetValue(new Circle()));
            miscMenu.AddItem(new MenuItem("ElRengar.Draw.E", "Draw E").SetValue(new Circle()));
            miscMenu.AddItem(new MenuItem("ElRengar.Draw.R", "Draw R").SetValue(new Circle()));
            miscMenu.AddItem(new MenuItem("ElRengar.Draw.Minimap", "Draw R on minimap").SetValue(true));

            //Here comes the moneyyy, money, money, moneyyyy
            var credits = _menu.AddSubMenu(new Menu("Credits", "jQuery"));
            credits.AddItem(new MenuItem("ElRengar.Paypal", "if you would like to donate via paypal:"));
            credits.AddItem(new MenuItem("ElRengar.Email", "info@zavox.nl"));

            _menu.AddItem(new MenuItem("422442fsaafs4242f", ""));
            _menu.AddItem(new MenuItem("422442fsaafsf", String.Format("Version: {0}", Rengar.ScriptVersion)));
            _menu.AddItem(new MenuItem("fsasfafsfsafsa", "Made By jQuery"));

            _menu.AddToMainMenu();

            Console.WriteLine("Menu Loaded");
        }
    }
}