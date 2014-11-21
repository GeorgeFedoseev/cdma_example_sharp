using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

namespace cdma_sockets
{
    class cdma_client
    {
        // Статус клиента
        private bool client_running;
        // Сокет клиента
        private Socket client;
        // Адрес сервера
        private IPAddress ip = IPAddress.Parse("127.0.0.1");
        // Порт, по которому будем присоединяться
        private int port = 1991;
        // Список потоков
        private List<Thread> threads = new List<Thread>();

        public string id;

        public void connnect(string clientID)
        {
            id = clientID;

            try
            {
                client_running = true;
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(ip, port);
                start_receiving();
                Console.WriteLine("connected");
            }
            catch {
                Console.WriteLine("failed to connect to server");
            }
        }

        void start_receiving()
        {
            Thread th = new Thread(delegate()
            {
                while (client_running)
                {
                    try
                    {
                        var lengthBytes = new byte[4];
                        client.Receive(lengthBytes);
                        int messageLength = BitConverter.ToInt32(lengthBytes, 0);

                        //Console.WriteLine("Message length: {0}", messageLength);

                        byte[] bytes = new byte[messageLength];                        
                        client.Receive(bytes);
                        if (bytes.Length != 0)
                        {
                            string msg = Encoding.UTF8.GetString(bytes);
                            receive(bytes);
                            receive(msg);                            
                        }
                    }
                    catch { }
                }
            });
            th.Start();
            threads.Add(th);
        }

        public virtual void receive(byte[] bytes)
        {

        }

        public virtual void receive(string message)
        {
            //Console.WriteLine("Received message: {0}", message);
        }

        public void send(byte[] bytes)
        {
            try{
                client.Send(BitConverter.GetBytes(bytes.Length));
                client.Send(bytes);
            }catch { }
        }

        public void send(string msg)
        {           
            byte[] bytes = new byte[1024];
            bytes = Encoding.UTF8.GetBytes(msg);
            send(bytes);            
        }

    }

    class cdma_server
    {
         // Здесь будет хранится статус сервера
        bool isServerRunning;
        // Здесь будет список наших клиентов
        private Hashtable clients;
        // Это сокет нашего сервера
        Socket listener;
        // Порт, на котором будем прослушивать входящие соединения
        int port = 1991;
        // Точка для прослушки входящих соединений (состоит из адреса и порта)
        IPEndPoint Point;
        // Список потоков
        private List<Thread> threads = new List<Thread>();


        protected void start_server()
        {
            clients = new Hashtable(30);
            isServerRunning = true;
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Определяем конечную точку, IPAddress.Any означает что наш сервер будет принимать входящие соединения с любых адресов
            Point = new IPEndPoint(IPAddress.Any, port);
            // Связываем сокет с конечной точкой
            listener.Bind(Point);
            // Начинаем слушать входящие соединения
            listener.Listen(10);

            start_receiving();
        }

        private void start_receiving()
        {
            // Запускаем цикл в отдельном потоке, чтобы приложение не зависло
            Thread th = new Thread(delegate()
            {
                while (isServerRunning)
                {
                    // Создаем новый сокет, по которому мы сможем обращаться клиенту
                    // Этот цикл остановится, пока какой-нибудь клиент не попытается присоединиться к серверу
                    Socket client = listener.Accept();
                    // Теперь, обратившись к объекту client, мы сможем отсылать и принимать пакеты от последнего подключившегося пользователя.
                    // Добавляем подключенного клиента в список всех клиентов, для дальнейшей массовой рассылки пакетов
                    clients.Add(client, "");
                    // Начинаем принимать входящие пакеты
                    Thread thh = new Thread(delegate()
                    {
                        start_receiving_from(client);
                    });
                    thh.Start();
                }
            });
            // Приведенный выше цикл пока что не работает, запускаем поток. Теперь цикл работает.
            th.Start();
            threads.Add(th);
        }

        private void start_receiving_from(Socket r_client)
        {
            // Для каждого нового подключения, будет создан свой поток для приема пакетов
            Thread th = new Thread(delegate()
            {
                while (isServerRunning)
                {
                    try
                    {

                        var lengthBytes = new byte[4];
                        r_client.Receive(lengthBytes);
                        int messageLength = BitConverter.ToInt32(lengthBytes, 0);
                        //Console.WriteLine("Message length: {0}", messageLength);

                        byte[] bytes = new byte[messageLength];                        
                        r_client.Receive(bytes);
                        if (bytes.Length != 0)
                        {
                            // call receive functions
                            receive(r_client, bytes);
                            receive(r_client, Encoding.UTF8.GetString(bytes));
                        }
                    }
                    catch { }
                }
            });
            th.Start();
            threads.Add(th);
        }

        public virtual void receive(Socket r_client, byte[] bytes) { 

        }

        public virtual void receive(Socket r_client, string message)
        {
            //Console.WriteLine("Received message: {0}", message);
        }

        public void send_to_all(byte[] bytes) {
            foreach (DictionaryEntry clientDic in clients)
            {
                send((Socket)clientDic.Key, bytes);
            }
        }

        public void send_to_all(string msg)
        {
            foreach (Socket client in clients)
            {
                send(client, msg);
            }
        }

        public void send(Socket c_client, byte[] bytes)
        {
            try
            {
                c_client.Send(BitConverter.GetBytes(bytes.Length));
                c_client.Send(bytes);
            }
            catch {
                Console.WriteLine("Failed to send to client");
            }
        }

        public void send(Socket c_client, string msg)
        {            
            byte[] bytes = new byte[1024];
            bytes = Encoding.UTF8.GetBytes(msg);
            send(c_client, bytes);            
        }

    }


    
    
}



