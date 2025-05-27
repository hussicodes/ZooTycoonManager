﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;

namespace ZooTycoonManager
{
    public class GameWorld : Game
    {
        // Tile and grid settings
        public const int TILE_SIZE = 32; // Size of each tile in pixels
        public const int GRID_WIDTH = 100;
        public const int GRID_HEIGHT = 100;

        private static GameWorld _instance;
        private static readonly object _lock = new object();
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;  // Add font field
        Map map;
        TileRenderer tileRenderer;
        Texture2D[] tileTextures;
        private FPSCounter _fpsCounter;  // Add FPS counter field

        // Money Management
        private MoneyDisplay _moneyDisplay;

        // Walkable map for pathfinding
        public bool[,] WalkableMap { get; private set; }

        // Fence and enclosure management
        private bool isPlacingEnclosure = true;
        private List<Habitat> habitats;
        private List<Visitor> visitors; // Add visitors list
        private int _nextHabitatId = 1;
        private int _nextAnimalId = 1;
        private int _nextVisitorId = 1;

        // Visitor spawning settings
        private float _visitorSpawnTimer = 0f;
        private const float VISITOR_SPAWN_INTERVAL = 10.0f; // Spawn every 10 seconds
        private Vector2 _visitorSpawnPosition;
        private const int VISITOR_SPAWN_REWARD = 20;

        // Buttons
        private Texture2D shopButtonBackground;
        private Texture2D shopIcon;
        private Texture2D wideButtonTexture;

        Button shopButton;
        Button minerBtn;
        Button knightBtn;
        Button archerBtn;
        Button villagerBtn;
        Button structuresButton;
        Button unitButton;
        Button fortressBtn;
        Button towerBtn;
        Button houseBtn;
        Button treeBtn;

        List<Button> structureButtons = new List<Button>();
        List<Button> unitButtons = new List<Button>();

        //For selected unit border
        private Texture2D pixel;

        bool showStructureMenu = false;
        bool showUnitMenu = false;

        public Unit Selected_unit { get; set; }

        bool isShopMenuOpen = false;

        // Public property to access the spawn/exit position
        public Vector2 VisitorSpawnExitPosition => _visitorSpawnPosition;

        // Camera instance
        private Camera _camera;

        // Window state
        private bool _isFullscreen = false;

        private List<Visitor> _visitorsToDespawn = new List<Visitor>(); // Added for despawning

        public List<Habitat> GetHabitats()
        {
            return habitats;
        }

        public List<Visitor> GetVisitors()
        {
            return visitors;
        }

