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

        static void Main(string[] args)
        {
            cdma_accumulator accumulator = new cdma_accumulator();
            accumulator.startServer();
            accumulator.start_accumulating();
        }

        public void start_accumulating () {
            Thread th = new Thread(delegate() { 
                Stopwatch timer = new Stopwatch();
                timer.Start();

                while (true)
                {
                    if (timer.ElapsedMilliseconds > cdma_helpers.sumWavesDelay)
                    {
                        List<int[]> wavesBuffer = new List<int[]>();

                        Dictionary<string, List<string>> modifiedIncomingMessages = new Dictionary<string, List<string>>(incomingMessages);
                        foreach (var clientMessagesDic in incomingMessages) {
                            var clientMessages = clientMessagesDic.Value;
                            if (clientMessages.Count > 0) {
                                string message = clientMessages[0];
                                clientMessages.RemoveAt(0);
                                modifiedIncomingMessages[clientMessagesDic.Key] = clientMessages;

                                int[] wave = cdma_helpers.GetIntArrayFromString(message);
                                wavesBuffer.Add(wave);
                            }
                        }

                        incomingMessages = modifiedIncomingMessages;

                        if (wavesBuffer.Count > 0) {
                            int[] result_wave = sum_waves(wavesBuffer);

                            if (result_wave.Length > 0)
                            {
                                Console.WriteLine("Result wave: ");
                                cdma_helpers.printIntArray(result_wave);
                                sendToAll(cdma_helpers.GetStringFromIntArray(result_wave));
                            }
                        }
                        
                        

                        timer.Restart();                        
                    }
                }
            });

            th.Start();
            threads.Add(th);
        }


        public int[] sum_waves(List<int[]> waves) {
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
