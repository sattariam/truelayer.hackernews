using System;

namespace TrueLayer.HackerNews.Wrappers
{
    public class ConsoleWrapper : IConsoleWrapper
    {
        public void Write(string message)
        {
            Console.Write(message);
        }

        public void WriteLine()
        {
            Console.WriteLine();
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public string ReadLine()
        {
            return Console.ReadLine();
        }

        public ConsoleKeyInfo ReadKey()
        {
            return Console.ReadKey();
        }
    }
}
