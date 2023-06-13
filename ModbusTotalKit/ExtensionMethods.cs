using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusTotalKit
{
    public static class ExtensionMethods
    {
        public static bool GetBit(this byte b, int bitNumber)
        {
            byte[] bytes = new byte[1] { b };
            BitArray bits = new BitArray(bytes);

            return bits[bitNumber];
        }

        public static byte[] SwapBytes(this byte[] byteArray, int i, int j)
        {
            byte temp = byteArray[i];
            byteArray[i] = byteArray[j];
            byteArray[j] = temp;

            return byteArray;
        }

        public static byte[] SwapBytes(this byte[] byteArray)
        {
            if (byteArray.Length % 2 != 0)
                return byteArray;

            for (int i = 0; i < byteArray.Length - 1; i = i + 2)
            {
                byte temp = byteArray[i];
                byteArray[i] = byteArray[i + 1];
                byteArray[i + 1] = temp;
            }

            return byteArray;
        }

        public static byte ToByte(this bool[] input)
        {
            byte ret = 0;
            for (int i = 0; i < input.Length; i += 8)
            {
                int value = 0;
                for (int j = 0; j < 8; j++)
                {
                    if (input[i + j])
                    {
                        value += 1 << (7 - j);
                    }
                }
                ret = (byte)value;
            }
            return ret;
        }

        public static bool[] Reverce(this bool[] boolArray)
        {
            bool[] result = new bool[boolArray.Length];

            for (int i = 0; i < boolArray.Length; i++)
                result[boolArray.Length - 1 - i] = boolArray[i];

            return result;
        }

        public static string ToHexString(this byte[] byteArray)
        {
            return BitConverter.ToString(byteArray).Replace("-", " ");
        }
    }
}
