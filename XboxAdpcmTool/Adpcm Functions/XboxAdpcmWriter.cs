using NAudio.Wave;
using System.IO;
using System.Text;

namespace XboxAdpcmTool
{
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------------------------------------------------
    public static partial class XboxAdpcm
    {
        //-------------------------------------------------------------------------------------------------------------------------------
        internal static void WriteAdpcmFile(byte[] adpcmData, string outputFilePath, int NumberOfChannels, int Frequency, int BitsPerChannel)
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
                BWritter.Write((short)NumberOfChannels); //Num Channels
                BWritter.Write((uint)(Frequency)); //Sample Rate
                BWritter.Write((uint)decimal.Divide(Frequency, (decimal)1.7777956)); //Byte Rate
                BWritter.Write((short)36); //Block Align
                BWritter.Write((short)BitsPerChannel); //Bits Per Sample
                BWritter.Write(4194306);
                BWritter.Write(Encoding.UTF8.GetBytes("data")); //Subchunk2 ID
                BWritter.Write((uint)adpcmData.Length); //Subchunk2 Size

                //Write Xbox Adpcm Data
                BWritter.Write(adpcmData);

                //Close Writter
                BWritter.Close();
            }
        }
    }

    //-------------------------------------------------------------------------------------------------------------------------------
}
