using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace ZooTycoonManager
{
    public class Shop : GameObject, ISaveable 
    {
        public int ShopId { get; set; }
        public string Type { get; set; }
        public int Cost { get; set; }

        public Shop()
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

        // Added Save method
        public void Save(SqliteTransaction transaction)
        {
            var command = transaction.Connection.CreateCommand();
            command.Transaction = transaction;

            command.Parameters.AddWithValue("$shop_id", ShopId);
            command.Parameters.AddWithValue("$type", Type);
            command.Parameters.AddWithValue("$cost", Cost);
            command.Parameters.AddWithValue("$position_x", PositionX);
            command.Parameters.AddWithValue("$position_y", PositionY);

            command.CommandText = @"
                UPDATE Shop 
                SET type = $type, 
                    cost = $cost, 
                    position_x = $position_x, 
                    position_y = $position_y
                WHERE shop_id = $shop_id;
            ";
            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                command.CommandText = @"
                    INSERT INTO Shop (shop_id, type, cost, position_x, position_y)
                    VALUES ($shop_id, $type, $cost, $position_x, $position_y);
                ";
                command.ExecuteNonQuery();
                Debug.WriteLine($"Inserted Shop: ID {ShopId}, Type: {Type}");
            }
            else
            {
                Debug.WriteLine($"Updated Shop: ID {ShopId}, Type: {Type}");
            }
        }
    }
} 