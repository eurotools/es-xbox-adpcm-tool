using System;

namespace XboxAdpcmTool
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    class Program
    {
        //-------------------------------------------------------------------------------------------------------------------------------
        static void Main(string[] args)
        {
            //Ensure that we have arguments
            if (args.Length > 0)
            {
                //Show help if required
                if (args.Length == 1)
                {
                    if (args[0].Equals("help", StringComparison.OrdinalIgnoreCase) || args[0].Equals("?"))
                    {
                        Console.WriteLine("Xbox Adpcm Tool. By jmarti856");
                        Console.WriteLine("Info: Supports 16 bit WAV files.");
                        Console.WriteLine("");
                        Console.WriteLine("------------------------------------For Encoding------------------------------------");
                        Console.WriteLine("Usage: <InputFile> <OutputFile>");
                        Console.WriteLine("");
                        Console.WriteLine("------------------------------------For Decoding------------------------------------");
                        Console.WriteLine("Usage: Decode <InputFile> <OutputFile>");
                    }
                }
                else
                {
                    if (args.Length > 1)
                    {
                        string inputFile = args[0];
                        //Check if we have to execute the decoder
                        if (inputFile.Equals("Decode", StringComparison.OrdinalIgnoreCase))
                        {
                            if (args.Length > 2)
                            {
                                inputFile = args[1];
                                if (ProgramFunctions.CheckFileExists(inputFile))
                                {
                                    ProgramFunctions.ExecuteDecoder(inputFile, args[2].Trim());
                                }
                            }
                        }
                        else
                        {
                            //Execute encoder
                            if (ProgramFunctions.CheckFileExists(inputFile))
                            {
                                ProgramFunctions.ExecuteEncoder(inputFile, args[1].Trim());
                            }
                        }
                    }
                }
            }
        }
    }
}
