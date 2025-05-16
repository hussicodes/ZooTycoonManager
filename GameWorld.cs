using Microsoft.Data.Sqlite;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using ZooTycoonManager.UI;
using System.Diagnostics;
using ZooTycoonManager.Subjects;
using System;
using System.Linq;

namespace ZooTycoonManager
{
    public class GameWorld : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private SqliteConnection _connection;

        private List<Button> _buttons;
        private Texture2D _buttonTexture;
        private SpriteFont _buttonFont;

        private MouseState previousMouseState;

        private StatsDisplay _statsDisplay;
        private GameStatsSubject _gameStatsSubject;

        // List to hold all game objects
        private List<GameObject> _gameObjects;

        private int _nextHabitatId = 1;
        private int _nextAnimalId = 1;
        private int _nextZookeeperId = 1;

        public GameWorld()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _connection = new SqliteConnection("Data Source=mydb.db");
            _connection.Open();

            var createTablesCmd = _connection.CreateCommand();

            createTablesCmd.CommandText = @"CREATE TABLE IF NOT EXISTS Habitat (
                  habitat_id INTEGER PRIMARY KEY,
                  size INTEGER NOT NULL,
                  max_animals INTEGER NOT NULL,
                  name NVARCHAR(50) NOT NULL,
                  type NVARCHAR(50) NOT NULL,
                  position_x INTEGER NOT NULL,
                  position_y INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Shop (
                  shop_id INTEGER PRIMARY KEY,
                  type NVARCHAR(50) NOT NULL,
                  cost INTEGER NOT NULL,
                  position_x INTEGER NOT NULL,
                  position_y INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Visitor (
                    visitor_id INTEGER PRIMARY KEY,
                    name NVARCHAR(50) NOT NULL,
                    money INTEGER NOT NULL,
                    mood INTEGER NOT NULL,
                    hunger INTEGER NOT NULL,
                    habitat_id INTEGER,
                    shop_id INTEGER,
                    position_x INTEGER NOT NULL,
                    position_y INTEGER NOT NULL,
                    FOREIGN KEY (habitat_id) REFERENCES Habitat(habitat_id),
                    FOREIGN KEY (shop_id) REFERENCES Shop(shop_id)
                );
                CREATE TABLE IF NOT EXISTS Zookeeper (
                    zookeeper_id INTEGER PRIMARY KEY,
                    name NVARCHAR(50) NOT NULL,
                    upkeep INTEGER NOT NULL,
                    habitat_id INTEGER,
                    position_x INTEGER NOT NULL,
                    position_y INTEGER NOT NULL,
                    FOREIGN KEY (habitat_id) REFERENCES Habitat(habitat_id)
                );
                CREATE TABLE IF NOT EXISTS [Transaction] (
                    transaction_id INTEGER PRIMARY KEY,
                    price INTEGER NOT NULL,
                    datetime DATETIME NOT NULL,
                    visitor_id INTEGER NOT NULL,
                    shop_id INTEGER NOT NULL,
                    FOREIGN KEY (visitor_id) REFERENCES Visitor(visitor_id) ON DELETE CASCADE,
                    FOREIGN KEY (shop_id) REFERENCES Shop(shop_id) ON DELETE CASCADE
                );
                CREATE TABLE IF NOT EXISTS Species (
                    species_id INTEGER PRIMARY KEY,
                    name NVARCHAR(50) NOT NULL UNIQUE
                );
                CREATE TABLE IF NOT EXISTS Animal (
                    animal_id INTEGER PRIMARY KEY,
                    name NVARCHAR(50) NOT NULL,
                    mood INTEGER NOT NULL,
                    hunger INTEGER NOT NULL,
                    stress INTEGER NOT NULL,
                    habitat_id INTEGER NOT NULL,
                    position_x INTEGER NOT NULL,
                    position_y INTEGER NOT NULL,
                    FOREIGN KEY (habitat_id) REFERENCES Habitat(habitat_id) ON DELETE RESTRICT
                );
                CREATE TABLE IF NOT EXISTS VisitorFavoriteSpecies (
                    visitor_id INTEGER NOT NULL,
                    species_id INTEGER NOT NULL,
                    PRIMARY KEY (visitor_id, species_id),
                    FOREIGN KEY (visitor_id) REFERENCES Visitor(visitor_id) ON DELETE CASCADE,
                    FOREIGN KEY (species_id) REFERENCES Species(species_id) ON DELETE CASCADE
                );";

            createTablesCmd.ExecuteNonQuery();

            var checkSpeciesCmd = _connection.CreateCommand();
            checkSpeciesCmd.CommandText = "SELECT COUNT(*) FROM Species";
            var speciesCount = Convert.ToInt32(checkSpeciesCmd.ExecuteScalar());

            if (speciesCount == 0)
            {
                using (var transaction = _connection.BeginTransaction())
                {
                    var insertSpeciesCmd = _connection.CreateCommand();
                    insertSpeciesCmd.Transaction = transaction;
                    insertSpeciesCmd.CommandText = @"
                        INSERT INTO Species (name) VALUES ('Lion');
                        INSERT INTO Species (name) VALUES ('Tiger');
                        INSERT INTO Species (name) VALUES ('Bear');
                        INSERT INTO Species (name) VALUES ('Penguin');
                        INSERT INTO Species (name) VALUES ('Zebra');
                    "; 
                    insertSpeciesCmd.ExecuteNonQuery();
                    transaction.Commit();
                    Debug.WriteLine("Populated default species into Species table.");
                }
            }

            _buttons = new List<Button>();
            _gameStatsSubject = new GameStatsSubject();

            _gameObjects = new List<GameObject>();

            LoadGame();

            previousMouseState = Mouse.GetState();

            _gameStatsSubject.UpdateStats(_gameObjects.OfType<Habitat>().Count(), _gameObjects.OfType<Animal>().Count());

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _buttonFont = Content.Load<SpriteFont>("DefaultFont");

            _buttonTexture = new Texture2D(GraphicsDevice, 1, 1);
            _buttonTexture.SetData(new[] { Color.White });

            int buttonWidth = 150;
            int buttonHeight = 50;
            int buttonSpacing = 10;
            Vector2 startPosition = new Vector2(10, 10);

            SpriteFont statsFont = Content.Load<SpriteFont>("DefaultFont");
            _statsDisplay = new StatsDisplay(statsFont, new Vector2(10, _graphics.PreferredBackBufferHeight - 40), _gameStatsSubject);
            _gameStatsSubject.Attach(_statsDisplay);

            Button buildHabitatButton = new Button(_buttonTexture, _buttonFont, startPosition, "Build habitat");
            buildHabitatButton.Rectangle = new Rectangle((int)startPosition.X, (int)startPosition.Y, buttonWidth, buttonHeight);
            buildHabitatButton.OnClick += OnBuildHabitatClicked;
            _buttons.Add(buildHabitatButton);

            Button addAnimalButton = new Button(_buttonTexture, _buttonFont, new Vector2(startPosition.X, startPosition.Y + buttonHeight + buttonSpacing), "Add animal");
            addAnimalButton.Rectangle = new Rectangle((int)addAnimalButton.Position.X, (int)addAnimalButton.Position.Y, buttonWidth, buttonHeight);
            addAnimalButton.OnClick += OnAddAnimalClicked;
            _buttons.Add(addAnimalButton);

            Button hireZookeeperButton = new Button(_buttonTexture, _buttonFont, new Vector2(startPosition.X, startPosition.Y + 2 * (buttonHeight + buttonSpacing)), "Hire Zookeeper");
            hireZookeeperButton.Rectangle = new Rectangle((int)hireZookeeperButton.Position.X, (int)hireZookeeperButton.Position.Y, buttonWidth, buttonHeight);
            hireZookeeperButton.OnClick += OnHireZookeeperClicked;
            _buttons.Add(hireZookeeperButton);

            Button saveGameButton = new Button(_buttonTexture, _buttonFont, new Vector2(startPosition.X, startPosition.Y + 3 * (buttonHeight + buttonSpacing)), "Save Game");
            saveGameButton.Rectangle = new Rectangle((int)saveGameButton.Position.X, (int)saveGameButton.Position.Y, buttonWidth, buttonHeight);
            saveGameButton.OnClick += OnSaveGameClicked;
            _buttons.Add(saveGameButton);

            // Load content for existing game objects if any (e.g., from a save file)
            foreach (var gameObject in _gameObjects) gameObject.LoadContent();
        }

