
import std.algorithm, std.stdio,std.socket;
import dlang_tcp_server;

static void Get_Test_Prctocol(Protocol *proto)
{
    writeln("About protocol TCP:");
    if (proto.getProtocolByType(ProtocolType.TCP))
    {
        writefln("  Name: %s", proto.name);
        foreach (string s; proto.aliases)
            writefln("  Alias: %s", s);
    }
    else
        writeln("  No information found");
}

static void Get_Service_(Service *serv)
{
    writeln("About service epmap:");
    if (serv.getServiceByName("epmap", "tcp"))
    {
        writefln("  Service: %s", serv.name);
        writefln("  Port: %d", serv.port);
        writefln("  Protocol: %s", serv.protocolName);
        foreach (string s; serv.aliases)
             writefln("  Alias: %s", s);
    }
    else
    writefln("  No service for epmap.");
}

static void Test_SocketPair()
{
    import std.datetime;
    import std.typecons;
    auto pair = socketPair();
    scope(exit) foreach (s; pair) s.close();
    /+
    // Set a receive timeout, and then wait at one end of
    // the socket pair, knowing that no data will arrive.
    pair[0].setOption(SocketOptionLevel.SOCKET,
        SocketOption.RCVTIMEO, dur!"seconds"(10));
    //auto sw = StopWatch(Yes.autoStart);
    ubyte[1] buffer;
    pair[0].receive(buffer);
    //writefln("Waited %s ms until the socket timed out.",
    //    sw.peek.msecs);
    writefln("Waited until the socket timed out.");
    +/
    pair[0].setOption(SocketOptionLevel.SOCKET,
        SocketOption.RCVTIMEO, dur!"seconds"(10));
    immutable ubyte[4] data = [1, 2, 3, 4];
    pair[0].send(data[]);
    auto buf = new ubyte[data.length];
    pair[1].receive(buf);
    writeln(buf); // data
    writefln("Waited until the socket timed out.");
}

static void Test_Socket_()
{
    
}

void main(string[] args) {
/+ this is for some socket test bench------------------------------------------
/*
    // get TCP Protocol
    auto proto = new Protocol;
    Get_Test_Prctocol(&proto);
    // get Service
    auto serv = new Service;
    Get_Service_(&serv);

    InternetHost ih = new InternetHost;
    ih.getHostByAddr(0x7F_00_00_01);
    writeln(ih.addrList[0]); // 0x7F_00_00_01
    ih.getHostByAddr("127.0.0.1");
    writeln(ih.addrList[0]); // 0x7F_00_00_01

    auto addr1 = new InternetAddress("127.0.0.1", 80);
    auto addr2 = new InternetAddress("127.0.0.2", 80);
    writeln(addr1); // addr1
    //writeln(addr1 != addr2);
*/
/*
    // Get Socket
    import std.datetime;
    import std.typecons;
    auto s = new TcpSocket();
    auto addr = new InternetAddress("localhost", 6454);
    s.bind(addr);
    s.setOption(SocketOptionLevel.SOCKET,SocketOption.RCVTIMEO, dur!"seconds"(10));
    // auto to   = new InternetAddress("localhost", 6455);
    s.listen(10);
    ubyte[ArtNetDMX.sizeof] bytes; //allocate memory on stack for ArtNetDMX struct
    ArtNetDMX *packet = cast(ArtNetDMX *)bytes.ptr;
    packet.data[0] = 255;          //work with ArtNetDMX struct

    while(true)
    {
        int len = 0;
        auto client = s.accept();
    }
    */
 end test socket---------------------------------------------------------------+/
    auto ts = new dlang_tcp_server.TCPServer("127.0.0.1",10240);
    
    ts.start();
    
    
}

