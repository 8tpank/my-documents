using System;
using System.Media;


namespace MagicalGirlLux.Helpers
{
    public class Soundplayer
    {
        public static void PlaySound(SoundPlayer sound = null)
        {
            if (sound != null)
            {
                try
                {
                    sound.Play();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
