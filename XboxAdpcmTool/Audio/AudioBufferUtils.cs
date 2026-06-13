using System;

namespace XboxAdpcmTool
{
    internal static class AudioBufferUtils
    {
        internal static short[][] SplitChannels(short[] input, int channels)
        {
            int total = input.Length / channels;
            short[][] result = new short[channels][];

            for (int c = 0; c < channels; c++)
                result[c] = new short[total];

            for (int i = 0; i < total; i++)
            {
                for (int c = 0; c < channels; c++)
                    result[c][i] = input[i * channels + c];
            }

            return result;
        }

        internal static short[] BytesToPcm16Samples(byte[] pcmData)
        {
            short[] samples = new short[pcmData.Length / 2];
            Buffer.BlockCopy(pcmData, 0, samples, 0, pcmData.Length);
            return samples;
        }
    }
}
