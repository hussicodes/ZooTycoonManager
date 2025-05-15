using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ZooTycoonManager.Interfaces;
using ZooTycoonManager.Subjects;

namespace ZooTycoonManager.UI
{
    public class StatsDisplay : IObserver
    {
        private SpriteFont _font;
        private Vector2 _position;
        private string _displayText;

        public int HabitatCount { get; private set; }
        public int AnimalCount { get; private set; }

        public StatsDisplay(SpriteFont font, Vector2 position, ISubject gameStatsSubject)
        {
            _font = font;
            _position = position;
            _displayText = "Habitats: 0, Animals: 0"; 
            Update(gameStatsSubject); 
        }

        public void Update(ISubject subject)
        {
            if (subject is GameStatsSubject gameStats)
            {
                HabitatCount = gameStats.HabitatCount;
                AnimalCount = gameStats.AnimalCount;
                _displayText = $"Habitats: {HabitatCount}, Animals: {AnimalCount}";
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(_font, _displayText, _position, Color.White);
        }
    }
} 