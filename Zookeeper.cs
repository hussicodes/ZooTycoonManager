using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System;

namespace ZooTycoonManager
{
    public class Zookeeper : GameObject, ISaveable
    {
        public int ZookeeperId { get; set; }
        public string Name { get; set; }
        public int Upkeep { get; set; }
        public int? HabitatId { get; set; }

        public Zookeeper()
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

            command.Parameters.AddWithValue("$zookeeper_id", ZookeeperId);
            command.Parameters.AddWithValue("$name", Name);
            command.Parameters.AddWithValue("$upkeep", Upkeep);
            command.Parameters.AddWithValue("$habitat_id", HabitatId.HasValue ? (object)HabitatId.Value : DBNull.Value);
            command.Parameters.AddWithValue("$position_x", PositionX);
            command.Parameters.AddWithValue("$position_y", PositionY);

            command.CommandText = @"
                UPDATE Zookeeper 
                SET name = $name, 
                    upkeep = $upkeep, 
                    habitat_id = $habitat_id, 
                    position_x = $position_x, 
                    position_y = $position_y
                WHERE zookeeper_id = $zookeeper_id;
            ";
            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                command.CommandText = @"
                    INSERT INTO Zookeeper (zookeeper_id, name, upkeep, habitat_id, position_x, position_y)
                    VALUES ($zookeeper_id, $name, $upkeep, $habitat_id, $position_x, $position_y);
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Inserted Zookeeper: ID {ZookeeperId}, Name: {Name}");
            }
            else
            {
                Debug.WriteLine($"Updated Zookeeper: ID {ZookeeperId}, Name: {Name}");
            }
        }
    }
} 