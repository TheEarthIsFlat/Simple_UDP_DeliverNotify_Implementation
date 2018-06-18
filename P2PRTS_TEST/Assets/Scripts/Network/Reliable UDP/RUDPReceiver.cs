
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class RUDPReceiver {

    bool isReceiveRunning = false;

    Socket socket;
    EndPoint remoteEndPoint;

    SocketAsyncEventArgs recvEventArgs;

    public Action<Queue<RUDPPayload>> OnReceivePayloads { get; set; }

    public RUDPReceiver() {
        recvEventArgs = new SocketAsyncEventArgs();
        recvEventArgs.SetBuffer(new byte[Config.MAX_SESSION_BUFFER_SIZE], 0, Config.MAX_SESSION_BUFFER_SIZE);
        recvEventArgs.Completed += OnReceive;
    }

    public void Disconnect() {
        this.socket.Shutdown(SocketShutdown.Receive);
        this.socket = null;
        isReceiveRunning = false;
    }

    public void SetHost(Socket connectedSocket, EndPoint remoteEp)
    {
        this.socket = connectedSocket;
        this.remoteEndPoint = remoteEp;

        recvEventArgs.RemoteEndPoint = this.remoteEndPoint;
    }

    public void RunReceive()
    {
        if (isReceiveRunning == false)
        {
            isReceiveRunning = true;
            this.StartReceive();
        }
    }

    private void StartReceive()
    {
        if (socket != null)
        {
            if (!socket.ReceiveFromAsync(recvEventArgs))
            {
                OnReceive(null, recvEventArgs);
            }
        }
    }


    private void OnReceive(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            Queue<RUDPPayload> recvPayloads = RUDPPayload.BytesToPayloads(e.Buffer, e.Offset, e.BytesTransferred);

            this.OnReceivePayloads.Invoke(recvPayloads);

            this.StartReceive();
        }
        else
        {
            Debug.Log("패킷 수신에 문제가 생겼습니다. " + e.SocketError.ToString());
        }
    }
}
