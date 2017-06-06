using System;

namespace TcpChatClient
{
    class Program
    {
        static void Main(string[] args)
        {
            UI newUI = new UI();
            newUI.SetUI();
            Console.ReadKey();
        }
    }
}
