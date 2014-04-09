using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ComS437_MMayer_Pong
{
    class ScoreBar
    {
        public Texture2D sprite;
        public Rectangle area;
        public SpriteFont mediumFont;
        public SpriteFont smallFont;
        public int score_playerOne;
        public int score_playerTwo;
        public string centerString;

        private Vector2 tempVector;

        public ScoreBar(Texture2D sprite, Rectangle area, SpriteFont mediumFont, SpriteFont smallFont)
        {
            this.sprite = sprite;
            this.area = area;
            this.mediumFont = mediumFont;
            this.smallFont = smallFont;
            this.score_playerOne = 0;
            this.score_playerTwo = 0;
            this.centerString = "";
            tempVector = new Vector2();
        }

        public void draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(sprite, area, Color.White);

            tempVector.X = area.Left + 50; tempVector.Y = area.Top + 10;
            spriteBatch.DrawString(mediumFont, "Player 1", tempVector, Color.Red);

            tempVector.X = area.Left + 50; tempVector.Y = area.Top + 40;
            spriteBatch.DrawString(smallFont, "Score: " + score_playerOne, tempVector, Color.Red);

            tempVector.X = area.Width - 120; tempVector.Y = area.Top + 10;
            spriteBatch.DrawString(mediumFont, "Player 2", tempVector, Color.Blue);
            
            tempVector.X = area.Width - 120; tempVector.Y = area.Top + 40;
            spriteBatch.DrawString(smallFont, "Score: " + score_playerTwo, tempVector, Color.Blue);

            tempVector.X = area.Width / 2 - 70; tempVector.Y = area.Top + 25;
            spriteBatch.DrawString(mediumFont, centerString, tempVector, Color.White);

        }
    }
}
