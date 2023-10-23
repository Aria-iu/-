using System.Net.Sockets;
using System.Net;

namespace Internet_test_2_client
{
    public class Client
    {
        // gbn协议设置
        public static int WINDOW_SIZE = 3;

        // 客户端请求连接本地端口10230
        public static Socket? client_listener;
        public static Socket? client_listener2;
        public static messsage mymessage;

        // 改进数据包结构体，支持双向同时传输
        public struct messsage
        {
            public int ack;
            public int package_num;
            public int length;
            public char s_c_flag; //若s_c_flag为2，为当前客户端发送给服务器的数据包，若s_c_flag为3，为当前服务器发送给客户端的数据包
            public byte[] msg;
        }
        public struct short_revc_messsage
        {
            public int ack;
            public int package_num;
            public int length;
            public char s_c_flag;
        }
        public static uint size = 37;

        public IPAddress iPAddress;
        public IPEndPoint iPEndPoint;
        public IPEndPoint remote_server_EndPoint;
        public IPEndPoint remote_server_EndPoint2;

        public void Run()
        {
            client_listener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            iPAddress = IPAddress.Parse("127.0.0.1");
            iPEndPoint = new IPEndPoint(iPAddress, 10231);
            client_listener.Bind(iPEndPoint);
            remote_server_EndPoint = new IPEndPoint(iPAddress,10230);
            remote_server_EndPoint2 = new IPEndPoint(iPAddress, 10233);
            client_listener2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            iPEndPoint = new IPEndPoint(iPAddress, 10232);
            client_listener2.Bind(iPEndPoint);

            /*Thread thread1 = new Thread(new ThreadStart(handler_send));
            Thread thread2 = new Thread(new ThreadStart(handler_recv));
            thread1.Start();
            thread2.Start();*/

            test_gbn_client_recv();
            //test_gbn_client_send();
        }
        public void handler_send() {
            string filename = "to_send_back.txt";
            while (true)
            {
                if (!send_msg(client_listener2, filename))
                    break;
            }
        }
        public void handler_recv()
        {
            string filename = "result.txt";
            while (true)
            {
                if (!recv_msg(client_listener, filename))
                    break;
            }
        }
        public void test_gbn_client_recv() {
            string filename = "result.txt";
            while (true)
            {
                /*if (!recv_gbn_msg(client_listener, filename))
                    break;*/
                if (!recv_gbn_msg_sr(client_listener, filename))
                    break;
            }
        }
        public void test_gbn_client_send() {
            string filename = "gbn_to_send_back.txt";
            while (true)
            {
                if (!send_gbn_msg(client_listener2, filename))
                    break;
            }
        }

