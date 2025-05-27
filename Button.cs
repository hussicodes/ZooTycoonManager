using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTycoonManager
{
    public class Button
    {
        public Rectangle Bounds;
        public string Text;
        public SpriteFont Font;
        public Texture2D Texture;
        public bool IsClicked;

        private MouseState previousMouseState;

        public Button(Texture2D texture, SpriteFont font, Rectangle bounds, string text)
        {
            Texture = texture;
            Font = font;
            Bounds = bounds;
            Text = text;
        }

        public void Update()
        {
            MouseState mouse = Mouse.GetState();
            IsClicked = false;

            if (Bounds.Contains(mouse.Position) && mouse.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                IsClicked = true;
            }
            previousMouseState = mouse;
        }

        public void Draw(SpriteBatch spriteBach)
        {
            spriteBach.Draw(Texture, Bounds, Color.White);

            //Text in center
            Vector2 textSize = Font.MeasureString(Text);
            Vector2 textPosition = new Vector2(Bounds.X + (Bounds.Width - textSize.X) / 2, Bounds.Y + (Bounds.Height - textSize.Y) / 2);

            spriteBach.DrawString(Font, Text, textPosition, Color.Black);
        }
    }
}