        private void OnBuildHabitatClicked()
        {
            var newHabitat = new Habitat
            {
                HabitatId = _nextHabitatId++,
                Name = $"Habitat {_nextHabitatId -1}",
                Type = "Generic",
                Size = 100,
                MaxAnimals = 10,
                PositionX = 200 + (_gameObjects.OfType<Habitat>().Count() * 60),
                PositionY = 100
            };
            newHabitat.LoadContent();
            _gameObjects.Add(newHabitat);
            Debug.WriteLine($"Built Habitat: ID {newHabitat.HabitatId}, Name: {newHabitat.Name}");
            _gameStatsSubject.UpdateStats(_gameObjects.OfType<Habitat>().Count(), _gameObjects.OfType<Animal>().Count());
        }

        private void OnAddAnimalClicked()
        {
            Habitat firstHabitat = _gameObjects.OfType<Habitat>().FirstOrDefault();
            if (firstHabitat != null)
            {
                var newAnimal = new Animal
                {
                    AnimalId = _nextAnimalId++,
                    Name = $"Animal {_nextAnimalId - 1}",
                    Mood = 100,
                    Hunger = 0,
                    Stress = 0,
                    HabitatId = firstHabitat.HabitatId,
                    PositionX = firstHabitat.PositionX + 10,
                    PositionY = firstHabitat.PositionY + 10
                };
                newAnimal.LoadContent();
                _gameObjects.Add(newAnimal);
                Debug.WriteLine($"Added Animal: ID {newAnimal.AnimalId}, Name: {newAnimal.Name} to Habitat ID: {newAnimal.HabitatId}");
                _gameStatsSubject.UpdateStats(_gameObjects.OfType<Habitat>().Count(), _gameObjects.OfType<Animal>().Count());
            }
            else
            {
                Debug.WriteLine("Cannot add animal: No habitat exists.");
            }
        }