        private bool send_msg(Socket socket, string filename)
        {
            // 用于假设设置超时重发
            Random random = new Random();
            int a1;
            /*定义收发数据包的成功与失败次数*/
            int send_num = 0;
            int err_num = 0;

            byte[] bytes = new byte[size];
            byte[] buffer = new byte[size + 13];

            FileStream fileStream = new FileStream(filename, FileMode.Open);

            int length = fileStream.Read(bytes, 0, (int)size);
            int i = 0;
            short_revc_messsage short_Revc_msg = new short_revc_messsage();
            while (length != 0)
            {
                mymessage.ack = i;
                mymessage.package_num = i;
                mymessage.length = length;
                mymessage.s_c_flag = (char)3;
                mymessage.msg = bytes;
            ack_wrong:
                //发送数据包
                int j;
                byte[] data = new byte[4];
                data = intToBytes(mymessage.ack);
                for (j = 0; j < 4; j++)
                    buffer[j] = data[j];
                data = intToBytes(mymessage.package_num);
                for (j = 0; j < 4; j++)
                    buffer[j + 4] = data[j];
                data = intToBytes(mymessage.length);
                for (j = 0; j < 4; j++)
                    buffer[j + 8] = data[j];
                buffer[12] = (byte)(char)3;
                for (j = 13; j < mymessage.length + 13; j++)
                    buffer[j] = (byte)mymessage.msg[j - 13];


                a1 = random.Next(1, 100);
                // 设置一些数据包不进行发送
                if (a1 < 5)
                {

                }
                else
                {
                    socket.SendTo(buffer, remote_server_EndPoint2);
                }
                send_num++;

                Console.WriteLine("client send package:" + mymessage.package_num);
                // 发送完一个数据包后，停下来等待客户端返回
                byte[] msg_ret = new byte[13];
                // 接受返回包时设置时间为2秒
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);

            wrong_package:
                try
                {
                    socket.Receive(msg_ret);
                }
                catch (Exception e)
                {
                    Console.WriteLine("超时接收");
                    err_num++;
                    goto ack_wrong;
                }


                short_Revc_msg.ack = bytesToInt(msg_ret, 0);
                short_Revc_msg.package_num = bytesToInt(msg_ret, 4);
                // 返回的响应只有头部，msg的length为0
                short_Revc_msg.length = bytesToInt(msg_ret, 8);
                short_Revc_msg.s_c_flag = (char)bytesToChar(msg_ret, 12);
                if (short_Revc_msg.s_c_flag != (char)4)
                {
                    goto wrong_package;
                }

                Console.WriteLine("client reveive package:" + short_Revc_msg.package_num);
                Console.WriteLine("client reveive ack:" + short_Revc_msg.ack);
                if (short_Revc_msg.ack != i + 1)
                {
                    // 若ack号不是下一个，停止，重新发送
                    goto ack_wrong;
                }
                length = fileStream.Read(bytes, 0, (int)size);
                i++;
                // 最后发送一个msg长度为0的包，使得client得知没有更多消息。
                if (length == 0)
                {
                    mymessage.ack = i;
                    mymessage.package_num = i;
                    mymessage.length = length;
                    data = intToBytes(mymessage.ack);
                    for (j = 0; j < 4; j++)
                        buffer[j] = data[j];
                    data = intToBytes(mymessage.package_num);
                    for (j = 0; j < 4; j++)
                        buffer[j + 4] = data[j];
                    data = intToBytes(mymessage.length);
                    for (j = 0; j < 4; j++)
                        buffer[j + 8] = data[j];
                    buffer[12] = (byte)(char)3;
                    socket.SendTo(buffer, remote_server_EndPoint2);
                }
            }
            Console.WriteLine("total send " + send_num + " packages");
            Console.WriteLine("total error " + err_num + " times");

            return false;
        }
        private bool recv_msg(Socket socket,string filename)
        {
            byte[] msg = new byte[50];
            byte[] msg_ret = new byte[13];
            
            StreamWriter file = new StreamWriter(filename);
            int length;
        wrong_package:
            try
            {
                length = socket.Receive(msg);
            }
            catch (Exception e) {
                goto wrong_package;
            }
            short_revc_messsage mymesssage = new short_revc_messsage();
            

            while (length!=0) {
                mymesssage.ack =  bytesToInt(msg,0);
                mymesssage.package_num = bytesToInt(msg, 4);
                mymesssage.length = bytesToInt(msg, 8);
                mymesssage.s_c_flag = bytesToChar(msg, 12);
                if (mymesssage.s_c_flag != (char)1) {
                    goto wrong_package;
                }

                // 最后发送的一个数据包msg长度为0，若是如此，得知结束通讯。
                if (mymesssage.length == 0) { break; }

                int j = 13;
                for (; j < mymesssage.length + 13; j++) {
                    Console.Write((char)msg[j]);
                    file.Write((char)msg[j]);
                }
                file.FlushAsync();
                // 发送返回ACK信息
                byte[] data = new byte[4];
                data = intToBytes(mymesssage.ack+1);
                for (j = 0; j < 4; j++)
                    msg_ret[j] = data[j];
                data = intToBytes(mymesssage.package_num);
                for (j = 0; j < 4; j++)
                    msg_ret[j + 4] = data[j];
                data = intToBytes(0);
                for (j = 0; j < 4; j++)
                    msg_ret[j + 8] = data[j];
                msg_ret[12] = (byte)(char)0;
                socket.SendTo(msg_ret,remote_server_EndPoint);
                // 返回响应后，继续等待下一个
                retry:
                try
                {
                    length = socket.Receive(msg);
                }catch (SocketException e)
                {
                    goto retry;
                }
            }
            return false;
        }


        private bool recv_gbn_msg(Socket socket, string filename)
        {
            byte[] msg = new byte[50];
            byte[] msg_ret = new byte[13];
            byte[] buffer_receive = new byte[1024]; //接收缓存 1kb
            int[] cons = new int[128];

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);

            StreamWriter file = new StreamWriter(filename);
            short_revc_messsage mymesssage = new short_revc_messsage(); //接受解析

