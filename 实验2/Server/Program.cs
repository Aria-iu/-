using System.Net.Sockets;

namespace Internet_test2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Test_server_to_client();
           
            
        }

        public void Test_server_to_client() { 
            Server server = new Server();
            server.Run();
            Console.ReadLine();
        }

        public void Test_client_to_server()
        {
            Server server = new Server();
            
        }
    }
}