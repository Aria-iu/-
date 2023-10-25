
import std.stdio;
import std.socket;

class TCPServer{
    string host;
    ushort port;
    
    @disable this();

    this(string host, ushort port){    
        this.host = host;
        this.port = port;
    }

    // echo function
    void start(){
        auto tcps = new TcpSocket(AddressFamily.INET);

        auto addr = new InternetAddress(host, port);

        tcps.bind(addr);

        tcps.listen(10);
        writeln("Listening at ", addr);

        auto s = tcps.accept();
        writeln("get connect!!!");
        writeln(s.localAddress);
        while(true){
            char[1024] data;
            long nbytes = s.receive(data);
            writeln(nbytes);
            writeln(data[0..nbytes]);
            writeln("after reveice");
            s.send(data);
        }
    }
}


/+ client
module client;


import std.socket, std.conv;

int main(){

    
    auto tcps = new TcpSocket(AddressFamily.INET);

    auto addr = new InternetAddress("127.0.0.1", 8888);
    
    tcps.connect(addr);
    tcps.sendTo("Selam şğüçö");
    tcps.close();
    return 0;

}

+/
