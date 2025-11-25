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
        /// A random number generator
        /// </summary>
        private readonly Random rng;

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



        private int _intCleanupCounter;

        private static readonly Brush BackgroundBrush = new SolidBrush(Color.FromArgb(217, 217, 217));

        private Bitmap backBuffer;
        private Graphics bufferG;

        public static float cameraX { get; private set; }
        public static float cameraY { get; private set; }



        public ShootMeUp()
        {
            InitializeComponent();

            ClientSize = new Size(WIDTH, HEIGHT);
            _intWaveNumber = 1;
            _gamestate = Gamestate.paused;

            // Create a new list of keys held down
            _keysHeldDown = new List<Keys>();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
              ControlStyles.OptimizedDoubleBuffer |
              ControlStyles.UserPaint, true);
            this.DoubleBuffered = true;

            rng = new();

            cameraX = 0;
            cameraY = 0;

            backBuffer = new Bitmap(WIDTH, HEIGHT);
            bufferG = Graphics.FromImage(backBuffer);
            bufferG.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            bufferG.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            // Change the background (TEMP TEST)
            Bitmap resizedImage = new Bitmap(OBSTACLE_SIZE, OBSTACLE_SIZE);
            using (Graphics graphics = Graphics.FromImage(resizedImage))
            {
                graphics.DrawImage(Resources.FloorStone, 0, 0, OBSTACLE_SIZE, OBSTACLE_SIZE);
            }

            BackgroundImage = resizedImage;
            BackgroundImageLayout = ImageLayout.Tile;

            ShowTitle();
        }

        /// <summary>
        /// Shows the game's title screen
        /// </summary>
        private void ShowTitle()
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
            _playButton.Click += _playButton_Click;
        }

        private async void _playButton_Click(object? sender, EventArgs e)
        {
            StartGame();
            await StartWaves();
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
        private void StartGame()
        {
            // Remove title screen controls
            Controls.Remove(_titleLabel);
            Controls.Remove(_playButton);

            // Reset values
            Score = 0;
            _intWaveNumber = 1;

            Characters.Clear();
            Obstacles.Clear();
            Projectiles.Clear();

            // Generate the world, then start it
            GenerateWorld();
        }

        /// <summary>
        /// Generate the game itself
        /// </summary>
        private void GenerateWorld()
        {
            // Set the game state to true
            _gamestate = Gamestate.running;

            // Create a new player
            _player = new Character(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Player, GAMESPEED);
            Characters.Add(_player);


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


            List<Enemy> WaveEnemies = new List<Enemy>();

            // Get a random number of total enemies (need to change this to use math algorythm that balance the total enemies count later)
            int totalEnemies = Math.Min(intWaveNumber * 2, 50);

            // Get the enemies
            for (int i = 0; i < totalEnemies; i++)
            {
                // Create new variables for enemy generation
                int intCharSize = DEFAULT_CHARACTER_SIZE;
                Character.Type enemyType;

                // Get a number from 1 to 5 (inclusive)
                int intRandom = rng.Next(1, 6);

                switch (intRandom)
                {
                    case 1:
                        enemyType = Character.Type.Zombie;
                        break;
                    case 2:
                        enemyType = Character.Type.Skeleton;
                        break;
                    case 3:
                        enemyType = Character.Type.Blaze;
                        break;
                    case 4:
                        enemyType = Character.Type.Baby_Zombie;
                        intCharSize = (int)(intCharSize* 0.75);
                        break;
                    case 5:
                        enemyType = Character.Type.Zombie_Pigman;
                        break;
                    default:
                        enemyType = Character.Type.Zombie;
                        break;
                }

                WaveEnemies.Add(new(0, 0, intCharSize, enemyType, GAMESPEED, _player));
            }

            return WaveEnemies;
        }

        /// <summary>
        /// Starts the wave system
        /// </summary>
        private async Task StartWaves()
        {
            while (_gamestate == Gamestate.running && (_player != null && _player.Lives > 0))
            { 
                // Get the wave's enemies
                List<Enemy> waveEnemies = GenerateWaves(_intWaveNumber);

                foreach (Enemy enemy in waveEnemies)
                {
                    // End the wave system if the game stopped
                    if (_gamestate != Gamestate.running)
                        return;

                    // Put the enemy in the right spot
                    enemy.Position = (512 + OBSTACLE_SIZE / 2 + enemy.Size.Width / 4, 512 + OBSTACLE_SIZE / 2 + enemy.Size.Height / 4);

                    // Add the enemy to the character list
                    Characters.Add(enemy);

                    // Add a wait before adding the next enemy
                    await Task.Delay(4000 / GAMESPEED);
                }

                // Clear the wave enemies table
                waveEnemies.Clear();

                // Wait until all enemies are dead
                while (Characters.Count > 1 && _gamestate == Gamestate.running)
                    await Task.Delay(25);

                // Increment the wave number
                _intWaveNumber++;
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
            bool overlapX = entity1.Position.X < entity2.Position.X + entity2.Size.Width && entity1.Position.X + entity1.Size.Width > entity2.Position.X;
            bool overlapY = entity1.Position.Y < entity2.Position.Y + entity2.Size.Height && entity1.Position.Y + entity1.Size.Height > entity2.Position.Y;
            Console.WriteLine(overlapX && overlapY);
            return overlapX && overlapY;
        }

        /// <summary>
        /// Render a new frame
        /// </summary>
        private void RenderFrame()
        {
            if (_gamestate == Gamestate.running && _player != null)
            {
                // Clear the frame
                bufferG.Clear(Color.FromArgb(217, 217, 217));

                // Draw all characters (not including player)
                foreach (var character in Characters)
                {
                    if (character.CharType == Character.Type.Player) continue;

                    float drawX = character.Position.X - cameraX;
                    float drawY = character.Position.Y - cameraY;

                    bufferG.DrawImage(character.Image,
                                drawX,
                                drawY,
                                character.Size.Width,
                                character.Size.Height);
                }

                // Draw obstacles
                foreach (var obstacle in Obstacles)
                {
                    float drawX = obstacle.Position.X - cameraX;
                    float drawY = obstacle.Position.Y - cameraY;

                    bufferG.DrawImage(obstacle.Image,
                                drawX,
                                drawY,
                                obstacle.Size.Width,
                                obstacle.Size.Height);
                }

                // Draw projectiles
                foreach (var projectile in Projectiles)
                {
                    float drawX = projectile.Position.X - cameraX;
                    float drawY = projectile.Position.Y - cameraY;

                    bufferG.DrawImage(projectile.Image,
                                drawX,
                                drawY,
                                projectile.Size.Width,
                                projectile.Size.Height);
                }

                // Draw the player along with their lives and the game's statistics
                if (_player != null)
                {
                    float px = _player.Position.X - cameraX;
                    float py = _player.Position.Y - cameraY;

                    bufferG.DrawImage(_player.Image, px, py, _player.Size.Width, _player.Size.Height);

                    bufferG.DrawString($"Wave {_intWaveNumber} | Score: {Score} | Lives remaining: {_player.Lives}", TextHelpers.drawFont, TextHelpers.writingBrush, 8, 8);

                    for (int i = 0; i < _player.Lives; i++)
                    {
                        int x = 8 + (i * 24);
                        bufferG.DrawImage(Resources.CharacterPlayer, x, 32, 16, 16);
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (backBuffer != null)
                e.Graphics.DrawImageUnscaled(backBuffer, 0, 0);
        }

        /// <summary>
        /// Clean up dead entities, broken obstacles, etc.
        /// </summary>
        private void CleanupEntities()
        {
            _intCleanupCounter++;

            // Only do this once the counter reaches 5 frames
            if (_intCleanupCounter < 5) return;

            _intCleanupCounter = 0;

            // Remove anything inactive
            Projectiles.RemoveAll(p =>
            {
                if (!p.Active)
                {
                    p.Image?.Dispose();
                    return true;
                }
                return false;
            });

            Obstacles.RemoveAll(o =>
            {
                if (o.Health <= 0)
                {
                    o.Image?.Dispose();
                    return true;
                }
                return false;
            });

            // Change the score if there's a dead enemy
            Characters.RemoveAll(c =>
            {
                if (c.Lives <= 0 && c is Enemy enemy)
                {
                    Score += enemy.ScoreValue;
                    enemy.Image?.Dispose();
                    return true;
                }
                else if (c.CharType == Character.Type.Player && c.Lives <= 0)
                {
                    _gamestate = Gamestate.paused;
                    ShowTitle();
                    c.Image?.Dispose();
                    return true;
                }
                return false;
            });
        }

        private void NewFrame(object sender, EventArgs e)
        {
            // Update the playspace if the player is in game
            if (_gamestate == Gamestate.running && _player != null)
            {
                // Update camera position to the player's
                cameraX = _player.Position.X - (Size.Width / 2);
                cameraY = _player.Position.Y - (Size.Height / 2);

                // Create a new frame
                RenderFrame();

                // Tell the program to render the game again
                Invalidate();

                // Attempt to clean up the dead entities
                _intCleanupCounter++;
                CleanupEntities();

                // Create movement-related boolean variables
                bool blnLeftHeld = _keysHeldDown.Contains(Keys.A) || _keysHeldDown.Contains(Keys.Left);
                bool blnRightHeld = _keysHeldDown.Contains(Keys.D) || _keysHeldDown.Contains(Keys.Right);
                bool blnUpHeld = _keysHeldDown.Contains(Keys.W) || _keysHeldDown.Contains(Keys.Up);
                bool blnDownHeld = _keysHeldDown.Contains(Keys.S) || _keysHeldDown.Contains(Keys.Down);

                // Create movement-related int variables
                int intMoveX = 0;
                int intMoveY = 0;

                // Increment/decrement the movement-related int variables based off of the boolean variables
                if (blnLeftHeld)
                    intMoveX -= 1;
                if (blnRightHeld)
                    intMoveX += 1;
                if (blnUpHeld)
                    intMoveY -= 1;
                if (blnDownHeld)
                    intMoveY += 1;

                // Multiple the movement-related int variables by the game speed
                intMoveX *= GAMESPEED;
                intMoveY *= GAMESPEED;


                // Move the player if he should
                if (intMoveX  != 0 || intMoveY != 0)
                    _player.Move(intMoveX, intMoveY);

                // Update the projectiles
                foreach (Projectile projectile in Projectiles)
                    projectile.Update();

                // Update the enemies
                foreach (Character character in Characters)
                    if (character is Enemy enemy)
                        enemy.Move();
                
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
                    type = Projectile.Type.Fireball_Big;

                // Create a new CFrame of where the click was
                CFrame target = new(e.X + cameraX, e.Y + cameraY);

                // Shoot an arrow using the player's shoot method and add it to the projectile list
                Projectile? possibleProjectile = _player.Shoot(target, type);

                if (possibleProjectile != null)
                {
                    Projectiles.Add(possibleProjectile);
                }
            }
        }
    }
}