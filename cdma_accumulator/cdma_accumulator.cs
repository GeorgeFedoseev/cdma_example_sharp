using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using cdma_sockets;

namespace cdma_accumulator
{
    class cdma_accumulator : cdma_server 
    {
        static void Main(string[] args)
        {
            cdma_accumulator accumulator = new cdma_accumulator();
            accumulator.start_server();
        }
    }
}
