module app;
import std.algorithm, std.stdio,std.socket;
import dlang_tcp_client;


void main(string[] args) {
    auto addr = new InternetAddress("127.0.0.1", 10240);
    auto tc = new dlang_tcp_client.TCPClient("127.0.0.1",10241,addr);
    tc.start();
}
