using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using SharpDX.Direct2D1;

namespace ZooTycoonManager
{
    public class GameWorld : Game
    {
        // Tile and grid settings
        public const int TILE_SIZE = 32; // Size of each tile in pixels
        public const int GRID_WIDTH = 70;
        public const int GRID_HEIGHT = 40;
        private const int IDX_GRASS = 0;
        private const int IDX_TREE = 2;
        private const int IDX_FLOOR1 = 3;
        private const int IDX_FLOOR2 = 4;

        private static GameWorld _instance;
        private static readonly object _lock = new object();
        private GraphicsDeviceManager _graphics;
        private Microsoft.Xna.Framework.Graphics.SpriteBatch _spriteBatch;
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
            _instance = this;
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.IsFullScreen = false;
            Window.AllowUserResizing = true;
            _graphics.ApplyChanges();
            visitors = new List<Visitor>();

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set to use monitor refresh rate instead of fixed 60 FPS
            IsFixedTimeStep = false;
            TargetElapsedTime = TimeSpan.FromTicks(1);

            // Initialize map first
            map = new Map(GRID_WIDTH, GRID_HEIGHT);
            SetupMapBordersAndEntryExit();
            int entryWidth = 3;
            int entryHeight = 2;
            int entryStartX = 1;
            int entryStartY = 1;

            for (int x = entryStartX; x < entryStartX + entryWidth; x++)
            {
                for (int y = entryStartY; y < entryStartY + entryHeight; y++)
                {
                    Tile tile = map.Tiles[x, y];
                    tile.TextureIndex = IDX_FLOOR1;
                    tile.Walkable = true;
                    tile.HasTree = false;  // no trees on entry
                }
            }

            // Exit top-right corner, floor2 tiles
            int exitWidth = 3;
            int exitHeight = 2;
            int exitStartX = GRID_WIDTH - exitWidth - 1;
            int exitStartY = 1;

            for (int x = exitStartX; x < exitStartX + exitWidth; x++)
            {
                for (int y = exitStartY; y < exitStartY + exitHeight; y++)
                {
                    Tile tile = map.Tiles[x, y];
                    tile.TextureIndex = IDX_FLOOR2;
                    tile.Walkable = true;
                    tile.HasTree = false;  // no trees on exit
                }
            }

            for (int x = 0; x < GRID_WIDTH; x++)
            {
                // bottom border at y = 0
                if (!(x >= entryStartX && x < entryStartX + entryWidth && 0 == entryStartY))
                {
                    // still skip only the entry floor tiles; all other bottom-edge tiles get trees
                    map.Tiles[x, 0].HasTree = true;
                }

                // top border at y = GRID_HEIGHT - 1
                if (!(x >= exitStartX && x < exitStartX + exitWidth && GRID_HEIGHT - 1 == exitStartY))
                {
                    // skip only the exit floor tiles; all other top-edge tiles get trees
                    map.Tiles[x, GRID_HEIGHT - 1].HasTree = true;
                }
            }





            // Left and Right borders
            for (int y = 0; y < GRID_HEIGHT; y++)
            {
                // left border at x = 0
                if (!(0 == entryStartX && y >= entryStartY && y < entryStartY + entryHeight))
                {
                    // skip only the entry-floor vertical strip
                    map.Tiles[0, y].HasTree = true;
                }

                // right border at x = GRID_WIDTH - 1
                if (!(GRID_WIDTH - 1 == exitStartX && y >= exitStartY && y < exitStartY + exitHeight))
                {
                    // skip only the exit-floor vertical strip
                    map.Tiles[GRID_WIDTH - 1, y].HasTree = true;
                }
            }



            // Initialize camera
            _camera = new Camera(_graphics);
            _camera.SetMapDimensions(GRID_WIDTH * TILE_SIZE, GRID_HEIGHT * TILE_SIZE);

            int entryCenterX = entryStartX + entryWidth / 2;
            int entryCenterY = entryStartY + entryHeight / 2;
            int exitCenterX = exitStartX + exitWidth / 2;
            int exitCenterY = exitStartY + exitHeight / 2;

            // Define half-width of the big platform (2 means a total of 5 tiles: -2, -1, 0, +1, +2)
            int halfSize = 2;

            // Carve out a filled 5×5 square at entry
            for (int dx = -halfSize; dx <= halfSize; dx++)
            {
                for (int dy = -halfSize; dy <= halfSize; dy++)
                {
                    int x = entryCenterX + dx;
                    int y = entryCenterY + dy;
                    if (x >= 0 && x < GRID_WIDTH && y >= 0 && y < GRID_HEIGHT)
                    {
                        var t = map.Tiles[x, y];
                        t.TextureIndex = IDX_FLOOR1;  // entry floor graphic
                        t.Walkable = true;
                        t.HasTree = false;
                    }
                }
            }

