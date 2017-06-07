using System;

namespace TcpChatTypes
{
    [Serializable]
    public enum MessageType
    {
        UserMessage,
        ServerMessage,
        Exception
    }
}
