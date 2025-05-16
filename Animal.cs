using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace ZooTycoonManager
{
    public class Animal : GameObject, ISaveable
    {
        public int AnimalId { get; set; }
        public string Name { get; set; }
        public int Mood { get; set; }
        public int Hunger { get; set; }
        public int Stress { get; set; }
        public int HabitatId { get; set; }

        public Animal()
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

            command.Parameters.AddWithValue("$animal_id", AnimalId);
            command.Parameters.AddWithValue("$name", Name);
            command.Parameters.AddWithValue("$mood", Mood);
            command.Parameters.AddWithValue("$hunger", Hunger);
            command.Parameters.AddWithValue("$stress", Stress);
            command.Parameters.AddWithValue("$habitat_id", HabitatId);
            command.Parameters.AddWithValue("$position_x", PositionX);
            command.Parameters.AddWithValue("$position_y", PositionY);

            command.CommandText = @"
                UPDATE Animal 
                SET name = $name, 
                    mood = $mood, 
                    hunger = $hunger, 
                    stress = $stress, 
                    habitat_id = $habitat_id, 
                    position_x = $position_x, 
                    position_y = $position_y
                WHERE animal_id = $animal_id;
            ";
            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                command.CommandText = @"
                    INSERT INTO Animal (animal_id, name, mood, hunger, stress, habitat_id, position_x, position_y)
                    VALUES ($animal_id, $name, $mood, $hunger, $stress, $habitat_id, $position_x, $position_y);
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Inserted Animal: ID {AnimalId}, Name: {Name}");
            }
            else
            {
                Debug.WriteLine($"Updated Animal: ID {AnimalId}, Name: {Name}");
            }
        }
    }
} 