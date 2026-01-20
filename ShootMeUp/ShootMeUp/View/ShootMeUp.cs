using Accessibility;
using ShootMeUp.Helpers;
using ShootMeUp.Model;
using ShootMeUp.Properties;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
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
        /// The game's speed multiplier for movement, projectiles, etc.)
        /// </summary>
        public static readonly int GAMESPEED = 8 * 30;

        /// <summary>
        /// Any obstacle's height and length
        /// </summary>
        public static readonly int OBSTACLE_SIZE = 32;

        /// <summary>
        /// The default size for characters and enemies
        /// </summary>
        public static readonly int DEFAULT_CHARACTER_SIZE = OBSTACLE_SIZE - 8;

        /// <summary>
        /// The number of tiles in one chunk
        /// </summary>
        private static readonly int CHUNK_SIZE_IN_TILES = 16;

        /// <summary>
        /// The amount of chunks on the x and y axis
        /// </summary>
        private static readonly int CHUNK_AMOUNT = 16;

        private bool isResizing = false;

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

        /// <summary>
        /// The RNG seed
        /// </summary>
        public static readonly int GAMESEED = (new Random()).Next(int.MinValue, int.MaxValue);
        private readonly Random rnd;

        /// <summary>
        /// A cache for floor textures, as they can make the game lag
        /// </summary>
        private static readonly Dictionary<Obstacle.Type, Bitmap> FloorChunkCache = new Dictionary<Obstacle.Type, Bitmap>();

        private Bitmap backBuffer;
        private Graphics bufferG;

        public static float cameraX { get; private set; }
        public static float cameraY { get; private set; }

        //create a modal and a button for the pause menu
        PictureBox pauseModale;
        Button resumeButton;

        // Variables used for time handling
        public static long LastFrameTime;
        public static float DeltaTime;

        /// <summary>
        /// The game's zoom (the smaller it is, the more you see)
        /// </summary>
        public static float Zoom = 1f;

        public ShootMeUp()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimumSize = new Size(640, 640);

            _intWaveNumber = 1;
            _gamestate = Gamestate.finished;

            // Create a new list of keys held down
            _keysHeldDown = [];

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
              ControlStyles.OptimizedDoubleBuffer |
              ControlStyles.UserPaint, true);
            this.DoubleBuffered = true;

            BackColor = ColorTranslator.FromHtml("#7f7f7f");

            rnd = new(GAMESEED);

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
            resumeButton = new()
            {
                Height = 100,
                Width = 300,
                Top = (this.ClientSize.Height - 100) / 2,
                Left = (this.ClientSize.Width - 300) / 2,
                Text = "Resume Game",
                BackColor = Color.White,
                Font = new Font("Consolas", 24, FontStyle.Bold),
            };
            this.resumeButton.Click += ResumeButton_Click;

            ResizeBackbuffer();

            ShowTitle();
        }

        private void ResizeBackbuffer()
        {
            backBuffer?.Dispose();
            bufferG?.Dispose();

            backBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);
            bufferG = Graphics.FromImage(backBuffer);
            bufferG.InterpolationMode = InterpolationMode.NearestNeighbor;
            bufferG.PixelOffsetMode = PixelOffsetMode.Half;
        }

        private void CenterTitleUI()
        {
            if (_titleLabel != null)
            {
                _titleLabel.Left = (ClientSize.Width - _titleLabel.Width) / 2;
                _titleLabel.Top = ClientSize.Height / 3 - _titleLabel.Height / 2;
            }

            if (_playButton != null)
            {
                _playButton.Left = (ClientSize.Width - _playButton.Width) / 2;
                _playButton.Top = (_titleLabel?.Bottom ?? (ClientSize.Height / 3)) + 256;
            }
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

            // Center the ui
            CenterTitleUI();
        }

        private async void _playButton_Click(object? sender, EventArgs e)
        {
            StartGame();
            await StartWaves();
        }


        private void ResumeButton_Click(object? sender, EventArgs e)
        {
            DisplayPauseMenu();
        }

        private void CenterPauseUI()
        {
            if (pauseModale != null)
            {
                pauseModale.Width = ClientSize.Width;
                pauseModale.Height = ClientSize.Height;
            }

            if (resumeButton != null)
            {
                resumeButton.Left = (ClientSize.Width - resumeButton.Width) / 2;
                resumeButton.Top = (ClientSize.Height - resumeButton.Height) / 2;
            }
        }

        /// <summary>
        /// pauses the game and displays the pause menu
        /// </summary>
        private void DisplayPauseMenu()
        {
            if (this._gamestate == Gamestate.running)
            {
                CenterPauseUI();
                Controls.Add(resumeButton);
                Controls.Add(pauseModale);

                resumeButton.BringToFront();

                //stops the ticker to pause the game
                this.ticker.Stop();
                this._gamestate = Gamestate.paused;

            }
            else if (this._gamestate == Gamestate.paused)
            {
                Controls.Remove(pauseModale);
                Controls.Remove(resumeButton);
                long now = Environment.TickCount64;
                DeltaTime = (now - LastFrameTime) / 1000f; // seconds
                LastFrameTime = now;

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
                case Character.Type.SpiderJockey:
                    ReturnedImage = Sprites.SpiderJockey;
                    break;
                case Character.Type.WitherSkeleton:
                    ReturnedImage = Sprites.WitherSkull;
                    break;
                case Character.Type.Wither:
                    ReturnedImage = Sprites.Wither;
                    break;
                case Character.Type.Dragon:
                    ReturnedImage = Sprites.Dragon;
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
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.SmoothingMode = SmoothingMode.None;

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
                case Projectile.Type.Arrow_Jockey:
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
                case Projectile.Type.DragonFireball:
                    ReturnedImage = Sprites.DragonFireball;
                    break;
                default:
                    ReturnedImage = Sprites.Player;

                    break;
            }

            // Return early if the image doesn't need any rotation
            Projectile.Type[] NoRotation = [Projectile.Type.WitherSkull, Projectile.Type.DragonFireball, Projectile.Type.Undefined];

            if (NoRotation.Contains(GivenType))
                return (Bitmap)ReturnedImage.Clone();

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

        private static void InitializeFloorChunks()
        {
            int chunkSizePx = CHUNK_SIZE_IN_TILES * OBSTACLE_SIZE;

            Obstacle.Type[] floorTypes =
            {
        Obstacle.Type.Grass,
        Obstacle.Type.Sand,
        Obstacle.Type.Stone
    };

            foreach (var type in floorTypes)
            {
                if (!FloorChunkCache.ContainsKey(type))
                {
                    FloorChunkCache[type] = GetSprite(
                        type,
                        (chunkSizePx, chunkSizePx)
                    );
                }
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
            LastFrameTime = Environment.TickCount64;

            Characters.Clear();
            Obstacles.Clear();
            Projectiles.Clear();

            // Cache floor textures for easier render
            InitializeFloorChunks();

            // Generate the world, then start it
            GenerateWorld();
        }

        /// <summary>
        /// Noise algorithm used for biome generation
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        static float BiomeNoise(int x, int y)
        {
            unchecked
            {
                int n = x * 1619 + y * 31337 + GAMESEED * 1013;
                n = (n << 13) ^ n;
                return 1f - ((n * (n * n * 60493 + 19990303) + 1376312589)
                             & 0x7fffffff) / 1073741824f;
            }
        }

        /// <summary>
        /// Generate the game itself
        /// </summary>
        private void GenerateWorld()
        {
            // Set the game state to true
            _gamestate = Gamestate.running;

            // Calculate the center of the map
            float fltMapSize = CHUNK_SIZE_IN_TILES * CHUNK_AMOUNT * OBSTACLE_SIZE;

            float fltAreaCenterX = fltMapSize / 2;

            // Create a new player
            _player = new(fltAreaCenterX, fltAreaCenterX, DEFAULT_CHARACTER_SIZE, Character.Type.Player, GAMESPEED);
            Characters.Add(_player);

            // Generate the border around the world
            for (int x = -32; x <= fltMapSize; x += 32)
            {
                for (int y = -32; y <= fltMapSize; y += 32)
                {
                    if (x == -32 || y == -32 || x == fltMapSize || y == fltMapSize)
                    {
                        // Create a new border block
                        Obstacles.Add(new(x, y, OBSTACLE_SIZE, Obstacle.Type.Barrier));
                    }
                }
            }


            // The max amount of health (from obstacles) inside of one chunk
            int intMaxHealthInAChunk = 120;

            // Generate the environment, chunk by chunk
            for (int chunkX = 0; chunkX < CHUNK_AMOUNT; chunkX++)
            {
                for (int chunkY = 0; chunkY < CHUNK_AMOUNT; chunkY++)
                {
                    // Get a random floor and apply it
                    List<Obstacle.Type> Floors = [Obstacle.Type.Grass, Obstacle.Type.Sand, Obstacle.Type.Stone];

                    float scale = 0.15f;
                    float noise = BiomeNoise(
                        (int)(chunkX * scale),
                        (int)(chunkY * scale)
                    );

                    Obstacle.Type randomFloor =
                        noise < -0.35f ? Obstacle.Type.Sand :
                        noise < 0.35f ? Obstacle.Type.Grass :
                                         Obstacle.Type.Stone;

                    // Chunk based values
                    int intChunkLength = (CHUNK_SIZE_IN_TILES * OBSTACLE_SIZE);

                    float fltX = 0 + chunkX * intChunkLength;
                    float fltY = 0 + chunkY * intChunkLength;

                    Obstacle floor = new(fltX, fltY, intChunkLength, randomFloor);
                    Obstacles.Add(floor);

                    // The total amount of health in the current chunk
                    int intHealthInChunk = 0;

                    Dictionary<Obstacle.Type, Obstacle.Type[]> biomeObstacles =
                        new()
                        {
                            [Obstacle.Type.Grass] = new[] { Obstacle.Type.Bush, Obstacle.Type.Wood },
                            [Obstacle.Type.Sand] = new[] { Obstacle.Type.Dirt },
                            [Obstacle.Type.Stone] = new[] { Obstacle.Type.CobbleStone }
                        };

                    while (intHealthInChunk < intMaxHealthInAChunk)
                    {
                        int intCurrentObstacleHealth;

                        int remainingHealth = intMaxHealthInAChunk - intHealthInChunk;

                        List<Obstacle.Type> valid = biomeObstacles[randomFloor].Where(t => (t == Obstacle.Type.Bush && 5 <= remainingHealth) || (t == Obstacle.Type.Dirt && 10 <= remainingHealth) || (t == Obstacle.Type.Wood && 20 <= remainingHealth) || (t == Obstacle.Type.CobbleStone && 50 <= remainingHealth)).ToList();

                        Obstacle.Type randomObstacleType;

                        if (valid.Count == 0)
                            break;

                        randomObstacleType = valid[rnd.Next(valid.Count)];

                        intCurrentObstacleHealth = randomObstacleType switch
                        {
                            Obstacle.Type.Bush => 5,
                            Obstacle.Type.Dirt => 10,
                            Obstacle.Type.Wood => 20,
                            Obstacle.Type.CobbleStone => 50,
                            _ => 0
                        };

                        // Add the current obstacle's health to the total
                        intHealthInChunk += intCurrentObstacleHealth;

                        Obstacle newObstacle = new(0, 0, OBSTACLE_SIZE, randomObstacleType);


                        // Set the obstacle up in a random spot in the chunk
                        int attempts = 0;

                        bool blnOk;
                        do
                        {
                            attempts++;
                            // Get random values for the obstacles
                            (int X, int Y) randomPos = (rnd.Next(0, CHUNK_SIZE_IN_TILES - 1), rnd.Next(0, CHUNK_SIZE_IN_TILES - 1));

                            randomPos.X *= OBSTACLE_SIZE;
                            randomPos.Y *= OBSTACLE_SIZE;

                            randomPos.X += intChunkLength * chunkX;
                            randomPos.Y += intChunkLength * chunkY;

                            // Try to place the obstacle somewhere
                            newObstacle.Position = randomPos;

                            blnOk = true;

                            // Check to see if the obstacle is colliding with anything
                            foreach (Obstacle obstacle in Obstacles)
                            {
                                if ((IsOverlapping(newObstacle, obstacle) || !blnOk) && !Floors.Contains(obstacle.ObstType))
                                {
                                    blnOk = false;
                                    break;
                                }
                            }

                            foreach (Character character in Characters)
                            {
                                if ((IsOverlapping(newObstacle, character) && newObstacle.CanCollide) || !blnOk)
                                {
                                    blnOk = false;
                                    break;
                                }
                            }

                            // Add the obstacle if everything is okay
                            if (blnOk)
                                Obstacles.Add(newObstacle);

                        } while (!blnOk && attempts < 100);
                    }
                }
            }
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

            // Base number of enemies plus some random variation
            int baseEnemies = 3;
            int totalEnemies = baseEnemies + rnd.Next(waveNumber, waveNumber * 3);

            // Define enemy types with score values
            var enemyTypes = new List<(Character.Type Type, int Weight, float SizeMultiplier)>
            {
                (Character.Type.Zombie,         1,       1f),
                (Character.Type.Skeleton,       3,       1f),
                (Character.Type.Baby_Zombie,    5,       0.75f),
                (Character.Type.Zombie_Pigman,  10,      1f),
                (Character.Type.Blaze,          20,      1f)
            };

            while (totalEnemies > 0)
            {
                // Weighted probability for each enemy type
                float[] weights = enemyTypes.Select(e =>
                {
                    // Base chance inversely proportional to Weight (rarer = lower chance)
                    float baseChance = 1f / e.Weight;

                    // Wave effect: increases chance for higher Weight as wave progresses
                    float waveEffect = 1f + (waveNumber * 0.05f * e.Weight);

                    return baseChance * waveEffect;
                }).ToArray();

                // Normalize and select enemy
                float weightSum = weights.Sum();
                float roll = (float)(rnd.NextDouble() * weightSum);

                Character.Type selectedType = enemyTypes[0].Type;
                float sizeMultiplier = 1f;
                float cumulative = 0;

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

                // Reduce totalEnemies based on Weight (rarer/stronger enemies "cost" more)
                totalEnemies -= Math.Max(1, enemyTypes.First(e => e.Type == selectedType).Weight);
            }

            // Adds bosses at specific waves
            if (waveNumber % 5 == 0)
            {
                // Create multiple checks
                bool isMultipleOf50 = waveNumber % 50 == 0;
                bool isMultipleOf25 = waveNumber % 25 == 0;
                bool isMultipleOf10 = waveNumber % 10 == 0;

                // Initialize spawn variables
                int intBossAmount;
                float fltSizeMultiplicator;
                Character.Type BossType;

                if (isMultipleOf50)
                {
                    intBossAmount = waveNumber / 50;
                    fltSizeMultiplicator = 8;
                    BossType = Character.Type.Dragon;
                }
                else if (isMultipleOf25)
                {
                    intBossAmount = waveNumber / 25;
                    fltSizeMultiplicator = 6;
                    BossType = Character.Type.Wither;
                }
                else if (isMultipleOf10)
                {
                    intBossAmount = waveNumber / 10;
                    fltSizeMultiplicator = 1.5f;
                    BossType = Character.Type.WitherSkeleton;
                }
                else
                {
                    intBossAmount = waveNumber / 5;
                    fltSizeMultiplicator = 2;
                    BossType = Character.Type.SpiderJockey;
                }

                for (int i = 0; i < intBossAmount; i++)
                    WaveEnemies.Add(new Enemy(0, 0, (int)(DEFAULT_CHARACTER_SIZE * fltSizeMultiplicator), BossType, GAMESPEED, _player));
            }

            return WaveEnemies;
        }

        private async Task Wait(int miliseconds)
        {
            do
            {
                do
                {
                    await Task.Delay(1);
                } while (_gamestate == Gamestate.paused);

                miliseconds--;
            } while (miliseconds > 0);
        }

        /// <summary>
        /// Starts the wave system
        /// </summary>
        private async Task StartWaves()
        {
            while (_gamestate != Gamestate.finished && (_player != null && _player.Lives > 0))
            {
                // Add a wait before the next wave
                await Wait(20000 / GAMESPEED);

                // Get the wave's enemies
                List<Enemy> waveEnemies = GenerateWaves(_intWaveNumber);

                foreach (Enemy enemy in waveEnemies)
                {
                    // End the wave system if the game stopped
                    if (_gamestate == Gamestate.finished)
                        return;

                    // Put the enemy in the right spot
                    bool reroll;
                    do
                    {
                        //determines the coordinates
                        reroll = false;
                        int spawn_angle = new Random().Next(361);
                        float random_X = (float)(Math.Sin(spawn_angle) * 8 + new Random().Next(10)) * DEFAULT_CHARACTER_SIZE;
                        float random_Y = (float)(Math.Cos(spawn_angle) * 8 + new Random().Next(10)) * DEFAULT_CHARACTER_SIZE;
                        if (spawn_angle > 270)
                        {
                            enemy.Position = (
                                _player.Position.X - random_X,
                                _player.Position.Y + random_Y
                            );
                        }
                        else if (spawn_angle > 180)
                        {
                            enemy.Position = (
                                _player.Position.X + random_X,
                                _player.Position.Y + random_Y
                            );
                        }
                        else if (spawn_angle > 90)
                        {
                            enemy.Position = (
                                _player.Position.X + random_X,
                                _player.Position.Y - random_Y
                            );
                        }
                        else
                        {
                            enemy.Position = (
                                _player.Position.X - random_X,
                                _player.Position.Y - random_Y
                            );
                        }

                        // Retry if the enemy is outside of the world
                        if (!IsInsideWorld(enemy))
                        {
                            reroll = true;
                            continue;
                        }

                        //check if the ennemy is in a wall
                        foreach (Obstacle obstacle in Obstacles)
                        {
                            if (IsOverlapping(enemy, obstacle) && obstacle.CanCollide)
                            {
                                reroll = true;
                                break;

                            }
                        }
                        //reroll if the ennemy is in a wall
                    } while (reroll);

                    // Update its LastDamage value
                    enemy.LastDamageTimer = 0;

                    // Add the enemy to the character list
                    Characters.Add(enemy);


                    // Add a wait before adding the next enemy
                    await Wait(20000 / GAMESPEED);
                }

                // Clear the wave enemies table
                waveEnemies.Clear();

                // Wait until all enemies are dead
                while (Characters.Count > 1 && _gamestate != Gamestate.finished)
                {
                    await Wait(1);
                }

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

        /// <summary>
        /// Tests to see if the given entity is inside or outside the world
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool IsInsideWorld(CFrame entity)
        {
            int intMapSize = CHUNK_SIZE_IN_TILES * CHUNK_AMOUNT * OBSTACLE_SIZE;
            return
                entity.Position.X >= 0 &&
                entity.Position.Y >= 0 &&
                entity.Position.X + entity.Size.Width <= intMapSize &&
                entity.Position.Y + entity.Size.Height <= intMapSize;
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

                // Draw all the background assets first
                foreach (Obstacle Floor in Obstacles)
                {
                    Obstacle.Type[] FloorTypes = [Obstacle.Type.Sand, Obstacle.Type.Grass, Obstacle.Type.Stone];

                    // Only continue if they're from one of the floor types
                    if (FloorTypes.Contains(Floor.ObstType))
                    {
                        float drawX = Floor.Position.X - cameraX;
                        float drawY = Floor.Position.Y - cameraY;

                        if (FloorChunkCache.TryGetValue(Floor.ObstType, out Bitmap? chunk))
                        {
                            bufferG.DrawImage(
                                chunk,
                                drawX * Zoom,
                                drawY * Zoom,
                                chunk.Width * Zoom,
                                chunk.Height * Zoom
                            );
                        }
                    }
                }


                // Draw all characters (not including player or characters with collisions off)
                foreach (Character character in Characters)
                {
                    if (character.CharType == Character.Type.Player || !character.CanCollide) continue;

                    float drawX = character.Position.X - cameraX;
                    float drawY = character.Position.Y - cameraY;

                    using (Bitmap Image = GetSprite(character.CharType))
                        bufferG.DrawImage(Image, drawX * Zoom, drawY * Zoom, character.Size.Width * Zoom, character.Size.Height * Zoom);
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
                            bufferG.DrawImage(Image, drawX * Zoom, drawY * Zoom, obstacle.Size.Width * Zoom, obstacle.Size.Height * Zoom);


                        // Draw its life
                        float screenX = (obstacle.Position.X - cameraX) * Zoom;
                        float screenY = (obstacle.Position.Y - cameraY) * Zoom;

                        using (Font scaledFont = new Font(TextHelpers.drawFont.FontFamily, TextHelpers.drawFont.Size * Zoom, TextHelpers.drawFont.Style))
                        {
                            SizeF textSize = bufferG.MeasureString($"{obstacle}", scaledFont);

                            float centeredX = screenX + (obstacle.Size.Width * Zoom / 2f) - (textSize.Width / 2f);

                            bufferG.DrawString(
                                $"{obstacle}",
                                scaledFont,
                                TextHelpers.writingBrush,
                                centeredX,
                                screenY - 16 * Zoom
                            );
                        }

                    }
                }

                // Draw all characters (not including player or characters with collisions on)
                foreach (Character character in Characters)
                {
                    if (character.CharType == Character.Type.Player || character.CanCollide) continue;

                    float drawX = character.Position.X - cameraX;
                    float drawY = character.Position.Y - cameraY;

                    using (Bitmap Image = GetSprite(character.CharType))
                        bufferG.DrawImage(Image, drawX * Zoom, drawY * Zoom, character.Size.Width * Zoom, character.Size.Height * Zoom);
                }

                // Draw all characters' health (not including player)
                foreach (Character character in Characters)
                {
                    if (character.CharType == Character.Type.Player) continue;

                    // Draw its life
                    float screenX = (character.Position.X - cameraX) * Zoom;
                    float screenY = (character.Position.Y - cameraY) * Zoom;

                    using (Font scaledFont = new Font(TextHelpers.drawFont.FontFamily, TextHelpers.drawFont.Size * Zoom, TextHelpers.drawFont.Style))
                    {
                        SizeF textSize = bufferG.MeasureString($"{character}", scaledFont);

                        float centeredX = screenX + (character.Size.Width * Zoom / 2f) - (textSize.Width / 2f);

                        bufferG.DrawString(
                            $"{character}",
                            scaledFont,
                            TextHelpers.writingBrush,
                            centeredX,
                            screenY - 16 * Zoom
                        );
                    }
                }

                // Draw projectiles
                foreach (Projectile projectile in Projectiles)
                {
                    float drawX = projectile.Position.X - cameraX;
                    float drawY = projectile.Position.Y - cameraY;

                    using (Bitmap Image = GetSprite(projectile.ProjType, projectile.RotationAngle))
                        bufferG.DrawImage(Image, drawX * Zoom, drawY * Zoom, projectile.Size.Width * Zoom, projectile.Size.Height * Zoom);
                }

                // Draw the player
                if (_player != null)
                {
                    float px = _player.Position.X - cameraX;
                    float py = _player.Position.Y - cameraY;

                    using (Bitmap Image = GetSprite(_player.CharType))
                        bufferG.DrawImage(Image, px * Zoom, py * Zoom, _player.Size.Width * Zoom, _player.Size.Height * Zoom);
                }
            }
        }

        private void ClampCamera()
        {
            int mapSize = CHUNK_SIZE_IN_TILES * CHUNK_AMOUNT * OBSTACLE_SIZE + OBSTACLE_SIZE;

            float viewportW = ClientSize.Width / Zoom;
            float viewportH = ClientSize.Height / Zoom;

            cameraX = Math.Clamp(cameraX, 0 - OBSTACLE_SIZE, mapSize - viewportW);
            cameraY = Math.Clamp(cameraY, 0 - OBSTACLE_SIZE, mapSize - viewportH);
        }

        private void UpdateCamera()
        {
            if (_player == null) return;

            float viewportW = ClientSize.Width / Zoom;
            float viewportH = ClientSize.Height / Zoom;

            cameraX = _player.Position.X + _player.Size.Width / 2f - viewportW / 2f;
            cameraY = _player.Position.Y + _player.Size.Height / 2f - viewportH / 2f;

            ClampCamera();
        }

        private void DrawUI(Graphics g)
        {
            if (_player != null)
            {
                g.DrawString($"Wave {_intWaveNumber} | Score: {Score} | Lives remaining: {_player.Lives}", TextHelpers.drawFont, TextHelpers.writingBrush, 8, 8);

                for (int i = 0; i < _player.Lives; i++)
                {
                    int x = 8 + (i * 20);
                    using (Bitmap Heart = (Bitmap)Sprites.Heart.Clone())
                        g.DrawImage(Heart, x, 32, 24, 24);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_gamestate == Gamestate.finished || backBuffer == null)
            {
                base.OnPaint(e);
                return;
            }

            e.Graphics.Clear(Color.Black);

            e.Graphics.DrawImage(backBuffer, 0, 0);

            // Draw UI on top
            DrawUI(e.Graphics);
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
            long now = Environment.TickCount64;
            DeltaTime = (now - LastFrameTime) / 1000f; // seconds
            LastFrameTime = now;

            if (_gamestate != Gamestate.running || _player == null)
                return;

            // Update camera position to the player's
            UpdateCamera();

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

            // Update the player's cooldown
            _player?.UpdateTimers();

            // Move the player if he should
            if (intMoveX != 0 || intMoveY != 0)
                _player?.Move(intMoveX, intMoveY);

            // Update the projectiles
            foreach (Projectile projectile in Projectiles)
                projectile.Update();

            // Update the enemies
            foreach (Character character in Characters)
                if (character is Enemy enemy)
                    enemy.Move();
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

        private CFrame ScreenToWorld(Point screen)
        {
            return new CFrame(screen.X / Zoom + cameraX, screen.Y / Zoom + cameraY);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Zoom += e.Delta > 0 ? 0.1f : -0.1f;
            Zoom = Math.Clamp(Zoom, 0.25f, 2f);
        }

        private void ShootMeUp_MouseClick(object sender, MouseEventArgs e)
        {
            // Only try and shoot something if the player is in game
            if (_gamestate != Gamestate.running || _player == null) return;

            // If it's a left click, shoot an arrow. Otherwise, shoot a fireball.
            Projectile.Type type = (e.Button == MouseButtons.Right)
                ? Projectile.Type.Fireball_Big
                : Projectile.Type.Arrow_Big;

            CFrame target = ScreenToWorld(e.Location);
            Projectile? projectile = _player.Shoot(target, type);

            if (projectile != null)
                Projectiles.Add(projectile);
        }

        protected override void OnResizeBegin(EventArgs e)
        {
            base.OnResizeBegin(e);
            isResizing = true;
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            isResizing = false;
            ResizeBackbuffer();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            CenterTitleUI();
            CenterPauseUI();

            if (!isResizing)
            {
                ResizeBackbuffer();
                Invalidate();
            }
        }
    }
}