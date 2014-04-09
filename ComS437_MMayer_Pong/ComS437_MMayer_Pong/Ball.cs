using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace ComS437_MMayer_Pong
{
    class Ball
    {
        public Texture2D  sprite;
        public Vector2    position;
        public Vector2    velocity;
        public float      scale;
        public float      rotationAngle;
        public float      rotationRate;
        private Rectangle collisionRectangle;

        public Ball(Texture2D sprite, Vector2 position, float scale)
        {
            this.sprite = sprite;
            this.position = position;
            this.velocity = new Vector2(0, 0);
            this.scale = scale;
            this.rotationAngle = 0f;
            this.rotationRate = 0f;
            collisionRectangle = new Rectangle();
        }

        public void reset()
        {
            velocity.X = 0;
            velocity.Y = 0;
            rotationAngle = 0;
            rotationRate = 0f;
        }

        public float radius()
        {
            return scale * sprite.Height / 2;
        }

        public Rectangle getUpdatedCollisionRectangle()
        {
            collisionRectangle.X = (int)position.X - (int)(sprite.Width * scale / 2);
            collisionRectangle.Y = (int)position.Y - (int)(sprite.Height * scale / 2);
            collisionRectangle.Width = (int)(sprite.Width * scale);
            collisionRectangle.Height = (int)(sprite.Height * scale);

            return collisionRectangle;
        }
    }
}
