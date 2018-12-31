using System;

namespace SingleThreadedServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 12200;
            Console.WriteLine($"Listening on port {port}...");
            var server = new LocalServer(port);
            server.Run();
        }
    }
}
