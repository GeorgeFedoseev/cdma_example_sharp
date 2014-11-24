using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;

using System.Diagnostics;

using cdma_sockets;

namespace cdma_bstation
{
    class cdma_bstation : cdma_client
    {
        private List<Thread> threads = new List<Thread>();
        private string buffer = string.Empty;

        int[] walsh_func;        

        static void Main(string[] args)
        {
            cdma_bstation station = new cdma_bstation();

            // select walsh func
            int[][] walsh_functions = { 
                                          new int[]{1, 1, 1, 1},
                                          new int[]{-1, 1, -1, 1},
                                          new int[]{-1, -1, 1, 1},
                                          new int[]{1, -1, -1, 1}
                                      };

            Console.WriteLine("Select Walsh func for bstation:");
            for (int i = 0; i < walsh_functions.Length; i++)
            {
                cdma_helpers.printIntArray(walsh_functions[i], true, i);
            }

            while (true)
            {
                int selected_func_num;
                if (int.TryParse(Console.ReadLine(), out selected_func_num) 
                    && selected_func_num >= 0
                    && selected_func_num < walsh_functions.Length)
                {
                    station.walsh_func = walsh_functions[selected_func_num];
                    Console.WriteLine("Selected:");
                    cdma_helpers.printIntArray(station.walsh_func);
                    break;
                }
                else
                {
                    Console.WriteLine("please enter func number");
                }
            }

            // function selected

            // connect to accumulator
            Console.WriteLine("Try connecting to server");
            station.connnect("bstation");
            
            // start sending strings from console
            station.start_sending();
      
            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)) {}            
            
        }

        void start_sending () {

            // main thread for sending strings
            Thread th = new Thread(delegate()
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();

                while (true){
                    if (timer.ElapsedMilliseconds > cdma_helpers.bufferMilliseconds)
                    {                        
                        // do stuff
                        if (buffer.Length > 0) {
                            int[] binary = cdma_helpers.ToBinary(cdma_helpers.ConvertToByteArray(buffer, Encoding.UTF8));
                            
                            int[] wave = cdma_helpers.ToWave(binary);
                            int[] encodedWave = cdma_helpers.encodeWave(wave, walsh_func);

                            if (cdma_helpers.DEBUG) {
                                cdma_helpers.printIntArray(binary);
                                Console.WriteLine("Wave: ");
                                cdma_helpers.printIntArray(wave);
                                Console.WriteLine("Encoded wave: ");
                                cdma_helpers.printIntArray(encodedWave);
                                Console.WriteLine("Send wave");
                            }

                            byte[] bytes = cdma_helpers.GetByteArrayFromIntArray(encodedWave);
                            send(bytes);
                        }                        

                        timer.Restart();
                        buffer = "";                        
                    }

                    
                }
            });

            th.Start();
            threads.Add(th);

            // thread for reading keys from console
            Thread th1 = new Thread(delegate()
            {                
                while (true){
                    string key = Console.ReadKey(true).KeyChar.ToString();
                    if(key != ""){
                       buffer = string.Concat(buffer, key);
                       buffer = buffer.Replace(((char)13).ToString(), Environment.NewLine);
                       Console.WriteLine(buffer);                   
                    }                    
                }
            });
            th1.Start();
            threads.Add(th1);
            
        }
        
       
    } // class
} // namespace
