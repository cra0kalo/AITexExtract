using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITexExtract
{
    public static class Cra0Utilz
    {
        static public void CreatePath(string pathName)
        {
            if (!(Directory.Exists(pathName)))
            {
                Directory.CreateDirectory(pathName);
            }
        }

        public static long PaddingAlign(long num, int alignTo)
        {
            if (num % alignTo == 0)
            {
                return 0;
            }
            else
            {
                return alignTo - (num % alignTo);
            }
        }

        public static byte[] ConvertEndian(byte[] InputByte)
        {

            byte[] L_InputByte = InputByte;
            Array.Reverse(L_InputByte); ////convert to Big-endian

            return L_InputByte;
        }

        /// <summary>
        /// Converts an Integer (4byte) to Byte array
        /// </summary>
        public static void IntegerToByte(int ValueInteger, ref byte[] Value4Byte, int Mode = 0)
        {
            Value4Byte = BitConverter.GetBytes(ValueInteger);
            if (Mode != 0)
            {
                ReverseBytes(ref Value4Byte, 0, 4);
            }
        }


        /// <summary>
        /// New method of reversing bytes
        /// </summary>
        public static void ReverseBytes(ref byte[] ValueByte, int StartIndex, int Length)
        {
            byte[] buffer = new byte[(Length - 1) + 1];
            int num3 = ((StartIndex + Length) - 1);
            int i = StartIndex;
            while (i <= num3)
            {
                buffer[(i - StartIndex)] = ValueByte[(((StartIndex + Length) - 1) - (i - StartIndex))];
                i += 1;
            }
            int num4 = ((StartIndex + Length) - 1);
            int j = StartIndex;
            while (j <= num4)
            {
                ValueByte[j] = buffer[(j - StartIndex)];
                j += 1;
            }
        }

        static public string GetString_ASCII(byte[] byteArray)
        {
            return System.Text.Encoding.ASCII.GetString(byteArray);
        }


    }





}
