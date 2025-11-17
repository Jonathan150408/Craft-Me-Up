using ShootMeUp.Properties;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace ShootMeUp.Model
{
    public class Projectile : CFrame
    {
        private Type _Type;

        /// <summary>
        /// The character that shot the projectile
        /// </summary>
        private Character _shotBy;

        /// <summary>
        /// The amount of damage the projectile deals (in HP)
        /// </summary>
        private int _intDamage;

        /// <summary>
        /// The rotation angle (in degrees)
        /// </summary>
        private float _fltRotationAngle;

        /// <summary>
        /// The projectile's movement speed
        /// </summary>
        private int _intMovementSpeed;

        /// <summary>
        ///  The projectile's speed in the X and Y axis
        /// </summary>
        private (int X, int Y) _intSpeed;

        /// <summary>
        /// The X and Y position of the target
        /// </summary>
        private (int X, int Y) _intTarget;

        /// <summary>
        /// The projectile's type (arrow, ...)
        /// </summary>
        public enum Type
        {
            Arrow,
            Fireball,
        }

        /// <summary>
        /// Whether or not the projectile is active or not
        /// </summary>
        public bool Active { get; set; }

        public Projectile(Type type, Character ShotBy, int intTargetX, int intTargetY, int GAMESPEED) : base(ShotBy.DisplayedImage.Location.X, ShotBy.DisplayedImage.Location.Y)
        {
            _Type = type;
            _shotBy = ShotBy;
            _intTarget.X = intTargetX;
            _intTarget.Y = intTargetY;

            Active = true;

            // Define the different properties depending on the projectile type
            switch (type)
            {
                case Type.Arrow:
                    DisplayedImage.Width = ShotBy.DisplayedImage.Width/2;
                    DisplayedImage.Height = ShotBy.DisplayedImage.Height/2;
                    DisplayedImage.Image = Resources.ProjectileArrow;

                    _intDamage = 1;
                    _intMovementSpeed = 3;

                    break;
                case Type.Fireball:
                    DisplayedImage.Width = ShotBy.DisplayedImage.Width;
                    DisplayedImage.Height = ShotBy.DisplayedImage.Height;
                    DisplayedImage.Image = Resources.ProjectileFireball;

                    _intDamage = 3;
                    _intMovementSpeed = 1;

                    break;
                default:
                    DisplayedImage.Width = ShotBy.DisplayedImage.Width;
                    DisplayedImage.Height = ShotBy.DisplayedImage.Height;
                    DisplayedImage.Image = Resources.CharacterPlayer;

                    _intDamage = 0;
                    _intMovementSpeed = 0;
                    break;
            }


            // Multiply the movement speed by the game speed
            _intMovementSpeed *= GAMESPEED;

            // Calculate direction to target
            float deltaX = _intTarget.X - DisplayedImage.Location.X;
            float deltaY = _intTarget.Y - DisplayedImage.Location.Y;

            // Calculate rotation angle in degrees
            // We add 90 here because the image faces upwards
            _fltRotationAngle = (float)(Math.Atan2(deltaY, deltaX) * (180.0 / Math.PI)) + 90;

            // Normalize direction
            float length = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (length != 0)
            {
                deltaX /= length;
                deltaY /= length;
            }

            // Store movement speed in X/Y components
            _intSpeed.X = (int)(deltaX * _intMovementSpeed);
            _intSpeed.Y = (int)(deltaY * _intMovementSpeed);
        }

        /// <summary>
        /// Update the projectile's position
        /// </summary>
        public void Update()
        {
            // Get the current CFrame
            CFrame currentCFrame = (CFrame)this;

            // Check to see if the projectile is gonna clip in anything
            bool blnColliding;

            blnColliding = CheckCollisions();

            // Move the arrow if it wouldn't hit anything
            if (!blnColliding)
            {
                DisplayedImage.Location = new Point(DisplayedImage.Location.X + _intSpeed.X, DisplayedImage.Location.Y + _intSpeed.Y);
            }
            else
            {
                // Mark the projectile as inactive
                Active = false;

                // Get the object and/or character that's been hit
                CFrame? Hit = GetColliding();

                if (Hit != null)
                {
                    if (Hit is Character)
                    {
                        Character characterHit = (Character)Hit;

                        // Deal damage to the character
                        characterHit.Lives -= _intDamage;
                    }
                    else
                    {
                        Obstacle obstacleHit = (Obstacle)Hit;

                        // Deal damage to the obstacle
                        obstacleHit.Health -= _intDamage;


                    }
                }
            }
        }

        /// <summary>
        /// Check to see if the projectile has hit anything
        /// </summary>
        /// <returns>true or false depending on if it hit anything or not</returns>
        public bool CheckCollisions()
        {
            bool blnColliding = false;

            // Create hypothetical CFrames to simulate movement along each axis independently
            CFrame cfrX = new CFrame(DisplayedImage.Location.X + _intSpeed.X, DisplayedImage.Location.Y, DisplayedImage.Width, DisplayedImage.Height);
            CFrame cfrY = new CFrame(DisplayedImage.Location.X, DisplayedImage.Location.Y + _intSpeed.Y, DisplayedImage.Width, DisplayedImage.Height);

            // Create a list that contains both obstacles and characters
            List<CFrame> listCFrames = new List<CFrame>();
            listCFrames = ShootMeUp.Characters.Cast<CFrame>().ToList();
            listCFrames.AddRange(ShootMeUp.Obstacles.Cast<CFrame>().ToList());

            // Check for collision
            foreach (CFrame singularCFrame in listCFrames)
            {
                // Check to see if the current CFrame is an obstacle
                if (singularCFrame is Obstacle)
                {
                    Obstacle obstacle = (Obstacle)singularCFrame;

                    // Skip the current obstacle if it has no collisions
                    if (!obstacle.CanCollide)
                        continue;
                }

                if (ShootMeUp.IsOverlapping(cfrX, singularCFrame))
                {
                    blnColliding = true;
                }

                if (ShootMeUp.IsOverlapping(cfrY, singularCFrame))
                {
                    blnColliding = true;
                }

                // Early exit if collision detected
                if (blnColliding)
                    break;
            }

            return blnColliding;
        }

        public CFrame? GetColliding()
        {
            // Create hypothetical CFrames to simulate movement along each axis independently
            CFrame cfrX = new CFrame(DisplayedImage.Location.X + _intSpeed.X, DisplayedImage.Location.Y, DisplayedImage.Width, DisplayedImage.Height);
            CFrame cfrY = new CFrame(DisplayedImage.Location.X, DisplayedImage.Location.Y + _intSpeed.Y, DisplayedImage.Width, DisplayedImage.Height);

            // Create a list that contains both obstacles and characters
            List<CFrame> listCFrames = new List<CFrame>();
            listCFrames = ShootMeUp.Characters.Cast<CFrame>().ToList();
            listCFrames.AddRange(ShootMeUp.Obstacles.Cast<CFrame>().ToList());

            foreach (CFrame singularCFrame in listCFrames)
            {
                // Check to see if the current CFrame is a character
                if (singularCFrame is Character)
                {
                    Character character = (Character)singularCFrame;

                    // Skip the ignored character
                    if (character == _shotBy)
                        continue;
                }


                // Check to see if the current CFrame is an obstacle
                if (singularCFrame is Obstacle)
                {
                    Obstacle obstacle = (Obstacle)singularCFrame;

                    // Skip the current obstacle if it has no collisions
                    if (!obstacle.CanCollide)
                        continue;
                }


                if (ShootMeUp.IsOverlapping(cfrX, singularCFrame))
                {
                    return singularCFrame;
                }

                if (ShootMeUp.IsOverlapping(cfrY, singularCFrame))
                {
                    return singularCFrame;
                }
            }

            return null;
        }
    }
}