using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace cdma_sockets_async
{
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();

        public ManualResetEvent receiveDone = new ManualResetEvent(false);        
    }

    class cdma_server_async {        
        
        // Thread signal.
        public ManualResetEvent acceptConnectionDone = new ManualResetEvent(false);
        public ManualResetEvent sumDone = new ManualResetEvent(false);    

        public List<Thread> threads = new List<Thread>();
        public List<StateObject> clients = new List<StateObject>();

        public cdma_server_async()
        {
        }
 
        public void StartServer() {           
         
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 11000);
             
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp );
 
            try {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                // thread for connecting
                Thread th = new Thread(delegate()
                {
                    while (true)
                    {
                        acceptConnectionDone.Reset();

                        Console.WriteLine("Waiting for a connection...");
                        listener.BeginAccept(
                            new AsyncCallback(AcceptCallback),
                            listener);

                        acceptConnectionDone.WaitOne();
                    }

                });

                th.Name = "Connections Thread";
                th.Start();
                threads.Add(th);

 
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        
        }
 
        public void AcceptCallback(IAsyncResult ar) {
            acceptConnectionDone.Set();

            // thread for receiving
            Thread th = new Thread(delegate()
            {
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);
                StateObject state = new StateObject();
                state.workSocket = handler;

                clients.Add(state); 

                while (true) {
                    state.receiveDone.Reset();                    
                    
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);

                    state.receiveDone.WaitOne();
                    sumDone.WaitOne();
                    
                }
                
            });

            th.Name = "Receiving Thread";
            th.Start();
            threads.Add(th);
        }
 
        public void ReadCallback(IAsyncResult ar) {
            String content = String.Empty;        
            
            StateObject state = (StateObject) ar.AsyncState;
            Socket handler = state.workSocket;

            int bytesRead = handler.EndReceive(ar);
 
            if (bytesRead > 0) {
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer,0,bytesRead));
                 
                content = state.sb.ToString();
                //Console.WriteLine("received: {0}", content);

                if (content.IndexOf("<EOF>") > -1) {
                    while (content.IndexOf("<EOF>") > -1) {
                        Console.WriteLine("INDEX OF <EOF>: {0}/{1}", content.IndexOf("<EOF>") + 5, content.Length);
                        string head = content.Substring(0, content.IndexOf("<EOF>"));
                        state.sb.Clear();
                        if (head.Length < content.Length - 5)
                        {
                            string tail = content.Substring(content.IndexOf("<EOF>") + 5, content.Length - (head.Length + 5));
                            state.sb.Append(tail);
                        }

                        content = state.sb.ToString();

                        //Console.WriteLine("Head: {0}", head);
                        receive(handler, head);
                    }                    
                } else {                    
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                }
            }
        }
        
        public virtual void receive(Socket socket, string message)
        {

        }

        public void send(Socket socket, string msg)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(msg+"<EOF>");

            // Begin sending the data to the remote device.
            socket.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), socket);
        }

        public void sendToAll(string msg) {
            byte[] byteData = Encoding.ASCII.GetBytes(msg);

            foreach (StateObject st in clients) {
                Socket socket = st.workSocket;
                send(socket, msg);    
            }
        }
 
        private void SendCallback(IAsyncResult ar) {
            try {
                // Retrieve the socket from the state object.
                Socket handler = (Socket) ar.AsyncState;
 
                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
    }


    /* CLIENT */

    public class cdma_client_async
    {      
        // The port number for the remote device.
        private const int port = 11000;

        // ManualResetEvent instances signal completion.
        private ManualResetEvent connectDone =
            new ManualResetEvent(false);
        public ManualResetEvent sendDone =
            new ManualResetEvent(false);
        

        private Socket socket;
        protected StateObject state;
        private List<Thread> threads = new List<Thread>();

        public void Connect()
        {            
            try
            {         
                IPHostEntry ipHostInfo = Dns.Resolve("127.0.0.1");
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                socket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                state = new StateObject();
                state.workSocket = socket;

                socket.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), socket);
                connectDone.WaitOne();

                // thread for receiving
                Thread th = new Thread(delegate()
                {                    
                    while (true) {
                        state.receiveDone.Reset();
                        Receive();
                        state.receiveDone.WaitOne();
                    }
                });

                th.Name = "Receiving Thread";
                th.Start();
                threads.Add(th);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {                
                Socket client = (Socket)ar.AsyncState;
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void Receive()
        {
            try
            {
                //Console.WriteLine("RECEIVE START");                
                socket.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            //Console.WriteLine("Receive callback");
            try
            {               
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;
             
                int bytesRead = client.EndReceive(ar);
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                string content = state.sb.ToString();

                if (content.IndexOf("<EOF>") > -1)
                {
                    string head = content.Substring(0, content.IndexOf("<EOF>"));
                    state.sb.Clear();                    
                    if (head.Length < content.Length - 5)
                    {
                        string tail = content.Substring(content.IndexOf("<EOF>") + 5, content.Length - (head.Length + 5));
                        state.sb.Append(tail);
                    }

                    receive(head);
                    state.receiveDone.Set();
                    
                }
                else
                {
                    //Console.WriteLine("get next part");
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        
        public virtual void receive(string message)
        {

        }

        public void send(string msg)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(msg+"<EOF>");

            // Begin sending the data to the remote device.
            socket.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), socket);
        }


        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                //Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
	
}
