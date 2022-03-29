using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class Server
{
    private static Socket tcpSocket;
    private static Socket udpSocket;
    private static Socket handlerOne;
    private static Socket handlerTwo;
    private static EndPoint remoteUDPClient;
    private static EndPoint clientOneUDPID;
    private static EndPoint clientTwoUDPID;
    private static byte[] TCP_inBuffer = new byte[32];
    private static byte[] TCP_outBuffer = new byte[32];
    private static byte[] UDP_inBuffer = new byte[12];
    private static byte[] UDP_outBuffer = new byte[12];
    private static float[] clientOnePos = new float[12];
    private static float[] clientTwoPos = new float[12];
    private static int recv = 0;

    public static void StartServer()
    {
        try
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");

            Console.WriteLine("Server name: {0} | IP: {1}", Dns.GetHostEntry(Dns.GetHostName()).HostName, ip);

            IPEndPoint localTCPEP = new IPEndPoint(ip, 11111);
            IPEndPoint localUDPEP = new IPEndPoint(ip, 11112);

            tcpSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            udpSocket = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            IPEndPoint udpClient = new IPEndPoint(IPAddress.Any, 0);
            remoteUDPClient = (EndPoint)udpClient;

            tcpSocket.Bind(localTCPEP);
            udpSocket.Bind(localUDPEP);

            tcpSocket.Listen(10);
            Console.WriteLine("Waiting for connection...");

            handlerOne = tcpSocket.Accept();
            Console.WriteLine("~ Connected! ~");
            IPEndPoint clientOneEP = (IPEndPoint)handlerOne.RemoteEndPoint;
            Console.WriteLine("TCP: Client {0} connected at port {1}", clientOneEP.Address, clientOneEP.Port);
            int rec = udpSocket.ReceiveFrom(UDP_inBuffer, ref remoteUDPClient);
            Console.WriteLine("UDP: Client {0} at {1}", Encoding.ASCII.GetString(UDP_inBuffer, 0, rec), remoteUDPClient.ToString());
            clientOneUDPID = remoteUDPClient;
            Console.WriteLine("--------------------------------------------");
            handlerOne.Send(Encoding.UTF8.GetBytes("1"));

            handlerTwo = tcpSocket.Accept();
            Console.WriteLine("~ Connected! ~");
            IPEndPoint clientTwoEP = (IPEndPoint)handlerTwo.RemoteEndPoint;
            Console.WriteLine("TCP: Client {0} connected at port {1}", clientTwoEP.Address, clientTwoEP.Port);
            rec = udpSocket.ReceiveFrom(UDP_inBuffer, ref remoteUDPClient);
            Console.WriteLine("UDP: Client {0} at {1}", Encoding.ASCII.GetString(UDP_inBuffer, 0, rec), remoteUDPClient.ToString());
            clientTwoUDPID = remoteUDPClient;
            Console.WriteLine("--------------------------------------------");
            handlerTwo.Send(Encoding.UTF8.GetBytes("2"));

            bool cos = false, cts = false;
            int recv = handlerOne.Receive(TCP_inBuffer);
            cos = Encoding.UTF8.GetString(TCP_inBuffer, 0, recv) == "Ready";
            recv = handlerTwo.Receive(TCP_inBuffer);
            cts = Encoding.UTF8.GetString(TCP_inBuffer, 0, recv) == "Ready";

            if (cos && cts)
            {
                handlerOne.Send(Encoding.UTF8.GetBytes("StartSession"));
                handlerTwo.Send(Encoding.UTF8.GetBytes("StartSession"));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected Exception: {0}", e.ToString());
        }
    }

    public static void ServerUpdate()
    {
        // Game Updates //
        if (udpSocket.Available > 0)
        {
            try
            {
                recv = udpSocket.ReceiveFrom(UDP_inBuffer, ref remoteUDPClient);
            }
            catch { }

            if (remoteUDPClient.ToString() == clientOneUDPID.ToString()) //Receive Client 1 Position > Send to Client 2
            {
                Buffer.BlockCopy(UDP_inBuffer, 0, clientOnePos, 0, recv);

                Console.WriteLine("Received {0},{1},{2} from {3} ... Sent {0},{1},{2} to {4} ...", 
                    clientOnePos[0], clientOnePos[1], clientOnePos[2], clientOneUDPID, clientTwoUDPID);

                Buffer.BlockCopy(clientOnePos, 0, UDP_outBuffer, 0, UDP_outBuffer.Length);
                udpSocket.SendTo(UDP_outBuffer, clientTwoUDPID);
            }
            else if (remoteUDPClient.ToString() == clientTwoUDPID.ToString()) //Receive Client 2 Position > Send to Client 1
            {
                Buffer.BlockCopy(UDP_inBuffer, 0, clientTwoPos, 0, recv);

                Console.WriteLine("Received {0},{1},{2} from {3} ... Sent {0},{1},{2} to {4} ...",
                    clientTwoPos[0], clientTwoPos[1], clientTwoPos[2], clientTwoUDPID, clientOneUDPID);

                Buffer.BlockCopy(clientTwoPos, 0, UDP_outBuffer, 0, UDP_outBuffer.Length);
                udpSocket.SendTo(UDP_outBuffer, clientOneUDPID);
            }
        }

        // Chat Updates //
        if (handlerOne.Available > 0)
        {
            try
            {
                recv = handlerOne.Receive(TCP_inBuffer);
            }
            catch { }

            if (recv > 0)
            {
                IPEndPoint clientOneEP = (IPEndPoint)handlerOne.RemoteEndPoint;
                Console.WriteLine("Received message \"{0}\" from {1} ...", Encoding.UTF8.GetString(TCP_inBuffer, 0, recv), clientOneEP.Address);


            }
        }
    }

    public static void Main(String[] args)
    {
        StartServer();

        tcpSocket.Blocking = false;
        handlerOne.Blocking = false;
        handlerTwo.Blocking = false;
        udpSocket.Blocking = false;

        while (true)
        {
            ServerUpdate();
        }
    }
}
