using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;


using System.Threading;
using System.Diagnostics;

using cdma_sockets_async;

namespace cdma_accumulator
{
    class cdma_accumulator : cdma_server_async
    {
        private List<Thread> threads = new List<Thread>();
        List<int[]> wavesBuffer = new List<int[]>();

        static void Main(string[] args)
        {
            cdma_accumulator accumulator = new cdma_accumulator();
            accumulator.StartServer();

            accumulator.start_accumulating();
        }

        public override void receive(Socket client, string msg)
        {
            //Console.WriteLine("Received: {0}", msg);
            int[] wave = cdma_helpers.GetIntArrayFromString(msg);            
            int[] Ddecoded_wave = cdma_helpers.decodeWave(wave, new int[] { -1, 1, -1, 1 });
            int[] Dbinary = cdma_helpers.waveToBinary(Ddecoded_wave);

            if (Dbinary.Length > 0)
            {
                string message = cdma_helpers.ConvertToString(Dbinary, Encoding.UTF8);
                Console.WriteLine("RECEIVED: {0}", message);
            }
            wavesBuffer.Add(wave);
        }

        public void start_accumulating () {
            Thread th = new Thread(delegate() { 
                Stopwatch timer = new Stopwatch();
                timer.Start();

                while (true)
                {                    
                    if (timer.ElapsedMilliseconds > cdma_helpers.bufferMilliseconds)
                    {                                            
                        // time to sum waves and send them out
                        if (wavesBuffer.Count > 0)
                        {
                            sumDone.Reset();

                            int[] result_wave = sum_waves(wavesBuffer);
                            

                            //Console.WriteLine("Result wave: ");
                            //cdma_helpers.printIntArray(result_wave);
                            string waveStr = cdma_helpers.GetStringFromIntArray(result_wave);                            
                            sendToAll(waveStr);

                            int[] Dwave = cdma_helpers.GetIntArrayFromString(waveStr);
                            int[] Ddecoded_wave = cdma_helpers.decodeWave(Dwave, new int[] { -1, 1, -1, 1 });
                            int[] Dbinary = cdma_helpers.waveToBinary(Ddecoded_wave);

                            if (Dbinary.Length > 0)
                            {
                                string message = cdma_helpers.ConvertToString(Dbinary, Encoding.UTF8);
                                Console.WriteLine("SENT: {0}", message);
                            }

                            wavesBuffer.Clear();
                            // allow to receive from all clients
                            foreach (StateObject state in clients)
                            {
                                state.receiveDone.Set();
                            }

                            sumDone.Set();
                        }

                        timer.Restart();
                    }


                    
                }
            });

            th.Name = "Accumulator Sum Thread";
            th.Start();
            threads.Add(th);
        }


        public int[] sum_waves(List<int[]> waves) {
            Console.WriteLine("Waves number count: {0}", waves.Count);
            // calc max length of given waves to create result_wave with right length
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
