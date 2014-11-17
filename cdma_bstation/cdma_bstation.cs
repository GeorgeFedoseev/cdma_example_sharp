using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using cdma_sockets;

namespace cdma_bstation
{
    class cdma_bstation : cdma_client
    {    
        

        static void Main(string[] args)
        {
            cdma_bstation station = new cdma_bstation();
            station.connnect("1");
            station.send("Hello!");
      
            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)) {}
            
        }

        
    }
}
