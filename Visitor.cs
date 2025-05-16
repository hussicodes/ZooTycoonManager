using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace ZooTycoonManager
{
    public class Visitor : GameObject, ISaveable
    {
        public int VisitorId { get; set; }
        public string Name { get; set; }
        public int Money { get; set; }
        public int Mood { get; set; }
        public int Hunger { get; set; }
        public int? HabitatId { get; set; }
        public int? ShopId { get; set; }

        public Visitor()
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

            command.Parameters.AddWithValue("$visitor_id", VisitorId);
            command.Parameters.AddWithValue("$name", Name);
            command.Parameters.AddWithValue("$money", Money);
            command.Parameters.AddWithValue("$mood", Mood);
            command.Parameters.AddWithValue("$hunger", Hunger);
            // Handle nullable foreign keys appropriately
            command.Parameters.AddWithValue("$habitat_id", HabitatId.HasValue ? (object)HabitatId.Value : System.DBNull.Value);
            command.Parameters.AddWithValue("$shop_id", ShopId.HasValue ? (object)ShopId.Value : System.DBNull.Value);
            command.Parameters.AddWithValue("$position_x", PositionX);
            command.Parameters.AddWithValue("$position_y", PositionY);

            command.CommandText = @"
                UPDATE Visitor 
                SET name = $name, 
                    money = $money, 
                    mood = $mood, 
                    hunger = $hunger, 
                    habitat_id = $habitat_id, 
                    shop_id = $shop_id, 
                    position_x = $position_x, 
                    position_y = $position_y
                WHERE visitor_id = $visitor_id;
            ";
            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                command.CommandText = @"
                    INSERT INTO Visitor (visitor_id, name, money, mood, hunger, habitat_id, shop_id, position_x, position_y)
                    VALUES ($visitor_id, $name, $money, $mood, $hunger, $habitat_id, $shop_id, $position_x, $position_y);
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Inserted Visitor: ID {VisitorId}, Name: {Name}");
            }
            else
            {
                Debug.WriteLine($"Updated Visitor: ID {VisitorId}, Name: {Name}");
            }
        }
    }
} 