using UnityEngine;
using UnityEngine.UI;

// Non-Unity Libraries
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

public class Client : MonoBehaviour
{
    private bool IPPass = false;
    private bool readyState = false;
    private bool SessionPass = false;
    private int clientNum;
    private string serverIP = "";
    private static Socket tcpSocket;
    private static Socket udpSocket;
    private static IPEndPoint remoteTCPEP;
    private static EndPoint remoteUDPEP;
    private static byte[] TCP_inBuffer = new byte[32];
    private static byte[] TCP_outBuffer = new byte[32];
    private static byte[] UDP_inBuffer = new byte[12];
    private static byte[] UDP_outBuffer = new byte[12];
    private static float[] playerPosition = new float[12];
    private static float[] otherPosition = new float[12];
    private static int recv = 0;
    private float elapsedTime;

    // Start Screen
    public GameObject StartScreen;
    public GameObject InputField;
    public GameObject IPError;
    // Waiting Screen
    public GameObject WaitingScreen;
    public GameObject WaitingText;
    // Prefabs & Players
    public GameObject BluePrefab;
    public GameObject GreenPrefab;
    public GameObject playerCube;
    public GameObject otherCube;

    public void GetServerIP()
    {
        serverIP = InputField.GetComponent<Text>().text;

        StartClient();
    }

    public void StartClient()
    {
        try
        {
            IPAddress ip = IPAddress.Parse(serverIP);

            remoteTCPEP = new IPEndPoint(ip, 11111);
            remoteUDPEP = (EndPoint)new IPEndPoint(ip, 11112);

            tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            Debug.Log("Connecting to server...");

            tcpSocket.Connect(remoteTCPEP);

            Debug.Log($"Connected to server IP: {tcpSocket.RemoteEndPoint.ToString()}");
            Debug.Log("--------------------------------------------");

            udpSocket.SendTo(Encoding.UTF8.GetBytes("connected"), remoteUDPEP);
            int recv = tcpSocket.Receive(TCP_inBuffer);
            clientNum = int.Parse(Encoding.UTF8.GetString(TCP_inBuffer, 0, recv));

            tcpSocket.Blocking = false;
            udpSocket.Blocking = false;

            StartScreen.SetActive(false);
            WaitingScreen.SetActive(true);
            IPPass = true;
        }
        catch (ArgumentNullException anexc)
        {
            IPPass = false;
            IPError.GetComponent<Text>().text = "~ No IP Address Entered ~";
            Debug.Log($"ArgumentNullException: {anexc.ToString()}");
        }
        catch (FormatException fexc)
        {
            IPPass = false;
            IPError.GetComponent<Text>().text = "~ Incorect IP Address Entered ~";
            Debug.Log($"FormatException: {fexc.ToString()}");
        }
        catch (SocketException se)
        {
            IPPass = false;
            IPError.GetComponent<Text>().text = "~ Socket Exception Encountered ~";
            Debug.Log($"SocketException: {se.ToString()}");
        }
        catch (Exception e)
        {
            IPPass = false;
            Debug.Log($"Unexpected Exception: {e.ToString()}");
        }
    }

    public void SetupSession()
    {
        try
        {
            if (clientNum == 1)
            {
                WaitingText.GetComponent<Text>().text = $"Connected to server IP: {tcpSocket.RemoteEndPoint.ToString()} --------------------------------------------                Waiting for Client 2";
            }
            else if (clientNum == 2)
            {
                WaitingText.GetComponent<Text>().text = $"Connected to server IP: {tcpSocket.RemoteEndPoint.ToString()} --------------------------------------------                Waiting for Client 1";
            }

            if (!readyState)
            {
                tcpSocket.Send(Encoding.UTF8.GetBytes("Ready"));
                readyState = true;
            }

            if (tcpSocket.Available > 0)
            {
                int recv = tcpSocket.Receive(TCP_inBuffer);
                if (Encoding.UTF8.GetString(TCP_inBuffer, 0, recv) == "StartSession")
                {
                    if (clientNum == 1)
                    {
                        Vector3 spawn = new Vector3(0f, 0.5f, 0f);
                        playerCube = Instantiate(BluePrefab, spawn, Quaternion.identity) as GameObject;
                        playerCube.AddComponent<Cube>();
                        otherCube = Instantiate(GreenPrefab, spawn, Quaternion.identity) as GameObject;
                    }
                    else if (clientNum == 2)
                    {
                        Vector3 spawn = new Vector3(0f, 0.5f, 0f);
                        playerCube = Instantiate(GreenPrefab, spawn, Quaternion.identity) as GameObject;
                        playerCube.AddComponent<Cube>();
                        otherCube = Instantiate(BluePrefab, spawn, Quaternion.identity) as GameObject;
                    }

                    WaitingScreen.SetActive(false);
                    IPPass = false;
                }
            }
        }
        catch (ArgumentNullException anexc)
        {
            Debug.Log($"ArgumentNullException: {anexc.ToString()}");
        }
        catch (FormatException fexc)
        {
            Debug.Log($"FormatException: {fexc.ToString()}");
        }
        catch (SocketException se)
        {
            Debug.Log($"SocketException: {se.ToString()}");
        }
        catch (Exception e)
        {
            Debug.Log($"Unexpected Exception: {e.ToString()}");
        }

        SessionPass = true;
    }

    public void ClientUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UDP_outBuffer = Encoding.UTF8.GetBytes("Shutdown");
            udpSocket.SendTo(UDP_outBuffer, remoteUDPEP);

            udpSocket.Shutdown(SocketShutdown.Both);
            tcpSocket.Shutdown(SocketShutdown.Both);
            udpSocket.Close();
            tcpSocket.Close();

            Application.Quit();
        }
        else
        {
            // Game Update //
            // Receive Position Data
            if (udpSocket.Available > 0)
            {
                try
                {
                    recv = udpSocket.ReceiveFrom(UDP_inBuffer, ref remoteUDPEP);
                }
                catch { }

                if (Encoding.UTF8.GetString(UDP_inBuffer, 0, recv) == "Shutdown")
                {
                    udpSocket.Shutdown(SocketShutdown.Both);
                    tcpSocket.Shutdown(SocketShutdown.Both);    
                    udpSocket.Close();
                    tcpSocket.Close();

                    Application.Quit();
                }
                else
                {
                    Buffer.BlockCopy(UDP_inBuffer, 0, otherPosition, 0, recv);

                    if (recv > 0)
                    {
                        otherCube.GetComponent<Transform>().position = new Vector3(otherPosition[0], otherPosition[1], otherPosition[3]);
                    }
                }
            }

            // Send Position Data
            elapsedTime += Time.deltaTime;

            playerPosition = new float[] {playerCube.GetComponent<Transform>().position.x, playerCube.GetComponent<Transform>().position.y, playerCube.GetComponent<Transform>().position.z};

            if ((elapsedTime > 0.025f)) //Interval set at 25ms
            {
                Buffer.BlockCopy(playerPosition, 0, UDP_outBuffer, 0, UDP_outBuffer.Length);
                udpSocket.SendTo(UDP_outBuffer, remoteUDPEP);

                elapsedTime = 0f;
            }

            // Chat Update //
            // Receive Chat Data
            // Send Chat Data
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        StartScreen.SetActive(true);
        WaitingScreen.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (IPPass && WaitingScreen.activeInHierarchy)
        {
            SetupSession();
        }

        if (SessionPass)
        {
            ClientUpdate();
        }
    }
}
