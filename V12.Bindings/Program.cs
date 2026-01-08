// See https://aka.ms/new-console-template for more information
using System;
using V12.Bindings;

namespace V12.Bindings
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // register a default bridge so other projects can call into it
            EngineBridgeProvider.Instance = new NullEngineBridge();

            Console.WriteLine("V12.Bindings initialized with default NullEngineBridge.");
        }
    }
}
