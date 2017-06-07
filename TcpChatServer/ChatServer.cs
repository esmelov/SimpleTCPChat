using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using TcpChatTypes;

namespace TcpChatServer
{
    class ChatServer
    {
        private int _connection_port;
        private int _max_connections;
        private TcpListener _server_listener;
        private bool _is_running = false;
        private List<ConnectedClient> _clients;

        public int ConnectionPort
        {
            get
            {
                return _connection_port;
            }
            private set
            {
                if (value < 1024 || value > 65536)
                    throw new Exception("Invalid port value.");
                else _connection_port = value;
            }
        }
        public int MaxConnection
        {
            get
            {
                return _max_connections;
            }
            private set
            {
                if (value < 1)
                    throw new Exception("Invalid number of connections.");
                else _max_connections = value;
            }
        }
        public bool IsRunning
        {
            get
            {
                return _is_running;
            }
        }
        public int ClientCount
        {
            get
            {
                return _clients.Count;
            }
        }

        public ChatServer(int port) : this(port, 3) { }
        public ChatServer(int port, int maxConnection)
        {
            ConnectionPort = port;
            MaxConnection = maxConnection;
            _clients = new List<ConnectedClient>();
        }

        public void StartServer()
        {
            try
            {
                _is_running = true;
                _server_listener = new TcpListener(IPAddress.Any, _connection_port);
                _server_listener.Start();
                Console.WriteLine("{0} Сервер запущен.", DateTime.Now.ToString());
                _server_listener.BeginAcceptTcpClient(_new_connections, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                _is_running = false;
                _server_listener?.Stop();
                Console.WriteLine("{0} Сервер остановлен.", DateTime.Now.ToString());
            }
        }

        public void StopServer()
        {
            _is_running = false;
            _server_listener?.Stop();
            Console.WriteLine("{0} Сервер остановлен.", DateTime.Now.ToString());
        }

        public void DisconnectAllClients()
        {
            if (ClientCount > 0)
            {
                var tmpClients = _clients.ToArray();
                for (int i = 0; i < tmpClients.Length; i++)
                {
                    tmpClients[i].Disconnect();
                }
            }
        }

        private void _new_connections(IAsyncResult ar)
        {
            AsyncResult result = ar as AsyncResult;
            if (_is_running)
            {
                try
                {
                    TcpClient newClient = _server_listener.EndAcceptTcpClient(ar);
                    Console.WriteLine("{0} Принят новый клиент.", DateTime.Now.ToString());
                    ConnectedClient newConnectedClient = new ConnectedClient(newClient, Guid.NewGuid().ToString());
                    if (ClientCount < MaxConnection)
                    {
                        newConnectedClient.ClientOutgoingMessageEvent += NewConnectedClient_ClientOutgoingMessageEvent;
                        newConnectedClient.ClientDisconnectEvent += NewConnectedClient_ClientDisconnectEvent;
                        newConnectedClient.ClientErrorEvent += NewConnectedClient_ClientErrorEvent;
                        _clients.Add(newConnectedClient);
                        newConnectedClient.AsyncJoin(null, null);
                        Console.WriteLine(DateTime.Now.ToString() + " " + newConnectedClient.Id + " подключен.");
                    }
                    else
                    {
                        newConnectedClient.AsyncSendIncomingMessage(new ClientMessage
                        {
                            PacketType = MessageType.UserMessage,
                            UserName = "Server",
                            DateOfMessage = DateTime.Now,
                            MessageText = "Превышено максимальное количество подключений к серверу."
                        }, _cancel_message, DateTime.Now.ToString() + " " + newConnectedClient.Id + " пытался подключиться.");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                }
                _server_listener?.BeginAcceptTcpClient(_new_connections, null);
            }
        }

        private void NewConnectedClient_ClientErrorEvent(object sender, Exception e)
        {
            Console.WriteLine(e.Message + "\r\n" + e.StackTrace);
        }

        private void NewConnectedClient_ClientDisconnectEvent(object sender, EventArgs e)
        {
            if (_clients != null && sender is ConnectedClient ClientForDisconnect)
            {
                try
                {
                    _clients.Remove(ClientForDisconnect);
                    Console.WriteLine("{0} Клиент {1} отключен.", DateTime.Now.ToString(), ClientForDisconnect.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\r\n" + ex.StackTrace);
                }
            }
        }

        private void NewConnectedClient_ClientOutgoingMessageEvent(object sender, ClientMessage e)
        {
            if (e != null && sender is ConnectedClient clientSender)
            {
                MessageProcessing(clientSender, e);
            }
        }

        private void MessageProcessing(ConnectedClient _client, ClientMessage cm)
        {
            switch (cm.PacketType)
            {
                case MessageType.UserMessage:
                    foreach (ConnectedClient tc in _clients)
                    {
                        if (!_client.Equals(tc))
                        {
                            tc.AsyncSendIncomingMessage(cm, null, null);
                        }
                    }
                    break;
                case MessageType.ServerMessage:
                    switch (cm.MessageText)
                    {
                        case "Сonnected.":
                            //Console.WriteLine("{0} Клиент {1} подключен.", DateTime.Now.ToString(), _client.Id);
                            break;
                        case "Disonnected.":
                            //Console.WriteLine("Клиент {0} отключен.", _client.Id);
                            break;
                    }
                    break;
            }
        }

        private void _cancel_message(IAsyncResult ar)
        {
            Console.WriteLine(ar.AsyncState);
        }
    }
}