            // Carve out a filled 5×5 square at exit
            for (int dx = -halfSize; dx <= halfSize; dx++)
            {
                for (int dy = -halfSize; dy <= halfSize; dy++)
                {
                    int x = exitCenterX + dx;
                    int y = exitCenterY + dy;
                    if (x >= 0 && x < GRID_WIDTH && y >= 0 && y < GRID_HEIGHT)
                    {
                        var t = map.Tiles[x, y];
                        t.TextureIndex = IDX_FLOOR2;  // exit floor graphic
                        t.Walkable = true;
                        t.HasTree = false;
                    }
                }
            }

            // Initialize walkable map from the map object
            WalkableMap = map.ToWalkableArray();

            int spawnX = entryStartX + entryWidth / 2;
            int spawnY = entryStartY + entryHeight / 2;
            SpawnVisitorAt(spawnX, spawnY);

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
            _spriteBatch = new Microsoft.Xna.Framework.Graphics.SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("font");  // Load the font
            _fpsCounter = new FPSCounter(_font, _graphics);  // Initialize FPS counter with graphics manager

            // Initialize MoneyDisplay here after _font is loaded
            Vector2 moneyPosition = new Vector2(10, 10); // Top-left corner
            _moneyDisplay = new MoneyDisplay(_font, moneyPosition, Color.Black, 2f);
            MoneyManager.Instance.Attach(_moneyDisplay); // Attach MoneyDisplay as observer
            MoneyManager.Instance.Notify(); // Initial notification to set initial money text

            tileTextures = new Texture2D[5];
            tileTextures[0] = Content.Load<Texture2D>("Grass1");
            tileTextures[1] = Content.Load<Texture2D>("Dirt1");
            tileTextures[2] = Content.Load<Texture2D>("tree1");
            tileTextures[3] = Content.Load<Texture2D>("floor1");
            tileTextures[4] = Content.Load<Texture2D>("floor2");

            // map = new Map(GRID_WIDTH, GRID_HEIGHT); // yo, this is where the size happens -- This line is now redundant
            tileRenderer = new TileRenderer(tileTextures);

            // Load fence textures
            FenceRenderer.LoadContent(Content);

            // Load content for all habitats and their animals
            foreach (var habitat in habitats)
            {
                habitat.LoadAnimalContent(Content);
            }
            Habitat.LoadContent(Content);
        }

        MouseState prevMouseState;
        KeyboardState prevKeyboardState;

        private void PlaceFence(Vector2 pixelPosition)
        {
            Debug.WriteLine($"PlaceFence called with pixel position: {pixelPosition}, isPlacingEnclosure: {isPlacingEnclosure}");

            // Create and execute the place habitat command
            var placeHabitatCommand = new PlaceHabitatCommand(pixelPosition);
            CommandManager.Instance.ExecuteCommand(placeHabitatCommand);
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
                // Create and execute the place animal command
                var placeAnimalCommand = new PlaceAnimalCommand(worldMousePosition);
                CommandManager.Instance.ExecuteCommand(placeAnimalCommand);
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
                CommandManager.Instance.Clear(); // Clear command history when clearing everything
            }

            // Handle 'M' key press for adding money (debugging)
            if (keyboard.IsKeyDown(Keys.M) && !prevKeyboardState.IsKeyDown(Keys.M))
            {
                MoneyManager.Instance.AddMoney(100000);
                Debug.WriteLine("Added $100,000 for debugging.");
            }

            // Handle Ctrl+Z for undo
            if (keyboard.IsKeyDown(Keys.LeftControl) && keyboard.IsKeyDown(Keys.Z) &&
                !prevKeyboardState.IsKeyDown(Keys.Z))
            {
                CommandManager.Instance.Undo();
            }

            // Handle Ctrl+Y for redo
            if (keyboard.IsKeyDown(Keys.LeftControl) && keyboard.IsKeyDown(Keys.Y) &&
                !prevKeyboardState.IsKeyDown(Keys.Y))
            {
                CommandManager.Instance.Redo();
            }

            if (mouse.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton != ButtonState.Pressed)
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

