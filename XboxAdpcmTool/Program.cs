using System;

namespace XboxAdpcmTool
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                CommandLineApp.Run(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
