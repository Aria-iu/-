using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;


namespace Internet_test
{
    public class ProxyHelper
    {
        public TCP_request? tCP_Request;
        public static Dictionary<string,string>? dict;
        public Socket listen;
        public static string[] address = { "bit.edu.cn" };
        public static string[] black_address = {  };
        public static string[] user_filter = { };

        public void StartProxyServer()
        {
            listen = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);

            IPAddress ips = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipNode = new IPEndPoint(ips, 10345);
            //网络端点：为IP地址和端口号
            //服务端必须绑定一个端口才能listen(),要不然客户端就不知道该连哪个端口了
            listen.Bind(ipNode);
            //监听后，如果客户端这时调用connect()发出连接请求，服务器端就会接收到这个请求
            //listen函数将socket变为被动类型的，等待客户的连接请求。
            listen.Listen(10);
            //服务端有两个socket；这里Accept()返回的相当于是客户端的socket，用于和客户端发送（send）和接收（recv）数据
            dict = new Dictionary<string, string>();

            //多线程连接，每个连接执行一个线程。
            while (true)
            {
                
                Socket socket_commu = listen.Accept();

                Console.WriteLine("进入单独线程");
                Thread thread = new Thread(new ParameterizedThreadStart(Do_single));
                thread.Start(socket_commu);

                /*#region 未实现多线程的代码
                while (true)
                {
                    #region 从源主机获取信息

                    byte[] buffer = new byte[1024 * 1024];
                    //接收数据到缓存buffer,1M缓冲区
                    int num;
                    
                    
                    num = socket_commu.Receive(buffer);
                    
                   

                    string str = Encoding.UTF8.GetString(buffer, 0, num);
                    Console.WriteLine("收到客户端数据 : " + str);
                    string[] args = str.Split(' ', '\n');
                    tCP_Request = new TCP_request();
                    tCP_Request.method = args[0];
                    tCP_Request.url = args[1];
                    tCP_Request.host = args[4];
                    tCP_Request.cookie = "";


                    string url;
                    if (tCP_Request.method == "CONNECT")
                    {
                        //url = "https://"+tCP_Request.url;
                        break;
                    }
                    else
                    {
                        url = tCP_Request.url;
                    }



                    Uri uri = new Uri(url);
                    #endregion

                    #region 实现缓存
                    //实现缓存
                    byte[] bytes;
                    foreach (var key in dict.Keys)
                    {
                        if (tCP_Request.url == key)
                        {
                            bytes = Encoding.UTF8.GetBytes(dict[key]);
                            socket_commu.SendToAsync(bytes, socket_commu.RemoteEndPoint);
                            Console.WriteLine("this work done here!!!!!!!!!!!");
                            goto restart;
                        }
                    }
                    #endregion

                    #region 发送请求，返回给源主机
                    string response = SendHttpRequest(tCP_Request.method,uri);

                    //Console.WriteLine(response);
                    bytes = Encoding.UTF8.GetBytes(response);

                    socket_commu.SendToAsync(bytes, socket_commu.RemoteEndPoint);

                    dict.Add(tCP_Request.url, response);

                    Console.WriteLine("this work done here!!!!!!!!!!!");

                    #endregion

                    break;
                }
                #endregion*/
            }


        }

