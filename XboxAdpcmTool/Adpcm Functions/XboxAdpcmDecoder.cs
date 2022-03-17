using System.IO;

namespace XboxAdpcmTool
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    public static partial class XboxAdpcm
    {
        //-------------------------------------------------------------------------------------------------------------------------------
        public static byte[] Decode(byte[] ImaFileData)
        {
            byte[] outBuff;
            int sign;               /* Current adpcm sign bit */
            int delta;              /* Current adpcm output value */
            int step;               /* Stepsize */
            int valpred;            /* Predicted value */
            int vpdiff;             /* Current change to valpred */
            int index;              /* Current step change index */
            int inputbuffer;        /* place to keep next 4-bit value */

            using (BinaryReader BReader = new BinaryReader(new MemoryStream(ImaFileData)))
            using (MemoryStream pcmStream = new MemoryStream())
            using (BinaryWriter pcmWriter = new BinaryWriter(pcmStream))
            {
                ImaAdpcmState state = new ImaAdpcmState();

                while (BReader.BaseStream.Position < BReader.BaseStream.Length)
                {
                    valpred = BReader.ReadInt16();
                    index = BReader.ReadInt16();
                    step = stepsizeTable[index];

                    for (int j = 0; j < 8; j++)
                    {
                        bool bufferstep = false;
                        for (int k = 0; k < 8; k++)
                        {
                            /* Step 1 - get the delta value */
                            inputbuffer = BReader.ReadByte();
                            BReader.BaseStream.Position -= 1;

                            if (bufferstep)
                            {
                                delta = (inputbuffer >> 4) & 0xf;
                                BReader.BaseStream.Position++;
                            }
                            else
                            {
                                delta = inputbuffer & 0xf;
                            }
                            bufferstep = !bufferstep;

                            /* Step 2 - Find new index value (for later) */
                            index += indexTable[delta & 7];
                            if (index < 0) index = 0;
                            if (index > 88) index = 88;

                            /* Step 3 - Separate sign and magnitude */
                            sign = delta & 8;
                            delta = delta & 7;

                            /* Step 4 - Compute difference and new predicted value */
                            /*
                            ** Computes 'vpdiff = (delta+0.5)*step/4', but see comment
                            ** in adpcm_coder.
                            */
                            vpdiff = step >> 3;
                            if ((delta & 4) != 0) vpdiff += step;
                            if ((delta & 2) != 0) vpdiff += step >> 1;
                            if ((delta & 1) != 0) vpdiff += step >> 2;

                            if (sign != 0)
                                valpred -= vpdiff;
                            else
                                valpred += vpdiff;

                            /* Step 5 - clamp output value */
                            if (valpred > short.MaxValue)
                                valpred = short.MaxValue;
                            else if (valpred < short.MinValue)
                                valpred = short.MinValue;

                            /* Step 6 - Update step value */
                            step = stepsizeTable[index];

                            /* Step 7 - Output value */
                            pcmWriter.Write((short)valpred);
                        }
                        state.valprev = valpred;
                        state.index = index;
                    }
                }
                outBuff = pcmStream.ToArray();

                pcmWriter.Close();
                pcmStream.Close();
                BReader.Close();
            }
            return outBuff;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------
}
