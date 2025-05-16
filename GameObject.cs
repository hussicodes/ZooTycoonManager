using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ZooTycoonManager
{
    public abstract class GameObject
    {
        public int PositionX { get; set; }
        public int PositionY { get; set; }

        public virtual void LoadContent()
        {
        }

        public virtual void Update(GameTime gameTime)
        {
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
        }
    }
} 