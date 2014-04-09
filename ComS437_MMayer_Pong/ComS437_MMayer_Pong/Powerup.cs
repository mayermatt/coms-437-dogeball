using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace ComS437_MMayer_Pong
{
    public enum PowerupType
    { 
        RedirectBall,
        EnlargePaddle,
        ShrinkOpponent
    };

    class Powerup
    {
        public PowerupType powerupType;
        public Texture2D currentSprite;
        public Vector2 position;
        public float scale;
        public bool isOnScreen;

        private Dictionary<PowerupType, Texture2D> typeMap;

        public Powerup(ContentManager content, float scale)
        {
            //Create and fill PowerupType/Sprite map
            typeMap = new Dictionary<PowerupType, Texture2D>();
            typeMap.Add(PowerupType.EnlargePaddle, content.Load<Texture2D>("enlarge"));
            typeMap.Add(PowerupType.RedirectBall, content.Load<Texture2D>("redirect"));
            typeMap.Add(PowerupType.ShrinkOpponent, content.Load<Texture2D>("shrink"));

            this.scale = scale;
        }

        //Call this to prepare the powerup for screen display
        public void randomizeAndDisplay()
        {
            //Choose a random powerup type
            Array values = Enum.GetValues(typeof(PowerupType));
            Random random = new Random();
            powerupType = (PowerupType)values.GetValue(random.Next(values.Length));
            
            //Set sprite
            currentSprite = typeMap[powerupType];
            
            //Denote that powerup is ready to display
            isOnScreen = true;
        }

        public void reset()
        {
            isOnScreen = false;
        }

        public float radius()
        {
            return currentSprite.Width * scale / 2;
        }
    }
}
