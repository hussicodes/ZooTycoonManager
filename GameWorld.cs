using Microsoft.Data.Sqlite;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using ZooTycoonManager.UI;
using System.Diagnostics;
using ZooTycoonManager.Subjects;
using System;

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
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var createTablesCmd = _connection.CreateCommand();

            createTablesCmd.CommandText = @"CREATE TABLE IF NOT EXISTS Habitat (
                  habitat_id INTEGER PRIMARY KEY,
                  size INTEGER NOT NULL,
                  max_animals INTEGER NOT NULL,
                  name NVARCHAR(50) NOT NULL,
                  type NVARCHAR(50) NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Shop (
                  shop_id INTEGER PRIMARY KEY,
                  type NVARCHAR(50) NOT NULL,
                  cost INTEGER NOT NULL
                );
                CREATE TABLE IF NOT EXISTS Visitor (
                    visitor_id INTEGER PRIMARY KEY,
                    name NVARCHAR(50) NOT NULL,
                    money INTEGER NOT NULL,
                    mood INTEGER NOT NULL,
                    hunger INTEGER NOT NULL,
                    habitat_id INTEGER,
                    shop_id INTEGER,
                    FOREIGN KEY (habitat_id) REFERENCES Habitat(habitat_id),
                    FOREIGN KEY (shop_id) REFERENCES Shop(shop_id)
                );
                CREATE TABLE IF NOT EXISTS Zookeeper (
                    zookeeper_id INTEGER PRIMARY KEY,
                    name NVARCHAR(50) NOT NULL,
                    upkeep INTEGER NOT NULL,
                    habitat_id INTEGER,
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

            _buttons = new List<Button>();
            _gameStatsSubject = new GameStatsSubject();

            previousMouseState = Mouse.GetState();

            int initialHabitatCount = GetHabitatCountFromDB();
            int initialAnimalCount = GetAnimalCountFromDB();
            _gameStatsSubject.UpdateStats(initialHabitatCount, initialAnimalCount);

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

            Button button1 = new Button(_buttonTexture, _buttonFont, startPosition, "Build habitat");
            button1.Rectangle = new Rectangle((int)startPosition.X, (int)startPosition.Y, buttonWidth, buttonHeight);
            button1.OnClick += BuildHabitat_Clicked; 
            _buttons.Add(button1);

            Button button2 = new Button(_buttonTexture, _buttonFont, new Vector2(startPosition.X, startPosition.Y + buttonHeight + buttonSpacing), "Add animal");
            button2.Rectangle = new Rectangle((int)button2.Position.X, (int)button2.Position.Y, buttonWidth, buttonHeight);
            button2.OnClick += AddAnimal_Clicked;
            _buttons.Add(button2);

            Button button3 = new Button(_buttonTexture, _buttonFont, new Vector2(startPosition.X, startPosition.Y + 2 * (buttonHeight + buttonSpacing)), "Hire Zookeeper");
            button3.Rectangle = new Rectangle((int)button3.Position.X, (int)button3.Position.Y, buttonWidth, buttonHeight);
            button3.OnClick += HireZookeeper_Clicked;
            _buttons.Add(button3);
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

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        // Click handler methods
        private void BuildHabitat_Clicked()
        {
            Debug.WriteLine("Build Habitat button clicked!");
            var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO Habitat (name, type, size, max_animals) VALUES ('New Habitat', 'Generic', 100, 10);";
            command.ExecuteNonQuery();
            _gameStatsSubject.UpdateStats(GetHabitatCountFromDB(), GetAnimalCountFromDB());
        }

        private void AddAnimal_Clicked()
        {
            Debug.WriteLine("Add Animal button clicked!");
            var checkHabitatCmd = _connection.CreateCommand();
            checkHabitatCmd.CommandText = "SELECT habitat_id FROM Habitat LIMIT 1;";
            var habitatIdObj = checkHabitatCmd.ExecuteScalar();

            if (habitatIdObj != null && habitatIdObj != DBNull.Value)
            {
                long habitatId = Convert.ToInt64(habitatIdObj);
                var command = _connection.CreateCommand();
                command.CommandText = $"INSERT INTO Animal (name, mood, hunger, stress, habitat_id) VALUES ('New Animal', 100, 0, 0, {habitatId});";
                command.ExecuteNonQuery();
                _gameStatsSubject.UpdateStats(GetHabitatCountFromDB(), GetAnimalCountFromDB());
            }
            else
            {
                Debug.WriteLine("No habitat available to add an animal.");
            }
        }

        private void HireZookeeper_Clicked()
        {
            Debug.WriteLine("Hire Zookeeper button clicked!");
            // TODO: Add SQLite code for hiring a zookeeper
        }

        private int GetHabitatCountFromDB()
        {
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Habitat;";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private int GetAnimalCountFromDB()
        {
            var command = _connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Animal;";
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}