        static void Do_single(object obj)
        {
            Socket? socket_commu = obj as Socket;

            #region 从源主机获取信息
            byte[] buffer = new byte[1024 * 1024];
            //接收数据到缓存buffer,1M缓冲区
            int num;
            try
            {
                num = socket_commu.Receive(buffer);
            }
            catch (Exception)
            {
                return;
            }

            string str = Encoding.UTF8.GetString(buffer, 0, num);
            Console.WriteLine("收到客户端数据 : " + str);
            string[] args = str.Split(' ', '\n');
            TCP_request tCP_Request;

            tCP_Request = new TCP_request();
            if (args.Length >= 5)
            {

                tCP_Request.method = args[0];
                if (args[1] != null)
                {
                    tCP_Request.url = args[1];
                }
                tCP_Request.host = args[4];
                tCP_Request.cookie = "";
            }
            else {
                socket_commu.Close();
                return;
            }

            string url;
            if (tCP_Request.method == "CONNECT")
            {
                //url = "https://"+tCP_Request.url;
                //Console.WriteLine("HTTPS协议不支持，进程退出");
                byte[] responseBytes = new byte[256];
                socket_commu.SendTo(responseBytes, socket_commu.RemoteEndPoint);
                socket_commu.Close();
                return;
            }
            else
            {
                url = tCP_Request.url;
            }



            Uri uri = new Uri(url);
            #endregion

            #region 实现缓存
            //实现缓存
            byte[] bytes;
            foreach (var key in dict.Keys)
            {
                if (tCP_Request.url == key)
                {
                    bytes = Encoding.UTF8.GetBytes(dict[key]);
                    socket_commu.SendToAsync(bytes, socket_commu.RemoteEndPoint);
                    Console.WriteLine("this work done here!!!!!!!!!!!");
                    return;
                }
            }
            #endregion

            #region 发送请求，返回给源主机

            foreach (var c in user_filter)
            {
                if (socket_commu.LocalEndPoint.ToString().Contains(c)) {
                    Console.WriteLine("用户过滤"+ socket_commu.LocalEndPoint.ToString());
                    socket_commu.Close();
                    return;
                }
            }


            foreach (var s in address) { 
                string ss = uri.ToString();
                if (ss.Contains(s))
                {
                    uri = new Uri("http://jwts.hit.edu.cn/");
                }
            }

            foreach (var s in black_address)
            {
                string ss = uri.ToString();
                if (ss.Contains(s))
                {
                    socket_commu.Close();
                    return;
                }
            }

            string response = SendHttpRequest(tCP_Request.method, uri);

            //Console.WriteLine(response);
            bytes = Encoding.UTF8.GetBytes(response);

            socket_commu.SendTo(bytes, socket_commu.RemoteEndPoint);

            dict.Add(tCP_Request.url, response);

            socket_commu.Close();
            Console.WriteLine("this work done here!!!!!!!!!!!");

            #endregion
        }

        public static string SendHttpRequest(string method,Uri? uri = null, int port = 80)
        {
            // Construct a minimalistic HTTP/1.1 request
            byte[] requestBytes = Encoding.ASCII.GetBytes(@$"{method} {uri.AbsoluteUri} HTTP/1.0
Host: {uri.Host}
if-modified-since:{DateTime.Now}
Connection: Close

");
            //if-modified-since: {DateTime.Now}
            Console.WriteLine("转发报文:");
            foreach (var c in requestBytes) {
                Console.Write((char)c);
            }
            // Create and connect a dual-stack socket
            using Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(uri.Host, port);

            // Send the request.
            // For the tiny amount of data in this example, the first call to Send() will likely deliver the buffer completely,
            // however this is not guaranteed to happen for larger real-life buffers.
            // The best practice is to iterate until all the data is sent.
            int bytesSent = 0;
            while (bytesSent < requestBytes.Length)
            {
                bytesSent += socket.Send(requestBytes, bytesSent, requestBytes.Length - bytesSent, SocketFlags.None);
            }

            // Do minimalistic buffering assuming ASCII response
            byte[] responseBytes = new byte[256];
            char[] responseChars = new char[256];
            string buf = "";
            while (true)
            {
                retry:
                try {
                    int bytesReceived = socket.Receive(responseBytes);

                    // Receiving 0 bytes means EOF has been reached
                    if (bytesReceived == 0) break;

                    // Convert byteCount bytes to ASCII characters using the 'responseChars' buffer as destination
                    int charCount = Encoding.UTF8.GetChars(responseBytes, 0, bytesReceived, responseChars, 0);

                    // Print the contents of the 'responseChars' buffer to Console.Out
                    // Console.Out.Write(responseChars, 0, charCount);

                    for (int i = 0; i < charCount; i++)
                    {
                        buf += responseChars[i];
                    }

                }
                catch (Exception e) {
                    buf = "";
                    goto retry;
                }
            }
            return buf;
            socket.Close();
        }
        
        public void UriTest()
        {
            Uri uriAddress = new Uri("http://www.aiaide.com:8080/Home/index.htm?a=1&b=2#search");
            Console.WriteLine(uriAddress.Scheme);
            Console.WriteLine(uriAddress.Authority);
            Console.WriteLine(uriAddress.Host);
            Console.WriteLine(uriAddress.Port);
            Console.WriteLine(uriAddress.AbsolutePath);
            Console.WriteLine(uriAddress.Query);
            Console.WriteLine(uriAddress.Fragment);
            //通过UriPartial枚举获取指定的部分
            Console.WriteLine(uriAddress.GetLeftPart(UriPartial.Path));
            //获取整个URI
            Console.WriteLine(uriAddress.AbsoluteUri);
        }
    }


    public class TCP_request
    {
        public string method;
        public string url;
        public string host;
        public string cookie;
    }
}
