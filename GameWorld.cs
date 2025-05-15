using Microsoft.Data.Sqlite;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ZooTycoonManager
{
    public class GameWorld : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public GameWorld()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            using var connection = new SqliteConnection("Data Source=mydb.db");
            connection.Open();

            var createTablesCmd = connection.CreateCommand();

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

            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
