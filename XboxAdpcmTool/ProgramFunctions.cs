using NAudio.Wave;
using System;
using System.IO;

namespace XboxAdpcmTool
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    internal static class ProgramFunctions
    {
        //-------------------------------------------------------------------------------------------------------------------------------
        internal static void ExecuteEncoder(string inputFile, string outputFile)
        {
            string fileExtension = Path.GetExtension(inputFile);
            if (fileExtension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                short[] pcmData;
                int frequency, channels;
                //Get File Data
                using (WaveFileReader reader = new WaveFileReader(inputFile))
                {
                    //Get basic info
                    frequency = reader.WaveFormat.SampleRate;
                    channels = reader.WaveFormat.Channels;

                    //Get pcm short array
                    byte[] pcmByteData = new byte[reader.Length];
                    reader.Read(pcmByteData, 0, pcmByteData.Length);
                    pcmData = WavFunctions.ConvertByteArrayToShortArray(pcmByteData);
                }

                //Start encode!
                byte[] adpcmData = XboxAdpcm.Encode(pcmData, pcmData.Length);

                //Write File
                XboxAdpcm.WriteAdpcmFile(adpcmData, outputFile, channels, frequency, 4);
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        internal static void ExecuteDecoder(string inputFile, string outputFile)
        {
            string fileExtension = Path.GetExtension(inputFile);
            if (fileExtension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                byte[] adpcmData;
                int frequency, channels;

                //Get File Data
                using (WaveFileReader reader = new WaveFileReader(inputFile))
                {
                    //Get basic info
                    frequency = reader.WaveFormat.SampleRate;
                    channels = reader.WaveFormat.Channels;

                    //Get pcm short array
                    adpcmData = new byte[reader.Length];
                    reader.Read(adpcmData, 0, adpcmData.Length);
                }

                //Start decoding!
                byte[] pcmByteData = XboxAdpcm.Decode(adpcmData);

                //Save file
                IWaveProvider provider = new RawSourceWaveStream(new MemoryStream(pcmByteData), new WaveFormat(frequency, 16, 1));
                try
                {
                    WaveFileWriter.CreateWaveFile(outputFile, provider);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        internal static bool CheckFileExists(string filePath)
        {
            bool fileExists = false;

            if (File.Exists(filePath))
            {
                fileExists = true;
            }
            else
            {
                Console.WriteLine("ERROR: file not found: " + filePath);
            }

            return fileExists;
        }
    }
}
