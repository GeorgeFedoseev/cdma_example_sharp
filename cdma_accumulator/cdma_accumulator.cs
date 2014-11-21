using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

using cdma_sockets;
using System.Threading;
using System.Diagnostics;



namespace cdma_accumulator
{
    class cdma_accumulator : cdma_server 
    {
        private List<Thread> threads = new List<Thread>();
        List<int[]> wavesBuffer = new List<int[]>();        

        static void Main(string[] args)
        {
            cdma_accumulator accumulator = new cdma_accumulator();
            accumulator.start_server();
            accumulator.start_accumulating();
        }

        public override void receive(Socket r_client, byte[] bytes)
        {
            if (bytes.Length > 0) {
                int[] wave = cdma_helpers.GetIntArrayFromByteArray(bytes);
                //Console.WriteLine("Received wave: ");
                //cdma_helpers.printIntArray(wave);
                wavesBuffer.Add(wave);
            }
            
        }

        public void start_accumulating () {
            Thread th = new Thread(delegate() { 
                Stopwatch timer = new Stopwatch();
                timer.Start();

                while (true)
                {
                    if (timer.ElapsedMilliseconds > cdma_helpers.bufferMilliseconds)
                    {
                        if (wavesBuffer.Count > 0) {
                            int[] result_wave = sum_waves(wavesBuffer);
                            Console.WriteLine("Result wave: ");
                            cdma_helpers.printIntArray(result_wave);
                            send_to_all(cdma_helpers.GetByteArrayFromIntArray(result_wave));                            
                        }

                        timer.Restart();
                        wavesBuffer.Clear();
                    }
                }
            });

            th.Start();
            threads.Add(th);
        }


        public int[] sum_waves(List<int[]> waves) {
            // calc max length
            int maxWaveLength = 0;
            foreach (int[] w in waves) {
                if (w.Length > maxWaveLength) {
                    maxWaveLength = w.Length;
                }
            }

            // create result wave array
            int [] result_wave = new int[maxWaveLength];
            for (int i = 0; i < result_wave.Length; i++) {
                result_wave[i] = 0;
            }

            // sum waves
            for (int i = 0; i < waves.Count; i++)
            {
                int[] wave = waves[i];
                for (int j = 0; j < wave.Length; j++)
                {
                    result_wave[j] += wave[j];
                }
            }

            return result_wave;
        }
    }
}
