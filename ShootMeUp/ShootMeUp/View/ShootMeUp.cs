using Accessibility;
using ShootMeUp.Helpers;
using ShootMeUp.Model;
using ShootMeUp.Properties;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ShootMeUp
{
    /// <summary>
    /// The ShootMeUp class is used to serve as a 2D playspace for the player and enemies.
    /// </summary>
    public partial class ShootMeUp : Form
    {
        /// <summary>
        /// Width of the game area
        /// </summary>
        public static readonly int WIDTH = 1024;

        /// <summary>
        /// Height of the game area
        /// </summary>
        public static readonly int HEIGHT = 1024;

        /// <summary>
        /// The game's speed multiplier for movement, projectiles, etc.)
        /// </summary>
        public static readonly int GAMESPEED = 1;

        /// <summary>
        /// Any obstacle's height and length
        /// </summary>
        public static readonly int OBSTACLE_SIZE = 32;

        /// <summary>
        /// The default size for characters and enemies
        /// </summary>
        public static readonly int DEFAULT_CHARACTER_SIZE = OBSTACLE_SIZE - 8;

        /// <summary>
        /// The number of barrier blocks shown on screen
        /// </summary>
        private static readonly int BORDER_SIZE = 29;

        /// <summary>
        /// The current state of the game
        /// </summary>
        private enum Gamestate
        {
            running, 
            paused,
            finished
        }
        private Gamestate _gamestate;

        /// <summary>
        /// The player
        /// </summary>
        private Character? _player;

        /// <summary>
        /// The list of keys currently held down
        /// </summary>
        private List<Keys> _keysHeldDown;

        /// <summary>
        /// The current wave number
        /// </summary>
        private int _intWaveNumber;

        /// <summary>
        /// The game's title screen
        /// </summary>
        private Label? _titleLabel;

        /// <summary>
        /// The game's play button
        /// </summary>
        private Button? _playButton;

        /// <summary>
        /// A list that contains all the projectiles
        /// </summary>
        private static List<Projectile> _projectiles = new List<Projectile>();
        public static List<Projectile> Projectiles
        {
            get { return _projectiles; }
            set { _projectiles = value; }
        }

        /// <summary>
        /// A list that contains all the obstacles
        /// </summary>
        private static List<Obstacle> _obstacles = new List<Obstacle>();
        public static List<Obstacle> Obstacles
        {
            get { return _obstacles; }
            set { _obstacles = value; }
        }

        /// <summary>
        /// A list that contains all the characters
        /// </summary>
        private static List<Character> _characters = new List<Character>();
        public static List<Character> Characters
        {
            get { return _characters; }
            set { _characters = value; }
        }

        /// <summary>
        /// The player's score
        /// </summary>
        private int _intScore;
        public int Score
        {
            get { return _intScore; }
            set { _intScore = value; }
        }

        public ShootMeUp()
        {
            InitializeComponent();
            ClientSize = new Size(WIDTH, HEIGHT);
            _intWaveNumber = 1;

            // Create a new list of keys held down
            _keysHeldDown = new List<Keys>();

            // run the main method
            Main();
        }

        /// <summary>
        /// the Main method contains all the necessary stuff to run the game
        /// </summary>
        private async void Main()
        {
            // Change the background (TEMP TEST)
            Bitmap resizedImage = new Bitmap(OBSTACLE_SIZE, OBSTACLE_SIZE);
            using (Graphics graphics = Graphics.FromImage(resizedImage))
            {
                graphics.DrawImage(Resources.FloorStone, 0, 0, OBSTACLE_SIZE, OBSTACLE_SIZE);
            }

            BackgroundImage = resizedImage;
            BackgroundImageLayout = ImageLayout.Tile;

            await ShowTitle();
            await StartGame();
        }


        /// <summary>
        /// Shows the game's title screen
        /// </summary>
        private async Task ShowTitle()
        {
            // Remove any previous controls
            Controls.Clear();

            // Create the text and add some style to it
            _titleLabel = new Label
            {
                Text = "Craft Me Up",
                Font = new Font("Consolas", 48, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent,
            };


            // Create and style Play button
            _playButton = new Button
            {
                Text = "Play the game",
                Font = new Font("Consolas", 24, FontStyle.Bold),
                AutoSize = true,
                Size = PreferredSize,
            };

            // Add controls to the form
            Controls.Add(_titleLabel);
            Controls.Add(_playButton);

            _titleLabel.Left = (ClientSize.Width - _titleLabel.Width) / 2;
            _titleLabel.Top = ClientSize.Height / 3 - _titleLabel.Height / 2;

            _playButton.Left = (ClientSize.Width - _playButton.Width) / 2;

            _playButton.Top = _titleLabel.Bottom + 256;

            // Set up the button wait
            TaskCompletionSource<bool> buttonClickedTcs = new TaskCompletionSource<bool>();

            _playButton.Click += (s, e) =>
            {
                buttonClickedTcs.TrySetResult(true);
            };

            await buttonClickedTcs.Task;
        }

        /// <summary>
        /// pauses the game and displays the pause menu
        /// </summary>
        private void DisplayPauseMenu()
        {
            //create a modale and display it
            PictureBox pauseModale = new()
            {
                Top = 0,
                Left = 0,
                Width = this.ClientRectangle.Width,
                Height = this.ClientRectangle.Height,
                BackColor = Color.FromArgb(100, 100, 100, 100)
            };
            Controls.Add(pauseModale);

            //stops the ticker to pause the game
            this.ticker.Stop();
        }

        /// <summary>
        /// Start the game up
        /// </summary>
        private async Task StartGame()
        {
            // Remove title screen controls
            Controls.Remove(_titleLabel);
            Controls.Remove(_playButton);

            //general variables
            Score = 0;
            _intWaveNumber = 1;

            // Generate the world, then start it
            GenerateWorld();

            DisplayControls();

            await StartWaves();
        }

        /// <summary>
        /// Generate the game itself
        /// </summary>
        private void GenerateWorld()
        {
            // Set the game state to true
            _gamestate = Gamestate.running;

            // Calculate the bottom-center, related to the player
            float intLeftBound = 32;
            float intRightBound = (BORDER_SIZE + 4) * 32;

            int intAreaCenterX = (int)((intLeftBound + intRightBound) / 2);

            // Center the character horizontally
            int intCharacterX = intAreaCenterX - (DEFAULT_CHARACTER_SIZE / 2);

            // Create a new player
            _player = new Character(intCharacterX, BORDER_SIZE * 32 + (32 - DEFAULT_CHARACTER_SIZE), DEFAULT_CHARACTER_SIZE, Character.Type.Player, GAMESPEED);
            Characters.Add( _player );

            _player.DisplayedImage.BringToFront();

            // Create a new border, piece by piece
            for (int x = 0; x <= BORDER_SIZE; x++)
                for (int y = 0; y <= BORDER_SIZE; y++)
                    if (x == 0 || x == BORDER_SIZE || y == 0 || y == BORDER_SIZE)
                        Obstacles.Add(new Obstacle(32 * (2 + x), 32 * (2 + y), 32, Obstacle.Type.Border));

            // Create a variable to store the border's size
            int intBorderLength = BORDER_SIZE * 32 + 32;

            //// Creating the world environment ////

            // Top left corner
            const Obstacle.Type BEDROCK = Obstacle.Type.Bedrock;

            Obstacles.Add(new Obstacle(32 * 3, 32 * 3, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new Obstacle(32 * 4, 32 * 3, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new Obstacle(32 * 3, 32 * 4, OBSTACLE_SIZE, BEDROCK));

            // Top right corner
            Obstacles.Add(new Obstacle(intBorderLength, 32 * 3, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new Obstacle(intBorderLength - 32, 32 * 3, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new Obstacle(intBorderLength, 32 * 4, OBSTACLE_SIZE, BEDROCK));

            // Bottom left corner
            Obstacles.Add(new Obstacle(32 * 3, intBorderLength, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new Obstacle(32 * 4, intBorderLength, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new Obstacle(32 * 3, intBorderLength - 32, OBSTACLE_SIZE, BEDROCK));

            // Bottom right corner
            Obstacles.Add(new Obstacle(intBorderLength, intBorderLength, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new Obstacle(intBorderLength - 32, intBorderLength, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new Obstacle(intBorderLength, intBorderLength - 32, OBSTACLE_SIZE, BEDROCK));

            // The pillars' health value
            const Obstacle.Type COBBLE = Obstacle.Type.Stone;

            // Top left pillars
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Obstacles.Add(new Obstacle(192 + (160 * x), 192 + (160 * y), OBSTACLE_SIZE * 2, COBBLE));
                }
            }

            // Top right pillars
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Obstacles.Add(new Obstacle(intBorderLength - 128 - (160 * x), 192 + (160 * y), OBSTACLE_SIZE * 2, COBBLE));
                }
            }

            // Bottom left pillars
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Obstacles.Add(new Obstacle(192 + (160 * x), intBorderLength - 128 - (160 * y), OBSTACLE_SIZE * 2, COBBLE));
                }
            }

            // Bottom right pillars
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Obstacles.Add(new Obstacle(intBorderLength - 128 - (160 * x), intBorderLength - 128 - (160 * y), OBSTACLE_SIZE * 2, COBBLE));
                }
            }


            // The barriers' health
            const Obstacle.Type WOOD = Obstacle.Type.Wood;

            // Top barriers
            for (int x = 0; x < 2; x++)
            {
                Obstacles.Add(new Obstacle(448 + (128 * x), 192, OBSTACLE_SIZE * 2, WOOD));
            }

            Obstacles.Add(new Obstacle(512, 352, OBSTACLE_SIZE * 2, WOOD));

            // Left barriers
            for (int x = 0; x < 2; x++)
            {
                Obstacles.Add(new Obstacle(192, 448 + (128 * x), OBSTACLE_SIZE, WOOD));
            }

            Obstacles.Add(new Obstacle(352, 512, OBSTACLE_SIZE, WOOD));

            // Right barriers
            for (int x = 0; x < 2; x++)
            {
                Obstacles.Add(new Obstacle(intBorderLength - 128, 448 + (128 * x), OBSTACLE_SIZE, WOOD));
            }

            Obstacles.Add(new Obstacle(intBorderLength - 256, 512, OBSTACLE_SIZE, WOOD));

            // Bottom barriers
            for (int x = 0; x < 2; x++)
            {
                Obstacles.Add(new Obstacle(448 + (128 * x), intBorderLength - 128, OBSTACLE_SIZE * 2, WOOD));
            }

            Obstacles.Add(new Obstacle(512, intBorderLength - 256, OBSTACLE_SIZE * 2, WOOD));


            // The smaller obstacles' health
            const Obstacle.Type DIRT = Obstacle.Type.Dirt;

            // Top left small obstacle
            Obstacles.Add(new Obstacle(288, 288, OBSTACLE_SIZE, DIRT));

            // Top right small obstacle
            Obstacles.Add(new Obstacle(intBorderLength - 192, 288, OBSTACLE_SIZE, DIRT));

            // Bottom left small obstacle
            Obstacles.Add(new Obstacle(288, intBorderLength - 192, OBSTACLE_SIZE, DIRT));

            // Bottom right small obstacle
            Obstacles.Add(new Obstacle(intBorderLength - 192, intBorderLength - 192, OBSTACLE_SIZE, DIRT));

            // Middle small obstacles
            Obstacles.Add(new Obstacle(448, 448, OBSTACLE_SIZE, DIRT));
            Obstacles.Add(new Obstacle(intBorderLength - 352, 448, OBSTACLE_SIZE, DIRT));
            Obstacles.Add(new Obstacle(448, intBorderLength - 352, OBSTACLE_SIZE, DIRT));
            Obstacles.Add(new Obstacle(intBorderLength - 352, intBorderLength - 352, OBSTACLE_SIZE, DIRT));
        }

        private List<Enemy> GenerateWaves(int intWaveNumber)
        {
            if (_player == null || _player.Lives <= 0)
                return new List<Enemy>();

            /* ALL THE ENEMIES
            new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player)
            new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Skeleton, GAMESPEED, _player)
            new Enemy(0, 0, (int)(DEFAULT_CHARACTER_SIZE * 0.75), Character.Type.Baby_Zombie, GAMESPEED, _player)
            new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Blaze, GAMESPEED, _player)
            new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie_Pigman, GAMESPEED, _player)
            */

            List<Enemy> WaveEnemies = new List<Enemy>();

            switch (intWaveNumber)
            {
                case 0:
                case 1:
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));

                    break;
                case 2:
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));

                    break;
                case 3:
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Skeleton, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));

                    break;
                case 4:
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Skeleton, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Skeleton, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));

                    break;
                case 5:
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Skeleton, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, (int)(DEFAULT_CHARACTER_SIZE * 0.75), Character.Type.Baby_Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));

                    break;
                case 6:
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, (int)(DEFAULT_CHARACTER_SIZE * 0.75), Character.Type.Baby_Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, (int)(DEFAULT_CHARACTER_SIZE * 0.75), Character.Type.Baby_Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Skeleton, GAMESPEED, _player));

                    break;
                case 7:
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Skeleton, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, (int)(DEFAULT_CHARACTER_SIZE * 0.75), Character.Type.Baby_Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Skeleton, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, (int)(DEFAULT_CHARACTER_SIZE * 0.75), Character.Type.Baby_Zombie, GAMESPEED, _player));

                    break;
                case 8:
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Skeleton, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Blaze, GAMESPEED, _player));

                    break;
                case 9:
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Skeleton, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, (int)(DEFAULT_CHARACTER_SIZE * 0.75), Character.Type.Baby_Zombie, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Blaze, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, (int)(DEFAULT_CHARACTER_SIZE * 0.75), Character.Type.Baby_Zombie, GAMESPEED, _player));

                    break;
                case 10:
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie_Pigman, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie_Pigman, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Zombie_Pigman, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Blaze, GAMESPEED, _player));
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Blaze, GAMESPEED, _player));

                    break;

                default:
                    break;
            }

            return WaveEnemies;
        }

        /// <summary>
        /// Starts the wave system
        /// </summary>
        private async Task StartWaves()
        {
            for (_intWaveNumber = 1; ; _intWaveNumber++)
            {
                // End the wave system if the game stopped
                if (_gamestate != Gamestate.running)
                    return;

                // Add a small wait before starting the current wave
                await Task.Delay(8000 / GAMESPEED);

                foreach (Enemy enemy in GenerateWaves(_intWaveNumber))
                {
                    // End the wave system if the game stopped
                    if (_gamestate != Gamestate.running)
                        return;

                    // Put the enemy in the right spot
                    enemy.DisplayedImage.Location = new Point(512 + OBSTACLE_SIZE/2 + enemy.DisplayedImage.Size.Width / 4, 512 + OBSTACLE_SIZE / 2 + enemy.DisplayedImage.Size.Height / 4);

                    // Add the enemy to the character handler
                    Characters.Add(enemy);

                    // Add it to the controls too
                    Controls.Add(enemy.DisplayedImage);
                    Controls.Add(enemy.HealthLabel);

                    // Add a wait before adding the next enemy
                    await Task.Delay(4000 / GAMESPEED);
                }

                while (Characters.Count != 1)
                {
                    // End the wave system if the game stopped
                    if (_gamestate != Gamestate.running)
                        return;

                    await Task.Delay(25);
                }
            }
        }

        /// <summary>
        /// Tests if the 2 entities are overlapping
        /// </summary>
        /// <param name="entity1"></param>
        /// <param name="entity2"></param>
        /// <returns>A bool that is true if the entities are overlapping</returns>
        public static bool IsOverlapping(CFrame entity1, CFrame entity2)
        {
            bool overlapX = entity1.DisplayedImage.Location.X < entity2.DisplayedImage.Location.X + entity2.DisplayedImage.Width && entity1.DisplayedImage.Location.X + entity1.DisplayedImage.Width > entity2.DisplayedImage.Location.X;
            bool overlapY = entity1.DisplayedImage.Location.Y < entity2.DisplayedImage.Location.Y + entity2.DisplayedImage.Height && entity1.DisplayedImage.Location.Y + entity1.DisplayedImage.Height > entity2.DisplayedImage.Location.Y;
            return overlapX && overlapY;

            //return entity1.DisplayedImage.DisplayRectangle.IntersectsWith(entity2.DisplayedImage.DisplayRectangle);
        }

        private async void NewFrame(object sender, EventArgs e)
        {
            // Update the playspace if the player is in game
            if (_gamestate == Gamestate.running && _player != null)
            {
                Console.WriteLine(_player.Lives);

                // Remove any inactive projectiles/obstacles
                foreach (Projectile projectile in Projectiles.Where(p => !p.Active).ToList())
                {
                    Controls.Remove(projectile.DisplayedImage);
                    projectile.DisplayedImage.Dispose();
                    Projectiles.Remove(projectile);
                }

                foreach (Obstacle obstacle in Obstacles.Where(o => o.Health <= 0).ToList())
                {
                    Controls.Remove(obstacle.DisplayedImage);
                    obstacle.DisplayedImage.Dispose();
                    Obstacles.Remove(obstacle);
                    if (obstacle.HealthLabel != null)
                        obstacle.HealthLabel.Dispose();
                }

                // Change the score if there's a dead enemy
                // Also restart the game if the player died
                foreach (Character character in Characters)
                {
                    if (character.Lives <= 0 && character is Enemy enemy)
                    {
                        Score += enemy.ScoreValue;
                    }
                    else if (character.CharType == Character.Type.Player && character.Lives <= 0)
                    {
                        _gamestate = Gamestate.paused;

                        await ShowTitle();

                        break;
                    }
                }

                foreach (Character character in Characters.Where(c => c.Lives <= 0).ToList())
                {
                    Controls.Remove(character.DisplayedImage);
                    character.DisplayedImage.Dispose();
                    Characters.Remove(character);

                    if (character.HealthLabel != null)
                        character.HealthLabel.Dispose();
                }

                // Show the healthbar
                foreach (var obj in Characters.Concat<CFrame>(Obstacles))
                {
                    if (obj is Character ch && ch.CharType == Character.Type.Player)
                    {
                        // Show the player's health at the top left

                    } else
                    {
                        if (obj.HealthLabel != null)
                        {

                            obj.HealthLabel.Text = obj.ToString();
                            obj.HealthLabel.Location = new Point(
                                obj.DisplayedImage.Left + (obj.DisplayedImage.Width / 2) - (obj.HealthLabel.Width / 2),
                                obj.DisplayedImage.Top - obj.HealthLabel.Height - 2
                            );
                        }
                    }
                }

                // Create movement-related boolean variables
                bool blnLeftHeld = _keysHeldDown.Contains(Keys.A) || _keysHeldDown.Contains(Keys.Left);
                bool blnRightHeld = _keysHeldDown.Contains(Keys.D) || _keysHeldDown.Contains(Keys.Right);
                bool blnUpHeld = _keysHeldDown.Contains(Keys.W) ||      _keysHeldDown.Contains(Keys.Up);
                bool blnDownHeld = _keysHeldDown.Contains(Keys.S) || _keysHeldDown.Contains(Keys.Down);

                // Create movement-related int variables
                int intMoveX = 0;
                int intMoveY = 0;

                // Increment/decrement the movement-related int variables based off of the boolean variables
                if (blnLeftHeld)
                {
                    intMoveX -= 1;
                }

                if (blnRightHeld)
                {
                    intMoveX += 1;
                }

                if (blnUpHeld)
                {
                    intMoveY -= 1;
                }

                if (blnDownHeld)
                {
                    intMoveY += 1;
                }

                // Multiple the movement-related int variables by the game speed
                intMoveX *= GAMESPEED;
                intMoveY *= GAMESPEED;

                // Move the player
                _player.Move(intMoveX, intMoveY);

                // Update the projectiles
                foreach (Projectile projectile in Projectiles)
                {
                    projectile.Update();
                }

                // Update the enemies
                foreach (Character character in Characters)
                {
                    if (character.CharType != Character.Type.Player && character is Enemy enemy)
                        enemy.Move();
                }


            }
        }

        private void ShootMeUp_KeyDown(object sender, KeyEventArgs e)
        {
            // Add the key to the list if it's not already in there
            if (!_keysHeldDown.Contains(e.KeyCode))
            {
                _keysHeldDown.Add(e.KeyCode);
            }
        }

        private void ShootMeUp_KeyUp(object sender, KeyEventArgs e)
        {
            // Remove the key from the list if it's in there
            if (_keysHeldDown.Contains(e.KeyCode))
            {
                _keysHeldDown.Remove(e.KeyCode);
            }
        }

        private void ShootMeUp_MouseClick(object sender, MouseEventArgs e)
        {
            // Only try and shoot something if the player is in game
            if (_gamestate == Gamestate.running && _player != null)
            {
                //default is arrow
                Projectile.Type type = Projectile.Type.Arrow;

                // If it's a left click, strType is "arrow". if its a right click, strType is "fireball".
                if (e.Button == MouseButtons.Left)
                    type = Projectile.Type.Arrow;
                else if (e.Button == MouseButtons.Right)
                    type = Projectile.Type.Fireball;

                // Shoot an arrow using the player's shoot method and add it to the projectile list
                Projectile? possibleProjectile = _player.Shoot(this.PointToClient(Cursor.Position), type);

                if (possibleProjectile != null)
                {
                    Projectiles.Add(possibleProjectile);
                    Controls.Add(possibleProjectile.DisplayedImage);
                }
            }
        }

        private void DisplayControls()
        {
            //displays the projectiles
            foreach (Projectile projectile in Projectiles)
            {
                Controls.Add(projectile.DisplayedImage);
            }
            //displays the characters
            foreach (Character character in Characters)
            {
                Controls.Add(character.DisplayedImage);
            }
            //displays the obstacles
            foreach (Obstacle obstacle in Obstacles)
            {
                Controls.Add(obstacle.DisplayedImage);
                Controls.Add(obstacle.HealthLabel);

            }
        }
    }
}