using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

using SimpleJson;
using System.Diagnostics;


namespace cdma_sockets
{
    class cdma_server
    {        
        Socket listener;
        int port = 1991;
        IPEndPoint Point;
        
        private List<Thread> threads = new List<Thread>();

        private Dictionary<string, List<string>> outgoingMessages = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> incomingMessages = new Dictionary<string, List<string>>();

        protected void startServer()
        {            
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Определяем конечную точку, IPAddress.Any означает что наш сервер будет принимать входящие соединения с любых адресов
            Point = new IPEndPoint(IPAddress.Any, port);
            // Связываем сокет с конечной точкой
            listener.Bind(Point);
            // Начинаем слушать входящие соединения
            listener.Listen(10);

            startListening();
        }

        private void startListening()
        {            
            Thread acceptThread = new Thread(delegate()
            {
                while (true)
                {
                    // wait for client connection
                    Socket clientHandler = listener.Accept();
                    beginReceive(clientHandler);                    
                }
            });
            
            acceptThread.Name = "Accept Thread";
            acceptThread.Start();
            threads.Add(acceptThread);
        }

        private void beginReceive(Socket clientHandler) {
            // begin communication with client in a separate thread
            Thread receiveThread = new Thread(delegate()
            {
                while (clientHandler.Available == 0) {
                    System.Threading.Thread.Sleep(10); 
                } 
                List<byte> bytes = new List<byte>();
                while (clientHandler.Available > 0)
                {
                    byte[] buffer = new byte[clientHandler.Available];
                    int bytesRead = clientHandler.Receive(buffer);
                    if (bytesRead > 0)
                    {
                        bytes.AddRange(buffer);
                    }
                }

                if (bytes.Count > 0)
                {
                    string data = Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.Count);


                    //Console.WriteLine("Data received: {0}", data);
                    JsonObject parsed = (JsonObject)SimpleJson.SimpleJson.DeserializeObject(data);

                    string clientId = parsed["clientId"].ToString();
                    string requestType = parsed["requestType"].ToString();
                    string message = parsed["message"].ToString();

                    JsonObject reply = new JsonObject();

                    switch (requestType)
                    {
                        case "GET":
                            addReceiver(clientId);                            
                            if (clientHasOutgoingMessages(clientId))
                            {
                                reply["status"] = "OK";
                                reply["message"] = getNextOutgoingMessageForClient(clientId);
                                reply["moreAvailable"] = clientHasOutgoingMessages(clientId);
                            }
                            else
                            {
                                reply["status"] = "NO_MESSAGES";
                            }

                            break;
                        case "SEND":
                            addIncomingMessageFor(clientId, message);
                            reply["status"] = "OK";
                            break;
                    }

                    // reply to client
                    //Console.WriteLine("Reply: {0}", SimpleJson.SimpleJson.SerializeObject(reply));
                    byte[] replyBytes = Encoding.UTF8.GetBytes(SimpleJson.SimpleJson.SerializeObject(reply));
                    clientHandler.Send(replyBytes);
                }


                // close connection
                clientHandler.Shutdown(SocketShutdown.Both);
                clientHandler.Close();
            });

            receiveThread.Name = "Receive Thread";
            receiveThread.Start();
            threads.Add(receiveThread);
        }


        

        /* INCOMING */
        private void addIncomingMessageFor(string clientId, string msg){
            if (!incomingMessages.ContainsKey(clientId)) { 
                incomingMessages.Add(clientId, new List<string>());
            }
            incomingMessages[clientId].Add(msg);
        }


        /* OUTGOING */
        private void addReceiver(string clientId) { 
            if (!outgoingMessages.ContainsKey(clientId)){
                outgoingMessages.Add(clientId, new List<string>());
                Console.WriteLine("Receiver '{0}' connected", clientId);
            }
        }
        private bool clientHasOutgoingMessages(string clientId) {
            return outgoingMessages.ContainsKey(clientId) && outgoingMessages[clientId].Count > 0;
        }

        private string getNextOutgoingMessageForClient(string clientId) {
            if (clientHasOutgoingMessages(clientId)) {
                string returnMessage = outgoingMessages[clientId][0];
                outgoingMessages[clientId].RemoveAt(0);
                return returnMessage;
            }

            return null;
        }

