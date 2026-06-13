using NAudio.Wave;
using System;
using System.IO;
using System.Text;

namespace XboxAdpcmTool
{
    internal static class XboxAdpcmWaveConverter
    {
        internal static void EncodePcmWave(string inputFile, string outputFile)
        {
            if (!HasWaveExtension(inputFile))
            {
                Console.WriteLine("Error: Only WAV files are supported.");
                return;
            }

            using (WaveFileReader reader = new WaveFileReader(inputFile))
            {
                WaveFormat format = reader.WaveFormat;
                if (!ValidatePcmInput(format))
                    return;

                byte[] pcmBytes = ReadWaveData(reader);
                if (pcmBytes.Length == 0)
                {
                    Console.WriteLine("Error: No audio data found.");
                    return;
                }

                short[] pcmData = AudioBufferUtils.BytesToPcm16Samples(pcmBytes);
                byte[] adpcmData = XboxAdpcmCodec.Encode(pcmData, format.Channels);
                WriteAdpcmWave(adpcmData, outputFile, format);

                Console.WriteLine("Encoding complete: " + outputFile);
            }
        }

        internal static void DecodeAdpcmWave(string inputFile, string outputFile)
        {
            if (!HasWaveExtension(inputFile))
            {
                Console.WriteLine("Error: Only WAV files are supported.");
                return;
            }

            byte[] adpcmData;
            int sampleRate;
            int channels;

            using (WaveFileReader reader = new WaveFileReader(inputFile))
            {
                WaveFormat format = reader.WaveFormat;
                if (!ValidateAdpcmInput(format))
                    return;

                sampleRate = format.SampleRate;
                channels = format.Channels;
                adpcmData = ReadWaveData(reader);
            }

            try
            {
                byte[] pcmByteData = XboxAdpcmCodec.Decode(adpcmData, channels);
                IWaveProvider provider = new RawSourceWaveStream(new MemoryStream(pcmByteData), new WaveFormat(sampleRate, 16, channels));
                WaveFileWriter.CreateWaveFile(outputFile, provider);
                Console.WriteLine("Decoding complete: " + outputFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private static bool ValidatePcmInput(WaveFormat format)
        {
            if (format.Encoding != WaveFormatEncoding.Pcm)
            {
                Console.WriteLine("Error: WAV must be PCM.");
                return false;
            }

            if (format.BitsPerSample != 16)
            {
                Console.WriteLine("Error: WAV must be 16-bit.");
                return false;
            }

            if (!IsSupportedChannelCount(format.Channels))
            {
                Console.WriteLine("Error: WAV must be mono or stereo.");
                return false;
            }

            return true;
        }

        private static bool ValidateAdpcmInput(WaveFormat format)
        {
            if ((int)format.Encoding != XboxAdpcmCodec.FormatTag)
            {
                Console.WriteLine("Error: WAV must use Xbox ADPCM format 0x0069.");
                return false;
            }

            if (!IsSupportedChannelCount(format.Channels))
            {
                Console.WriteLine("Error: WAV must be mono or stereo.");
                return false;
            }

            if (format.BitsPerSample != XboxAdpcmCodec.BitsPerSample)
            {
                Console.WriteLine("Error: Xbox ADPCM WAV must be 4-bit.");
                return false;
            }

            return true;
        }

        private static void WriteAdpcmWave(byte[] adpcmData, string outputFilePath, WaveFormat sourceFormat)
        {
            int blockAlign = XboxAdpcmCodec.BlockBytes * sourceFormat.Channels;
            int averageBytesPerSecond = (sourceFormat.SampleRate * blockAlign) / XboxAdpcmCodec.BlockSamples;
            const int fmtChunkSize = 20;
            uint riffChunkSize = (uint)(4 + (8 + fmtChunkSize) + (8 + adpcmData.Length));

            using (BinaryWriter writer = new BinaryWriter(File.Open(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.ASCII))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(riffChunkSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));

                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write((uint)fmtChunkSize);
                writer.Write((short)XboxAdpcmCodec.FormatTag);
                writer.Write((short)sourceFormat.Channels);
                writer.Write((uint)sourceFormat.SampleRate);
                writer.Write((uint)averageBytesPerSecond);
                writer.Write((short)blockAlign);
                writer.Write((short)XboxAdpcmCodec.BitsPerSample);
                writer.Write((short)XboxAdpcmCodec.FormatExtraSize);
                writer.Write((short)XboxAdpcmCodec.BlockSamples);

                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write((uint)adpcmData.Length);
                writer.Write(adpcmData);
            }
        }

        private static byte[] ReadWaveData(WaveFileReader reader)
        {
            byte[] data = new byte[reader.Length];
            int offset = 0;

            while (offset < data.Length)
            {
                int bytesRead = reader.Read(data, offset, data.Length - offset);
                if (bytesRead == 0)
                    break;

                offset += bytesRead;
            }

            if (offset == data.Length)
                return data;

            byte[] trimmed = new byte[offset];
            Buffer.BlockCopy(data, 0, trimmed, 0, offset);
            return trimmed;
        }

        private static bool HasWaveExtension(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".wav", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSupportedChannelCount(int channels)
        {
            return channels == 1 || channels == 2;
        }
    }
}
