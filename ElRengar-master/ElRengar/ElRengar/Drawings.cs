using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace ElRengar
{
    public class Drawings
    {
        public static void Drawing_OnDraw(EventArgs args)
        {
            if (Rengar.Player.IsDead)
                return;

            var drawOff = ElRengarMenu._menu.Item("ElRengar.Draw.off").GetValue<bool>();
            var drawW = ElRengarMenu._menu.Item("ElRengar.Draw.W").GetValue<Circle>();
            var drawE = ElRengarMenu._menu.Item("ElRengar.Draw.E").GetValue<Circle>();
            var drawR = ElRengarMenu._menu.Item("ElRengar.Draw.R").GetValue<Circle>();

            if (drawOff)
                return;

            if (drawW.Active)
                if (Rengar.spells[Spells.W].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Rengar.spells[Spells.W].Range, Color.White);

            if (drawE.Active)
                if (Rengar.spells[Spells.E].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Rengar.spells[Spells.E].Range, Color.White);

            if (drawR.Active)
                if (Rengar.spells[Spells.R].Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Rengar.spells[Spells.R].Range, Color.White);
        }

        public static void OnDrawEndScene(EventArgs args)
        {
            if (Rengar.Player.IsDead)
                return;

            var drawRminimap = ElRengarMenu._menu.Item("ElRengar.Draw.Minimap").GetValue<bool>();
            if (drawRminimap && Rengar.spells[Spells.R].Level > 0)
            {
                Utility.DrawCircle(ObjectManager.Player.Position, Rengar.spells[Spells.R].Range, Color.White, 1, 23, true);
            }
    
            //Render.Circle.DrawCircle(ObjectManager.Player.Position, Rengar.spells[Spells.R].Range, Color.White, 5, true);
        }
    }
}