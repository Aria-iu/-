// See https://aka.ms/new-console-template for more information
using Internet_test;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

ProxyHelper proxyHelper = new ProxyHelper();
proxyHelper.StartProxyServer();
