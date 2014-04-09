/**
 * Dogeball
 * 
 * Author: Matt Mayer
 **/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace ComS437_MMayer_Pong
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class DogeBallGame : Microsoft.Xna.Framework.Game
    {
        //Debug AI flag (use to enable left AI)
        bool debugLeftAiEnabled = false;

        //Game constants
        const int   CONTROL_SENSITIVITY          = 15;
        const int   AI_SPEED                     = 12;
        const int   FIELD_WIDTH                  = 1024;
        const int   FIELD_HEIGHT                 = 668;
        const float PADDLE_GRIP_COEFFICIENT      = 1.0f/90.0f;
        const float BALL_SCALE_DEFAULT           = 0.075f;
        const float BALL_SCALE_ZOOM              = 1.6f;
        const float BALL_SPEEDUP_COEFFICIENT     = 1.15f;
        const float BALL_SPEED_MAX               = 2000f;
        const float BALL_SPIN_VELOCITY_INFLUENCE = 50f;
        const float BALL_ROTATION_DISSIPATION    = 0.99f;
        const float POWERUP_SCALE_DEFAULT        = 0.12f;
        const float COLLISION_SCALAR             = 600f;        //Strength of change in ball Y-dir due to collision location on paddle
        const float POWERUP_PROBABILITY          = 0.102f;

        //Game objects
        Ball ball;
        Paddle paddleLeft;
        Paddle paddleRight;
        Powerup powerup;
        ScoreBar scoreBar;
        
        //Game state variables
        bool aiEnabled;                  //Determines whether or not Player 2 is computer-controlled
        KeyboardState prevKeyboardState; //Track keyboard state to detect key toggles
        int paddleHitCount;              //Track paddle hits for increases in ball velocity
        bool isBetweenRounds = true;
        bool isBallDropping = false;
        
        SpriteBatch spriteBatch;
        Random random = new Random();

        //TTS-related objects
        List<SpeechSynthesizer> asyncSpeakers;
        int readerNum = 0;
        string[] victoryPhrases = 
                {
                "The doge is pleased",
                "So ability, wow",
                "Wow",
                "Such points, so skill",
                "Much doge, very ball, wow",
                "Such amaze"
                };

        //Sprites
        SpriteFont dogeFontSmall;
        SpriteFont dogeFontMedium;
        SpriteFont dogeFontLarge;

        //Collision detection variables
        int xMax;
        int yMax;
        Rectangle ballRect;
        Rectangle paddleRect;

        

        public DogeBallGame()
        {
            GraphicsDeviceManager graphics = new GraphicsDeviceManager(this);

            graphics.PreferredBackBufferHeight = FIELD_HEIGHT + 100;
            graphics.PreferredBackBufferWidth = FIELD_WIDTH;

            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            IsMouseVisible = true;

            //Initialize game state
            prevKeyboardState = Keyboard.GetState();
            aiEnabled = false;
            paddleHitCount = 0;

            //Set up ball
            ball = new Ball(
                    Content.Load<Texture2D>("ball"), 
                    new Vector2((FIELD_WIDTH / 2), (FIELD_HEIGHT / 2)), 
                    BALL_SCALE_DEFAULT);

            //Set up paddles
            Texture2D paddleSpriteReference = Content.Load<Texture2D>("paddle_mid");
            paddleLeft = new Paddle(
                    paddleSpriteReference, 
                    new Vector2(30, 50));
            paddleRight = new Paddle(
                    paddleSpriteReference, 
                    new Vector2(FIELD_WIDTH - 30 - (int)(paddleSpriteReference.Width*Paddle.PADDLE_SCALE_X_DEFAULT), 50));
            
            //Set up powerup
            powerup = new Powerup(Content, POWERUP_SCALE_DEFAULT);

            //Set up score bar
            scoreBar = new ScoreBar(
                    Content.Load<Texture2D>("bottom"),
                    new Rectangle(0, FIELD_HEIGHT, FIELD_WIDTH, 100),
                    Content.Load<SpriteFont>("dogefont_medium"),
                    Content.Load<SpriteFont>("dogefont_small"));
            
            //Set up TTS speakers
            asyncSpeakers = new List<SpeechSynthesizer>();
            for (int i = 0; i < 100; i++)
            {
                asyncSpeakers.Add(new SpeechSynthesizer());
            }

            //Play welcome message
            sayAsync("Welcome, to doge ball.", 0);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Instantiate sprites
            //bottomBar = Content.Load<Texture2D>("bottom");
            dogeFontSmall = Content.Load<SpriteFont>("dogefont_small");
            dogeFontMedium = Content.Load<SpriteFont>("dogefont_medium");
            dogeFontLarge = Content.Load<SpriteFont>("dogefont_large");

            ballRect = new Rectangle();
            paddleRect = new Rectangle();

            //Game state defaults
            scoreBar.score_playerOne = 0;
            scoreBar.score_playerTwo = 0;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Check for quit signal (ESC or Back)
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();

            // AI toggle (Backspace)
            if (isKeyToggled(Keys.Back))
            {
                aiEnabled = !aiEnabled;
            }

            //Left paddle AI logic
            if (debugLeftAiEnabled)
            {
                
                if (ball.velocity.X < 0)
                {
                    float ballDiff = paddleLeft.getCenterpointY() - ball.position.Y;
                    ballDiff += 10; //Added to prevent AI strategy deadlock
                    if (ballDiff < 0)
                    {
                        paddleLeft.velocityY = Math.Max(-AI_SPEED, ballDiff);
                    }
                    else
                    {
                        paddleLeft.velocityY = Math.Min(AI_SPEED, ballDiff);
                    }
                }
                else
                {
                    if (paddleLeft.getCenterpointY() < FIELD_HEIGHT / 2 - 2)
                    {
                        paddleLeft.velocityY = Math.Max(-3, (paddleLeft.getCenterpointY() - FIELD_HEIGHT / 2 + 2) / 10);
                    }
                    else if (paddleLeft.getCenterpointY() > FIELD_HEIGHT / 2 + 2)
                    {
                        paddleLeft.velocityY = Math.Min(3, (paddleLeft.getCenterpointY() - FIELD_HEIGHT / 2 + 2) / 10);
                    }
                    else
                    {
                        paddleLeft.velocityY = 0;
                    }
                }
            }
            //Left paddle human-control logic
            else
            {
                paddleLeft.velocityY = CONTROL_SENSITIVITY * GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y;
                if (Keyboard.GetState().IsKeyDown(Keys.W)) paddleLeft.velocityY += CONTROL_SENSITIVITY;
                if (Keyboard.GetState().IsKeyDown(Keys.S)) paddleLeft.velocityY -= CONTROL_SENSITIVITY;
            }

            //Right paddle AI logic
            if (aiEnabled)
            {
                if (ball.velocity.X > 0)
                {
                    float ballDiff = paddleRight.getCenterpointY() - ball.position.Y;
                    ballDiff += 10; //Added to prevent AI strategy deadlock
                    if (ballDiff < 0)
                    {
                        paddleRight.velocityY = Math.Max(-AI_SPEED, ballDiff);
                    }
                    else
                    {
                        paddleRight.velocityY = Math.Min(AI_SPEED, ballDiff);
                    }
                }
                else
                {
                    if (paddleRight.getCenterpointY() < FIELD_HEIGHT / 2 - 2)
                    {
                        paddleRight.velocityY = Math.Max(-3, (paddleRight.getCenterpointY() - FIELD_HEIGHT / 2 + 2) / 10);
                    }
                    else if (paddleRight.getCenterpointY() > FIELD_HEIGHT / 2 + 2)
                    {
                        paddleRight.velocityY = Math.Min(3, (paddleRight.getCenterpointY() - FIELD_HEIGHT / 2 + 2 )/10);
                    }
                    else
                    {
                        paddleRight.velocityY = 0;
                    }
                }
            }
            else
            {
                paddleRight.velocityY = CONTROL_SENSITIVITY * GamePad.GetState(PlayerIndex.Two).ThumbSticks.Left.Y;
                if (Keyboard.GetState().IsKeyDown(Keys.Up)) paddleRight.velocityY += CONTROL_SENSITIVITY;
                if (Keyboard.GetState().IsKeyDown(Keys.Down)) paddleRight.velocityY -= CONTROL_SENSITIVITY;
            }

            // Move paddles based on velocity
            paddleLeft.update();
            paddleRight.update();

            //Ensure paddles are in bounds
            if (paddleLeft.position.Y < 0) paddleLeft.position.Y = 0;
            if (paddleLeft.bottom() > FIELD_HEIGHT) paddleLeft.position.Y = FIELD_HEIGHT - paddleLeft.height();
            if (paddleRight.position.Y < 0) paddleRight.position.Y = 0;
            if (paddleRight.bottom() > FIELD_HEIGHT) paddleRight.position.Y = FIELD_HEIGHT - paddleRight.height();

            //"Between rounds" logic
            if (isBetweenRounds)
            {
                //Set parameters to default
                ball.reset();
                powerup.reset();
                paddleLeft.reset();
                paddleRight.reset();

                //If ball is "suspended" above field
                if (!isBallDropping)
                {
                    ball.scale = BALL_SCALE_ZOOM;
                    ball.position.X = FIELD_WIDTH / 2;
                    ball.position.Y = FIELD_HEIGHT / 2;

                    //Check for start signal
                    if (GamePad.GetState(PlayerIndex.One).Buttons.Start == ButtonState.Pressed ||
                            GamePad.GetState(PlayerIndex.Two).Buttons.Start == ButtonState.Pressed ||
                            Keyboard.GetState().IsKeyDown(Keys.Space))
                    {
                        isBallDropping = true;
                    }

                }
                else
                {
                    if(!(scoreBar.centerString.Equals("")))
                    {
                        scoreBar.centerString = "";
                        scoreBar.score_playerOne = 0;
                        scoreBar.score_playerTwo = 0;
                    }
                    ball.scale -= .05f;
                    ball.position.X = FIELD_WIDTH / 2;
                    ball.position.Y = FIELD_HEIGHT / 2;
                    if (ball.scale <= .075f)
                    {
                        //Begin game
                        ball.scale = .075f;
                        ball.velocity.X = (random.NextDouble() > .5 ? -350 : 350);
                        ball.velocity.Y = (int)(random.NextDouble()*700 - 350);
                        paddleHitCount = 0;
                        isBetweenRounds = false;
                        isBallDropping = false;
                    }
                }
            }
            else
            {
                // Move the sprite by speed, scaled by elapsed time
                ball.position += ball.velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

                xMax = FIELD_WIDTH - (int)(ball.sprite.Width * ball.scale);
                yMax = FIELD_HEIGHT - (int)(ball.sprite.Height * ball.scale);

                //Check left wall collision
                if (ball.position.X - (ball.sprite.Width*ball.scale/2) < 0)
                {
                    //Player two scores
                    scoreBar.score_playerTwo++;
                    isBetweenRounds = true;
                    readerNum = 4;

                    if (scoreBar.score_playerTwo >= 7)
                    {
                        sayAsync("Player two is victorious! " + victoryPhrases[(int)(random.NextDouble() * victoryPhrases.Count())], 0);
                        scoreBar.centerString = "Player two wins!";
                    }
                    else
                    {
                        sayAsync("Player two has scored, " + victoryPhrases[(int)(random.NextDouble() * victoryPhrases.Count())], 0);
                    }
                }
                //Check right wall collision
                else if (ball.position.X - (ball.sprite.Width * ball.scale/2) > xMax)
                {
                    //Player one scores
                    scoreBar.score_playerOne++;
                    isBetweenRounds = true;
                    readerNum = 4;
                    
                    if (scoreBar.score_playerOne >= 7)
                    {
                        sayAsync("Player one is victorious! " + victoryPhrases[(int)(random.NextDouble() * victoryPhrases.Count())], 0);
                        scoreBar.centerString = "Player one wins!";
                    }
                    else
                    {
                        sayAsync("Player one has scored, " + victoryPhrases[(int)(random.NextDouble() * victoryPhrases.Count())], 0);
                    }
                    
                }
                //We hit the top
                if (ball.position.Y - ball.radius() < 0 && ball.velocity.Y < 0)
                {
                    ball.velocity.Y *= -1;
                    //Give the ball a nudge if so it doesn't glitch through the wall
                    if (ball.velocity.Y < 30)
                        ball.velocity.Y += 300;
                    sayAsync("Doge", (int)(random.NextDouble() * 10) - 5);
                }
                //We hit the bottom
                if(ball.position.Y - ball.radius() > yMax && ball.velocity.Y > 0)
                {
                    ball.velocity.Y *= -1;
                    //Give the ball a nudge if so it doesn't glitch through the wall
                    if (ball.velocity.Y > -30)
                        ball.velocity.Y -= 300;
                    sayAsync("Doge", (int)(random.NextDouble() * 10) - 5);
                }

                //Check powerup collision
                if (powerup.isOnScreen)
                {
                    float dist = Vector2.Distance(ball.position, powerup.position);
                    if(dist < (ball.sprite.Width*ball.scale/2) + (powerup.currentSprite.Width*powerup.scale/2))
                    {
                        powerup.isOnScreen = false;
                        //"Enlarge" Powerup
                        if (powerup.powerupType == PowerupType.EnlargePaddle)
                        {
                            sayAsync("much large, very paddle", 0);
                            if (ball.velocity.X > 0)
                            {
                                paddleLeft.enlarge();
                            }
                            else
                            {
                                paddleRight.enlarge();
                            }
                        }
                        //"Redirect" Powerup
                        else if (powerup.powerupType == PowerupType.RedirectBall)
                        {
                            sayAsync("so confuse", 0);
                            ball.velocity.Y = (float)random.NextDouble() * 1000 - 500;
                        }
                        else if (powerup.powerupType == PowerupType.ShrinkOpponent)
                        {
                            sayAsync("very shrink, such opponent", 0);
                            if (ball.velocity.X > 0)
                            {
                                paddleRight.shrink();
                            }
                            else
                            {
                                paddleLeft.shrink();
                            }
                        }
                    }
                }

                //Get ball collision rectangle
                ballRect = ball.getUpdatedCollisionRectangle();

                //Get left paddle collision rectangle
                paddleRect = paddleLeft.getUpdatedCollisionRectangle();
                
                //Check for Left Paddle + Ball collision
                if (ballRect.Intersects(paddleRect) && ball.velocity.X < 0)
                {
                    //Apply rotation
                    ball.rotationRate = paddleLeft.velocityY * PADDLE_GRIP_COEFFICIENT;
                    sayAsync("Doge", (int)(random.NextDouble() * 10) - 5);
                    paddleHitCount++;
                    //Every 4 paddle hits, speed up the ball (up to a maximum)
                    if (paddleHitCount % 4 == 0 && Math.Abs(ball.velocity.X) < BALL_SPEED_MAX)
                    {
                        ball.velocity *= BALL_SPEEDUP_COEFFICIENT;
                    }
                    ball.velocity.X *= -1;

                    //Apply change in Y-direction based on ball strike location
                    ball.velocity.Y -= COLLISION_SCALAR*(.5f- (ball.position.Y - paddleLeft.position.Y) / (paddleLeft.sprite.Height * paddleLeft.scale.Y));
                }

                //Get left paddle collision rectangle
                paddleRect = paddleRight.getUpdatedCollisionRectangle();

                //Check for Right Paddle + Ball collision
                if (ballRect.Intersects(paddleRect) && ball.velocity.X > 0)
                {
                    //Apply rotation
                    ball.rotationRate = - paddleRight.velocityY * PADDLE_GRIP_COEFFICIENT;
                    sayAsync("Doge", (int)(random.NextDouble() * 10) - 5);
                    paddleHitCount++;
                    //Every 4 paddle hits, speed up the ball (up to a maximum)
                    if (paddleHitCount % 4 == 0 && Math.Abs(ball.velocity.X) < BALL_SPEED_MAX)
                    {
                        ball.velocity *= BALL_SPEEDUP_COEFFICIENT;
                    }
                    ball.velocity.X *= -1;

                    //Apply change in Y-direction based on ball strike location
                    ball.velocity.Y -= COLLISION_SCALAR * (.5f - (ball.position.Y - paddleRight.position.Y) / (paddleRight.sprite.Height * paddleRight.scale.Y));
                }
            }
            
            //Rotate ball and curve trajectory
            ball.rotationAngle += ball.rotationRate;
            ball.velocity.Y += ball.rotationRate*BALL_SPIN_VELOCITY_INFLUENCE;

            //Dissipate rotation
            ball.rotationRate *= BALL_ROTATION_DISSIPATION;

            //Uncomment to enable simple gravity
            //ball.velocity.Y += 30;

            //Update powerup state
            if (!powerup.isOnScreen && !isBallDropping && !isBetweenRounds)
            {
                if (random.NextDouble() < POWERUP_PROBABILITY)
                {
                    powerup.randomizeAndDisplay();

                    //Select random on-screen position, ensuring it's not too close to the paddles
                    powerup.position = new Vector2((float)random.NextDouble() * (FIELD_WIDTH - 200) + 100, (float)random.NextDouble() * (FIELD_HEIGHT - (powerup.currentSprite.Height * powerup.scale)) + (powerup.currentSprite.Height * powerup.scale / 2));
                }
            }

            //Update keyboard "previous state" for key toggle checking
            prevKeyboardState = Keyboard.GetState();

            
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            //Draw background
            spriteBatch.DrawString(dogeFontLarge, "wow", new Vector2(200, 100), Color.Yellow, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(dogeFontLarge, "very doge, so game", new Vector2(300, 200), Color.Orange, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(dogeFontSmall, "wow tennis", new Vector2(750, 600), Color.Red, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(dogeFontSmall, "hardcor gaming", new Vector2(550, 550), Color.OldLace, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(dogeFontMedium, "such cornflwer blue", new Vector2(700, 400), Color.LightGreen, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(dogeFontMedium, "indie levl=100", new Vector2(200, 500), Color.Plum, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            if (aiEnabled)
            {
                spriteBatch.DrawString(dogeFontMedium, "computer so smart, wow", new Vector2(500, 100), Color.LightGreen, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }


            //Draw main sprites
            spriteBatch.Draw(ball.sprite, ball.position, null, Color.White, ball.rotationAngle, new Vector2(ball.sprite.Width/2, ball.sprite.Height/2), ball.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(paddleLeft.sprite, paddleLeft.position, null, Color.White, 0f, Vector2.Zero, paddleLeft.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(paddleRight.sprite, paddleRight.position, null, Color.White, 0f, Vector2.Zero, paddleRight.scale, SpriteEffects.None, 0f);

            //Draw powerup
            if (powerup.isOnScreen)
            {
                spriteBatch.Draw(powerup.currentSprite, powerup.position, null, Color.White, 0f, new Vector2(powerup.currentSprite.Width/2, powerup.currentSprite.Height / 2), powerup.scale, SpriteEffects.None, 0f);
            }

            //Draw bottom panel
            scoreBar.draw(spriteBatch);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void sayAsync(string toSay, int speechSpeed)
        {
            SpeechSynthesizer speaker = asyncSpeakers[readerNum];
            readerNum = (readerNum == asyncSpeakers.Count - 1 ? 0 : readerNum + 1);

            speaker.Rate = speechSpeed;
                
            speaker.SpeakAsync(toSay);
        }

        private bool isKeyToggled(Keys key)
        {
            KeyboardState keyboardState = Keyboard.GetState();

            if (!prevKeyboardState.IsKeyDown(key) && keyboardState.IsKeyDown(key))
                return true;
            else
                return false;
        }
    }
}
