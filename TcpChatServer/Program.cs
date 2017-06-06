using System;

namespace TcpChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            ChatServer chatServer = new ChatServer(ServerCfg.Default.ServerPort, ServerCfg.Default.MaxConnections);
            chatServer.StartServer();
            Console.WriteLine("Для выхода введите \"quit\".");
            while (chatServer.IsRunning)
            {
                string action = Console.ReadLine();
                if (action == "quit" && chatServer.IsRunning)
                    chatServer.StopServer();
            }
            Console.ReadKey();
        }
    }
}
