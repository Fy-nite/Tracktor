namespace V12.Wrapper.Grouch
{
    using System;


    public class Logger
    {
        public string? name = "basic Grouch";
        public Logger()
        {
        }
        public Logger(string name)
        {
        }
        public static Logger getLogger(string name)
        {
            return new Logger(name);
        }
        public void info(string message)
        {
            Console.WriteLine($"{name} [INFO] " + message);
        }
        public void warn(string message)
        {
            Console.WriteLine($"{name} [WARN] " + message);
        }
        public void error(string message)
        {
            Console.WriteLine($"{name} [ERROR] " + message);
        }
    }
}