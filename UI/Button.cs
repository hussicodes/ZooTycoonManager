using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace ZooTycoonManager.UI
{
    public class Button
    {
        public Texture2D Texture { get; set; }
        public Vector2 Position { get; set; }
        public Rectangle Rectangle { get; set; }
        public string Text { get; set; }
        public SpriteFont Font { get; set; }
        public Color TextColor { get; set; }

        public event Action OnClick;

        public Button(Texture2D texture, SpriteFont font, Vector2 position, string text)
        {
            Texture = texture;
            Font = font;
            Position = position;
            Text = text;
            TextColor = Color.Black;

            if (Texture != null)
            {
                Rectangle = new Rectangle((int)Position.X, (int)Position.Y, Texture.Width, Texture.Height);
            }
            else if (Font != null && !string.IsNullOrEmpty(Text))
            {
                Vector2 textSize = Font.MeasureString(Text);
                Rectangle = new Rectangle((int)Position.X, (int)Position.Y, (int)textSize.X + 20, (int)textSize.Y + 10); // Add some padding
            }
            else
            {
                Rectangle = new Rectangle((int)Position.X, (int)Position.Y, 100, 30); 
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (Texture != null)
            {
                spriteBatch.Draw(Texture, Rectangle, Color.White);
            }

            if (Font != null && !string.IsNullOrEmpty(Text))
            {
                Vector2 textSize = Font.MeasureString(Text);
                Vector2 textPosition = new Vector2(
                    Rectangle.X + (Rectangle.Width - textSize.X) / 2,
                    Rectangle.Y + (Rectangle.Height - textSize.Y) / 2
                );
                spriteBatch.DrawString(Font, Text, textPosition, TextColor);
            }
        }

        // Optional: Method to update button state (e.g., hover, click)
        public void Update(MouseState mouseState)
        {
        }

        public void Update(MouseState currentMouseState, MouseState previousMouseState)
        {
            bool isMouseOver = Rectangle.Contains(currentMouseState.Position);

            if (isMouseOver && currentMouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
            {
                OnClick?.Invoke();
            }
        }

        public void SetTexture(Texture2D texture)
        {
            Texture = texture;
        }
    }
} 