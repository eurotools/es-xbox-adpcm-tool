using System;
using System.IO;

namespace XboxAdpcmTool
{
    public static class XboxAdpcmCodec
    {
        internal const int BlockBytes = 36;
        internal const int BlockSamples = 64;
        internal const int BitsPerSample = 4;
        internal const int FormatExtraSize = 2;
        internal const int FormatTag = 0x0069;

        private struct AdpcmState
        {
            public sbyte Index;
            public short StepSize;
            public short Predictor;
        }

        private static readonly int[] IndexTable = {
            -1, -1, -1, -1, 2, 4, 6, 8,
            -1, -1, -1, -1, 2, 4, 6, 8,
        };

        private static readonly int[] StepSizeTable = {
            7, 8, 9, 10, 11, 12, 13, 14, 16, 17,
            19, 21, 23, 25, 28, 31, 34, 37, 41, 45,
            50, 55, 60, 66, 73, 80, 88, 97, 107, 118,
            130, 143, 157, 173, 190, 209, 230, 253, 279, 307,
            337, 371, 408, 449, 494, 544, 598, 658, 724, 796,
            876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066,
            2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358,
            5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
            15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
        };

        public static byte[] Encode(short[] input, int channels)
        {
            ValidateChannels(channels);
            if (input == null)
                throw new ArgumentNullException("input");
            if (input.Length % channels != 0)
                throw new ArgumentException("Input sample count must be divisible by channel count.", "input");

            int totalSamples = input.Length / channels;
            if (totalSamples == 0)
                return new byte[0];

            short[][] pcmCh = AudioBufferUtils.SplitChannels(input, channels);
            int blocks = (totalSamples + BlockSamples - 1) / BlockSamples;

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                for (int blk = 0; blk < blocks; blk++)
                {
                    AdpcmState[] state = new AdpcmState[channels];

                    for (int ch = 0; ch < channels; ch++)
                    {
                        int start = blk * BlockSamples;
                        short predictor = pcmCh[ch][start];
                        int bestIndex = FindInitialStepIndex(pcmCh[ch], start);

                        state[ch].Predictor = predictor;
                        state[ch].Index = (sbyte)bestIndex;
                        state[ch].StepSize = (short)StepSizeTable[bestIndex];

                        bw.Write(predictor);
                        bw.Write((short)bestIndex);
                    }

                    for (int group = 0; group < 8; group++)
                    {
                        for (int c = 0; c < channels; c++)
                        {
                            uint pack = 0;
                            int shift = 0;

                            for (int i = 0; i < 8; i++)
                            {
                                int sampleIndex = blk * BlockSamples + group * 8 + i;
                                short sample = sampleIndex < pcmCh[c].Length
                                    ? pcmCh[c][sampleIndex]
                                    : state[c].Predictor;

                                int code = EncodeNibble(sample, ref state[c]);
                                pack |= (uint)(code & 0x0F) << shift;
                                shift += 4;
                            }

                            bw.Write(pack);
                        }
                    }
                }

                return ms.ToArray();
            }
        }

