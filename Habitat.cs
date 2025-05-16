using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace ZooTycoonManager
{
    public class Habitat : GameObject, ISaveable
    {
        public int HabitatId { get; set; }
        public int Size { get; set; }
        public int MaxAnimals { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        public Habitat()
        {
        }

        public override void LoadContent()
        {
            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
        }

        public void Save(SqliteTransaction transaction)
        {
            var command = transaction.Connection.CreateCommand();
            command.Transaction = transaction;

            command.Parameters.AddWithValue("$habitat_id", HabitatId);
            command.Parameters.AddWithValue("$size", Size);
            command.Parameters.AddWithValue("$max_animals", MaxAnimals);
            command.Parameters.AddWithValue("$name", Name);
            command.Parameters.AddWithValue("$type", Type);
            command.Parameters.AddWithValue("$position_x", PositionX);
            command.Parameters.AddWithValue("$position_y", PositionY);

            command.CommandText = @"
                UPDATE Habitat 
                SET size = $size, 
                    max_animals = $max_animals, 
                    name = $name, 
                    type = $type, 
                    position_x = $position_x, 
                    position_y = $position_y
                WHERE habitat_id = $habitat_id;
            ";
            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                command.CommandText = @"
                    INSERT INTO Habitat (habitat_id, size, max_animals, name, type, position_x, position_y)
                    VALUES ($habitat_id, $size, $max_animals, $name, $type, $position_x, $position_y);
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Inserted Habitat: ID {HabitatId}");
            }
            else
            {
                Debug.WriteLine($"Updated Habitat: ID {HabitatId}");
            }
        }
    }
} 