        void SpawnVisitorAt(int x, int y)
        {
            // Create a new visitor entity or object at the tile coordinates (x, y)
            Vector2 spawnPos = GameWorld.TileToPixel(new Vector2(x, y));
            Visitor visitor = new Visitor(spawnPos);

            visitors.Add(visitor);
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

            // 1) World‐space batch: draw ground, trees, habitats, visitors
            Matrix transform = _camera.GetTransformMatrix();
            _spriteBatch.Begin(transformMatrix: transform, samplerState: SamplerState.PointClamp);


            // --- A: Draw every tile's BASE texture ---
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    var tile = map.Tiles[x, y];
                    var pos = new Vector2(x * TILE_SIZE, y * TILE_SIZE);
                    // Draw grass, floor1, floor2, or road—whatever tile.TextureIndex says
                    _spriteBatch.Draw(tileTextures[tile.TextureIndex], pos, Color.White);
                }
            }

            // --- B: Draw all trees ON TOP ---
            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    if (map.Tiles[x, y].HasTree)
                    {
                        var pos = new Vector2(x * TILE_SIZE, y * TILE_SIZE);
                        _spriteBatch.Draw(tileTextures[IDX_TREE], pos, Color.White);
                    }
                }
            }

            // --- C: Draw habitats & animals ---
            foreach (var habitat in habitats)
                habitat.Draw(_spriteBatch);

            // --- D: Draw visitors ---
            foreach (var visitor in visitors)
                visitor.Draw(_spriteBatch);

            _spriteBatch.End();  // end world‐space batch

            // 2) Screen‐space batch: draw UI on top of everything
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            _fpsCounter.Draw(_spriteBatch);
            _moneyDisplay.Draw(_spriteBatch);

            // Instructions text
            string instructions =
                "Press right click for habitat\n" +
                "Press 'A' for placing animal\n" +
                "Press 'B' for spawning visitor\n" +
                "Press 'S' to save\n" +
                "Press 'O' to clear everything\n" +
                "Press 'M' to add $100k (debug)\n" +
                "Press 'F11' to toggle fullscreen\n" +
                "Use middle mouse or arrow keys to move camera\n" +
                "Use mouse wheel to zoom\n" +
                "Ctrl+Z to undo, Ctrl+Y to redo";
            Vector2 instrPos = new Vector2(10, _graphics.PreferredBackBufferHeight - 200);
            _spriteBatch.DrawString(_font, instructions, instrPos, Color.White);

            // Undo/redo text
            Vector2 urPos = new Vector2(10, 40);
            string urText = $"Undo: {CommandManager.Instance.GetUndoDescription()}\n" +
                            $"Redo: {CommandManager.Instance.GetRedoDescription()}";
            _spriteBatch.DrawString(_font, urText, urPos, Color.LightBlue);

            _spriteBatch.End();  // end UI batch

            base.Draw(gameTime);
        }

        public int GetNextAnimalId()
        {
            return _nextAnimalId++;
        }

        public int GetNextHabitatId()
        {
            return _nextHabitatId++;
        }

        public bool GetOriginalWalkableState(int x, int y)
        {
            return map.IsWalkable(x, y);
        }

        public List<Vector2> GetWalkableTileCoordinates()
        {
            var walkableTiles = new List<Vector2>();
            if (WalkableMap == null)
            {
                Debug.WriteLine("Warning: WalkableMap is null in GameWorld.GetWalkableTileCoordinates.");
                return walkableTiles; // Return empty list if map not initialized
            }



            for (int x = 0; x < GRID_WIDTH; x++)
            {
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    if (WalkableMap[x, y])
                    {
                        walkableTiles.Add(new Vector2(x, y));
                    }
                }
            }
            return walkableTiles;
        }



        public void SetupMapBordersAndEntryExit()
        {
            try
            {


                if (map == null)
                    throw new Exception("map object is null!");

                // Initialize Tiles array if null
                if (map.Tiles == null)
                    map.Tiles = new Tile[GRID_WIDTH, GRID_HEIGHT];

                // Initialize WalkableMap if null
                if (WalkableMap == null)
                    WalkableMap = new bool[GRID_WIDTH, GRID_HEIGHT];

                // Initialize each tile object if null
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    for (int y = 0; y < GRID_HEIGHT; y++)
                    {
                        if (map.Tiles[x, y] == null)
                            map.Tiles[x, y] = new Tile();  // Make sure Tile() ctor works correctly

                        // Safe to assign now
                        map.Tiles[x, y].TextureIndex = IDX_GRASS;
                        WalkableMap[x, y] = true;
                    }
                }

                // Impassable tree border
                for (int x = 0; x < GRID_WIDTH; x++)
                {
                    map.Tiles[x, 0].TextureIndex = IDX_TREE; WalkableMap[x, 0] = false;
                    map.Tiles[x, GRID_HEIGHT - 1].TextureIndex = IDX_TREE; WalkableMap[x, GRID_HEIGHT - 1] = false;
                }
                for (int y = 0; y < GRID_HEIGHT; y++)
                {
                    map.Tiles[0, y].TextureIndex = IDX_TREE; WalkableMap[0, y] = false;
                    map.Tiles[GRID_WIDTH - 1, y].TextureIndex = IDX_TREE; WalkableMap[GRID_WIDTH - 1, y] = false;
                }

                // 3. Entry (now at top-left)
                int ew = 3, eh = 2;
                int sx = 1, sy = 1;                      // top
                for (int x = sx; x < sx + ew; x++)
                    for (int y = sy; y < sy + eh; y++)
                    {
                        map.Tiles[x, y].TextureIndex = IDX_FLOOR1;
                        WalkableMap[x, y] = true;
                    }

                // 4. Exit (now bottom-right)
                int xw = 3, xh = 2;
                int ex = GRID_WIDTH - xw - 1, ey = 1;
                for (int x = ex; x < ex + xw; x++)
                    for (int y = ey; y < ey + xh; y++)
                    {
                        map.Tiles[x, y].TextureIndex = IDX_FLOOR2;
                        WalkableMap[x, y] = true;
                    }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in SetupMapBordersAndEntryExit: {ex.Message}");
                throw; // Optional: re-throw or handle as needed
            }
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
    }
}