        private void OnHireZookeeperClicked()
        {
            var newZookeeper = new Zookeeper
            {
                ZookeeperId = _nextZookeeperId++,
                Name = $"Zookeeper {_nextZookeeperId - 1}",
                Upkeep = 50,
                PositionX = 50,
                PositionY = 200 + (_gameObjects.OfType<Zookeeper>().Count() * 30)
            };
            newZookeeper.LoadContent();
            _gameObjects.Add(newZookeeper);
            Debug.WriteLine($"Hired Zookeeper: ID {newZookeeper.ZookeeperId}, Name: {newZookeeper.Name}");
        }

        private void OnSaveGameClicked()
        {
            SaveGame();
        }

        private void SaveGame()
        {
            Debug.WriteLine("Save Game button clicked. Saving game state...");
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    foreach (Habitat habitatInstance in _gameObjects.OfType<Habitat>())
                    {
                        Debug.WriteLine($"Attempting to save Habitat ID: {habitatInstance.HabitatId}, Name: {habitatInstance.Name}, Type: {habitatInstance.Type}");
                        if (habitatInstance is ISaveable saveableObject)
                        {
                            saveableObject.Save(transaction);
                        }
                        else
                        {
                            Debug.WriteLine($"Warning: Habitat ID {habitatInstance.HabitatId} Name: {habitatInstance.Name} is not ISaveable and was not saved.");
                        }
                    }

                    foreach (Shop shopInstance in _gameObjects.OfType<Shop>())
                    {
                        Debug.WriteLine($"Attempting to save Shop ID: {shopInstance.ShopId}, Type: {shopInstance.Type}");
                        if (shopInstance is ISaveable saveableObject)
                        {
                            saveableObject.Save(transaction);
                        }
                        else
                        {
                            Debug.WriteLine($"Warning: Shop ID {shopInstance.ShopId} Type: {shopInstance.Type} is not ISaveable and was not saved.");
                        }
                    }

                    foreach (Animal animalInstance in _gameObjects.OfType<Animal>())
                    {
                        Debug.WriteLine($"Attempting to save Animal ID: {animalInstance.AnimalId}, Name: {animalInstance.Name}, HabitatID: {animalInstance.HabitatId}");
                        if (animalInstance is ISaveable saveableObject)
                        {
                            saveableObject.Save(transaction);
                        }
                        else
                        {
                            Debug.WriteLine($"Warning: Animal ID {animalInstance.AnimalId} Name: {animalInstance.Name} is not ISaveable and was not saved.");
                        }
                    }

                    foreach (Zookeeper zookeeperInstance in _gameObjects.OfType<Zookeeper>())
                    {
                        Debug.WriteLine($"Attempting to save Zookeeper ID: {zookeeperInstance.ZookeeperId}, Name: {zookeeperInstance.Name}, HabitatID: {(zookeeperInstance.HabitatId.HasValue ? zookeeperInstance.HabitatId.Value.ToString() : "NULL")}");
                        if (zookeeperInstance is ISaveable saveableObject)
                        {
                            saveableObject.Save(transaction);
                        }
                        else
                        {
                            Debug.WriteLine($"Warning: Zookeeper ID {zookeeperInstance.ZookeeperId} Name: {zookeeperInstance.Name} is not ISaveable and was not saved.");
                        }
                    }

                    foreach (Visitor visitorInstance in _gameObjects.OfType<Visitor>())
                    {
                        Debug.WriteLine($"Attempting to save Visitor ID: {visitorInstance.VisitorId}, Name: {visitorInstance.Name}, HabitatID: {(visitorInstance.HabitatId.HasValue ? visitorInstance.HabitatId.Value.ToString() : "NULL")}, ShopID: {(visitorInstance.ShopId.HasValue ? visitorInstance.ShopId.Value.ToString() : "NULL")}");
                        if (visitorInstance is ISaveable saveableObject)
                        {
                            saveableObject.Save(transaction);
                        }
                        else
                        {
                            Debug.WriteLine($"Warning: Visitor ID {visitorInstance.VisitorId} Name: {visitorInstance.Name} is not ISaveable and was not saved.");
                        }
                    }

                    transaction.Commit();
                    Debug.WriteLine("Game state saved successfully.");
                }
                catch (SqliteException sqliteEx)
                {
                    Debug.WriteLine($"SQLite Error saving game state: {sqliteEx.Message}");
                    Debug.WriteLine($"SQLite Error Code: {sqliteEx.SqliteErrorCode}");
                    Debug.WriteLine($"SQLite Extended Error Code: {sqliteEx.SqliteExtendedErrorCode}");
                    transaction.Rollback();
                    Debug.WriteLine("Save transaction rolled back due to SQLite error.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Generic error saving game state: {ex.Message}");
                    Debug.WriteLine($"Exception Type: {ex.GetType().FullName}");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    transaction.Rollback();
                    Debug.WriteLine("Save transaction rolled back due to generic error.");
                }
            }
        }

        private void LoadGame()
        {
            Debug.WriteLine("Loading game state...");
            try
            {
                var command = _connection.CreateCommand();
                command.CommandText = "SELECT habitat_id, size, max_animals, name, type, position_x, position_y FROM Habitat";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var habitat = new Habitat
                        {
                            HabitatId = reader.GetInt32(0),
                            Size = reader.GetInt32(1),
                            MaxAnimals = reader.GetInt32(2),
                            Name = reader.GetString(3),
                            Type = reader.GetString(4),
                            PositionX = reader.GetInt32(5),
                            PositionY = reader.GetInt32(6)
                        };
                        _gameObjects.Add(habitat);

                        if (habitat.HabitatId >= _nextHabitatId)
                        {
                            _nextHabitatId = habitat.HabitatId + 1;
                        }
                    }
                }

                command.CommandText = "SELECT animal_id, name, mood, hunger, stress, habitat_id, position_x, position_y FROM Animal";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var animal = new Animal
                        {
                            AnimalId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Mood = reader.GetInt32(2),
                            Hunger = reader.GetInt32(3),
                            Stress = reader.GetInt32(4),
                            HabitatId = reader.GetInt32(5),
                            PositionX = reader.GetInt32(6),
                            PositionY = reader.GetInt32(7)
                        };
                        _gameObjects.Add(animal);

                        if (animal.AnimalId >= _nextAnimalId)
                        {
                            _nextAnimalId = animal.AnimalId + 1;
                        }
                    }
                }

                command.CommandText = "SELECT zookeeper_id, name, upkeep, habitat_id, position_x, position_y FROM Zookeeper";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var zookeeper = new Zookeeper
                        {
                            ZookeeperId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Upkeep = reader.GetInt32(2),
                            HabitatId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                            PositionX = reader.GetInt32(4),
                            PositionY = reader.GetInt32(5)
                        };
                        _gameObjects.Add(zookeeper);
                        if (zookeeper.ZookeeperId >= _nextZookeeperId)
                        {
                            _nextZookeeperId = zookeeper.ZookeeperId + 1;
                        }
                    }
                }
                
                command.CommandText = "SELECT shop_id, type, cost, position_x, position_y FROM Shop";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var shop = new Shop
                        {
                            ShopId = reader.GetInt32(0),
                            Type = reader.GetString(1),
                            Cost = reader.GetInt32(2),
                            PositionX = reader.GetInt32(3),
                            PositionY = reader.GetInt32(4)
                        };
                        _gameObjects.Add(shop);
                    }
                }

                command.CommandText = "SELECT visitor_id, name, money, mood, hunger, habitat_id, shop_id, position_x, position_y FROM Visitor";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var visitor = new Visitor
                        {
                            VisitorId = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Money = reader.GetInt32(2),
                            Mood = reader.GetInt32(3),
                            Hunger = reader.GetInt32(4),
                            HabitatId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                            ShopId = reader.IsDBNull(6) ? (int?)null : reader.GetInt32(6),
                            PositionX = reader.GetInt32(7),
                            PositionY = reader.GetInt32(8)
                        };
                        _gameObjects.Add(visitor);
                    }
                }

                Debug.WriteLine("Game state loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading game state: {ex.Message}");
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            MouseState currentMouseState = Mouse.GetState();
            foreach (var button in _buttons)
            {
               button.Update(currentMouseState, previousMouseState);
            }
            previousMouseState = currentMouseState;

            // Update all game objects
            foreach (var gameObject in _gameObjects) gameObject.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            foreach (var button in _buttons)
            {
                button.Draw(_spriteBatch);
            }

            _statsDisplay.Draw(_spriteBatch);

            foreach (var gameObject in _gameObjects) gameObject.Draw(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
