using Accessibility;
using ShootMeUp.Helpers;
using ShootMeUp.Model;
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
        public static readonly int GAMESPEED = 6;

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
            this.WindowState = FormWindowState.Maximized;

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
        private void Main()
        {
            ShowTitle();
            StartGame();
        }


        /// <summary>
        /// Shows the game's title screen
        /// </summary>
        private void ShowTitle()
        {
            // Remove any previous controls
            this.Controls.Clear();

            // Create the text and add some style to it
            _titleLabel = new Label
            {
                Text = "Craft Me Up",
                Font = new Font("Consolas", 48, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                BackColor = Color.Transparent,
                Left = (ClientSize.Width - PreferredSize.Width) / 2,
                Top = ClientSize.Height / 3 - PreferredSize.Height / 2
            };

            // Create and style Play button
            _playButton = new Button
            {
                Text = "Play the game",
                Font = new Font("Consolas", 24, FontStyle.Bold),
                AutoSize = true,
                Size = PreferredSize,
                Left = (ClientSize.Width - Width) / 2,
                Top = _titleLabel.Bottom + 256
            };

            // Add controls to the form
            this.Controls.Add(_titleLabel);
            this.Controls.Add(_playButton);

            // Add event handling
            bool clicked = false;

            //finish this method -> do while (not click)
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
            this.Controls.Add(pauseModale);

            //stops the ticker to pause the game
            this.ticker.Stop();
        }

        /// <summary>
        /// Start the game up
        /// </summary>
        private async Task StartGame()
        {
            // Remove title screen controls
            this.Controls.Remove(_titleLabel);
            this.Controls.Remove(_playButton);

            //general variables
            Score = 0;
            _intWaveNumber = 1;

            // Generate the world, then start it
            GenerateWorld();
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
            float fltLeftBound = 32;
            float fltRightBound = (BORDER_SIZE + 4) * 32;

            float fltAreaCenterX = (fltLeftBound + fltRightBound) / 2.0f;

            // Center the character horizontally
            float characterX = fltAreaCenterX - (DEFAULT_CHARACTER_SIZE / 2);

            // Create a new player
            _player = new Character((int)characterX, BORDER_SIZE * 32 + (32 - DEFAULT_CHARACTER_SIZE), DEFAULT_CHARACTER_SIZE, Character.Type.Player, GAMESPEED);
            Characters.Add( _player );

            // Create a new border, piece by piece
            for (int x = 0; x <= BORDER_SIZE; x++)
                for (int y = 0; y <= BORDER_SIZE; y++)
                    if (x == 0 || x == BORDER_SIZE || y == 0 || y == BORDER_SIZE)
                        Obstacles.Add(new Obstacle(32 * (2 + x), 32 * (2 + y), 32, Obstacle.Type.Border));

            // Create a variable to store the border's size
            int intBorderLength = BORDER_SIZE * 32 + 32;

            //// Creating the world environment ////

            // Top left corner
            const Obstacle.Type BORDER = Obstacle.Type.Border;

            Obstacles.Add(new Obstacle(32 * 3, 32 * 3, OBSTACLE_SIZE, BORDER));
            Obstacles.Add(new Obstacle(32 * 4, 32 * 3, OBSTACLE_SIZE, BORDER));
            Obstacles.Add(new Obstacle(32 * 3, 32 * 4, OBSTACLE_SIZE, BORDER));

            // Top right corner
            Obstacles.Add(new Obstacle(intBorderLength, 32 * 3, OBSTACLE_SIZE, BORDER));
            Obstacles.Add(new Obstacle(intBorderLength - 32, 32 * 3, OBSTACLE_SIZE, BORDER));
            Obstacles.Add(new Obstacle(intBorderLength, 32 * 4, OBSTACLE_SIZE, BORDER));

            // Bottom left corner
            Obstacles.Add(new Obstacle(32 * 3, intBorderLength, OBSTACLE_SIZE, BORDER));
            Obstacles.Add(new Obstacle(32 * 4, intBorderLength, OBSTACLE_SIZE, BORDER));
            Obstacles.Add(new Obstacle(32 * 3, intBorderLength - 32, OBSTACLE_SIZE, BORDER));

            // Bottom right corner
            Obstacles.Add(new Obstacle(intBorderLength, intBorderLength, OBSTACLE_SIZE, BORDER));
            Obstacles.Add(new Obstacle(intBorderLength - 32, intBorderLength, OBSTACLE_SIZE, BORDER));
            Obstacles.Add(new Obstacle(intBorderLength, intBorderLength - 32, OBSTACLE_SIZE, BORDER));


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

        private List<Enemy> GenerateWaves(int waveNumber)
        {
            return new List<Enemy>();
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

                    // Add a wait before adding the next enemy
                    await Task.Delay(16000 / GAMESPEED);
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
            return entity1.DisplayedImage.DisplayRectangle.IntersectsWith(entity2.DisplayedImage.DisplayRectangle);
        }

        // Calcul du nouvel état après que 'interval' millisecondes se sont écoulées
        private void NewFrame(object sender, EventArgs e)
        {
            // Update the playspace if the player is in game
            if (_gamestate == Gamestate.running && _player != null)
            {
                // Remove any inactive projectiles/obstacles
                Projectiles.RemoveAll(projectile => !projectile.Active);
                Obstacles.RemoveAll(obstacle => obstacle.Health <= 0);

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

                        ShowTitle();

                        break;
                    }
                }

                Characters.RemoveAll(character => character.Lives <= 0);

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
                        enemy.Move(_player);
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

                // Shoot an arrow using the player's shoot method and add it to the projetile list
                Projectile? possibleProjectile = _player.Shoot(this.PointToClient(Cursor.Position), type);

                if (possibleProjectile != null)
                {
                    Projectiles.Add(possibleProjectile);
                }
            }
        }

        private void DisplayControls()
        {
            //displays the projectiles
            foreach (Projectile projectile in Projectiles)
            {
                this.Controls.Add(projectile.DisplayedImage);
            }
            //displays the characters
            foreach (Character character in Characters)
            {
                this.Controls.Add(character.DisplayedImage);
            }
            //displays the obstacles
            foreach (Obstacle obstacle in Obstacles)
            {
                this.Controls.Add(obstacle.DisplayedImage);
            }
        }
    }
}