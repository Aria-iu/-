module dlang_tcp_client;

import std.stdio;
import std.socket;

class TCPClient{
    string host;
    ushort port;
    InternetAddress to;
    @disable this();

    this(string host, ushort port,InternetAddress to){    
        this.host = host;
        this.port = port;
        this.to   = to;
    }
    
    void start()
    {
        auto tcps = new TcpSocket(AddressFamily.INET);

        auto addr = new InternetAddress(host, port);

        tcps.bind(addr);
        tcps.connect(to);
        char[] msg = cast(char[])"time that go";
        while(true){
            ulong len = msg.length;
            tcps.send(msg[0..len]);
            auto nbytes = tcps.receive(msg);
            writeln(nbytes, "-", msg[0..nbytes]);
            msg = cast(char[])readln();
        }
    }
}

