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

        long bufferMilliseconds = 1000;
        double chipsPerBit = 4;

        static void Main(string[] args)
        {
            cdma_bstation station = new cdma_bstation();
            station.connnect("1");
            station.send("Hello!");

            station.start_listening();
      
            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)) {}            
        }

        void start_listening () {

            // main thread for timer
            Thread th = new Thread(delegate()
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();

                while (true){
                    if (timer.ElapsedMilliseconds > 10000) {
                        // do stuff
                        

                        string binary = ToBinary(ConvertToByteArray(buffer, Encoding.UTF8));
                        Console.WriteLine(binary);

                        timer.Restart();
                        buffer = "";
                        Console.WriteLine("Restart timer");
                    }

                    
                }
            });

            th.Start();
            threads.Add(th);

            // thread for reading keys
            Thread th1 = new Thread(delegate()
            {                
                while (true){
                    string key = Console.ReadKey(true).KeyChar.ToString();
                    if(key != ""){
                       buffer = string.Concat(buffer, key);
                       Console.WriteLine(buffer);                   
                    }                    
                }
            });
            th1.Start();
            threads.Add(th1);
            
        }

        public static byte[] ConvertToByteArray(string str, Encoding encoding)
        {
            return encoding.GetBytes(str);
        }

        public static String ToBinary(Byte[] data)
        {
            return string.Join("", data.Select(byt => Convert.ToString(byt, 2).PadLeft(8, '0')));
        }

        public static List<int> ToWave(string binary) {
            List<int> wave = new List<int>();
            foreach (char c in binary){
                if (c == '0')
                {
                    wave.Add(-1);
                }
                else {
                    wave.Add(1);
                }
            }

            return wave;
        }
       
    } // class
} // namespace
