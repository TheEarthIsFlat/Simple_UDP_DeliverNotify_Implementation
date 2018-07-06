using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// 아직 개발안됨.
/// </summary>
public class RUDPRemotePeer {

    #region 피드백 패킷들
    static readonly Packet FB_ACCEPT_START = new Packet(Packet.HEADER.FB_ACCEPT_START);
    static readonly Packet FB_CONNECT_START = new Packet(Packet.HEADER.FB_CONNECT_START);
    static readonly Packet FB_ACCEPT_CANCEL = new Packet(Packet.HEADER.FB_ACCEPT_CANCEL);
    static readonly Packet FB_CONNECT_SUCCESS = new Packet(Packet.HEADER.FB_CONNECT_SUCCESS);
    static readonly Packet FB_CONNECT_FAIL = new Packet(Packet.HEADER.FB_CONNECT_FAILED);
    static readonly Packet FB_DISCONNECT = new Packet(Packet.HEADER.FB_DISCONNECT);
    #endregion

    string rawIp;
    ushort port;

    RUDPConnector connector = new RUDPConnector();
    RUDPCommunicator communicator = new RUDPCommunicator();

    private Queue<Packet> receivedPacketQueue = new Queue<Packet>(10);
    private object receivePacketQueueLock = new object();

    private Dictionary<ushort, Action<bool, ushort>> deliverNotifyTable = new Dictionary<ushort, Action<bool, ushort>>(sizeof(ushort));

    /// <summary>
    ///  반드시 Lock을 걸고 접근해야함.
    /// </summary>
    public Queue<Packet> ReceivedPacktes { get { return receivedPacketQueue; } }
    public object ReceivedPacketsLock { get { return receivePacketQueueLock; } }

    public RUDPRemotePeer()
    {
        RegistConnectCallback();
    }

    private void RegistConnectCallback() {
        // 각 이벤트가 발생하면 이벤트에 맞게 FEEDBACK Packet을 receivedQueue에 넣는 콜백 작성
        connector.OnWaitStart += () =>
        {
            lock (receivePacketQueueLock)
            {
                receivedPacketQueue.Enqueue(FB_ACCEPT_START);
            }
        };

        connector.OnConnectStart += () =>
        {
            lock (receivePacketQueueLock)
            {
                receivedPacketQueue.Enqueue(FB_CONNECT_START);
            }
        };

        connector.OnWaitCancel += () =>
        {
            lock (receivePacketQueueLock)
            {
                receivedPacketQueue.Enqueue(FB_ACCEPT_CANCEL);
            }
        };

        connector.OnConnectFail += () =>
        {
            lock (receivePacketQueueLock)
            {
                receivedPacketQueue.Enqueue(FB_CONNECT_FAIL);
            }
        };

        // 연결 성공하면 소켓과 엔드포인트 받아옴
        connector.OnConnectSuccess += (Socket socket, EndPoint ep) =>
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 0);

            this.communicator.SetHost(socket, ep);

            this.communicator.OnPacketReceive = (Packet recvPacket) => {
                lock (this.receivePacketQueueLock)
                {
                    this.receivedPacketQueue.Enqueue(recvPacket);
                }
            };

            this.communicator.OnDisconnect = () => {
                lock (this.receivePacketQueueLock)
                {
                    this.receivedPacketQueue.Enqueue(FB_DISCONNECT);
                }
            };

            lock (this.receivePacketQueueLock)
            {
                this.receivedPacketQueue.Enqueue(FB_CONNECT_SUCCESS);
            }

            this.communicator.RunReceive();
        };
    }
}
