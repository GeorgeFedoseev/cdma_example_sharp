﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using cdma_sockets;


namespace cdma_receiver
{
    class cdma_receiver : cdma_client
    {

        int[] walsh_func;

        static void Main(string[] args)
        {
            cdma_receiver receiver = new cdma_receiver();
            receiver.connnect("receiver1");

            // select walsh func
            int[][] walsh_functions = { 
                                          new int[]{1, 1, 1, 1},
                                          new int[]{-1, 1, -1, 1},
                                          new int[]{-1, -1, 1, 1},
                                          new int[]{1, -1, -1, 1}
                                      };

            Console.WriteLine("Select Walsh func for receiver:");
            for (int i = 0; i < walsh_functions.Length; i++) {
                cdma_helpers.printIntArray(walsh_functions[i], true, i);
            }

            while (true)
            {
                int selected_func_num;
                if (int.TryParse(Console.ReadLine(), out selected_func_num))
                {                    
                    receiver.walsh_func = walsh_functions[selected_func_num];
                    Console.WriteLine("Selected:");
                    cdma_helpers.printIntArray(receiver.walsh_func);
                    break;
                }
                else
                {
                    Console.WriteLine("please enter func number");
                }
            }            

            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)) { }
           
        } // main

        public override void receive(byte[] bytes)
        {
            if (bytes.Length > 0)
            {
                int[] wave = cdma_helpers.GetIntArrayFromByteArray(bytes);
                int[] decoded_wave = cdma_helpers.decodeWave(wave, walsh_func);
                int[] binary = cdma_helpers.waveToBinary(decoded_wave);

                if (cdma_helpers.DEBUG) {
                    Console.WriteLine("Received wave: ");
                    cdma_helpers.printIntArray(wave);
                    Console.WriteLine("Decoded wave: ");
                    cdma_helpers.printIntArray(decoded_wave);
                    Console.WriteLine("Decoded binary: ");
                    cdma_helpers.printIntArray(binary);
                }
                
                
                if (binary.Length > 0) {
                    string message = cdma_helpers.ConvertToString(binary, Encoding.UTF8);
                    Console.Write(message);
                }
            }
        }

    } // class
} // namespace
