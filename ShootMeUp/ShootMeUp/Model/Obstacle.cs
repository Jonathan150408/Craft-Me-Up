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
        public int Health { get; set; }

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
        /// <param name="type">The obstacle's type (Bush, Border, ...)</param>
        public Obstacle(float x, float y, int intLength, Obstacle.Type type) : base(x, y, intLength)
        {

            switch (type)
            {
                case Type.Bush:
                    Image = Resources.ObstacleBush;

                    _canCollide = false;
                    Health = int.MaxValue;
                    break;
                case Type.Bedrock:
                    Image = Resources.ObstacleUnbreakable;

                    _invincible = true;
                    Health = int.MaxValue;
                    break;
                case Type.Border:
                    Image = Resources.ObstacleBorder;

                    _invincible = true;
                    Health = int.MaxValue;
                    break;
                case Type.Spawner:
                    Image = Resources.ObstacleSpawner;

                    _invincible = true;
                    Health = int.MaxValue;
                    _canCollide = false;
                    break;
                case Type.Dirt:
                    Image = Resources.ObstacleWeak;

                    Health = 5;
                    break;
                case Type.Wood:
                    Image = Resources.ObstacleNormal;

                    Health = 10;
                    break;
                case Type.Stone:
                    Image = Resources.ObstacleStrong;

                    Health = 25;
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

            if (Health > 0)
                return $"{Health} HP";
            else
                return "";
        }
    }
}
