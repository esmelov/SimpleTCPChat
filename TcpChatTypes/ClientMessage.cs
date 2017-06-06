using System;
using System.Runtime.Serialization;
using static TcpChatTypes.MessageTypes;

namespace TcpChatTypes
{
    [Serializable]
    public class ClientMessage : EventArgs
    {
        [DataMember(IsRequired = true)]
        public MessageType PacketType { get; set; }

        [DataMember(IsRequired = true)]
        public string UserName { get; set; }

        [DataMember(IsRequired = true)]
        public DateTime DateOfMessage { get; set; }

        [DataMember(IsRequired = true)]
        public string MessageText { get; set; }
    }
}
