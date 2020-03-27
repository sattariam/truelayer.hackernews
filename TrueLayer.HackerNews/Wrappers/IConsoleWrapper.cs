using System;

namespace TrueLayer.HackerNews.Wrappers
{
    public interface IConsoleWrapper
    {
        string ReadLine();
        void Write(string message);
        void WriteLine(string message);
        void WriteLine();
        ConsoleKeyInfo ReadKey();
    }
}