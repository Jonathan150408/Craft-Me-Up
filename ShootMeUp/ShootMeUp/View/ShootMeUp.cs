using ShootMeUp.Helpers;
using ShootMeUp.Model;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using static ShootMeUp.Model.GameSettings;
using Font = System.Drawing.Font;

namespace ShootMeUp
{
    /// <summary>
    /// The ShootMeUp class is used to serve as a 2D playspace for the player and enemies.
    /// </summary>
    public partial class ShootMeUp : Form
    {
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
        private static readonly int CHUNK_AMOUNT = 32;

        private bool isResizing = false;

        /// <summary>
        /// The current state of the game
        /// </summary>
        private enum GameState
        {
            running,
            loading,
            paused,
            finished
        }
        private GameState _gameState;

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
        /// The game's settings button
        /// </summary>
        private Button? _settingsButton;

        /// <summary>
        /// The settings' title
        /// </summary>
        private Label? _settingsTitle;

        /// <summary>
        /// The settings' done button 
        /// </summary>
        private Button? _settingsDone;

        /// <summary>
        /// The settings' speed label
        /// </summary>
        private Label? _settingsSpeedLabel;

        /// <summary>
        /// The settings' speed panel
        /// </summary>
        private Panel? _settingsSpeedPanel;

        /// <summary>
        /// A list that contains all the projectiles
        /// </summary>
        private static List<Projectile> _projectiles = [];
        public static List<Projectile> Projectiles
        {
            get { return _projectiles; }
            set { _projectiles = value; }
        }

        /// <summary>
        /// A list that contains all the obstacles
        /// </summary>
        private static List<Obstacle> _obstacles = [];
        public static List<Obstacle> Obstacles
        {
            get { return _obstacles; }
            set { _obstacles = value; }
        }

        /// <summary>
        /// A list that contains all the characters
        /// </summary>
        private static List<Character> _characters = [];
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
        private static readonly Dictionary<Obstacle.Type, Bitmap> FloorChunkCache = [];

        private Bitmap backBuffer;
        private Graphics bufferG;

        public static float CameraX { get; private set; }
        public static float CameraY { get; private set; }

        //create a modal and a button for the pause menu
        PictureBox pauseModale;
        Button resumeButton;
        Button quitButton;

        // Variables used for time handling
        public static long LastFrameTime { get; set; }
        public static float DeltaTime { get; set; }

        /// <summary>
        /// The game's zoom (the smaller it is, the more you see)
        /// </summary>
        public static float Zoom { get; set; }
        private enum Biome
        {
            Plains,
            Desert,
            Mountain
        }

        private struct BiomeInfo
        {
            public int X;
            public int Y;
            public Biome Type;
        }

        private static readonly List<BiomeInfo> Biomes = [];

        /// <summary>
        /// Whether the world has generated or not
        /// </summary>
        private bool _worldReady;

        public ShootMeUp()
        {
            InitializeComponent();

            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimumSize = new Size(640, 640);

            _intWaveNumber = 1;
            _gameState = GameState.finished;

            // Create a new list of keys held down
            _keysHeldDown = [];

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
              ControlStyles.OptimizedDoubleBuffer |
              ControlStyles.UserPaint, true);
            this.DoubleBuffered = true;

            BackColor = ColorTranslator.FromHtml("#7f7f7f");

            rnd = new(GAMESEED);

            Zoom = 1;

            CameraX = 0;
            CameraY = 0;

            _worldReady = false;

            GameSettings.Load();

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
                Height = 64,
                Width = 256,
                AutoSize = false,
                Text = "Resume Game",
                BackColor = Color.White,
                Font = new Font("Consolas", 24, FontStyle.Bold),
            };

            quitButton = new()
            {
                Height = 32,
                Width = 64,
                Text = "Quit",
                BackColor = Color.White,
                Font = new Font("Consolas", 12, FontStyle.Bold),
            };

            resumeButton.Click += ResumeButton_Click;
            quitButton.Click += QuitButton_Click;

