using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTycoonManager
{
    public class Unit : GameObject
    {
        public Vector2 Position { get; set; }
        public Texture2D Sprite { get; set; }
        public Vector2 moveTo;
        public override void Draw(SpriteBatch spriteBatch)
        {
            //throw new NotImplementedException();
        }

        public override void LoadContent(ContentManager contentManager)
        {
            //throw new NotImplementedException();
        }

        public override void Update()
        {
            //throw new NotImplementedException();
        }

        public virtual void MoveTo(Vector2 newPos)
        {
            moveTo = newPos;
        }
    }
}
