using System;

namespace TcpChatClient
{
    class UI
    {
        private string serverAddress;
        private int port;
        private string UserName;
        private bool error;
        public void SetUI()
        {
            try
            {
                do
                {
                    Console.Write("Введите адрес сервера: ");
                    serverAddress = Console.ReadLine();
                    error = string.IsNullOrEmpty(serverAddress) || string.IsNullOrWhiteSpace(serverAddress);
                }
                while (error);

                do
                {
                    Console.Write("Введите порт подключения к серверу: ");
                    string tmpPort = Console.ReadLine();
                    error = !(int.TryParse(tmpPort, out port));
                }
                while (error || (port < 1));

                do
                {
                    Console.Write("Введите свое имя: ");
                    UserName = Console.ReadLine();
                    error = string.IsNullOrEmpty(UserName) || string.IsNullOrWhiteSpace(UserName);
                }
                while (error);

                Client newClient = new Client(serverAddress, port, UserName);

                bool isConnected = newClient.Connect();

                if (isConnected)
                {
                    Console.WriteLine("{0} {1}: Connected.\r\n", DateTime.Now.ToString(), newClient.UserName);

                    newClient.IncomingMessageEvent += NewClient_IncomingMessageEvent;
                    newClient.OnErrorEvent += NewClient_OnErrorEvent;
                    newClient.AsyncReceiveData();

                    while (newClient.IsRunning)
                    {
                        Console.Write(newClient.UserName + ": ");
                        string message = Console.ReadLine();
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        if (message != string.Empty)
                        {
                            string tempMessage = newClient.SendMessage(message);
                            if (tempMessage != string.Empty)
                            {
                                Console.WriteLine(tempMessage + "\r\n");
                            }
                        }
                    }
                    Console.WriteLine("{0} {1}: Disconnected.\r\n", DateTime.Now.ToString(), newClient.UserName);

                }
                else Console.WriteLine("{0} {1}: Server reject connection.\r\n", DateTime.Now.ToString(), newClient.UserName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + " " + ex.StackTrace);
            }
        }

        private void NewClient_OnErrorEvent(object sender, Exception e)
        {
            Console.WriteLine(e.Message + "\r\n" + e.StackTrace + "\r\n");
        }

        private void NewClient_IncomingMessageEvent(object sender, string[] recieveMessages)
        {
            if (sender is Client && recieveMessages.Length > 0)
            {
                Console.Beep(440, 500);
                int cursorBeginingTop = Console.CursorTop;
                int cursorBeginingLeft = Console.CursorLeft;
                for (int i = 0; i < recieveMessages.Length; i++)
                {
                    Console.MoveBufferArea(0, cursorBeginingTop, Console.BufferWidth, 1, 0, cursorBeginingTop + 2);
                    Console.SetCursorPosition(0, cursorBeginingTop);
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine(recieveMessages[i]);
                    Console.ResetColor();
                    Console.SetCursorPosition(cursorBeginingLeft, cursorBeginingTop + 2);
                }
            }
        }
    }
}
