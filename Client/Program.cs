using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;



namespace TCPChat
{


    class encryption
    {
        private static string xorKey = "Ztrdl3C2pDsxiEU4qKtS";


        public static string XORCipher(string data)
        {
            string key = xorKey;
            int dataLen = data.Length;
            int keyLen = key.Length;
            char[] output = new char[dataLen];

            for (int i = 0; i < dataLen; ++i)
            {
                output[i] = (char)(data[i] ^ key[i % keyLen]);
            }

            return new string(output);
        }

    }


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
        public Dictionary<TcpClient, string> clientNames = new Dictionary<TcpClient, string>();
        public Dictionary<string, TcpClient> inverseClientNames = new Dictionary<string, TcpClient>();

        private async void handleClient(TcpClient client)
        {
            await Task.Run(async () =>
            {
                NetworkStream nwStream = client.GetStream();
                while (client.Connected)
                {
                    try
                    {
                        byte[] buffer = new byte[client.ReceiveBufferSize];
                        int bytesRead = await nwStream.ReadAsync(buffer, 0, client.ReceiveBufferSize).ConfigureAwait(false);
                        if (client.ReceiveBufferSize != 0)
                        {
                            string dataReceived = encryption.XORCipher(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                            if (dataReceived.StartsWith("::"))
                            {
                                string usernamey = dataReceived.Remove(0, 2);
                                publicMessage($"{usernamey} joined");
                                clientNames[client] = usernamey;
                                inverseClientNames[usernamey] = client;
                                continue;
                            }
                            Console.WriteLine($"{dataReceived}");
                            foreach (TcpClient tClient in clients)
                            {
                                Console.WriteLine($"[DEBUG] relayed to {clientNames[tClient]}");
                                NetworkStream cStream = tClient.GetStream();
                                cStream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                    catch (System.Exception err)
                    {
                        var user = clientNames[client];
                        clientNames.Remove(client);
                        inverseClientNames.Remove(user);
                        clients.Remove(client);
                        publicMessage($"{user} disconnected");
                    }

                }
            });
        }

        private void publicMessage(string msg)
        {
            msg = $"[SERVER] {msg}";
            byte[] data = new byte[1024];
            data = Encoding.ASCII.GetBytes(encryption.XORCipher(msg));


            foreach (TcpClient tClient in clients)
            {
                Console.WriteLine($"sending : {msg}");
                NetworkStream cStream = tClient.GetStream();
                cStream.Write(data, 0, data.Length);
            }
        }

        private void clientJoined(TcpClient client)
        {
            clients.Add(client);
            handleClient(client);
        }

        public void startListener()
        {
            listener.Start();
            Console.Clear();
            Console.WriteLine($"Started listener on {localAdd}:{serverPort}");
            Console.WriteLine("xor encryption active");
            listining = true;
            listenLoop();
            while (true)
            {
                var command = Console.ReadLine();

                if (command.ToLower().Contains("say"))
                {
                    publicMessage(command.Trim("say".ToCharArray()));
                }
                else if (command.ToLower().Contains("kick"))
                {
                    var user = command.Remove(0, 5);
                    user.Replace(" ", string.Empty);
                    var client = inverseClientNames[user];
                    publicMessage($"kicking {user}");
                    client.Close();
                    Console.WriteLine($"Kicked user {user}");
                }
                else if (command.ToLower().Contains("exit"))
                {
                    exit();
                }
                else if (command.ToLower().Contains("help"))
                {
                    Console.WriteLine("say - publicly say anything, ex: say Hello World");
                    Console.WriteLine("kick - kick a user, ex: kick user");
                    Console.WriteLine("exit - shutdown server instance");
                }


            }
        }

        private async void listenLoop()
        {
            while (listining)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    clientJoined(client);
                }
                catch
                {
                    Console.WriteLine("Closed");
                    Environment.Exit(0);
                }
                
            }
        }

        public void exit()
        {
            listining = false;
            listener.Stop();
            Environment.Exit(0);
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

            Console.WriteLine("Connected! (CTRL + C to exit)");
            serverCheck();
            sendMessage($"::{user}");
            while (client.Connected)
            {
                string msg = Console.ReadLine();
                sendMessage($"[{user}] {msg}");

                
            }
        }

        private byte[] encodeMsg(string msg)
        {
            return ASCIIEncoding.ASCII.GetBytes(encryption.XORCipher(msg));
        }

        private void sendMessage(string msg)
        {
            nwStream.Write(encodeMsg(msg), 0, encodeMsg(msg).Length);
        }

        private async void serverCheck()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        byte[] bToRead = new byte[client.ReceiveBufferSize];
                        if (bToRead.Length > 0)
                        {
                            int bRead = nwStream.Read(bToRead, 0, client.ReceiveBufferSize);
                            string sMsg = Encoding.ASCII.GetString(bToRead, 0, bRead);
                            sMsg = encryption.XORCipher(sMsg);
                            if (!sMsg.Contains($"[{user}]"))
                            {
                                Console.WriteLine(encryption.XORCipher(Encoding.ASCII.GetString(bToRead, 0, bRead)));

                                bToRead = new byte[client.ReceiveBufferSize];
                            }


                        }
                    }
                    catch
                    {
                        Console.WriteLine("Closed");
                        Environment.Exit(0);
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
