using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using TcpChatTypes;

namespace TcpChatClient
{
    class Client
    {
        private int _connection_port;
        private string _server_hostname;
        private string _user_name;
        private TcpClient _client;
        private NetworkStream _stream;

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
        public string ServerHostName
        {
            get
            {
                return _server_hostname;
            }
            private set
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException("Host Name can not be null or empty.");
                Ping pingRemoteMachine = new Ping();
                try
                {
                    PingReply replayMachine = pingRemoteMachine.Send(value.Trim(), 500);
                    if (replayMachine.Status != IPStatus.Success)
                        throw new Exception("Bad host name.");
                }
                catch
                {
                    throw new Exception("Bad host name.");
                }
                finally
                {
                    pingRemoteMachine.Dispose();
                }
                _server_hostname = value.Trim();
            }

        }
        public string UserName
        {
            get
            {
                return _user_name;
            }
            private set
            {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException("User Name can not be empty or null.");
                _user_name = value;
            }
        }
        public string Id { get; private set; }
        public bool IsRunning
        {
            get
            {
                return IsConnected();
            }
        }
        public event EventHandler<string[]> IncomingMessageEvent;
        public event EventHandler<Exception> OnErrorEvent;

        public Client(string serverHostName, int port, string userName)
        {
            ServerHostName = serverHostName;
            ConnectionPort = port;
            UserName = userName;
        }

        public bool Connect()
        {
            int tryCount = 0;
            _client = new System.Net.Sockets.TcpClient(ServerHostName, ConnectionPort);
            _stream = _client.GetStream();
            while (tryCount < 3)
            {
                if (_stream.DataAvailable)
                {
                    BinaryFormatter _messageFormatter = new BinaryFormatter();
                    ClientMessage recieveMessage = (ClientMessage)_messageFormatter.Deserialize(_stream);
                    string verificationMessage = recieveMessage.MessageText;
                    bool check = recieveMessage.UserName == "Server" && !(string.IsNullOrEmpty(recieveMessage.MessageText));

                    if (check)
                    {
                        check = recieveMessage.MessageText.StartsWith(_client.Client.LocalEndPoint.ToString() + " your id: ");
                        if (check)
                        {
                            Id = recieveMessage.MessageText.Split(new[] { " your id: " }, StringSplitOptions.None)[1].TrimEnd('.');
                            char[] t = Id.ToCharArray();
                            Array.Reverse(t);
                            string reverseId = new string(t);
                            _send_message(new ClientMessage
                            {
                                PacketType = MessageTypes.MessageType.ServerMessage,
                                UserName = "Client",
                                DateOfMessage = DateTime.Now,
                                MessageText = _client.Client.RemoteEndPoint.ToString() + " my id: " + reverseId + "."
                            });
                            Thread.Sleep(100);
                            if (IsConnected())
                                return true;
                        }
                    }
                }
                Thread.Sleep(50);
                tryCount++;
            }
            _client?.Close();
            return false;
        }

        public IAsyncResult BeginReceiveData(AsyncCallback callback, object @object)
        {
            Func<string[]> startAsyncRecieve = new Func<string[]>(ReceiveData);
            return startAsyncRecieve.BeginInvoke(callback, @object);
        }

        public string[] EndReceiveData(IAsyncResult result)
        {
            AsyncResult ar = result as AsyncResult;
            Func<string[]> startAsyncRecieve = ar.AsyncDelegate as Func<string[]>;
            return startAsyncRecieve.EndInvoke(result);
        }

        public string[] ReceiveData()
        {
            List<string> recieveMessages = new List<string>();
            if (_stream.DataAvailable)
            {
                do
                {
                    BinaryFormatter _messageFormatter = new BinaryFormatter();
                    ClientMessage recieveMessage = (ClientMessage)_messageFormatter.Deserialize(_stream);

                    // проверка на тип сообщения

                    recieveMessages.Add(string.Format("{0} {1}: {2}", recieveMessage.DateOfMessage.ToString(), recieveMessage.UserName, recieveMessage.MessageText));
                }
                while (_stream.DataAvailable);
            }
            return recieveMessages.ToArray();
        }

        public void AsyncReceiveData()
        {
            Action startReceive = new Action(_async_recieve_data);
            startReceive.BeginInvoke(null, null);
        }

        private void _async_recieve_data()
        {
            try
            {
                while (IsRunning)
                {
                    string[] recieveMessages = ReceiveData();
                    if (recieveMessages.Length > 0)
                    {
                        IncomingMessageEvent?.Invoke(this, recieveMessages);
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                OnErrorEvent?.Invoke(this, ex);
            }
        }

        public string SendMessage(string messageText)
        {
            if (_client == null)
                throw new NullReferenceException("First initialize instanse of chat client.");
            if (string.IsNullOrEmpty(messageText) || string.IsNullOrWhiteSpace(messageText))
                throw new ArgumentNullException(nameof(messageText) + " Message can not be null or empty.");
            ClientMessage outgoingMessage = new ClientMessage()
            {
                PacketType = MessageTypes.MessageType.UserMessage,
                UserName = UserName,
                DateOfMessage = DateTime.Now,
                MessageText = messageText
            };
            _send_message(outgoingMessage);
            return string.Format("{0} {1}: {2}", outgoingMessage.DateOfMessage, outgoingMessage.UserName, outgoingMessage.MessageText);
        }

        private void _send_message(ClientMessage sendingMessage)
        {
            BinaryFormatter _messageFormatter = new BinaryFormatter();
            _messageFormatter.Serialize(_stream, sendingMessage);
        }

        private bool IsConnected()
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation c in tcpConnections)
            {
                if (c.LocalEndPoint.Equals(_client?.Client.LocalEndPoint) && c.RemoteEndPoint.Equals(_client?.Client.RemoteEndPoint))
                {
                    if (c.State == TcpState.Established)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }
    }
}