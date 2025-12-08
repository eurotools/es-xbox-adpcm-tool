using NAudio.Wave;
using System;
using System.IO;
using System.Text;

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
            if (!fileExtension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Error: Only WAV files are supported.");
                return;
            }

            short[] pcmData;
            int sampleRate, channels;

            using (WaveFileReader reader = new WaveFileReader(inputFile))
            {
                WaveFormat format = reader.WaveFormat;

                // Validate: PCM, 16-bit, mono or stereo
                if (format.Encoding != WaveFormatEncoding.Pcm)
                {
                    Console.WriteLine("Error: WAV must be PCM.");
                    return;
                }

                if (format.BitsPerSample != 16)
                {
                    Console.WriteLine("Error: WAV must be 16-bit.");
                    return;
                }

                if (format.Channels != 1 && format.Channels != 2)
                {
                    Console.WriteLine("Error: WAV must be mono or stereo.");
                    return;
                }

                sampleRate = format.SampleRate;
                channels = format.Channels;

                // Read raw PCM data
                byte[] pcmBytes = new byte[reader.Length];
                int bytesRead = reader.Read(pcmBytes, 0, pcmBytes.Length);

                if (bytesRead == 0)
                {
                    Console.WriteLine("Error: No audio data found.");
                    return;
                }

                pcmData = ToolUtils.ConvertByteArrayToShortArray(pcmBytes);

                // Encode to Xbox ADPCM && write
                byte[] adpcmData = XboxAdpcm.Encode(pcmData, channels);
                WriteAdpcmFile(adpcmData, outputFile, format);

                Console.WriteLine("Encoding complete: " + outputFile);
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private static void WriteAdpcmFile(byte[] adpcmData, string outputFilePath, WaveFormat format)
        {
            using (BinaryWriter BWritter = new BinaryWriter(File.Open(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.ASCII))
            {
                //Write WAV Header
                BWritter.Write(Encoding.UTF8.GetBytes("RIFF")); //Chunk ID
                BWritter.Write((uint)(adpcmData.Length - 8)); //Chunk Size
                BWritter.Write(Encoding.UTF8.GetBytes("WAVE")); //Format
                BWritter.Write(Encoding.UTF8.GetBytes("fmt ")); //Subchunk1 ID
                BWritter.Write((uint)20); //Subchunk1 Size
                BWritter.Write((short)WaveFormatEncoding.WAVE_FORMAT_VOXWARE_BYTE_ALIGNED); //Audio Format
                BWritter.Write((short)format.Channels); //Num Channels
                BWritter.Write((uint)format.SampleRate); //Sample Rate
                BWritter.Write((uint)format.AverageBytesPerSecond); //Byte Rate
                BWritter.Write((short)(36 * format.Channels)); //Block Align
                BWritter.Write((short)4); //Bits Per Sample
                BWritter.Write(4194306);
                BWritter.Write(Encoding.UTF8.GetBytes("data")); //Subchunk2 ID
                BWritter.Write((uint)adpcmData.Length); //Subchunk2 Size

                //Write Xbox Adpcm Data
                BWritter.Write(adpcmData);

                //Close Writter
                BWritter.Close();
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
                byte[] pcmByteData = null;
                if (channels == 2)
                {
                    pcmByteData = XboxAdpcm.Decode(adpcmData, channels);
                }
                else
                {
                    pcmByteData = XboxAdpcm.Decode(adpcmData, channels);
                }

                //Save file
                IWaveProvider provider = new RawSourceWaveStream(new MemoryStream(pcmByteData), new WaveFormat(frequency, 16, channels));
                try
                {
                    WaveFileWriter.CreateWaveFile(outputFile, provider);
                    Console.WriteLine("Decoding complete: " + outputFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------
}