        public static byte[] Decode(byte[] input, int channels)
        {
            ValidateChannels(channels);
            if (input == null)
                throw new ArgumentNullException("input");
            if (input.Length == 0)
                return new byte[0];
            if (input.Length % (BlockBytes * channels) != 0)
                throw new ArgumentException("Input length must be aligned to the Xbox ADPCM block size.", "input");

            using (MemoryStream pcmStream = new MemoryStream())
            using (BinaryWriter pcmOut = new BinaryWriter(pcmStream))
            {
                int inPos = 0;
                int totalBlocks = input.Length / (BlockBytes * channels);

                for (int block = 0; block < totalBlocks; block++)
                {
                    AdpcmState[] state = new AdpcmState[channels];

                    for (int c = 0; c < channels; c++)
                    {
                        short predictor = (short)(input[inPos++] | (input[inPos++] << 8));
                        int index = input[inPos++] | (input[inPos++] << 8);
                        if (index < 0) index = 0;
                        if (index > StepSizeTable.Length - 1) index = StepSizeTable.Length - 1;

                        state[c].Predictor = predictor;
                        state[c].Index = (sbyte)index;
                        state[c].StepSize = (short)StepSizeTable[index];
                    }

                    for (int group = 0; group < 8; group++)
                    {
                        short[,] buffer = new short[channels, 8];

                        for (int ch = 0; ch < channels; ch++)
                        {
                            uint nibblePack =
                                (uint)(input[inPos] |
                                (input[inPos + 1] << 8) |
                                (input[inPos + 2] << 16) |
                                (input[inPos + 3] << 24));

                            inPos += 4;

                            for (int j = 0; j < 8; j++)
                            {
                                int code = (int)(nibblePack & 0x0F);
                                buffer[ch, j] = DecodeSample(code, ref state[ch]);
                                nibblePack >>= 4;
                            }
                        }

                        for (int j = 0; j < 8; j++)
                        {
                            for (int c = 0; c < channels; c++)
                                pcmOut.Write(buffer[c, j]);
                        }
                    }
                }

                return pcmStream.ToArray();
            }
        }

        private static int FindInitialStepIndex(short[] channelSamples, int start)
        {
            int avgDiff = 0;
            int count = 0;

            for (int k = 1; k < 8; k++)
            {
                int si = start + k;
                if (si >= channelSamples.Length)
                    break;

                avgDiff += Math.Abs(channelSamples[si] - channelSamples[si - 1]);
                count++;
            }

            if (count > 0)
                avgDiff /= count;

            int bestIndex = 0;
            int bestDelta = Math.Abs(StepSizeTable[0] - avgDiff);

            for (int i = 1; i < StepSizeTable.Length; i++)
            {
                int d = Math.Abs(StepSizeTable[i] - avgDiff);
                if (d < bestDelta)
                {
                    bestDelta = d;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static int EncodeNibble(short val, ref AdpcmState st)
        {
            int valpred = st.Predictor;
            int step = st.StepSize;
            int diff = val - valpred;
            int sign = diff < 0 ? 8 : 0;
            if (sign != 0) diff = -diff;

            int delta = 0;
            int vpdiff = step >> 3;

            if (diff >= step)
            {
                delta = 4;
                diff -= step;
                vpdiff += step;
            }

            step >>= 1;
            if (diff >= step)
            {
                delta |= 2;
                diff -= step;
                vpdiff += step;
            }

            step >>= 1;
            if (diff >= step)
            {
                delta |= 1;
                vpdiff += step;
            }

            valpred = sign != 0 ? valpred - vpdiff : valpred + vpdiff;
            valpred = ClampToPcm16(valpred);

            delta |= sign;
            st.Index += (sbyte)IndexTable[delta];
            if (st.Index < 0) st.Index = 0;
            if (st.Index > 88) st.Index = 88;

            st.StepSize = (short)StepSizeTable[st.Index];
            st.Predictor = (short)valpred;
            return delta;
        }

        private static short DecodeSample(int code, ref AdpcmState st)
        {
            int delta = st.StepSize >> 3;

            if ((code & 4) != 0) delta += st.StepSize;
            if ((code & 2) != 0) delta += st.StepSize >> 1;
            if ((code & 1) != 0) delta += st.StepSize >> 2;
            if ((code & 8) != 0) delta = -delta;

            int valpred = ClampToPcm16(st.Predictor + delta);

            st.Index += (sbyte)IndexTable[code];
            if (st.Index < 0) st.Index = 0;
            if (st.Index > 88) st.Index = 88;

            st.StepSize = (short)StepSizeTable[st.Index];
            st.Predictor = (short)valpred;
            return (short)valpred;
        }

        private static int ClampToPcm16(int value)
        {
            if (value > short.MaxValue)
                return short.MaxValue;
            if (value < short.MinValue)
                return short.MinValue;

            return value;
        }

        private static void ValidateChannels(int channels)
        {
            if (channels != 1 && channels != 2)
                throw new ArgumentOutOfRangeException("channels", "Xbox ADPCM supports mono and stereo audio.");
        }
    }
}
