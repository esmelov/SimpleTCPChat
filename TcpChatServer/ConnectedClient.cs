using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using TcpChatTypes;

namespace TcpChatServer
{
    class ConnectedClient
    {
        private TcpClient _tcp_client;
        private NetworkStream _stream;
        private bool _is_running = false;

        public string Id { get; set; }
        public event EventHandler<ClientMessage> ClientOutgoingMessageEvent;
        public event EventHandler<Exception> ClientErrorEvent;
        public event EventHandler ClientDisconnectEvent;

        public ConnectedClient(TcpClient tcpClient, string id)
        {
            _tcp_client = tcpClient;
            _stream = tcpClient.GetStream();
            Id = id;
        }

        private bool verification()
        {
            SendIncomingMessage(new ClientMessage
            {
                PacketType = MessageType.ServerMessage,
                UserName = "Server",
                DateOfMessage = DateTime.Now,
                MessageText = _tcp_client.Client.RemoteEndPoint.ToString() + " your id: " + Id + "."
            });
            int tryCount = 0;
            do
            {
                if (_stream.DataAvailable)
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    ClientMessage newMessage = (ClientMessage)formatter.Deserialize(_stream);
                    char[] t = Id.ToCharArray();
                    Array.Reverse(t);
                    string reverseId = new string(t);
                    bool check = newMessage.PacketType == MessageType.ServerMessage && newMessage.UserName == "Client" && newMessage.MessageText == _tcp_client.Client.LocalEndPoint.ToString() + " my id: " + reverseId + ".";
                    if (check) return true;
                    else break;
                }
                else
                {
                    Thread.Sleep(100);
                    tryCount++;
                }
            }
            while (tryCount < 3);
            return false;
        }

        public void Join()
        {
            try
            {
                _is_running = verification() ? true : throw new Exception($"{Id} is not chat client.");
                while (IsClientConnected() && _is_running)
                {
                    if (_stream.DataAvailable)
                    {
                        // десиарелизация сообщения
                        BinaryFormatter formatter = new BinaryFormatter();
                        ClientMessage newMessage = (ClientMessage)formatter.Deserialize(_stream);
                        ClientOutgoingMessageEvent?.Invoke(this, newMessage);
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                _is_running = false;
                ClientErrorEvent?.Invoke(this, ex);
            }
            finally
            {
                _tcp_client?.Close();
                ClientDisconnectEvent?.Invoke(this, null);
            }
        }

        public IAsyncResult AsyncJoin(AsyncCallback callback, object @object)
        {
            Action runAsync = new Action(Join);
            return runAsync.BeginInvoke(callback, @object);
        }

        public void Disconnect()
        {
            _is_running = false;
        }

        public void SendIncomingMessage(ClientMessage newIncomingMessage)
        {
            if (IsClientConnected())
            {
                if (_stream != null)
                {
                    BinaryFormatter sendMessageFormatter = new BinaryFormatter();
                    sendMessageFormatter.Serialize(_stream, newIncomingMessage);
                }
            }
        }

        public IAsyncResult AsyncSendIncomingMessage(ClientMessage newIncomingMessage, AsyncCallback callback, object @object)
        {
            Action<ClientMessage> sendMessageAction = new Action<ClientMessage>(SendIncomingMessage);
            return sendMessageAction.BeginInvoke(newIncomingMessage, callback, @object);
        }

        private bool IsClientConnected()
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation c in tcpConnections)
            {
                if (c.LocalEndPoint.Equals(_tcp_client?.Client.LocalEndPoint) && c.RemoteEndPoint.Equals(_tcp_client?.Client.RemoteEndPoint))
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