            int i = 0;
            int order = 0;//记录接受到的文件顺序
            int length = 1;
            while (length != 0)
            {

                i = 0;
                do
                {
                    try
                    {
                        socket.Receive(msg);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("未能接收完全" + WINDOW_SIZE + "个分组");
                        goto miss_error;
                    }

                    #region 解析接收的数据包
                    mymesssage.ack = bytesToInt(msg, 0);
                    mymesssage.package_num = bytesToInt(msg, 4);
                    mymesssage.length = bytesToInt(msg, 8);
                    mymesssage.s_c_flag = bytesToChar(msg, 12);
                    #endregion
                    Console.WriteLine("client reveive package:" + mymesssage.package_num);
                    length = mymesssage.length;
                    if (length == 0) { break; }
                    if (mymesssage.package_num == order)
                    {
                        if (cons[order] == 1)
                        {
                            write_buffer(buffer_receive, mymesssage.package_num, msg, 13);
                        }
                        else
                        {
                            write_buffer(buffer_receive, mymesssage.package_num, msg, 13);
                            cons[mymesssage.package_num] = 1;
                        }
                        order++;
                    }
                    else
                    {
                        if (cons[order] == 1)
                        {
                        }
                        else
                        {
                            write_buffer(buffer_receive, mymesssage.package_num, msg, 13);
                            cons[mymesssage.package_num] = 1;
                        }
                    }
                    i++;

                } while (i < WINDOW_SIZE);

            miss_error:
                // 发送返回ACK信息
                int j;
                byte[] data = new byte[4];
                for (j = 0; j < 128; j++)
                {
                    if (cons[j] == 1)
                    {
                        order = j + 1;
                    }
                    else
                    {
                        break;
                    }
                }
                data = intToBytes(order);
                for (j = 0; j < 4; j++)
                    msg_ret[j] = data[j];
                data = intToBytes(mymesssage.package_num);
                for (j = 0; j < 4; j++)
                    msg_ret[j + 4] = data[j];
                data = intToBytes(0);
                for (j = 0; j < 4; j++)
                    msg_ret[j + 8] = data[j];
                msg_ret[12] = (byte)(char)0;
                socket.SendTo(msg_ret, remote_server_EndPoint);

            }

