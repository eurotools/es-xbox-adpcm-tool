using System;
using System.IO;

namespace XboxAdpcmTool
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    public static class XboxAdpcm
    {
        //-------------------------------------------------------------------------------------------------------------------------------
        private const int XBOX_ADPCM_BLOCKSIZE = 36;

        //-------------------------------------------------------------------------------------------------------------------------------
        private struct AdpcmState
        {
            public sbyte Index;       /* Current step index                */
            public short StepSize;    /* Current stepsize                  */
            public short Predictor;   /* Last predicted output value       */
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        /* Intel ADPCM step variation table */
        private static readonly int[] indexTable = {
            -1, -1, -1, -1, 2, 4, 6, 8,
            -1, -1, -1, -1, 2, 4, 6, 8,
        };

        //-------------------------------------------------------------------------------------------------------------------------------
        /* Intel ADPCM step size table (shared with Xbox ADPCM) */
        private static readonly int[] stepsizeTable = {
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

        //-------------------------------------------------------------------------------------------------------------------------------
        public static byte[] Encode(short[] input, int channels)
        {
            int totalSamples = input.Length / channels;

            /* Split interleaved PCM into separate channel buffers */
            short[][] pcmCh = ToolUtils.SplitChannels(input, channels);

            int blocks = (totalSamples + 63) / 64;

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                for (int blk = 0; blk < blocks; blk++)
                {
                    /* ----------------------------------------------------------------------
                    ** Step 0 - Write block header per channel (predictor + index)
                    ** Xbox ADPCM stores a 4-byte header per channel at the start of each block.
                    ** ---------------------------------------------------------------------- */
                    AdpcmState[] state = new AdpcmState[channels];

                    for (int ch = 0; ch < channels; ch++)
                    {
                        int start = blk * 64;
                        short predictor = pcmCh[ch][start];

                        /* ------------------------------------------------------------------
                        ** Step 0b - Choose initial step index using IMA-style slope analysis
                        ** Instead of only diff(0→1), we compute the average slope across
                        ** the first samples of the block (similar to how IMA naturally adapts).
                        ** ------------------------------------------------------------------ */
                        int avgDiff = 0;
                        int count = 0;

                        for (int k = 1; k < 8; k++)
                        {
                            int si = start + k;
                            if (si >= pcmCh[ch].Length)
                                break;

                            avgDiff += Math.Abs(pcmCh[ch][si] - pcmCh[ch][si - 1]);
                            count++;
                        }

                        if (count > 0)
                            avgDiff /= count;

                        /* Choose the closest stepsizeTable entry */
                        int bestIndex = 0;
                        int bestDelta = Math.Abs(stepsizeTable[0] - avgDiff);

                        for (int i = 1; i < 89; i++)
                        {
                            int d = Math.Abs(stepsizeTable[i] - avgDiff);
                            if (d < bestDelta)
                            {
                                bestDelta = d;
                                bestIndex = i;
                            }
                        }

                        /* Initialize ADPCM state */
                        state[ch].Predictor = predictor;
                        state[ch].Index = (sbyte)bestIndex;
                        state[ch].StepSize = (short)stepsizeTable[bestIndex];

                        /* Write block header */
                        bw.Write(predictor);
                        bw.Write((short)bestIndex);
                    }

                    /* ----------------------------------------------------------------------
                    ** Step 1 - Encode 64 samples per block in 8 groups of 8 samples
                    ** Xbox ADPCM stores:
                    **   - For each group:
                    **         For each channel:
                    **             4 bytes (8 nibbles) = 8 encoded samples
                    ** ---------------------------------------------------------------------- */
                    for (int group = 0; group < 8; group++)
                    {
                        for (int c = 0; c < channels; c++)
                        {
                            uint pack = 0;
                            int shift = 0;

                            for (int i = 0; i < 8; i++)
                            {
                                int sampleIndex = blk * 64 + group * 8 + i;

                                short sample;
                                if (sampleIndex < pcmCh[c].Length)
                                {
                                    sample = pcmCh[c][sampleIndex];
                                }
                                else
                                {
                                    sample = state[c].Predictor; /* Padding if out of range */
                                }

                                /* Encode using Xbox-IMA nibble encoder */
                                int code = EncodeNibble(sample, ref state[c]);

                                /* Pack 4-bit code into 32-bit output buffer */
                                pack |= (uint)(code & 0x0F) << shift;
                                shift += 4;
                            }

                            /* Write packed ADPCM codes (4 bytes) */
                            bw.Write(pack);
                        }
                    }
                }

                return ms.ToArray();
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private static int EncodeNibble(short val, ref AdpcmState st)
        {
            /* Step 1 - Get the predicted value and current step size */
            int valpred = st.Predictor;
            int step = st.StepSize;

            /* Step 2 - Compute the difference and sign bit */
            int diff = val - valpred;
            int sign = (diff < 0) ? 8 : 0;
            if (sign != 0) diff = (-diff);

            /* Step 3 - Quantize the difference into the 4-bit ADPCM code
            ** This code *approximately* computes:
            **    delta = diff * 4 / step;
            **    vpdiff = (delta + 0.5) * step / 4;
            ** but uses shifting for speed, dropping some bits.
            */
            int delta = 0;
            int vpdiff = (step >> 3);

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

            /* Step 4 - Update the predicted value */
            if (sign != 0)
                valpred -= vpdiff;
            else
                valpred += vpdiff;

            /* Step 5 - Clamp the predicted value to 16 bits */
            if (valpred > short.MaxValue)
                valpred = short.MaxValue;
            else if (valpred < short.MinValue)
                valpred = short.MinValue;

            /* Add the sign bit */
            delta |= sign;

            /* Step 6 - Update index and step size */
            st.Index += (sbyte)indexTable[delta];
            if (st.Index < 0) st.Index = 0;
            if (st.Index > 88) st.Index = 88;
            st.StepSize = (short)stepsizeTable[st.Index];

            /* Store the updated predictor and return */
            st.Predictor = (short)valpred;
            return delta;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        public static byte[] Decode(byte[] input, int channels)
        {
            using (MemoryStream pcmStream = new MemoryStream())
            using (BinaryWriter pcmOut = new BinaryWriter(pcmStream))
            {
                int inPos = 0;
                int totalBlocks = (input.Length / XBOX_ADPCM_BLOCKSIZE) / channels;

                for (int block = 0; block < totalBlocks; block++)
                {
                    /* Step 0 - Load header state for each channel */
                    AdpcmState[] state = new AdpcmState[channels];

                    for (int c = 0; c < channels; c++)
                    {
                        /* Predictor (2 bytes LE) */
                        short predictor = (short)(input[inPos++] | (input[inPos++] << 8));

                        /* Index (2 bytes LE) */
                        int index = input[inPos++] | (input[inPos++] << 8);
                        if (index < 0) index = 0;
                        if (index > 88) index = 88;

                        state[c].Predictor = predictor;
                        state[c].Index = (sbyte)index;
                        state[c].StepSize = (short)stepsizeTable[index];
                    }

                    /* Decode 8 groups of 8 samples per channel */
                    for (int group = 0; group < 8; group++)
                    {
                        short[,] buffer = new short[channels, 8];

                        for (int ch = 0; ch < channels; ch++)
                        {
                            /* Load packed 8 nibbles */
                            uint nibblePack =
                                (uint)(input[inPos] |
                                (input[inPos + 1] << 8) |
                                (input[inPos + 2] << 16) |
                                (input[inPos + 3] << 24));

                            inPos += 4;

                            /* Step 1 - Extract and decode each nibble */
                            for (int j = 0; j < 8; j++)
                            {
                                int code = (int)(nibblePack & 0x0F);
                                buffer[ch, j] = DecodeSample(code, ref state[ch]);
                                nibblePack >>= 4;
                            }
                        }

                        /* Step 2 - Interleave decoded samples */
                        for (int j = 0; j < 8; j++)
                        {
                            for (int c = 0; c < channels; c++)
                            {
                                short sample = buffer[c, j];
                                pcmOut.Write(sample);
                            }
                        }
                    }
                }

                return pcmStream.ToArray();
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        private static short DecodeSample(int code, ref AdpcmState st)
        {
            int delta = st.StepSize >> 3; /* Base diff = step/8 */

            /* Step 1 - Compute difference */
            if ((code & 4) != 0) delta += st.StepSize;
            if ((code & 2) != 0) delta += (st.StepSize >> 1);
            if ((code & 1) != 0) delta += (st.StepSize >> 2);
            if ((code & 8) != 0) delta = -delta;

            /* Step 2 - Update predicted output value */
            int valpred = st.Predictor + delta;

            /* Step 3 - Clamp output */
            if (valpred > short.MaxValue)
                valpred = short.MaxValue;
            else if (valpred < short.MinValue)
                valpred = short.MinValue;

            /* Step 4 - Update index */
            st.Index += (sbyte)indexTable[code];
            if (st.Index < 0) st.Index = 0;
            if (st.Index > 88) st.Index = 88;

            /* Update step */
            st.StepSize = (short)stepsizeTable[st.Index];

            /* Store predictor */
            st.Predictor = (short)valpred;
            return (short)valpred;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------
}
