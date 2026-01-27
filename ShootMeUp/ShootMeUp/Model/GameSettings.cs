using System;
using System.Collections.Generic;
using System.Text;

namespace ShootMeUp.Model
{
    public class GameSettings
    {
        public enum GameSpeedOption
        {
            Slow,
            Normal,
            Fast
        }

        public static GameSpeedOption GameSpeed { get; set; } = GameSpeedOption.Normal;

        public static int GameSpeedValue =>
            GameSpeed switch
            {
                GameSpeedOption.Slow => 128,
                GameSpeedOption.Normal => 256,
                GameSpeedOption.Fast => 512,
                _ => 256
            };

    }
}
