///ETML
///10.11.2025
///This is the obstacle class
using ShootMeUp.Helpers;
using ShootMeUp.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShootMeUp.Model
{
    /// <summary>
    /// A basic obstacle. you can give it a size, position, and health (if none then it's invincible).
    /// </summary>
    public class Obstacle : CFrame
    {
        /// <summary>
        /// The obstacle's max health
        /// </summary>
        private int _intMaxHealth;

        /// <summary>
        /// The obstacle's type (border, ...)
        /// </summary>
        private enum Type
        {
            Dirt,
            Wood,
            Stone,
            Spawner,
            Border,
            Bedrock,
            Bush
        }
        private Type _type;

        /// <summary>
        /// The obstacle's health (set to int.MaxValue if invincible)
        /// </summary>
        private int _health;
        public int Health
        {
            get { return _health; }
            set { _health = value; }
        }

        /// <summary>
        /// Whether the obstacle has collisions or not
        /// </summary>
        private bool _canCollide;
        public bool CanCollide
        {
            get { return _canCollide; }
        }

        /// <summary>
        /// Whether the obstacle is invincible or not
        /// </summary>
        private bool _invincible;
        public bool Invincible
        {
            get { return _invincible; }
        }

        /// <summary>
        /// The obstacle constructor
        /// </summary>
        /// <param name="x">The obstacle's X pos</param>
        /// <param name="y">The obstacle's Y pos</param>
        /// <param name="intLength">The obstacle's Length</param>
        /// <param name="intHealth">The obstacle's max health</param>
        public Obstacle(int x, int y, int intLength, int intHealth) : base(x, y, intLength)
        {
            //default values
            _intMaxHealth = _health;
            _canCollide = true;
            _invincible = false;

            switch (intHealth)
            {
                case -3:
                    _type = Type.Bush;
                    _canCollide = false;
                    _health = int.MaxValue;
                    break;
                case -2:
                    _type = Type.Bedrock;
                    _invincible = true;
                    _health = int.MaxValue;
                    break;
                case -1 :
                    _type = Type.Border;
                    _invincible = true;
                    _health = int.MaxValue;
                    break;
                case 0:
                    _type = Type.Spawner;
                    _canCollide = false;
                    break;
                case 5:
                    _type = Type.Dirt;
                    break;
                case 10:
                    _type = Type.Wood;
                    break;
                case 25:
                    _type = Type.Stone;
                    break;
            }
        }

        /// <summary>
        /// Permit to get the dispayed status
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_invincible)
            {
                return "";
            }

            if (_health > 0)
                return $"{((int)((double)_health)).ToString()} HP";
            else
                return "";
        }
    }
}
