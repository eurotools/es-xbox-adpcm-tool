//-------------------------------------------------------------------------------------------------------------------------------
// __   ___                          _____  _____   _____ __  __
// \ \ / / |                   /\   |  __ \|  __ \ / ____|  \/  |
//  \ V /| |__   _____  __    /  \  | |  | | |__) | |    | \  / |
//   > < | '_ \ / _ \ \/ /   / /\ \ | |  | |  ___/| |    | |\/| |
//  / . \| |_) | (_) >  <   / ____ \| |__| | |    | |____| |  | |
// /_/ \_\_.__/ \___/_/\_\ /_/    \_\_____/|_|     \_____|_|  |_|
//
//-------------------------------------------------------------------------------------------------------------------------------
// Command Line Interface
//-------------------------------------------------------------------------------------------------------------------------------
using System;
using System.IO;

namespace XboxAdpcmTool
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    internal static class CommandLineApp
    {
        //-------------------------------------------------------------------------------------------------------------------------------
        internal static int Run(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return 2;
            }

            if (IsHelp(args[0]))
            {
                ShowHelp();
                return 0;
            }

            if (args[0].Equals("Decode", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length != 3)
                {
                    ShowHelp();
                    return 2;
                }

                if (!CheckFileExists(args[1])) return 1;
                XboxAdpcmWaveConverter.DecodeAdpcmWave(args[1], args[2].Trim());

                return 0;
            }

            if (args.Length != 2)
            {
                ShowHelp();
                return 2;
            }

            if (!CheckFileExists(args[0])) return 1;
            XboxAdpcmWaveConverter.EncodePcmWave(args[0], args[1].Trim());
            return 0;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private static bool CheckFileExists(string filePath)
        {
            if (File.Exists(filePath))
                return true;

            Console.WriteLine("Error: file not found: " + filePath);
            return false;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private static bool IsHelp(string value)
        {
            return value.Equals("help", StringComparison.OrdinalIgnoreCase)
                || value.Equals("?")
                || value.Equals("--help", StringComparison.OrdinalIgnoreCase);
        }

        //-------------------------------------------------------------------------------------------------------------------------------
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