            int to_recv_length = buffer_receive.Length;
            for (int t = 0; t < to_recv_length; t++)
            {
                file.Write((char)buffer_receive[t]);
            }
            file.Flush();
            file.Close();
            return false;
        }
        private bool send_gbn_msg(Socket socket, string filename)
        {
            int lastsend = 0;     // 最后发送出的package_num
            int waitngForack = 0; // 接收的要发送的ack_num

            // 用于假设设置超时重发
            Random random = new Random();
            /*定义收发数据包的成功与失败次数*/
            int send_num = 0;
            int err_num = 0;


            byte[] bytes = new byte[size];
            byte[] buffer = new byte[size + 13];

            #region 将文件内容读入fileBytes
            FileStream fileStream = new FileStream(filename, FileMode.Open);
            byte[] fileBytes = new byte[1024*2];
            int totallength = fileStream.Read(fileBytes);
            #endregion

            bytes = copy_to(fileBytes, 0, totallength);
            int length = bytes.Length;
            int i = 0;
            short_revc_messsage short_Revc_msg = new short_revc_messsage();

            while (length != 0)
            {
                while (lastsend - waitngForack < WINDOW_SIZE)
                {
                    #region 准备数据
                    //bytes = copy_to(fileBytes, i, totallength);
                    mymessage.ack = i;
                    mymessage.package_num = i;
                    mymessage.length = length;
                    mymessage.s_c_flag = (char)3;
                    mymessage.msg = bytes;

                    int j;
                    byte[] data = new byte[4];
                    data = intToBytes(mymessage.ack);
                    for (j = 0; j < 4; j++)
                        buffer[j] = data[j];
                    data = intToBytes(mymessage.package_num);
                    for (j = 0; j < 4; j++)
                        buffer[j + 4] = data[j];
                    data = intToBytes(mymessage.length);
                    for (j = 0; j < 4; j++)
                        buffer[j + 8] = data[j];
                    buffer[12] = (byte)(char)3;
                    for (j = 13; j < mymessage.length + 13; j++)
                        buffer[j] = (byte)mymessage.msg[j - 13];
                    #endregion
                    #region 发送数据包
                    int a1 = random.Next(1, 100);
                    // 设置一些数据包不进行发送
                    if (a1 > 80)
                    { }
                    else
                    { socket.SendTo(buffer, remote_server_EndPoint2); }
                    send_num++;
                    Console.WriteLine("client send package:" + mymessage.package_num);
                    i++;
                    lastsend = i;
                    #endregion
                    bytes = copy_to(fileBytes, i, totallength);
                    if (bytes == null)
                    {
                        length = 0;
                    }
                    else
                    {
                        length = bytes.Length;
                    }
                    #region 若读取完
                    // 最后发送一个msg长度为0的包，使得client得知没有更多消息。
                    if (length == 0)
                    {
                        mymessage.ack = i;
                        mymessage.package_num = i;
                        mymessage.length = length;
                        data = intToBytes(mymessage.ack);
                        for (j = 0; j < 4; j++)
                            buffer[j] = data[j];
                        data = intToBytes(mymessage.package_num);
                        for (j = 0; j < 4; j++)
                            buffer[j + 4] = data[j];
                        data = intToBytes(mymessage.length);
                        for (j = 0; j < 4; j++)
                            buffer[j + 8] = data[j];
                        buffer[12] = (byte)(char)3;
                        socket.SendTo(buffer, remote_server_EndPoint2);
                        break;
                    }
                    #endregion
                }
                #region 等待返回
                // 发送完多个数据包后，停下来等待客户端返回
                byte[] msg_ret = new byte[13];
                // 接受返回包时设置时间为2秒
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);
            retry:
                try
                {
                    socket.Receive(msg_ret);
                }
                catch (Exception e)
                {
                    Console.WriteLine("接收超时");
                    goto retry;
                }
                #endregion
                #region 解析接收的数据包
                short_Revc_msg.ack = bytesToInt(msg_ret, 0);
                short_Revc_msg.package_num = bytesToInt(msg_ret, 4);
                // 返回的响应只有头部，msg的length为0
                short_Revc_msg.length = bytesToInt(msg_ret, 8);
                short_Revc_msg.s_c_flag = (char)bytesToChar(msg_ret, 12);
                #endregion
                if (short_Revc_msg.s_c_flag != (char)4)
                {
                    goto retry;
                }
                Console.WriteLine("client reveive package:" + short_Revc_msg.package_num);
                Console.WriteLine("client reveive ack:" + short_Revc_msg.ack);
                if (short_Revc_msg.ack != i)
                {
                    // 若ack号不是下一组，停止，重新发送
                    i = short_Revc_msg.ack;
                }
                lastsend = i;
                waitngForack = i;
            }
            return false;
        }
        private bool recv_gbn_msg_sr(Socket socket, string filename)
        {
            byte[] msg = new byte[50];
            byte[] msg_ret = new byte[13];
            byte[] buffer_receive = new byte[1024]; //接收缓存 1kb
            int[] cons = new int[128];

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 2000);

            StreamWriter file = new StreamWriter(filename);
            short_revc_messsage mymesssage = new short_revc_messsage(); //接受解析

            int i = 0;
            int order = 0;//记录接受到的文件顺序
            int length = 1;
            int to_reveive = 0;
            while (length != 0)
            {

                i = 0;
                do
                {
                    try
                    {
                        socket.Receive(msg);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("未能接收完全" + WINDOW_SIZE + "个分组");
                        goto miss_error;
                    }

                    #region 解析接收的数据包
                    mymesssage.ack = bytesToInt(msg, 0);
                    mymesssage.package_num = bytesToInt(msg, 4);
                    mymesssage.length = bytesToInt(msg, 8);
                    mymesssage.s_c_flag = bytesToChar(msg, 12);
                    #endregion
                    Console.WriteLine("client reveive package:" + mymesssage.package_num);
                    length = mymesssage.length;
                    if (length == 0) { break; }
                    if (mymesssage.package_num == order)
                    {
                        if (cons[order] == 1)
                        {
                            write_buffer(buffer_receive, mymesssage.package_num, msg, 13);
                        }
                        else
                        {
                            write_buffer(buffer_receive, mymesssage.package_num, msg, 13);
                            cons[mymesssage.package_num] = 1;
                        }
                        order++;
                    }
                    else
                    {
                        if (cons[order] == 1)
                        {
                        }
                        else
                        {
                            write_buffer(buffer_receive, mymesssage.package_num, msg, 13);
                            cons[mymesssage.package_num] = 1;
                        }
                    }
                    i++;

                } while (i < WINDOW_SIZE);


            miss_error:
                to_reveive += 3;
                // 发送返回ACK信息
                int j;
                byte[] data = new byte[4];
                for (j = 0; j < 128; j++)
                {
                    if (cons[j] == 1)
                    {
                        order = j + 1;
                    }
                    else
                    {
                        break;
                    }
                }


                while (order != to_reveive)
                {
                    data = intToBytes(order);
                    for (j = 0; j < 4; j++)
                        msg_ret[j] = data[j];
                    data = intToBytes(mymesssage.package_num);
                    for (j = 0; j < 4; j++)
                        msg_ret[j + 4] = data[j];
                    data = intToBytes(0);
                    for (j = 0; j < 4; j++)
                        msg_ret[j + 8] = data[j];
                    msg_ret[12] = (byte)(char)0;
                    socket.SendTo(msg_ret, remote_server_EndPoint);

                    // 等待单独返回数据包
                    int flag = 0;
                retry:
                    try
                    {
                        socket.Receive(msg);
                        flag = 0;
                    }
                    catch (Exception e)
                    {
                        flag++;
                        if (flag > 5)
                        {
                            goto end;
                        }
                        goto retry;
                    }
                    #region 解析接收的数据包
                    mymesssage.ack = bytesToInt(msg, 0);
                    mymesssage.package_num = bytesToInt(msg, 4);
                    mymesssage.length = bytesToInt(msg, 8);
                    mymesssage.s_c_flag = bytesToChar(msg, 12);
                    #endregion
                    Console.WriteLine("client add in SR reveive package:" + mymesssage.package_num);
                    if (mymesssage.package_num == order)
                    {
                        if (cons[order] == 1)
                        {
                            write_buffer(buffer_receive, mymesssage.package_num, msg, 13);
                        }
                        else
                        {
                            write_buffer(buffer_receive, mymesssage.package_num, msg, 13);
                            cons[mymesssage.package_num] = 1;
                        }
                        order++;
                    }
                    else
                    {
                        if (cons[order] == 1)
                        {
                        }
                        else
                        {
                            write_buffer(buffer_receive, mymesssage.package_num, msg, 13);
                            cons[mymesssage.package_num] = 1;
                        }
                    }
                    // 发送返回ACK信息
                    data = new byte[4];
                    for (j = 0; j < 128; j++)
                    {
                        if (cons[j] == 1)
                        {
                            order = j + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (order == to_reveive)
                    {
                        #region 单独发送分组结束，返回正常ack
                        data = intToBytes(order);
                        for (j = 0; j < 4; j++)
                            msg_ret[j] = data[j];
                        data = intToBytes(mymesssage.package_num);
                        for (j = 0; j < 4; j++)
                            msg_ret[j + 4] = data[j];
                        data = intToBytes(0);
                        for (j = 0; j < 4; j++)
                            msg_ret[j + 8] = data[j];
                        msg_ret[12] = (byte)(char)0;
                        socket.SendTo(msg_ret, remote_server_EndPoint);
                        #endregion
                        break;
                    }
                }

            }
        end:
            int to_recv_length = buffer_receive.Length;
            for (int t = 0; t < to_recv_length; t++)
            {
                file.Write((char)buffer_receive[t]);
            }
            file.Flush();
            file.Close();
            return false;
        }

        public void write_buffer(byte[] buffer,int num, byte[] source,int offset) 
        {
            int index = (int)(num * (int)size);
            int i = 0;
            for (; i < size; i++) {
                buffer[index] = source[i+offset];
                index++;
            }
        }
        private byte[] copy_to(byte[] source, int num, int length)
        {
            if (num * size >= length)
            {
                return null;
            }
            byte[] temp = new byte[size];
            int i = (int)(num * size);
            int index = 0;
            for (; i < length && index < size; i++)
            {
                temp[index] = source[i];
                index++;
            }
            return temp;
        }
        public int bytesToInt(byte[] src, int offset)
        {
            int value;
            value = (int)((src[offset] & 0xFF)
                    | ((src[offset + 1] & 0xFF) << 8)
                    | ((src[offset + 2] & 0xFF) << 16)
                    | ((src[offset + 3] & 0xFF) << 24));
            return value;
        }
        public byte[] intToBytes(int value)
        {
            byte[] src = new byte[4];
            src[3] = (byte)((value >> 24) & 0xFF);
            src[2] = (byte)((value >> 16) & 0xFF);
            src[1] = (byte)((value >> 8) & 0xFF);
            src[0] = (byte)(value & 0xFF);
            return src;
        }
        public char bytesToChar(byte[] src, int offset)
        {
            char value;
            value = (char)(src[offset] & 0xFF);
            return value;
        }
    }
}
