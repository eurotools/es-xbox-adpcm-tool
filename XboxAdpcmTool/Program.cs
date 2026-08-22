using System;

namespace XboxAdpcmTool
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                return CommandLineApp.Run(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }
    }
}
