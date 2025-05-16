using System.Threading;
using System;
using ZooTycoonManager;

namespace ZooTycoonManager
{
    public static class Program
    {
        private static Mutex mutex;

        [STAThread]
        static void Main()
        {
            bool createdNew;
            mutex = new Mutex(true, "GameMutex", out createdNew);

            if (!createdNew)
            {
                Console.WriteLine("Already running");
                return;
            }

            using var game = new GameWorld();
            game.Run();
        }
    }
}
