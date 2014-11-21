cdma_example_sharp
==================

<b>cdma_sockets.cs</b> - <i>cdma_client</i> and <i>cdma_server</i><br>
<b>cdma_bstation.cs</b> (derived from cdma_client) - encodes data with CDMA algorithm and sends to cdma_accumulator <br>
<b>cdma_accumulator.cs</b> (derived from cdma_server) - gets data from cdma_bstation's and sums them, then sends summed data to cdma_receivers<br>
<b>cdma_receiver.cs</b> (derived from cdma_client) - receives summed data and extracts it's data from summed (cdma_accumulated)<br>
<b>cdma_helpers.cs</b> - binary conversion functions, configuration and other<br>
