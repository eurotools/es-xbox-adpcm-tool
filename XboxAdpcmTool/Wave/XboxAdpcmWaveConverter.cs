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
                throw new InvalidDataException("Only WAV files are supported.");

            using (WaveFileReader reader = new WaveFileReader(inputFile))
            {
                WaveFormat format = reader.WaveFormat;
                ValidatePcmInput(format);

                byte[] pcmBytes = ReadWaveData(reader);
                if (pcmBytes.Length == 0)
                {
                    throw new InvalidDataException("No audio data found.");
                }

                int pcmFrameSize = format.Channels * sizeof(short);
                if (pcmBytes.Length % pcmFrameSize != 0)
                {
                    throw new InvalidDataException("WAV contains an incomplete PCM sample frame.");
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
                throw new InvalidDataException("Only WAV files are supported.");

            byte[] adpcmData;
            int sampleRate;
            int channels;

            using (WaveFileReader reader = new WaveFileReader(inputFile))
            {
                WaveFormat format = reader.WaveFormat;
                ValidateAdpcmInput(format);

                sampleRate = format.SampleRate;
                channels = format.Channels;
                adpcmData = ReadWaveData(reader);
            }

            byte[] pcmByteData = XboxAdpcmCodec.Decode(adpcmData, channels);
            using (MemoryStream pcmStream = new MemoryStream(pcmByteData))
            using (RawSourceWaveStream provider = new RawSourceWaveStream(pcmStream, new WaveFormat(sampleRate, 16, channels)))
            {
                WaveFileWriter.CreateWaveFile(outputFile, provider);
            }
            Console.WriteLine("Decoding complete: " + outputFile);
        }

        private static void ValidatePcmInput(WaveFormat format)
        {
            if (format.Encoding != WaveFormatEncoding.Pcm)
                throw new InvalidDataException("WAV must be PCM.");

            if (format.BitsPerSample != 16)
                throw new InvalidDataException("WAV must be 16-bit.");

            if (!IsSupportedChannelCount(format.Channels))
                throw new InvalidDataException("WAV must be mono or stereo.");

            if (format.SampleRate <= 0 || format.SampleRate > 384000)
                throw new InvalidDataException("WAV has an invalid sample rate.");
        }

        private static void ValidateAdpcmInput(WaveFormat format)
        {
            if ((int)format.Encoding != XboxAdpcmCodec.FormatTag)
                throw new InvalidDataException("WAV must use Xbox ADPCM format 0x0069.");

            if (!IsSupportedChannelCount(format.Channels))
                throw new InvalidDataException("WAV must be mono or stereo.");

            if (format.BitsPerSample != XboxAdpcmCodec.BitsPerSample)
                throw new InvalidDataException("Xbox ADPCM WAV must be 4-bit.");

            if (format.BlockAlign != XboxAdpcmCodec.BlockBytes * format.Channels)
                throw new InvalidDataException("Xbox ADPCM WAV has an invalid block alignment.");

            if (format.SampleRate <= 0 || format.SampleRate > 384000)
                throw new InvalidDataException("WAV has an invalid sample rate.");
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
            if (reader.Length > int.MaxValue)
            {
                throw new InvalidDataException("WAV data is too large to process in memory.");
            }

            byte[] data = new byte[(int)reader.Length];
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