        public void sendToAll(string msg) {
            foreach (var clientMessages in outgoingMessages) {
                sendTo(clientMessages.Key, msg);
            }
        }

        public void sendTo(string clientId, string msg){
            if (!outgoingMessages.ContainsKey(clientId)){
                addReceiver(clientId);
            }
            outgoingMessages[clientId].Add(msg);
            printOutgoingMessagesCount();
        }


        public void printOutgoingMessagesCount() {
            Console.WriteLine("OutgoingMessages:");
            foreach (var clientMessages in outgoingMessages){
                Console.WriteLine("  To {0}: {1}", clientMessages.Key, clientMessages.Value.Count);
            }
        }
    }

    class cdma_client
    {           
        private IPAddress serverIp = IPAddress.Parse("127.0.0.1");        
        private int serverPort = 1991;

        private string clientId = cdma_helpers.GetRandomString();

        private List<Thread> threads = new List<Thread>();


        public void startReceiving() {

            Thread receiveThread = new Thread(delegate() {
                Stopwatch timer = new Stopwatch();
                timer.Start();

                while (true) {                    
                    if (timer.ElapsedMilliseconds > cdma_helpers.askForNewMessagesDelay) { 
                        // connect
                        Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        sender.Connect(serverIp, serverPort);

                        // make request
                        JsonObject message = new JsonObject();
                        message["clientId"] = clientId;
                        message["requestType"] = "GET";
                        message["message"] = "";

                        

                        // send
                        byte[] msgBytes = Encoding.UTF8.GetBytes(SimpleJson.SimpleJson.SerializeObject(message));
                        int bytesSent = sender.Send(msgBytes);

                        // get reply                       
                        while (sender.Available == 0) {
                            System.Threading.Thread.Sleep(10); 
                        }                        
                        List<byte> bytes = new List<byte>();
                        while (sender.Available > 0)
                        {
                            byte[] buffer = new byte[sender.Available];
                            int bytesRead = sender.Receive(buffer);
                            if (bytesRead > 0)
                            {
                                bytes.AddRange(buffer);
                            }
                        }

                        //Console.WriteLine("Ask server {0}", bytes.Count);

                        if (bytes.Count > 0) {
                            string data = Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.Count);
                            //Console.WriteLine("Data received: {0}", data);
                            JsonObject parsed = (JsonObject)SimpleJson.SimpleJson.DeserializeObject(data);

                            if (parsed["status"].ToString() == "OK")
                            {
                                receive(parsed["message"].ToString());
                            }
                            else
                            {
                                //Console.WriteLine("No messages for you");
                            }

                            
                        }

                        sender.Shutdown(SocketShutdown.Both);
                        sender.Close();

                        timer.Restart();
                    }
                    
                }
                

            });

            receiveThread.Name = "Receive Thread";
            receiveThread.Start();
            threads.Add(receiveThread);
        }
        
        public void send(string msg){
            // connect
            Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(serverIp, serverPort);            

            // make request
            JsonObject message = new JsonObject();
            message["clientId"] = clientId;
            message["requestType"] = "SEND";
            message["message"] = msg;

            // send
            byte[] msgBytes = Encoding.UTF8.GetBytes(SimpleJson.SimpleJson.SerializeObject(message));
            int bytesSent = sender.Send(msgBytes);

            // get reply
            while (sender.Available == 0) {
                System.Threading.Thread.Sleep(10); 
            } 
            List<byte> bytes = new List<byte>();
            while (sender.Available > 0)
            {
                byte[] buffer = new byte[sender.Available];
                int bytesRead = sender.Receive(buffer);
                if (bytesRead > 0)
                {
                    bytes.AddRange(buffer);
                }
            }

            if (bytes.Count > 0) {
                string data = Encoding.UTF8.GetString(bytes.ToArray(), 0, bytes.Count);
                Console.WriteLine("Data received: {0}", data);
                JsonObject parsed = (JsonObject)SimpleJson.SimpleJson.DeserializeObject(data);

                if (parsed["status"].ToString() == "OK")
                {
                    Console.WriteLine("server received message");
                }
                else
                {
                    Console.WriteLine("Error sending message: {0}", parsed["status"].ToString());
                }
            }
            

            // close socket
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();           
        }        

        public virtual void receive(string message)
        {
            
        }
    }

    


    
    
}



