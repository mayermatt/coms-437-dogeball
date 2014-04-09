using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ComS437_MMayer_Pong
{
    class Paddle
    {
        const float ENLARGE_RATE = .001f;

        public const float PADDLE_SCALE_X_DEFAULT = 0.05f;
        public const float PADDLE_SCALE_Y_DEFAULT = 0.2f;

        public Texture2D sprite;
        public Vector2 position;
        public float velocityY;
        public Vector2 scale;
        private Rectangle collisionRectangle;
        private float targetScaleY;
        private int paddleSize;               //Size ranges from 0-2

        public Paddle(Texture2D sprite, Vector2 position)
        {
            this.sprite = sprite;
            this.position = position;
            this.velocityY = 0f;
            this.scale = new Vector2(PADDLE_SCALE_X_DEFAULT, PADDLE_SCALE_Y_DEFAULT);
            this.targetScaleY = scale.Y;
            this.paddleSize = 1;
        }

        public float getSizeY()
        {
            return sprite.Height * scale.Y;
        }

        public float getCenterpointY()
        {
            return position.Y + sprite.Height * scale.Y / 2;
        }

        public void enlarge()
        {
            if (paddleSize == 0)
            {
                targetScaleY = .2f;
                paddleSize++;
            }
            else if (paddleSize == 1)
            {
                targetScaleY = .32f;
                paddleSize++;
            }
        }

        public void shrink()
        {
            if (paddleSize == 1)
            {
                targetScaleY = .1f;
                paddleSize--;
            }
            else if (paddleSize == 2)
            {
                targetScaleY = .2f;
                paddleSize--;
            }
        }

        //Update position based on velocity, update scale if needed
        public void update()
        {
            position.Y -= velocityY;
            if (scale.Y < targetScaleY)
            {
                scale.Y += ENLARGE_RATE;

                //Magic number to make paddle upwards and downwards at roughly the same rate
                position.Y -= .3f; 
            }
            if (scale.Y > targetScaleY)
            {
                scale.Y -= ENLARGE_RATE;

                //Magic number to make paddle upwards and downwards at roughly the same rate
                position.Y += .3f;
            }
        }

        public float height()
        {
            return sprite.Height * scale.Y;
        }

        public float bottom()
        {
            return position.Y + (sprite.Height * scale.Y);
        }

        public void reset()
        {
            scale.Y = PADDLE_SCALE_Y_DEFAULT;
            targetScaleY = PADDLE_SCALE_Y_DEFAULT;
            paddleSize = 1;
        }

        public Rectangle getUpdatedCollisionRectangle()
        {
            collisionRectangle.X = (int)position.X;
            collisionRectangle.Y = (int)position.Y;
            collisionRectangle.Width = (int)(sprite.Width * scale.X);
            collisionRectangle.Height = (int)(sprite.Height * scale.Y);



            return collisionRectangle;
        }
    }
}