        public static GameWorld Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GameWorld();
                        }
                    }
                }
                return _instance;
            }
        }

        private GameWorld()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.IsFullScreen = false;
            Window.AllowUserResizing = true;
            _graphics.ApplyChanges();
            
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set to use monitor refresh rate instead of fixed 60 FPS
            IsFixedTimeStep = false;
            TargetElapsedTime = TimeSpan.FromTicks(1);

            // Initialize map first
            map = new Map(GRID_WIDTH, GRID_HEIGHT); 

            // Initialize camera
            _camera = new Camera(_graphics);
            _camera.SetMapDimensions(GRID_WIDTH * TILE_SIZE, GRID_HEIGHT * TILE_SIZE);

            // Initialize walkable map from the map object
            WalkableMap = map.ToWalkableArray();

            habitats = new List<Habitat>();
            visitors = new List<Visitor>(); // Initialize visitors list

            // Find the top-most path tile for visitor spawning
            Vector2 pathSpawnTile = Vector2.Zero; // Default to top-left if no path found
            bool foundSpawn = false;
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    // TextureIndex 1 is the path tile (Dirt1)
                    if (map.Tiles[x, y].TextureIndex == 1) 
                    {
                        pathSpawnTile = new Vector2(x, y);
                        foundSpawn = true;
                        break; 
                    }
                }
                if (foundSpawn) break;
            }
            _visitorSpawnPosition = TileToPixel(pathSpawnTile);

            // Initialize MoneyManager and MoneyDisplay
            MoneyManager.Instance.Initialize(0); // Initialize with 0, actual value loaded in Initialize()
            _moneyDisplay = new MoneyDisplay();
            MoneyManager.Instance.Attach(_moneyDisplay); // Attach MoneyDisplay as observer
            MoneyManager.Instance.Notify(); // Initial notification to set initial money text

            // Subscribe to window resize event
            Window.ClientSizeChanged += OnClientSizeChanged;
        }

        private void OnClientSizeChanged(object sender, EventArgs e)
        {
            if (!_isFullscreen)
            {
                _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                _graphics.ApplyChanges();
                _camera.UpdateViewport(_graphics.GraphicsDevice.Viewport);
            }
        }

        // Convert pixel position to tile position
        public static Vector2 PixelToTile(Vector2 pixelPos)
        {
            return new Vector2(
                (int)(pixelPos.X / TILE_SIZE),
                (int)(pixelPos.Y / TILE_SIZE)
            );
        }

        // Convert tile position to pixel position (center of tile)
        public static Vector2 TileToPixel(Vector2 tilePos)
        {
            return new Vector2(
                tilePos.X * TILE_SIZE + TILE_SIZE / 2,
                tilePos.Y * TILE_SIZE + TILE_SIZE / 2
            );
        }

        protected override void Initialize()
        {
            var (loadedHabitats, nextHabitatId, nextAnimalId, nextVisitorId, loadedMoney) = DatabaseManager.Instance.LoadGame(Content);
            habitats = loadedHabitats;
            _nextHabitatId = nextHabitatId;
            _nextAnimalId = nextAnimalId;
            _nextVisitorId = nextVisitorId;
            MoneyManager.Instance.Initialize(loadedMoney);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("font");  // Load the font
            _fpsCounter = new FPSCounter(_font, _graphics);  // Initialize FPS counter with graphics manager
            tileTextures = new Texture2D[2];
            tileTextures[0] = Content.Load<Texture2D>("Grass1");
            tileTextures[1] = Content.Load<Texture2D>("Dirt1");

            //Used for selected unit border
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            // map = new Map(GRID_WIDTH, GRID_HEIGHT); // yo, this is where the size happens -- This line is now redundant
            tileRenderer = new TileRenderer(tileTextures);

            // Load content for all habitats and their animals
            foreach (var habitat in habitats)
            {
                habitat.LoadAnimalContent(Content);
            }
            Habitat.LoadContent(Content);

            shopButtonBackground = Content.Load<Texture2D>("Button_Disable");
            shopIcon = Content.Load<Texture2D>("Disable_07");
            wideButtonTexture = Content.Load<Texture2D>("Button_Disable_3Slides");

            Rectangle buttonBounds = new Rectangle(
                GraphicsDevice.Viewport.Width - 64 - 10,
                10,
                64,
                64
                );
            shopButton = new Button(shopButtonBackground, _font, buttonBounds, "");



            int menuOffsetX = 140; // hvor langt ind fra knappen, så intet klippes
            int menuX = shopButton.Bounds.X - menuOffsetX;
            int menuY = shopButton.Bounds.Bottom + 20;

            Rectangle structuresRect = new Rectangle(
                menuX + 10, menuY, 160, 40);
            structuresButton = new Button(wideButtonTexture, _font, structuresRect, "Structures");

            Rectangle unitRect = new Rectangle(
                menuX + 10, menuY + 50, 160, 40);
            unitButton = new Button(wideButtonTexture, _font, unitRect, "Unit");

            fortressBtn = new Button(wideButtonTexture, _font, new Rectangle(1050, 200, 192, 40), "Fortress - 50");
            structureButtons.Add(fortressBtn);

            towerBtn = new Button(wideButtonTexture, _font, new Rectangle(1050, 250, 192, 40), "Tower - 40");
            structureButtons.Add(towerBtn);

            houseBtn = new Button(wideButtonTexture, _font, new Rectangle(1050, 300, 192, 40), "House - 25");
            structureButtons.Add(houseBtn);

            treeBtn = new Button(wideButtonTexture, _font, new Rectangle(1050, 350, 192, 40), "Tree - 2");
            structureButtons.Add(treeBtn);

            villagerBtn = new Button(wideButtonTexture, _font, new Rectangle(1050, 200, 192, 40), "Villager - 5");
            unitButtons.Add(villagerBtn);

            minerBtn = new Button(wideButtonTexture, _font, new Rectangle(1050, 250, 192, 40), "Miner - 15");
            unitButtons.Add(minerBtn);

            knightBtn = new Button(wideButtonTexture, _font, new Rectangle(1050, 300, 192, 40), "Knight - 30");
            unitButtons.Add(knightBtn);

            archerBtn = new Button(wideButtonTexture, _font, new Rectangle(1050, 350, 192, 40), "Archer - 25");
            unitButtons.Add(archerBtn);

            int topPadding = 10;
            int spacing = 10;
            int displayWidth = wideButtonTexture.Width;
            int displayHeight = wideButtonTexture.Height;
        }

        MouseState prevMouseState;
        KeyboardState prevKeyboardState;

        private void PlaceFence(Vector2 pixelPosition)
        {
            Debug.WriteLine($"PlaceFence called with pixel position: {pixelPosition}, isPlacingEnclosure: {isPlacingEnclosure}");

            // Cost of placing a habitat
            decimal habitatCost = 10000;

            // Attempt to spend money
            if (MoneyManager.Instance.SpendMoney(habitatCost))
            {
                // Create a new habitat and place its enclosure
                Habitat newHabitat = new Habitat(pixelPosition, Habitat.DEFAULT_ENCLOSURE_SIZE, Habitat.DEFAULT_ENCLOSURE_SIZE, _nextHabitatId++);
                habitats.Add(newHabitat);
                newHabitat.PlaceEnclosure(pixelPosition);
            }
            else
            {
                // Optionally, provide feedback to the user that they don't have enough money
                Debug.WriteLine("Not enough money to place a habitat.");
            }

            
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _fpsCounter.Update(gameTime);  // Update FPS counter

            MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();

            // Handle F11 for fullscreen toggle
            if (keyboard.IsKeyDown(Keys.F11) && !prevKeyboardState.IsKeyDown(Keys.F11))
            {
                ToggleFullscreen();
            }

            // Update camera
            _camera.Update(gameTime, mouse, prevMouseState, keyboard, prevKeyboardState);

            // Handle 'C' key press for toggling camera clamping
            if (keyboard.IsKeyDown(Keys.C) && !prevKeyboardState.IsKeyDown(Keys.C))
            {
                _camera.ToggleClamping();
            }

            // Convert mouse position to world coordinates
            Vector2 worldMousePosition = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));

            // Handle 'A' key press for spawning animals
            if (keyboard.IsKeyDown(Keys.A) && !prevKeyboardState.IsKeyDown(Keys.A))
            {
                // Find the habitat that contains the world mouse position
                Habitat targetHabitat = habitats.FirstOrDefault(h => h.ContainsPosition(worldMousePosition));
                if (targetHabitat != null)
                {
                    targetHabitat.SpawnAnimal(worldMousePosition);
                }
            }

            // Handle automatic visitor spawning
            bool animalsExist = habitats.Any(h => h.GetAnimals().Count > 0);
            if (animalsExist)
            {
                _visitorSpawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_visitorSpawnTimer >= VISITOR_SPAWN_INTERVAL)
                {
                    _visitorSpawnTimer = 0f; // Reset timer

                    // Spawn visitor
                    Visitor newVisitor = new Visitor(_visitorSpawnPosition, _nextVisitorId++);
                    newVisitor.LoadContent(Content);
                    visitors.Add(newVisitor);

                    // Add money
                    MoneyManager.Instance.AddMoney(VISITOR_SPAWN_REWARD);
                    Debug.WriteLine($"Visitor spawned at {_visitorSpawnPosition}. Added ${VISITOR_SPAWN_REWARD}.");
                }
            }
            else
            {
                _visitorSpawnTimer = 0f; // Reset timer if no animals exist to prevent instant spawn when an animal is added
            }

            // Handle 'B' key press for manually spawning visitors (debugging)
            if (keyboard.IsKeyDown(Keys.B) && !prevKeyboardState.IsKeyDown(Keys.B))
            {
                Visitor newVisitor = new Visitor(_visitorSpawnPosition, _nextVisitorId++);
                newVisitor.LoadContent(Content);
                visitors.Add(newVisitor);
                Debug.WriteLine($"Manually spawned visitor at {_visitorSpawnPosition} for debugging.");
            }

            if (keyboard.IsKeyDown(Keys.S) && !prevKeyboardState.IsKeyDown(Keys.S))
            {
                DatabaseManager.Instance.SaveGame(habitats);
            }

            // Handle 'O' key press for clearing everything
            if (keyboard.IsKeyDown(Keys.O) && !prevKeyboardState.IsKeyDown(Keys.O))
            {
                habitats.Clear();
                visitors.Clear();
                _nextHabitatId = 1;
                _nextAnimalId = 1;
                _nextVisitorId = 1;
            }

            // Handle 'M' key press for adding money (debugging)
            if (keyboard.IsKeyDown(Keys.M) && !prevKeyboardState.IsKeyDown(Keys.M))
            {
                MoneyManager.Instance.AddMoney(100000);
                Debug.WriteLine("Added $100,000 for debugging.");
            }

            if (mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed && prevMouseState.LeftButton != Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                // Make the first animal in the first habitat pathfind to the world mouse position
                if (habitats.Count > 0 && habitats[0].GetAnimals().Count > 0)
                {
                    habitats[0].GetAnimals()[0].PathfindTo(worldMousePosition);
                }
            }

            // Handle right mouse button for fence placement
            if (mouse.RightButton == ButtonState.Pressed && prevMouseState.RightButton != ButtonState.Pressed)
            {
                PlaceFence(worldMousePosition);
            }

            // Update all habitats and their animals
            foreach (var habitat in habitats)
            {
                habitat.Update(gameTime);
            }

            // Process despawning visitors
            if (_visitorsToDespawn.Count > 0)
            {
                foreach (var visitorToRemove in _visitorsToDespawn)
                {
                    visitors.Remove(visitorToRemove);
                    Debug.WriteLine($"Visitor {visitorToRemove.VisitorId} has been despawned.");
                }
                _visitorsToDespawn.Clear();
            }

            prevMouseState = mouse;
            prevKeyboardState = keyboard;

            base.Update(gameTime);
        }

        private void ToggleFullscreen()
        {
            _isFullscreen = !_isFullscreen;
            if (_isFullscreen)
            {
                _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
                _graphics.IsFullScreen = true;
            }
            else
            {
                _graphics.PreferredBackBufferWidth = 1280;
                _graphics.PreferredBackBufferHeight = 720;
                _graphics.IsFullScreen = false;
            }
            _graphics.ApplyChanges();
            _camera.UpdateViewport(_graphics.GraphicsDevice.Viewport);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Draw game elements with camera transform
            Matrix transform = _camera.GetTransformMatrix();
            _spriteBatch.Begin(transformMatrix: transform, samplerState: SamplerState.PointClamp);

            tileRenderer.Draw(_spriteBatch, map);

            // Draw all habitats and their animals
            foreach (var habitat in habitats)
            {
                habitat.Draw(_spriteBatch);
            }

            // Draw all visitors
            foreach (var visitor in visitors)
            {
                visitor.Draw(_spriteBatch);
            }

            _spriteBatch.End();

            // Draw UI elements without camera offset
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Draw FPS counter
            _fpsCounter.Draw(_spriteBatch);

            // Draw instructions at the bottom of the screen
            string instructions = "Press right click for habitat\nPress 'A' for placing animal\nPress 'B' for spawning visitor\nPress 'S' to save\nPress 'O' to clear everything\nPress 'M' to add $100k (debug)\nPress 'F11' to toggle fullscreen\nUse middle mouse or arrow keys to move camera\nUse mouse wheel to zoom";
            Vector2 textPosition = new Vector2(10, _graphics.PreferredBackBufferHeight - 180);
            _spriteBatch.DrawString(_font, instructions, textPosition, Color.White);

            // Draw current money from MoneyDisplay
            Vector2 moneyPosition = new Vector2(10, 10); // Top-left corner
            _spriteBatch.DrawString(_font, _moneyDisplay.MoneyText, moneyPosition, Color.Gold);

            shopButton.Draw(_spriteBatch);
            _spriteBatch.Draw(
                shopIcon,
                new Vector2(shopButton.Bounds.X, shopButton.Bounds.Y),
                Color.White
                );

            Vector2 iconPos = new Vector2(
                shopButton.Bounds.X + (shopButton.Bounds.Width - shopIcon.Width) / 2,
                shopButton.Bounds.Y + (shopButton.Bounds.Height - shopIcon.Height) / 2
);
            _spriteBatch.Draw(shopIcon, iconPos, Color.White);


            if (Selected_unit != null)
            {
                var pos = Selected_unit.Position;
                var spriteSize = new Point(Selected_unit.Sprite.Width, Selected_unit.Sprite.Height);
                Rectangle rect = new Rectangle((int)(pos.X - spriteSize.X / 2), (int)(pos.Y - spriteSize.Y / 2), spriteSize.X, spriteSize.Y);
                DrawRectangle(_spriteBatch, rect, 2, Color.Yellow);

                if (isShopMenuOpen)
                {
                    int menuOffsetX = 140;
                    int menuX = shopButton.Bounds.X - menuOffsetX;
                    int menuY = shopButton.Bounds.Bottom + 5;

                    // Tegn menu-baggrund
                    Rectangle menuRect = new Rectangle(menuX, menuY, 180, 120);
                    _spriteBatch.Draw(wideButtonTexture, menuRect, Color.White);

                    // Menu-overskrift
                    _spriteBatch.DrawString(_font, "", new Vector2(menuRect.X + 10, menuRect.Y + 10), Color.White);

                    // Tegn knapper
                    structuresButton.Draw(_spriteBatch);
                    unitButton.Draw(_spriteBatch);

                    if (showStructureMenu)
                    {
                        foreach (var btn in structureButtons)
                            btn.Draw(_spriteBatch);
                    }

                    if (showUnitMenu)
                    {
                        foreach (var btn in unitButtons)
                            btn.Draw(_spriteBatch);
                    }
                }

                

                base.Draw(gameTime);
            }
            _spriteBatch.End();
        }

        public int GetNextAnimalId()
        {
            return _nextAnimalId++;
        }

        public void ConfirmDespawn(Visitor visitor)
        {
            if (visitor != null && !_visitorsToDespawn.Contains(visitor) && !visitors.Contains(visitor)) // Ensure not already added and not already removed from main list
            {
                // Visitor should have already stopped its own update loop.
                _visitorsToDespawn.Add(visitor);
                Debug.WriteLine($"Visitor {visitor.VisitorId} confirmed exit and added to despawn queue.");
            }
            else if (visitor != null && visitors.Contains(visitor) && !_visitorsToDespawn.Contains(visitor)) // Standard case: visitor exists in main list and not yet in despawn queue
            {
                 _visitorsToDespawn.Add(visitor);
                Debug.WriteLine($"Visitor {visitor.VisitorId} confirmed exit and added to despawn queue.");
            }
            else if (visitor != null && _visitorsToDespawn.Contains(visitor))
            {
                Debug.WriteLine($"Visitor {visitor.VisitorId} already in despawn queue. Confirmation ignored.");
            }
            else
            {
                Debug.WriteLine($"Attempted to confirm despawn for a null or already processed/removed visitor.");
            }
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Rectangle rectangle, int thickness, Color color)
        {
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y + rectangle.Height - thickness, rectangle.Width, thickness), color);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + rectangle.Width - thickness, rectangle.Y, thickness, rectangle.Height), color);
        }

    }
}
