using System;
using System.IO;

namespace XboxAdpcmTool
{
    internal static class CommandLineApp
    {
        internal static void Run(string[] args)
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                ShowHelp();
                return;
            }

            if (args[0].Equals("Decode", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 3)
                {
                    ShowHelp();
                    return;
                }

                if (CheckFileExists(args[1]))
                    XboxAdpcmWaveConverter.DecodeAdpcmWave(args[1], args[2].Trim());

                return;
            }

            if (args.Length != 2)
            {
                ShowHelp();
                return;
            }

            if (CheckFileExists(args[0]))
                XboxAdpcmWaveConverter.EncodePcmWave(args[0], args[1].Trim());
        }

        private static bool CheckFileExists(string filePath)
        {
            if (File.Exists(filePath))
                return true;

            Console.WriteLine("Error: file not found: " + filePath);
            return false;
        }

        private static bool IsHelp(string value)
        {
            return value.Equals("help", StringComparison.OrdinalIgnoreCase)
                || value.Equals("?")
                || value.Equals("--help", StringComparison.OrdinalIgnoreCase);
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Xbox ADPCM Tool");
            Console.WriteLine("Info: Supports 16-bit PCM WAV files.");
            Console.WriteLine();
            Console.WriteLine("Encoding:");
            Console.WriteLine("  xbadpcmencode.exe <InputFile.wav> <OutputFile.wav>");
            Console.WriteLine();
            Console.WriteLine("Decoding:");
            Console.WriteLine("  xbadpcmencode.exe Decode <InputFile.wav> <OutputFile.wav>");
        }
    }
}
