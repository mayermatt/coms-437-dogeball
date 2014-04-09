using System;

namespace ComS437_MMayer_Pong
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (DogeBallGame game = new DogeBallGame())
            {
                game.Run();
            }
        }
    }
#endif
}

