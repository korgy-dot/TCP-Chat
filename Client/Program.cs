using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TCPChat
{
    class ServerHandler
    {
        public ServerHandler(string ip, int port)
        {
            localAdd = IPAddress.Parse(ip);
            listener = new TcpListener(localAdd, port);
            serverPort = port;
        }

        public int serverPort;
        public IPAddress localAdd;
        public TcpListener listener;
        public List<TcpClient> clients = new List<TcpClient>();
        public bool listining = false;


        public async void handleClient(TcpClient client)
        {
            await Task.Run(async () =>
            {
                NetworkStream nwStream = client.GetStream();
                while (client.Connected)
                {
                    byte[] buffer = new byte[client.ReceiveBufferSize];
                    int bytesRead = await nwStream.ReadAsync(buffer, 0, client.ReceiveBufferSize).ConfigureAwait(false);
                    if(client.ReceiveBufferSize != 0)
                    {
                        string dataReceived = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        if (dataReceived.StartsWith("::"))
                        {
                            string usernamey = dataReceived.Remove(0, 2);
                            publicMessage($"{usernamey} joined");
                            continue;
                        }
                        Console.WriteLine($"Got : {dataReceived}");
                        foreach (TcpClient tClient in clients)
                        {
                            Console.WriteLine("Sending data to client");
                            NetworkStream cStream = tClient.GetStream();
                            cStream.Write(buffer, 0, bytesRead);
                        }
                    }

                }
            });
        }

        public void publicMessage(string msg)
        {
            msg = $"[SERVER] {msg}";
            byte[] data = new byte[1024];
            data = Encoding.ASCII.GetBytes(msg);


            foreach (TcpClient tClient in clients)
            {
                Console.WriteLine($"sending : {msg}");
                NetworkStream cStream = tClient.GetStream();
                cStream.Write(data, 0, data.Length);
            }
        }

        public void clientJoined(TcpClient client)
        {
            clients.Add(client);
            handleClient(client);
        }

        public void startListener()
        {
            listener.Start();
            Console.WriteLine($"Started listener on {localAdd}:{serverPort}");
            listining = true;
            listenLoop();
            Console.ReadLine();
            exit();
        }

        public async void listenLoop()
        {
            while (listining)
            {
                TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                clientJoined(client);
                
            }
        }

        public void exit()
        {
            listining = false;
            listener.Stop();
        }


    }

    class ClientHandler
    {
        public ClientHandler(string username)
        {
            user = username;
        }

        public string user;
        public TcpClient client;
        public NetworkStream nwStream;

        public void connect(string ip, int port)
        {
            client = new TcpClient(ip, port);
            nwStream = client.GetStream();

            Console.WriteLine("Connected!");
            serverCheck();
            sendMessage($"::{user}");
            while (client.Connected)
            {
                string msg = Console.ReadLine();
                if(msg == "exit")
                {
                    exit();
                    Environment.Exit(0);
                }
                sendMessage($"[{user}] {msg}");
            }
        }

        public byte[] encodeMsg(string msg)
        {
            return ASCIIEncoding.ASCII.GetBytes(msg);
        }

        public void sendMessage(string msg)
        {
            nwStream.Write(encodeMsg(msg), 0, encodeMsg(msg).Length);
        }

        public async void serverCheck()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    byte[] bToRead = new byte[client.ReceiveBufferSize];
                    if (bToRead.Length > 0)
                    {
                        int bRead = nwStream.Read(bToRead, 0, client.ReceiveBufferSize);
                        string sMsg = Encoding.ASCII.GetString(bToRead, 0, bRead);
                        if (!sMsg.Contains($"[{user}]"))
                        {
                            Console.WriteLine(Encoding.ASCII.GetString(bToRead, 0, bRead));

                            bToRead = new byte[client.ReceiveBufferSize];
                        }

                        
                    }
                }
            });
        }

        public void exit()
        {
            client.Close();
        }
    }

    class Program
    {
       
        static void Main(string[] args)
        {
            Console.WriteLine("Are you a client or server? [C/S]");
            string option = Console.ReadLine();

            switch (option)
            {
                case "S":
                    Console.WriteLine("What ip are you running this on?");
                    string ip = Console.ReadLine();
                    int port = 5000;
                    ServerHandler serverHandler = new ServerHandler(ip,port);
                    serverHandler.startListener();
                    break;
                case "C":
                    Console.WriteLine("What server are you connecting to?");
                    string sIp = Console.ReadLine();
                    int sPort = 5000;

                    Console.WriteLine("Whats your username?: ");
                    string username = Console.ReadLine();
                    ClientHandler clientHandler = new ClientHandler(username);
                    clientHandler.connect(sIp, sPort);
                    break;
            }
        }
    }
}
