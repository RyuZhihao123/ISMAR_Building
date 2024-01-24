using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;


//----------网络通信数据包(勿动)-------------//
[System.Serializable]
public class OutUnity
{
    public float[] pts;
}

[System.Serializable]
public class OutTypeNetwork
{
    public int type;  // 整个stroke的类型
}


//-------------------------------------------//
//----------网络通信处理类(TcpConnector)-------------//
//-------------------------------------------//
public class TcpConnector
{
    private Socket tcpSocket;


    // [1] 初始化：连接到python服务器
    public void ConnectedToServer()
    {
        //创建socket
        tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        //连接服务器
        tcpSocket.Connect(IPAddress.Parse("127.0.0.1"), 10086);
        Debug.Log("连接服务器");
    }

    // [2] 发送包：发送一个stroke给python服务器
    public void SendToServer(List<Vector3> stroke)
    {
        OutUnity msg = new OutUnity();
        msg.pts = new float[stroke.Count * 3];
        for (int i = 0; i < stroke.Count; ++i)
        {
            msg.pts[3 * i + 0] = stroke[i].x;
            msg.pts[3 * i + 1] = stroke[i].y;
            msg.pts[3 * i + 2] = stroke[i].z;
        }

        string json = JsonUtility.ToJson(msg);

        //发送消息
        tcpSocket.Send(ASCIIEncoding.UTF8.GetBytes(json));
    }

    // [3] 接收包：
    public StrokeType RecieveLSTMmsg()
    {
        byte[] bt = new byte[100000];
        int messgeLength = tcpSocket.Receive(bt);
        Debug.Log(ASCIIEncoding.UTF8.GetString(bt));
        string jsonText = ASCIIEncoding.UTF8.GetString(bt);

        string type = jsonText.Split(new char[2] { ' ', '}' })[1];
        Debug.Log(jsonText.Split(new char[2] { ' ', '}' })[1]);
        Debug.Log("预测的线条类型: "+ type);
        return (StrokeType)int.Parse(type);
    }

    public List<PointType> RecievePointNetmsg(int ptsCount)
    {
        byte[] bt = new byte[100000];
        int messgeLength = tcpSocket.Receive(bt);

        string jsonText = ASCIIEncoding.UTF8.GetString(bt);

        jsonText = jsonText.Split(new char[2] { '[', ']' })[1]; 

        Debug.Log(jsonText);

        string[] numbers = jsonText.Split(new char[1] { ',' });
        
        List<PointType> labels = new List<PointType>();

        string result = "";
        for (int i = 0; i < ptsCount; ++i)
        {
            int label = Mathf.Min(int.Parse(numbers[i]), 2);
            result += label + " ";

            labels.Add((PointType)label);
        }
        Debug.Log(result);
        return labels;
    }


    public static System.Diagnostics.Process CreateCmdProcess(string cmd, string args, string workdir = null)
    {
        var pStartInfo = new System.Diagnostics.ProcessStartInfo(cmd);
        pStartInfo.Arguments = args;
        pStartInfo.CreateNoWindow = false;
        pStartInfo.UseShellExecute = false;
        pStartInfo.RedirectStandardError = true;
        pStartInfo.RedirectStandardInput = true;
        pStartInfo.RedirectStandardOutput = true;
        pStartInfo.StandardErrorEncoding = System.Text.UTF8Encoding.UTF8;
        pStartInfo.StandardOutputEncoding = System.Text.UTF8Encoding.UTF8;
        if (!string.IsNullOrEmpty(workdir))
            pStartInfo.WorkingDirectory = workdir;
        return System.Diagnostics.Process.Start(pStartInfo);
    }
}