            backBuffer = new Bitmap(ClientSize.Width, ClientSize.Height);
            bufferG = Graphics.FromImage(backBuffer);

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
                _playButton.Top = (_titleLabel?.Bottom ?? (ClientSize.Height / 3)) + 128;
            }

            if (_settingsButton != null)
            {
                _settingsButton.Left = (ClientSize.Width - _settingsButton.Width) / 2;
                _settingsButton.Top = (_playButton?.Bottom ?? (ClientSize.Height / 3)) + 64;
            }
        }

        private void StopRound()
        {
            _gameState = GameState.finished;
            _player = null;
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
            if (_titleLabel != null)
            { 
                Controls.Remove(_titleLabel);
                _titleLabel.Dispose();
            }

            if (_playButton != null)
            {
                Controls.Remove(_playButton);
                _playButton.Dispose();
            }
            
            if (_settingsButton != null)
            {
                Controls.Remove(_settingsButton);
                _settingsButton.Dispose();
            }

            // Create the title label
            _titleLabel = new Label
            {
                Text = "Craft Me Up",
                Font = new Font("Consolas", 48, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent,
            };


            // Create the play button
            _playButton = new Button
            {
                Text = "Play the game",
                Font = new Font("Consolas", 24, FontStyle.Bold),
                BackColor = Color.White,
                Size = new(384, 64),
                AutoSize = false
            };

            // Create the settings button
            _settingsButton = new Button
            {
                Text = "Settings",
                Font = new Font("Consolas", 18, FontStyle.Regular),
                BackColor = Color.White,
                Size = new(224, 48),
                AutoSize = false
            };

            // Add controls to the form
            Controls.Add(_titleLabel);
            Controls.Add(_playButton);
            Controls.Add(_settingsButton);

            _titleLabel.Left = (ClientSize.Width - _titleLabel.Width) / 2;
            _titleLabel.Top = ClientSize.Height / 3 - _titleLabel.Height / 2;

            _playButton.Left = (ClientSize.Width - _playButton.Width) / 2;

            _playButton.Top = _titleLabel.Bottom + 256;

            // Set up the play button
            _playButton.Click += PlayButton_Click;

            // Set up the pause button

            _settingsButton.Click += SettingsButton_Click;

            // Center the ui
            CenterTitleUI();
        }

        private async void PlayButton_Click(object? sender, EventArgs e)
        {
            await StartGame();
            await StartWaves();
        }

        private void CenterSettingsUI()
        {
            if (_settingsTitle != null)
            {
                _settingsTitle.Left = (ClientSize.Width - _settingsTitle.Width) / 2;
                _settingsTitle.Top = ClientSize.Height / 3 - _settingsTitle.Height / 2;
            }

            if (_settingsSpeedLabel != null)
            {
                _settingsSpeedLabel.Left = (ClientSize.Width - _settingsSpeedLabel.Width) / 2;
                _settingsSpeedLabel.Top = (_settingsTitle?.Bottom ?? (ClientSize.Height / 3)) + 48;
            }

            if (_settingsSpeedPanel != null)
            {
                _settingsSpeedPanel.Left = (ClientSize.Width - _settingsSpeedPanel.Width) / 2;
                _settingsSpeedPanel.Top = (_settingsSpeedLabel?.Bottom ?? 0) + 8;

                int spacing = 16;

                List<Button> buttons = _settingsSpeedPanel.Controls.OfType<Button>().ToList();

                if (buttons.Count > 0)
                {
                    int totalWidth =
                        buttons.Sum(b => b.Width) +
                        spacing * (buttons.Count - 1);

                    int startX = (_settingsSpeedPanel.Width - totalWidth) / 2;
                    int y = (_settingsSpeedPanel.Height - buttons[0].Height) / 2;

                    for (int i = 0; i < buttons.Count; i++)
                    {
                        buttons[i].Left = startX;
                        buttons[i].Top = y;
                        startX += buttons[i].Width + spacing;
                    }
                }
            }

            if (_settingsDone != null)
            {
                _settingsDone.Left = (ClientSize.Width - _settingsDone.Width) / 2;
                _settingsDone.Top = (_settingsSpeedPanel?.Bottom ?? (_settingsTitle?.Bottom ?? 0)) + 64;
            }
        }

        private void RefreshSpeedButtons()
        {
            if (_settingsSpeedPanel == null)
                return;

            foreach (Control c in _settingsSpeedPanel.Controls)
            {
                if (c is Button b)
                    b.BackColor = Color.White;
            }

            int index = GameSettings.Current.GameSpeed switch
            {
                GameSettings.GameSpeedOption.Slow => 0,
                GameSettings.GameSpeedOption.Normal => 1,
                GameSettings.GameSpeedOption.Fast => 2,
                _ => 1
            };

            if (index < _settingsSpeedPanel.Controls.Count &&
                _settingsSpeedPanel.Controls[index] is Button selected)
            {
                selected.BackColor = Color.LightGreen;
            }
        }

        private static Button CreateOptionButton(string text, Action onClick, bool selected = false)
        {
            // Create the button
            Button newButton = new()
            {
                Text = text,
                Width = 100,
                Height = 40,
                BackColor = selected ? Color.LightGreen : Color.White,
                Font = new Font("Consolas", 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };

            // Bind onClick to it
            newButton.Click += (_, _) => onClick();

            return newButton;
        }

        /// <summary>
        /// Displays the settings
        /// </summary>
        private void DisplaySettings()
        {
            // Removes main menu controls
            Controls.Remove(_titleLabel);
            Controls.Remove(_playButton);
            Controls.Remove(_settingsButton);

            _settingsTitle = new Label
            {
                Text = "Settings",
                Font = new Font("Consolas", 32, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent,
            };

            // Speed settings
            _settingsSpeedLabel = new()
            {
                Text = "Game Speed",
                Font = new Font("Consolas", 16, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                BackColor = Color.Transparent,
            };

            _settingsSpeedPanel = new()
            {
                Width = 512,
                Height = 64,
                BackColor = Color.Transparent
            };

            Button slowBtn = CreateOptionButton("Slow", () =>
            {
                GameSettings.Current.GameSpeed = GameSettings.GameSpeedOption.Slow;
                GameSettings.Save();
                RefreshSpeedButtons();
            });

            Button normalBtn = CreateOptionButton("Normal", () =>
            {
                GameSettings.Current.GameSpeed = GameSettings.GameSpeedOption.Normal;
                GameSettings.Save();
                RefreshSpeedButtons();
            });

            Button fastBtn = CreateOptionButton("Fast", () =>
            {
                GameSettings.Current.GameSpeed = GameSettings.GameSpeedOption.Fast;
                GameSettings.Save();
                RefreshSpeedButtons();
            });


            _settingsSpeedPanel?.Controls.AddRange([slowBtn, normalBtn, fastBtn]);

            RefreshSpeedButtons();

            _settingsDone = new Button
            {
                Text = "Done",
                Font = new Font("Consolas", 18, FontStyle.Regular),
                BackColor = Color.White,
                Size = new(224, 48),
                AutoSize = false
            };

            Controls.Add(_settingsTitle);
            Controls.Add(_settingsDone);
            Controls.Add(_settingsSpeedLabel);
            Controls.Add(_settingsSpeedPanel);


            _settingsDone.Click += SettingsDone_Click;

            // Center the ui
            CenterSettingsUI();
        }

        private void SettingsDone_Click(object? sender, EventArgs e)
        {
            GameSettings.Save();

            // Remove everything from the settings
            Controls.Remove(_settingsTitle);
            _settingsTitle?.Dispose();

            Controls.Remove(_settingsDone);
            _settingsDone?.Dispose();

            Controls.Remove(_settingsSpeedLabel);
            _settingsSpeedLabel?.Dispose();

            Controls.Remove(_settingsSpeedPanel);
            _settingsSpeedPanel?.Dispose();

            // Add the main menu controls back
            Controls.Add(_titleLabel);
            Controls.Add(_playButton);
            Controls.Add(_settingsButton);
        }

        private void SettingsButton_Click(object? sender, EventArgs e)
        {
            DisplaySettings();
        }

        private void QuitButton_Click(object? sender, EventArgs e)
        {
            DisplayPauseMenu();
            StopRound();
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

            if (quitButton != null)
            {
                quitButton.Left = (ClientSize.Width - quitButton.Width) / 2;
                quitButton.Top = (resumeButton?.Bottom ?? (ClientSize.Height / 3)) + 32;

            }
        }

        /// <summary>
        /// Pauses the game and displays the pause menu
        /// </summary>
        private void DisplayPauseMenu()
        {
            if (this._gameState == GameState.running)
            {
                Controls.Add(resumeButton);
                Controls.Add(pauseModale);
                Controls.Add(quitButton);

                quitButton.BringToFront();
                resumeButton.BringToFront();

                //stops the ticker to pause the game
                this.ticker.Stop();
                this._gameState = GameState.paused;

                CenterPauseUI();
            }
            else if (this._gameState == GameState.paused)
            {
                Controls.Remove(pauseModale);
                Controls.Remove(resumeButton);
                Controls.Remove(quitButton);

                long now = Environment.TickCount64;
                DeltaTime = (now - LastFrameTime) / 1000f; // seconds
                LastFrameTime = now;

                //restart the ticker
                this.ticker.Start();
                this._gameState = GameState.running;
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
        private async Task StartGame()
        {
            BackgroundImage = null;
            BackgroundImageLayout = ImageLayout.None;

            // Remove title screen controls
            Controls.Remove(_titleLabel);
            Controls.Remove(_playButton);
            Controls.Remove(_settingsButton);
            _titleLabel?.Dispose();
            _playButton?.Dispose();
            _settingsButton?.Dispose();

            // Reset values
            Score = 0;
            _intWaveNumber = 1;
            LastFrameTime = Environment.TickCount64;

            Characters.Clear();
            Obstacles.Clear();
            Projectiles.Clear();

            // Cache floor textures for easier render
            InitializeFloorChunks();

            GenerateBiomeInfo();

            // Cache floor textures for easier render
            InitializeFloorChunks();

            // Force redraw
            RenderFrame();
            Invalidate();

            _gameState = GameState.loading;
            _worldReady = false;

            await Task.Run(() => GenerateWorld());

            _worldReady = true;
            _gameState = GameState.running;
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
            // Unchecked keyword used to ignore overflow and roll back to 0
            unchecked
            {
                int n = x * 1619 + y * 31337 + GAMESEED * 1013;
                n = (n << 13) ^ n;
                return 1f - ((n * (n * n * 60493 + 19990303) + 1376312589)
                             & 0x7fffffff) / 1073741824f;
            }
        }

        static float SmoothBiomeNoise(float x, float y)
        {
            int x0 = (int)Math.Floor(x);
            int y0 = (int)Math.Floor(y);

            float xFrac = x - x0;
            float yFrac = y - y0;

            float n00 = BiomeNoise(x0, y0);
            float n10 = BiomeNoise(x0 + 1, y0);
            float n01 = BiomeNoise(x0, y0 + 1);
            float n11 = BiomeNoise(x0 + 1, y0 + 1);

            float u = Fade(xFrac);
            float v = Fade(yFrac);

            float nx0 = Lerp(n00, n10, u);
            float nx1 = Lerp(n01, n11, u);

            return Lerp(nx0, nx1, v);
        }

        static float Fade(float t)
        {
            // Perlin fade function (smooth curve)
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// Generate biome informations
        /// </summary>
        private void GenerateBiomeInfo()
        {
            Biomes.Clear();

            // Seed count proportional to map area
            int seedCount = Math.Max(10, (CHUNK_AMOUNT * CHUNK_AMOUNT) / 150);

            for (int i = 0; i < seedCount; i++)
            {
                Biomes.Add(new BiomeInfo
                {
                    X = rnd.Next(CHUNK_AMOUNT),
                    Y = rnd.Next(CHUNK_AMOUNT),
                    Type = (Biome)rnd.Next(3)
                });
            }
        }

        /// <summary>
        /// Gets the chunk's biome info and uses it
        /// </summary>
        /// <param name="chunkX"></param>
        /// <param name="chunkY"></param>
        /// <returns></returns>
        private Biome GetChunkBiome(int chunkX, int chunkY)
        {
            BiomeInfo closest = Biomes[0];
            float bestDist = float.MaxValue;

            // This controls the size of the biome "blobs"
            float biomeScale = 1f;

            foreach (var seed in Biomes)
            {
                float dx = (chunkX - seed.X) * biomeScale;
                float dy = (chunkY - seed.Y) * biomeScale;

                // Distort edges
                float distortion = SmoothBiomeNoise(chunkX * 0.1f, chunkY * 0.1f) * 1.5f;

                // Add distortion after scaling so it doesn't grow with world size
                float dist = dx * dx + dy * dy + distortion * distortion;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    closest = seed;
                }
            }

            return closest.Type;
        }

        /// <summary>
        /// Generate the game itself
        /// </summary>
        private void GenerateWorld()
        {
            // Set the game state to true
            _gameState = GameState.running;

            // Calculate the center of the map
            float fltMapSize = CHUNK_SIZE_IN_TILES * CHUNK_AMOUNT * OBSTACLE_SIZE;

            float fltAreaCenterX = fltMapSize / 2;

            // Create a new player
            _player = new(fltAreaCenterX, fltAreaCenterX, DEFAULT_CHARACTER_SIZE, Character.Type.Player, GameSettings.Current.GameSpeedValue);
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

                    Biome biome = GetChunkBiome(chunkX, chunkY);

                    Obstacle.Type randomFloor = biome switch
                    {
                        Biome.Plains => Obstacle.Type.Grass,
                        Biome.Desert => Obstacle.Type.Sand,
                        Biome.Mountain => Obstacle.Type.Stone,
                        _ => Obstacle.Type.Barrier
                    };

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

                        if (attempts < 100)
                            break;
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
                WaveEnemies.Add(new Enemy(0, 0, intCharSize, selectedType, GameSettings.Current.GameSpeedValue, _player));

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
                    WaveEnemies.Add(new Enemy(0, 0, (int)(DEFAULT_CHARACTER_SIZE * fltSizeMultiplicator), BossType, GameSettings.Current.GameSpeedValue, _player));
            }

            return WaveEnemies;
        }

        /// <summary>
        /// Wait for a specified amount of time
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        private async Task Wait(int amount)
        {
            do
            {
                do
                {
                    await Task.Delay(1);
                } while (_gameState == GameState.paused);

                amount--;
            } while (amount > 0);
        }

        /// <summary>
        /// Starts the wave system
        /// </summary>
        private async Task StartWaves()
        {
            while (_gameState != GameState.finished && (_player != null && _player.Lives > 0))
            {
                // Add a wait before the next wave
                await Wait(20000 / GameSettings.Current.GameSpeedValue);

                // Get the wave's enemies
                List<Enemy> waveEnemies = GenerateWaves(_intWaveNumber);

                foreach (Enemy enemy in waveEnemies)
                {
                    // End the wave system if the game stopped
                    if (_gameState == GameState.finished)
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
                    await Wait(20000 / GameSettings.Current.GameSpeedValue);
                }

                // Clear the wave enemies table
                waveEnemies.Clear();

                // Wait until all enemies are dead
                while (Characters.Count > 1 && _gameState != GameState.finished)
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

        private void DrawBackground(Graphics g)
        {
            Bitmap tile = Sprites.Stone;
            int tileSize = OBSTACLE_SIZE;

            int tilesX = (int)Math.Ceiling(ClientSize.Width / (float)tileSize) + 1;
            int tilesY = (int)Math.Ceiling(ClientSize.Height / (float)tileSize) + 1;

            for (int x = 0; x < tilesX; x++)
            {
                for (int y = 0; y < tilesY; y++)
                {
                    g.DrawImage(
                        tile,
                        x * tileSize,
                        y * tileSize,
                        tileSize,
                        tileSize
                    );
                }
            }
        }

        private bool IsOnScreen(float x, float y, float w, float h)
        {
            float screenW = ClientSize.Width;
            float screenH = ClientSize.Height;

            float screenX = x * Zoom;
            float screenY = y * Zoom;
            float screenWObj = w * Zoom;
            float screenHObj = h * Zoom;

            return screenX + screenWObj >= 0 &&
                   screenX <= screenW &&
                   screenY + screenHObj >= 0 &&
                   screenY <= screenH;
        }

        /// <summary>
        /// Render a new frame
        /// </summary>
        private void RenderFrame()
        {
            // Show a loading text
            if (_gameState == GameState.loading || !_worldReady)
            {
                bufferG.Clear(Color.Black);

                using Font font = new Font("Consolas", 32, FontStyle.Bold);
                SizeF textSize = bufferG.MeasureString("Loading...", font);

                float x = (ClientSize.Width - textSize.Width) / 2f;
                float y = (ClientSize.Height - textSize.Height) / 2f;

                bufferG.DrawString("Loading...", font, Brushes.White, x, y);
                return;
            }

            if (_player != null)
            {
                // Clear the frame
                DrawBackground(bufferG);

                using Font scaledFont = new Font(TextHelpers.drawFont.FontFamily, TextHelpers.drawFont.Size * Zoom, TextHelpers.drawFont.Style);

                // Draw all the background assets first
                Obstacle.Type[] FloorTypes = [Obstacle.Type.Sand, Obstacle.Type.Grass, Obstacle.Type.Stone];

                foreach (Obstacle Floor in Obstacles)
                {
                    // Only continue if they're from one of the floor types
                    if (FloorTypes.Contains(Floor.ObstType))
                    {
                        float drawX = Floor.Position.X - CameraX;
                        float drawY = Floor.Position.Y - CameraY;

                        if (!IsOnScreen(drawX, drawY, Floor.Size.Width, Floor.Size.Height))
                            continue;

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

                    float drawX = character.Position.X - CameraX;
                    float drawY = character.Position.Y - CameraY;

                    if (!IsOnScreen(drawX, drawY, character.Size.Width, character.Size.Height))
                        continue;
                    bufferG.DrawImage(GetSprite(character.CharType), drawX * Zoom, drawY * Zoom, character.Size.Width * Zoom, character.Size.Height * Zoom);
                }

                // Draw obstacles that can collide
                foreach (Obstacle obstacle in Obstacles)
                {
                    // Only continue if they're not from one of the floor types, and have collisions on
                    if (FloorTypes.Contains(obstacle.ObstType) && !obstacle.CanCollide)
                        continue;

                    float drawX = obstacle.Position.X - CameraX;
                    float drawY = obstacle.Position.Y - CameraY;

                    if (!IsOnScreen(drawX, drawY, obstacle.Size.Width, obstacle.Size.Height))
                        continue;

                    bufferG.DrawImage(GetSprite(obstacle.ObstType, obstacle.Size), drawX * Zoom, drawY * Zoom, obstacle.Size.Width * Zoom, obstacle.Size.Height * Zoom);

                }

                // Draw obstacle health
                foreach (Obstacle obstacle in Obstacles)
                {
                    // Only continue if they're not from one of the floor types
                    if (!(obstacle.ObstType == Obstacle.Type.Sand || obstacle.ObstType == Obstacle.Type.Grass || obstacle.ObstType == Obstacle.Type.Stone))
                    {
                        float drawX = obstacle.Position.X - CameraX;
                        float drawY = obstacle.Position.Y - CameraY;

                        if (!IsOnScreen(drawX, drawY, obstacle.Size.Width, obstacle.Size.Height))
                            continue;

                        float screenX = (obstacle.Position.X - CameraX) * Zoom;
                        float screenY = (obstacle.Position.Y - CameraY) * Zoom;


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

                // Draw all characters (not including player or characters with collisions on)
                foreach (Character character in Characters)
                {
                    if (character.CharType == Character.Type.Player || character.CanCollide) continue;

                    float drawX = character.Position.X - CameraX;
                    float drawY = character.Position.Y - CameraY;

                    if (!IsOnScreen(drawX, drawY, character.Size.Width, character.Size.Height))
                        continue;
                    bufferG.DrawImage(GetSprite(character.CharType), drawX * Zoom, drawY * Zoom, character.Size.Width * Zoom, character.Size.Height * Zoom);
                }

                // Draw all characters' health (not including player)
                foreach (Character character in Characters)
                {
                    if (character.CharType == Character.Type.Player) continue;

                    // Draw its life
                    float screenX = (character.Position.X - CameraX) * Zoom;
                    float screenY = (character.Position.Y - CameraY) * Zoom;

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

                // Draw projectiles
                foreach (Projectile projectile in Projectiles)
                {
                    float drawX = projectile.Position.X - CameraX;
                    float drawY = projectile.Position.Y - CameraY;

                    if (!IsOnScreen(drawX, drawY, projectile.Size.Width, projectile.Size.Height))
                        continue;

                    bufferG.DrawImage(GetSprite(projectile.ProjType, projectile.RotationAngle), drawX * Zoom, drawY * Zoom, projectile.Size.Width * Zoom, projectile.Size.Height * Zoom);
                }

                // Draw the player
                if (_player != null)
                {
                    float px = _player.Position.X - CameraX;
                    float py = _player.Position.Y - CameraY;

                    bufferG.DrawImage(GetSprite(_player.CharType), px * Zoom, py * Zoom, _player.Size.Width * Zoom, _player.Size.Height * Zoom);
                }

                // Draw obstacles that has collisions off
                foreach (Obstacle obstacle in Obstacles)
                {
                    // Only continue if they're not from one of the floor types, and have collisions off
                    if (FloorTypes.Contains(obstacle.ObstType) && !obstacle.CanCollide)
                        continue;

                    float drawX = obstacle.Position.X - CameraX;
                    float drawY = obstacle.Position.Y - CameraY;

                    if (!IsOnScreen(drawX, drawY, obstacle.Size.Width, obstacle.Size.Height))
                        continue;

                    bufferG.DrawImage(GetSprite(obstacle.ObstType, obstacle.Size), drawX * Zoom, drawY * Zoom, obstacle.Size.Width * Zoom, obstacle.Size.Height * Zoom);
                }
            }
        }

        /// <summary>
        /// Clamp the camera
        /// </summary>
        private void ClampCamera()
        {
            float mapSize = CHUNK_SIZE_IN_TILES * CHUNK_AMOUNT * OBSTACLE_SIZE;

            float viewportW = ClientSize.Width / Zoom;
            float viewportH = ClientSize.Height / Zoom;

            // Center map if viewport is larger than world
            if (viewportW >= mapSize)
            {
                CameraX = ((mapSize + OBSTACLE_SIZE) - viewportW) / 2f;
            }
            else
            {
                CameraX = Math.Clamp(CameraX, -OBSTACLE_SIZE, (mapSize + OBSTACLE_SIZE) - viewportW);
            }

            if (viewportH >= mapSize)
            {
                CameraY = ((mapSize + OBSTACLE_SIZE) - viewportH) / 2f;
            }
            else
            {
                CameraY = Math.Clamp(CameraY, -OBSTACLE_SIZE, (mapSize + OBSTACLE_SIZE) - viewportH);
            }
        }

        private void UpdateCamera()
        {
            if (_player == null) return;

            float viewportW = ClientSize.Width / Zoom;
            float viewportH = ClientSize.Height / Zoom;

            CameraX = _player.Position.X + _player.Size.Width / 2f - viewportW / 2f;
            CameraY = _player.Position.Y + _player.Size.Height / 2f - viewportH / 2f;

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
                    g.DrawImage(Sprites.Heart, x, 32, 24, 24);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_gameState == GameState.finished || backBuffer == null)
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
                    StopRound();
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

            if (_player == null)
                return;

            // Update camera position to the player's
            UpdateCamera();

            // Create a new frame
            RenderFrame();

            // Tell the program to render the game again
            Invalidate();

            // Don't continue if the game is finished/paused
            if (_gameState != GameState.running)
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
            intMoveX *= GameSettings.Current.GameSpeedValue;
            intMoveY *= GameSettings.Current.GameSpeedValue;

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

        private static CFrame ScreenToWorld(Point screen)
        {
            return new CFrame(screen.X / Zoom + CameraX, screen.Y / Zoom + CameraY);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            // Small zoom value = zoom out
            // Big zoom value = zoom in

            Zoom += e.Delta > 0 ? 0.1f : -0.1f;
            Zoom = Math.Clamp(Zoom, 0.5f, 2f);
        }

        private void ShootMeUp_MouseClick(object sender, MouseEventArgs e)
        {
            // Only try and shoot something if the player is in game
            if (_gameState != GameState.running || _player == null) return;

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
            CenterSettingsUI();

            if (!isResizing)
            {
                ResizeBackbuffer();
                Invalidate();
            }
        }
    }
}