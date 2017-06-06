using System;

namespace TcpChatTypes
{
    [Serializable]
    public class MessageTypes
    {
        public enum MessageType
        {
            UserMessage,
            ServerMessage,
            Exception
        }
    }
}
