using System;
using System.IO;

namespace XboxAdpcmTool
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    public static partial class XboxAdpcm
    {
        //-------------------------------------------------------------------------------------------------------------------------------
        public static byte[] Encode(short[] input, int samplesToEncode)
        {
            byte[] outBuff;
            int inp;			    /* Input buffer pointer */
            int val;                /* Current input sample value */
            int sign;               /* Current adpcm sign bit */
            int delta;              /* Current adpcm output value */
            int diff;               /* Difference between val and valprev */
            int step;               /* Stepsize */
            int valpred;            /* Predicted output value */
            int vpdiff;             /* Current change to valpred */
            int index;              /* Current step change index */
            int outputbuffer;       /* place to keep previous 4-bit value */
            bool bufferstep;        /* toggle between outputbuffer/output */

            ImaAdpcmState state = new ImaAdpcmState();
            outputbuffer = 0;
            inp = 0;
            valpred = state.valprev;
            index = state.index;
            step = stepsizeTable[index];

            //Ensure that we have chunks of 64 bytes
            int newLength = (samplesToEncode + (64 - 1)) & ~(64 - 1);
            short[] inputBuffer = new short[newLength];
            Buffer.BlockCopy(input, 0, inputBuffer, 0, samplesToEncode * sizeof(short));

            //Start magic
            using (MemoryStream adpcmStream = new MemoryStream())
            {
                using (BinaryWriter adpcmWriter = new BinaryWriter(adpcmStream))
                {
                    for (int i = 0; i < inputBuffer.Length; i += 64)
                    {
                        adpcmWriter.Write((short)state.valprev);
                        adpcmWriter.Write((short)state.index);

                        // 4 bytes per channel
                        for (int j = 0; j < 8; j++)
                        {
                            bufferstep = true;

                            for (int k = 0; k < 8; k++)
                            {
                                val = inputBuffer[inp++];

                                /* Step 1 - compute difference with previous value */
                                diff = val - valpred;
                                sign = (diff < 0) ? 8 : 0;
                                if (sign != 0) diff = (-diff);

                                /* Step 2 - Divide and clamp */
                                /* Note:
                                ** This code *approximately* computes:
                                **    delta = diff*4/step;
                                **    vpdiff = (delta+0.5)*step/4;
                                ** but in shift step bits are dropped. The net result of this is
                                ** that even if you have fast mul/div hardware you cannot put it to
                                ** good use since the fixup would be too expensive.
                                */
                                delta = 0;
                                vpdiff = (step >> 3);

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

                                /* Step 3 - Update previous value */
                                if (sign != 0)
                                    valpred -= vpdiff;
                                else
                                    valpred += vpdiff;

                                /* Step 4 - Clamp previous value to 16 bits */
                                if (valpred > short.MaxValue)
                                    valpred = short.MaxValue;
                                else if (valpred < short.MinValue)
                                    valpred = short.MinValue;

                                /* Step 5 - Assemble value, update index and step values */
                                delta |= sign;

                                index += indexTable[delta];
                                if (index < 0) index = 0;
                                if (index > 88) index = 88;
                                step = stepsizeTable[index];

                                /* Step 6 - Output value */
                                if (bufferstep)
                                {
                                    outputbuffer = delta;
                                }
                                else
                                {
                                    adpcmWriter.Write((byte)((outputbuffer & 0x0f) | delta << 4));
                                }
                                bufferstep = !bufferstep;
                            }

                            /* Output last step, if needed */
                            if (!bufferstep)
                            {
                                adpcmWriter.Write((byte)outputbuffer);
                            }

                            state.valprev = valpred;
                            state.index = index;
                        }
                    }
                }
                outBuff = adpcmStream.ToArray();
            }
            return outBuff;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------
}
