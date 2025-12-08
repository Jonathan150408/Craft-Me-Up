using Accessibility;
using ShootMeUp.Helpers;
using ShootMeUp.Model;
using ShootMeUp.Properties;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using Font = System.Drawing.Font;

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
        public static readonly int GAMESPEED = 4;

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
        [DefaultValue(0)]
        public int Score { get; set; }



        private Bitmap backBuffer;
        private Graphics bufferG;

        public static float cameraX { get; private set; }
        public static float cameraY { get; private set; }
        //create a modale and display it
        PictureBox pauseModale;


        public ShootMeUp()
        {
            InitializeComponent();

            ClientSize = new Size(WIDTH, HEIGHT);
            _intWaveNumber = 1;
            _gamestate = Gamestate.finished;

            // Create a new list of keys held down
            _keysHeldDown = new List<Keys>();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
              ControlStyles.OptimizedDoubleBuffer |
              ControlStyles.UserPaint, true);
            this.DoubleBuffered = true;

            BackColor = ColorTranslator.FromHtml("#7f7f7f");

            cameraX = 0;
            cameraY = 0;

            pauseModale = new()
            {
                Top = 0,
                Left = 0,
                Width = this.ClientRectangle.Width,
                Height = this.ClientRectangle.Height,
                BackColor = Color.FromArgb(75, 100, 100, 100)
            };

            backBuffer = new Bitmap(WIDTH, HEIGHT);
            bufferG = Graphics.FromImage(backBuffer);
            bufferG.InterpolationMode = InterpolationMode.NearestNeighbor;
            bufferG.PixelOffsetMode = PixelOffsetMode.Half;

            ShowTitle();
        }

        /// <summary>
        /// Shows the game's title screen
        /// </summary>
        private void ShowTitle()
        {
            if (BackgroundImage != null)
                BackgroundImage.Dispose();

            Bitmap tile = new Bitmap(Sprites.Stone, ShootMeUp.OBSTACLE_SIZE, ShootMeUp.OBSTACLE_SIZE);
            BackgroundImage = tile;
            BackgroundImageLayout = ImageLayout.Tile;

            // Remove any previous controls
            if (_titleLabel != null) Controls.Remove(_titleLabel);
            if (_playButton != null) Controls.Remove(_playButton);

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
                BackColor = Color.White,
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
            if (this._gamestate == Gamestate.running)
            { 
                Controls.Add(pauseModale);
                //stops the ticker to pause the game
                this.ticker.Stop();
                this._gamestate = Gamestate.paused;
                
            }
            else
            {
                Controls.Remove(pauseModale);
                //restart the ticker
                this.ticker.Start();
                this._gamestate = Gamestate.running;
            }
            
        }

        /// <summary>
        /// Get a character's sprite
        /// </summary>
        /// <param name="GivenType">The Character.Type of the given character</param>
        /// <returns>A sprite</returns>
        private static Bitmap GetSprite(Character.Type GivenType)
        {
            Bitmap ReturnedImage;

            switch (GivenType)
            {
                case Character.Type.Player:
                    ReturnedImage = Sprites.Player;
                    break;
                case Character.Type.Zombie:
                    ReturnedImage = Sprites.Zombie;
                    break;
                case Character.Type.Skeleton:
                    ReturnedImage = Sprites.Skeleton;
                    break;
                case Character.Type.Baby_Zombie:
                    ReturnedImage = Sprites.Zombie;
                    break;
                case Character.Type.Blaze:
                    ReturnedImage = Sprites.Blaze;
                    break;
                case Character.Type.Zombie_Pigman:
                    ReturnedImage = Sprites.Pigman;
                    break;
                case Character.Type.Wither:
                    ReturnedImage = Sprites.Wither;
                    break;
                default:
                    ReturnedImage = Sprites.Player;
                    break;
            }

            return (Bitmap)ReturnedImage.Clone();
        }

        /// <summary>
        /// Recolor a gray texture
        /// </summary>
        /// <param name="gray">Default gray image</param>
        /// <param name="tint">The tint</param>
        /// <returns>The same image, but recolored</returns>
        private static Bitmap ApplyTint(Bitmap gray, Color tint)
        {
            Bitmap result = new Bitmap(gray.Width, gray.Height);

            for (int x = 0; x < gray.Width; x++)
            {
                for (int y = 0; y < gray.Height; y++)
                {
                    Color px = gray.GetPixel(x, y);

                    // Multiply the grayscale pixel by the tint (Minecraft-style)
                    int r = (px.R * tint.R) / 255;
                    int g = (px.G * tint.G) / 255;
                    int b = (px.B * tint.B) / 255;

                    result.SetPixel(x, y, Color.FromArgb(px.A, r, g, b));
                }
            }

            return result;
        }

        /// <summary>
        /// Get a obstacle's sprite
        /// </summary>
        /// <param name="GivenType">The Obstacle.Type of the given obstacle</param>
        /// <returns>A sprite</returns>
        private static Bitmap GetSprite(Obstacle.Type GivenType, (int Width, int Height) Size)
        {
            Bitmap ReturnedImage;

            switch (GivenType)
            {
                case Obstacle.Type.Dirt:
                    ReturnedImage = Sprites.Dirt;
                    break;
                case Obstacle.Type.Wood:
                    ReturnedImage = Sprites.Wooden_Planks;
                    break;
                case Obstacle.Type.CobbleStone:
                    ReturnedImage = Sprites.Cobblestone;
                    break;

                case Obstacle.Type.Barrier:
                    ReturnedImage = Sprites.Barrier;
                    break;
                case Obstacle.Type.Bedrock:
                    ReturnedImage = Sprites.Bedrock;
                    break;

                case Obstacle.Type.Spawner:
                    ReturnedImage = Sprites.Spawner;
                    break;
                case Obstacle.Type.Bush:
                    ReturnedImage = Sprites.Bush;
                    break;
                case Obstacle.Type.Grass:
                    // Minecraft plains biome grass color
                    Color grassTint = Color.FromArgb(0x91, 0xBD, 0x59);

                    // Apply tint to grayscale texture
                    Bitmap tinted = ApplyTint(Sprites.Grass, grassTint);

                    ReturnedImage = tinted;
                    break;
                case Obstacle.Type.Stone:
                    ReturnedImage = Sprites.Stone;
                    break;
                case Obstacle.Type.Sand:
                    ReturnedImage = Sprites.Sand;
                    break;

                default:
                    ReturnedImage = Sprites.Player;
                    break;
            }

            // Return the sprite without any modifications if the obstacle isn't a floor
            if (GivenType != Obstacle.Type.Grass &&
                GivenType != Obstacle.Type.Stone &&
                GivenType != Obstacle.Type.Sand)
            {
                return (Bitmap)ReturnedImage.Clone();
            }

            // Create a new bitmap matching obstacle size
            Bitmap TiledSprite = new Bitmap(Size.Width, Size.Height);

            // Create the tiles
            using (Graphics g = Graphics.FromImage(TiledSprite))
            {
                for (int x = 0; x < Size.Width; x += ShootMeUp.OBSTACLE_SIZE)
                {
                    for (int y = 0; y < Size.Height; y += ShootMeUp.OBSTACLE_SIZE)
                    {
                        g.DrawImage(
                            ReturnedImage,
                            new Rectangle(x, y, ShootMeUp.OBSTACLE_SIZE, ShootMeUp.OBSTACLE_SIZE),
                            new Rectangle(0, 0, ReturnedImage.Width, ReturnedImage.Height),
                            GraphicsUnit.Pixel
                        );
                    }
                }
            }

            return TiledSprite;
        }

        /// <summary>
        /// Get a projectile sprite
        /// </summary>
        /// <param name="GivenType">The Projectile.Type of the given projectile</param>
        /// <param name="fltRotationAngle">The projetile's rotation angle</param>
        /// <returns>A sprite</returns>
        private static Bitmap GetSprite(Projectile.Type GivenType, float fltRotationAngle)
        {
            Bitmap ReturnedImage;

            switch (GivenType)
            {
                case Projectile.Type.Arrow_Small:
                case Projectile.Type.Arrow_Big:
                    fltRotationAngle -= 45f;

                    ReturnedImage = Sprites.Arrow;
                    break;
                case Projectile.Type.Fireball_Small:
                case Projectile.Type.Fireball_Big:
                    ReturnedImage = Sprites.Fireball;
                    break;
                case Projectile.Type.WitherSkull:
                    ReturnedImage = Sprites.WitherSkull;
                    break;
                default:
                    ReturnedImage = Sprites.Player;

                    break;
            }

            using (Bitmap OriginalImage = (Bitmap)ReturnedImage.Clone())
            {
                Bitmap RotatedImage = new Bitmap(OriginalImage.Width, OriginalImage.Height);
                RotatedImage.SetResolution(OriginalImage.HorizontalResolution, OriginalImage.VerticalResolution);

                using (Graphics g = Graphics.FromImage(RotatedImage))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    // Transform to center → rotate → restore offset
                    g.TranslateTransform(OriginalImage.Width / 2f, OriginalImage.Height / 2f);
                    g.RotateTransform(fltRotationAngle);
                    g.TranslateTransform(-OriginalImage.Width / 2f, -OriginalImage.Height / 2f);

                    g.DrawImage(OriginalImage, 0, 0);
                }

                OriginalImage.Dispose();
                return RotatedImage;
            }
        }

        /// <summary>
        /// Start the game up
        /// </summary>
        private void StartGame()
        {
            BackgroundImage = null;
            BackgroundImageLayout = ImageLayout.None;

            // Remove title screen controls
            Controls.Remove(_titleLabel);
            Controls.Remove(_playButton);
            _titleLabel?.Dispose();
            _playButton?.Dispose();

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

            // Calculate the bottom-center, related to the player
            float fltLeftBound = 32;
            float fltRightBound = (BORDER_SIZE + 4) * 32;

            float fltAreaCenterX = (fltLeftBound + fltRightBound) / 2.0f;

            // Center the character horizontally
            float characterX = fltAreaCenterX - (DEFAULT_CHARACTER_SIZE / 2);

            // Create a new player
            _player = new(characterX, BORDER_SIZE * 32 + (32 - DEFAULT_CHARACTER_SIZE), DEFAULT_CHARACTER_SIZE, Character.Type.Player, GAMESPEED);
            Characters.Add(_player);

            // Create a new border, piece by piece
            for (int x = 0; x <= BORDER_SIZE; x++)
                for (int y = 0; y <= BORDER_SIZE; y++)
                    if (x == 0 || x == BORDER_SIZE || y == 0 || y == BORDER_SIZE)
                        Obstacles.Add(new(32 * (2 + x), 32 * (2 + y), 32, Obstacle.Type.Bedrock));

            // Create a variable to store the border's size
            int intBorderLength = BORDER_SIZE * 32 + 32;

            //// Creating the world environment ////

            // Top left corner
            const Obstacle.Type BEDROCK = Obstacle.Type.Bedrock;

            Obstacles.Add(new(32 * 3, 32 * 3, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new(32 * 4, 32 * 3, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new(32 * 3, 32 * 4, OBSTACLE_SIZE, BEDROCK));

            // Top right corner
            Obstacles.Add(new(intBorderLength, 32 * 3, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new(intBorderLength - 32, 32 * 3, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new(intBorderLength, 32 * 4, OBSTACLE_SIZE, BEDROCK));

            // Bottom left corner
            Obstacles.Add(new(32 * 3, intBorderLength, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new(32 * 4, intBorderLength, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new(32 * 3, intBorderLength - 32, OBSTACLE_SIZE, BEDROCK));

            // Bottom right corner
            Obstacles.Add(new(intBorderLength, intBorderLength, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new(intBorderLength - 32, intBorderLength, OBSTACLE_SIZE, BEDROCK));
            Obstacles.Add(new(intBorderLength, intBorderLength - 32, OBSTACLE_SIZE, BEDROCK));

            // The pillars' type value
            const Obstacle.Type COBBLE = Obstacle.Type.CobbleStone;

            // Top left pillars
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Obstacles.Add(new(192 + (160 * x), 192 + (160 * y), OBSTACLE_SIZE * 2, COBBLE));
                }
            }

            // Top right pillars
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Obstacles.Add(new(intBorderLength - 128 - (160 * x), 192 + (160 * y), OBSTACLE_SIZE * 2, COBBLE));
                }
            }

            // Bottom left pillars
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Obstacles.Add(new(192 + (160 * x), intBorderLength - 128 - (160 * y), OBSTACLE_SIZE * 2, COBBLE));
                }
            }

            // Bottom right pillars
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Obstacles.Add(new(intBorderLength - 128 - (160 * x), intBorderLength - 128 - (160 * y), OBSTACLE_SIZE * 2, COBBLE));
                }
            }


            // The barriers' type
            const Obstacle.Type WOOD = Obstacle.Type.Wood;

            // Top barriers
            for (int x = 0; x < 2; x++)
            {
                Obstacles.Add(new(448 + (128 * x), 192, OBSTACLE_SIZE * 2, WOOD));
            }

            Obstacles.Add(new(512, 352, OBSTACLE_SIZE * 2, WOOD));

            // Left barriers
            for (int x = 0; x < 2; x++)
            {
                Obstacles.Add(new(192, 448 + (128 * x), OBSTACLE_SIZE, WOOD));
            }

            Obstacles.Add(new(352, 512, OBSTACLE_SIZE, WOOD));

            // Right barriers
            for (int x = 0; x < 2; x++)
            {
                Obstacles.Add(new(intBorderLength - 128, 448 + (128 * x), OBSTACLE_SIZE, WOOD));
            }

            Obstacles.Add(new(intBorderLength - 256, 512, OBSTACLE_SIZE, WOOD));

            // Bottom barriers
            for (int x = 0; x < 2; x++)
            {
                Obstacles.Add(new(448 + (128 * x), intBorderLength - 128, OBSTACLE_SIZE * 2, WOOD));
            }

            Obstacles.Add(new(512, intBorderLength - 256, OBSTACLE_SIZE * 2, WOOD));


            // The smaller obstacles' type
            const Obstacle.Type DIRT = Obstacle.Type.Dirt;

            // Top left small obstacle
            Obstacles.Add(new(288, 288, OBSTACLE_SIZE, DIRT));

            // Top right small obstacle
            Obstacles.Add(new(intBorderLength - 192, 288, OBSTACLE_SIZE, DIRT));

            // Bottom left small obstacle
            Obstacles.Add(new(288, intBorderLength - 192, OBSTACLE_SIZE, DIRT));

            // Bottom right small obstacle
            Obstacles.Add(new(intBorderLength - 192, intBorderLength - 192, OBSTACLE_SIZE, DIRT));

            // Middle small obstacles
            Obstacles.Add(new(448, 448, OBSTACLE_SIZE, DIRT));
            Obstacles.Add(new(intBorderLength - 352, 448, OBSTACLE_SIZE, DIRT));
            Obstacles.Add(new(448, intBorderLength - 352, OBSTACLE_SIZE, DIRT));
            Obstacles.Add(new(intBorderLength - 352, intBorderLength - 352, OBSTACLE_SIZE, DIRT));
        }

        /// <summary>
        /// Generate random waves of enemies
        /// </summary>
        /// <param name="waveNumber">The current wave's number</param>
        /// <returns></returns>
        private List<Enemy> GenerateWaves(int waveNumber)
        {
            if (_player == null || _player.Lives <= 0)
                return new List<Enemy>();

            List<Enemy> WaveEnemies = new List<Enemy>();
            Random rnd = new Random();

            // Base number of enemies plus some random variation
            int baseEnemies = 3; // minimum
            int totalEnemies = baseEnemies + rnd.Next(waveNumber, waveNumber * 3);

            // Define enemy types with score values
            var enemyTypes = new List<(Character.Type Type, int ScoreValue, float SizeMultiplier)>
            {
                (Character.Type.Zombie, 1, 1f),
                (Character.Type.Skeleton, 3, 1f),
                (Character.Type.Baby_Zombie, 4, 0.75f),
                (Character.Type.Blaze, 6, 1f),
                (Character.Type.Zombie_Pigman, 5, 1f)
            };

            while (totalEnemies > 0)
            {
                // Weighted probability for each enemy type
                var weights = enemyTypes.Select(e =>
                {
                    // Base chance inversely proportional to ScoreValue (rarer = lower chance)
                    float baseChance = 1f / e.ScoreValue;

                    // Wave effect: increases chance for higher ScoreValue as wave progresses
                    float waveEffect = 1f + (waveNumber * 0.05f * e.ScoreValue);

                    return baseChance * waveEffect;
                }).ToArray();

                // Normalize and select enemy
                float weightSum = weights.Sum();
                float roll = (float)(rnd.NextDouble() * weightSum);

                Character.Type selectedType = enemyTypes[0].Type;
                float sizeMultiplier = 1f;
                float cumulative = 0f;

                for (int i = 0; i < enemyTypes.Count; i++)
                {
                    cumulative += weights[i];
                    if (roll <= cumulative)
                    {
                        selectedType = enemyTypes[i].Type;
                        sizeMultiplier = enemyTypes[i].SizeMultiplier;
                        break;
                    }
                }

                // Determine enemy size
                int intCharSize = (int)(DEFAULT_CHARACTER_SIZE * sizeMultiplier);

                // Add enemy to wave
                WaveEnemies.Add(new Enemy(0, 0, intCharSize, selectedType, GAMESPEED, _player));

                // Reduce totalEnemies based on ScoreValue (rarer/stronger enemies "cost" more)
                totalEnemies -= Math.Max(1, enemyTypes.First(e => e.Type == selectedType).ScoreValue);
            }

            // Adds bosses at specific waves
            if (waveNumber % 5 == 0)
            {
                for (int i = -2; i < waveNumber / 5; i++)
                    WaveEnemies.Add(new Enemy(0, 0, DEFAULT_CHARACTER_SIZE, Character.Type.Wither, GAMESPEED, _player));
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
                // Add a wait before the next wave
                await Task.Delay(2000);

                // Get the wave's enemies
                List<Enemy> waveEnemies = GenerateWaves(_intWaveNumber);

                foreach (Enemy enemy in waveEnemies)
                {
                    // End the wave system if the game stopped
                    if (_gamestate == Gamestate.finished)
                        return;

                    // Put the enemy in the right spot
                    enemy.Position = (512 + OBSTACLE_SIZE / 2 + enemy.Size.Width / 4, 512 + OBSTACLE_SIZE / 2 + enemy.Size.Height / 4);

                    // Add the enemy to the character list
                    Characters.Add(enemy);

                    // Add a wait before adding the next enemy
                    await Task.Delay(2000);
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
            return overlapX && overlapY;
        }

        private void DrawBackground(Graphics g, float cameraX, float cameraY)
        {
            Bitmap tile = Sprites.Stone;
            int tileSize = ShootMeUp.OBSTACLE_SIZE;

            // How many tiles are needed to cover the screen
            int tilesX = (int)Math.Ceiling(bufferG.VisibleClipBounds.Width / (float)tileSize) + 2;
            int tilesY = (int)Math.Ceiling(bufferG.VisibleClipBounds.Height / (float)tileSize) + 2;

            // Determine starting tile index based on camera offset
            int startX = (int)Math.Floor(cameraX / tileSize) - 1;
            int startY = (int)Math.Floor(cameraY / tileSize) - 1;

            for (int x = startX; x < startX + tilesX; x++)
            {
                for (int y = startY; y < startY + tilesY; y++)
                {
                    int drawX = x * tileSize - (int)cameraX;
                    int drawY = y * tileSize - (int)cameraY;
                    g.DrawImage(tile, drawX, drawY, tileSize, tileSize);
                }
            }
        }

        /// <summary>
        /// Render a new frame
        /// </summary>
        private void RenderFrame()
        {
            if (_player != null)
            {
                // Clear the frame
                DrawBackground(bufferG, cameraX, cameraY);

				// Draw all characters' health (not including player)
                foreach (Character character in Characters)
                {
                    if (character.CharType == Character.Type.Player) continue;
                    // Draw their health bar
                    SizeF textSize = bufferG.MeasureString($"{character}", TextHelpers.drawFont);

                    // Calculate the X coordinate to center the text
                    float centeredX = character.Position.X + (character.Size.Width / 2f) - (textSize.Width / 2f);

                    bufferG.DrawString($"{character}", TextHelpers.drawFont, TextHelpers.writingBrush, centeredX - cameraX, (character.Position.Y - 16) - cameraY);

                }

                // Draw all the background assets first
                foreach (Obstacle Floor in Obstacles)
                {
                    // Only continue if they're from one of the floor types
                    if (Floor.ObstType == Obstacle.Type.Sand || Floor.ObstType == Obstacle.Type.Grass || Floor.ObstType == Obstacle.Type.Stone)
                    {
                        float drawX = Floor.Position.X - cameraX;
                        float drawY = Floor.Position.Y - cameraY;
                        
                        using (Bitmap Image = GetSprite(Floor.ObstType, Floor.Size))
                            bufferG.DrawImage(Image, drawX, drawY, Floor.Size.Width, Floor.Size.Height);
                    }
                }


                // Draw all characters (not including player)
                foreach (Character character in Characters)
                {
                    if (character.CharType == Character.Type.Player) continue;

                    float drawX = character.Position.X - cameraX;
                    float drawY = character.Position.Y - cameraY;

                    using (Bitmap Image = GetSprite(character.CharType))
                        bufferG.DrawImage(Image, drawX, drawY, character.Size.Width, character.Size.Height);
                }

                // Draw obstacles
                foreach (Obstacle obstacle in Obstacles)
                {
                    // Only continue if they're not from one of the floor types
                    if (!(obstacle.ObstType == Obstacle.Type.Sand || obstacle.ObstType == Obstacle.Type.Grass || obstacle.ObstType == Obstacle.Type.Stone))
                    {
                        float drawX = obstacle.Position.X - cameraX;
                        float drawY = obstacle.Position.Y - cameraY;

                        using (Bitmap Image = GetSprite(obstacle.ObstType, obstacle.Size))
                            bufferG.DrawImage(Image, drawX, drawY, obstacle.Size.Width, obstacle.Size.Height);


                        // Draw its life
                        SizeF textSize = bufferG.MeasureString($"{obstacle}", TextHelpers.drawFont);

                        // Calculate the X coordinate to center the text
                        float centeredX = obstacle.Position.X + (obstacle.Size.Width / 2f) - (textSize.Width / 2f);

                        bufferG.DrawString($"{obstacle}", TextHelpers.drawFont, TextHelpers.writingBrush, centeredX - cameraX, (obstacle.Position.Y - 16) - cameraY);

                    }
                }

                // Draw projectiles
                foreach (Projectile projectile in Projectiles)
                {
                    float drawX = projectile.Position.X - cameraX;
                    float drawY = projectile.Position.Y - cameraY;

                    using(Bitmap Image = GetSprite(projectile.ProjType, projectile.RotationAngle))
                        bufferG.DrawImage(Image, drawX, drawY, projectile.Size.Width, projectile.Size.Height);
                }

                // Draw the player along with their lives and the game's statistics
                if (_player != null)
                {
                    float px = _player.Position.X - cameraX;
                    float py = _player.Position.Y - cameraY;

                    using (Bitmap Image = GetSprite(_player.CharType))
                        bufferG.DrawImage(Image, px, py, _player.Size.Width, _player.Size.Height);

                    bufferG.DrawString($"Wave {_intWaveNumber} | Score: {Score} | Lives remaining: {_player.Lives}", TextHelpers.drawFont, TextHelpers.writingBrush, 8, 8);

                    for (int i = 0; i < _player.Lives; i++)
                    {
                        int x = 8 + (i * 24);
                        using (Bitmap Image = GetSprite(_player.CharType))
                            bufferG.DrawImage(Image, x, 32, 16, 16);
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_gamestate != Gamestate.finished && backBuffer != null)
            {
                e.Graphics.DrawImageUnscaled(backBuffer, 0, 0);
            }
            else
            {
                // Let WinForms draw the normal background (including BackgroundImage)
                base.OnPaint(e);
            }
        }

        /// <summary>
        /// Clean up dead entities, broken obstacles, etc.
        /// </summary>
        private void CleanupEntities()
        {
            // Remove anything inactive
            Projectiles.RemoveAll(p => !p.Active);

            Obstacles.RemoveAll(o => o.Health <= 0 && !o.Invincible);

            // Change the score if there's a dead enemy
            Characters.RemoveAll(c =>
            {
                if (c.Lives <= 0 && c is Enemy enemy)
                {
                    Score += enemy.ScoreValue;
                    return true;
                }
                else if (c.CharType == Character.Type.Player && c.Lives <= 0)
                {
                    _gamestate = Gamestate.finished;
                    _player = null;
                    ShowTitle();
                    return true;
                }
                return false;
            });
        }

        private void NewFrame(object sender, EventArgs e)
        {
            // Update the playspace if the player is in game
            if (_player != null)
            {
                // Update camera position to the player's
                cameraX = _player.Position.X - (Size.Width / 2);
                cameraY = _player.Position.Y - (Size.Height / 2);

                // Create a new frame
                RenderFrame();

                // Tell the program to render the game again
                Invalidate();

                if (_gamestate != Gamestate.running)
                    return;

                // Clean up the dead entities
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
                    _player?.Move(intMoveX, intMoveY);

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
                _keysHeldDown.Add(e.KeyCode);

            if (_keysHeldDown.Contains(Keys.Escape))
            {
                DisplayPauseMenu();
                _keysHeldDown.Remove(Keys.Escape);
            }
        }

        private void ShootMeUp_KeyUp(object sender, KeyEventArgs e)
        {
            // Remove the key from the list if it's in there
            if (_keysHeldDown.Contains(e.KeyCode))
                _keysHeldDown.Remove(e.KeyCode);
        }

        private void ShootMeUp_MouseClick(object sender, MouseEventArgs e)
        {
            // Only try and shoot something if the player is in game
            if (_gamestate == Gamestate.running && _player != null)
            {
                //default is arrow
                Projectile.Type type = Projectile.Type.Arrow_Big;

                // If it's a left click, strType is "arrow". if its a right click, strType is "fireball".
                if (e.Button == MouseButtons.Left)
                    type = Projectile.Type.Arrow_Big;
                else if (e.Button == MouseButtons.Right)
                    type = Projectile.Type.Fireball_Big;

                // Create a new CFrame of where the click was
                CFrame target = new(e.X + cameraX, e.Y + cameraY);

                // Shoot an arrow using the player's shoot method and add it to the projectile list
                Projectile? possibleProjectile = _player.Shoot(target, type);

                if (possibleProjectile != null)
                    Projectiles.Add(possibleProjectile);
            }
        }
    }
}