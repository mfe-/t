// See https://aka.ms/new-console-template for more information

namespace t.Client
{
    public class Program
    {
        static SocketClient socketClient;
        public static int Main(String[] args)
        {
            socketClient = new SocketClient ();
            socketClient.StartClient();
            return 0;
        }
    }
}


