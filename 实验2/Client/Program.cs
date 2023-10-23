namespace Internet_test_2_client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Program program = new Program();
            program.Test_server_to_client();
        }

        public void Test_server_to_client()
        {
            Client client = new Client();
            client.Run();
        }
        
    }
}