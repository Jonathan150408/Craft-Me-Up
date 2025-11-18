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
        /// The obstacle's type (border, ...)
        /// </summary>
        public enum Type
        {
            Dirt,
            Wood,
            Stone,
            Spawner,
            Border,
            Bedrock,
            Bush,
            Undefined
        }

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
        public Obstacle(int x, int y, int intLength, Obstacle.Type type) : base(x, y, intLength)
        {
            //default values
            _canCollide = true;
            _invincible = false;

            HealthLabel = new Label();
            HealthLabel.AutoSize = true;
            HealthLabel.BackColor = Color.Transparent;
            HealthLabel.ForeColor = Color.White;
            HealthLabel.Font = new Font("Arial", 10, FontStyle.Bold);

            switch (type)
            {
                case Type.Bush:
                    DisplayedImage.Image = Resources.ObstacleBush;

                    _canCollide = false;
                    _health = int.MaxValue;
                    break;
                case Type.Bedrock:
                    DisplayedImage.Image = Resources.ObstacleUnbreakable;

                    _invincible = true;
                    _health = int.MaxValue;
                    break;
                case Type.Border:
                    DisplayedImage.Image = Resources.ObstacleBorder;

                    _invincible = true;
                    _health = int.MaxValue;
                    break;
                case Type.Spawner:
                    DisplayedImage.Image = Resources.ObstacleSpawner;

                    _invincible = true;
                    _health = int.MaxValue;
                    _canCollide = false;
                    break;
                case Type.Dirt:
                    DisplayedImage.Image = Resources.ObstacleWeak;

                    _health = 5;
                    break;
                case Type.Wood:
                    DisplayedImage.Image = Resources.ObstacleNormal;

                    _health = 10;
                    break;
                case Type.Stone:
                    DisplayedImage.Image = Resources.ObstacleStrong;

                    _health = 25;
                    break;
                default:
                    _invincible = true;
                    _canCollide = false;
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
