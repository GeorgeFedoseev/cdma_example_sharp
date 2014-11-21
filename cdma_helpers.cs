using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;



static class cdma_helpers {

    public const bool DEBUG = false;
    public const long bufferMilliseconds = 1000;

    public static byte[] GetByteArrayFromIntArray(int[] intArray)
    {
        byte[] data = new byte[intArray.Length * 4];
        for (int i = 0; i < intArray.Length; i++)
            Array.Copy(BitConverter.GetBytes(intArray[i]), 0, data, i * 4, 4);
        return data;
    }


    public static int[] GetIntArrayFromByteArray(byte[] byteArray)
    {
        int[] intArray = new int[byteArray.Length / 4];
        for (int i = 0; i < byteArray.Length; i += 4)
            intArray[i / 4] = BitConverter.ToInt32(byteArray, i);
        return intArray;
    }

    public static byte[] ConvertToByteArray(string str, Encoding encoding)
    {
        return encoding.GetBytes(str);
    }

    public static byte[] BitArrayToByteArray(int[] binary)
    {
        BitArray bits = new BitArray(binary.Select(x => x == 1 ? true : false).ToArray());
        int numBytes = bits.Count / 8;
        if (bits.Count % 8 != 0) numBytes++;

        byte[] bytes = new byte[numBytes];
        int byteIndex = 0, bitIndex = 0;

        for (int i = 0; i < bits.Count; i++)
        {
            if (bits[i])
                bytes[byteIndex] |= (byte)(1 << (7 - bitIndex));

            bitIndex++;
            if (bitIndex == 8)
            {
                bitIndex = 0;
                byteIndex++;
            }
        }

        return bytes;
    }
   

    public static string ConvertToString(int[] binary, Encoding encoding)
    {
        byte[] bytes = BitArrayToByteArray(binary);
        return encoding.GetString(bytes);
    }

    public static int[] ToBinary(Byte[] data)
    {
        if (data.Length > 0)
        {
            string bits = string.Join("", data.Select(byt => Convert.ToString(byt, 2).PadLeft(8, '0')));
            List<int> bitsList = new List<int>();
            foreach (char c in bits)
            {
                bitsList.Add(c == '1' ? 1 : 0);
            }

            return bitsList.ToArray();
        }
        else
        {
            return new int[0];
        }
    }

    public static bool zeroArray(int[] array) {
        foreach (int v in array) {
            if (v != 0) return false;
        }

        return true;
    }

    public static int[] ToWave(int[] binary)
    {
        List<int> wave = new List<int>();
        foreach (int bit in binary)
        {
            if (bit == 0)
            {
                wave.Add(-1);
            }
            else
            {
                wave.Add(1);
            }
        }

        return wave.ToArray();
    }

    public static int[] waveToBinary(int[] wave)
    {
        List<int> binary = new List<int>();
        foreach (int signal in wave)
        {
            if (signal == 1)
            {
                binary.Add(1);
            }
            else
            {
                binary.Add(0);
            }
        }

        if (!zeroArray(binary.ToArray()))
        {
            return binary.ToArray();
        }
        else {
            return new int[0];
        }
        
    }

    public static int[] encodeWave(int[] wave, int[] code) { 
        int[] result_wave = new int[wave.Length*code.Length];

        for (int i = 0; i < wave.Length; i++) {
            for (int j = 0; j < code.Length; j++) {
                result_wave[i * code.Length + j] = wave[i] * code[j];
            }
        }

        return result_wave;
    }

    public static int[] decodeWave(int[] wave, int[] code)
    {
        int[] result_wave = new int[wave.Length/code.Length];

        for (int i = 0; i < wave.Length/code.Length; i++)
        {
            int result_signal = 0;
            for (int j = 0; j < code.Length; j++)
            {
                result_signal += wave[i * code.Length + j]*code[j];
            }

            result_signal = result_signal>0?1:-1;

            result_wave[i] = result_signal;
        }

        return result_wave;
    }

    public static void printIntArray(int[] array, bool withNumbers = false, int number = 0)
    {
        List<string> vals = new List<string>();
        foreach (int el in array)
        {
            vals.Add(el.ToString());
        }
        if (withNumbers)
        {
            Console.WriteLine("[{0}]: [{1}]", number, string.Join(", ", vals));
        }
        else {
            Console.WriteLine("[{0}]", string.Join(", ", vals));
        }
        
    }
}	

