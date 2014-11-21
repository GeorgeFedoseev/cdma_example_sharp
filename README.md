cdma_example_sharp
==================

cdma_sockets.cs - cdma_client and cdma_server classes to derive from<br>
cdma_bstation (derived from cdma_client) - encodes data with CDMA algorithm and sends to cdma_accumulator <br>
cdma_accumulator (derived from cdma_server) - gets data from cdma_bstation's and sums them, then sends summed data to cdma_receivers<br>
cdma_receiver (derived from cdma_client) - receives summed data and extracts it's data from summed (cdma_accumulated)
