
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System;
using System.Threading;

public class RUDPUnityManager : MonoBehaviour {

    static readonly Packet FB_ACCEPT_START = new Packet(Packet.HEADER.FB_ACCEPT_START);
    static readonly Packet FB_CONNECT_START = new Packet(Packet.HEADER.FB_CONNECT_START);
    static readonly Packet FB_ACCEPT_CANCEL = new Packet(Packet.HEADER.FB_ACCEPT_CANCEL);
    static readonly Packet FB_CONNECT_SUCCESS = new Packet(Packet.HEADER.FB_CONNECT_SUCCESS);
    static readonly Packet FB_CONNECT_FAIL = new Packet(Packet.HEADER.FB_CONNECT_FAILED);
    static readonly Packet FB_DISCONNECT = new Packet(Packet.HEADER.FB_DISCONNECT);

    static RUDPUnityManager instance = null;

    RUDPConnector connector = new RUDPConnector();
    RUDPCommunicator client = new RUDPCommunicator();

    public static RUDPUnityManager Instance
    {
        get
        {
            if (instance != null)
            {
                return instance;
            }
            else
            {
                return null;
            }
        }
    }

    //public static Action<string, ReadonlyPacket> onPacketReceive;
    public static Action<ReadonlyPacket> onPacketReceive;

    private Dictionary<ushort, Action<bool, ushort>> deliverNotifyTable = new Dictionary<ushort, Action<bool,ushort>>(sizeof(ushort));

    private Queue<Packet> receivedPacketQueue = new Queue<Packet>(10);
    private object receivePacketQueueLock = new object();

    private void Awake()
    {
        if (instance == null)
        {
            GameObject.DontDestroyOnLoad(this);
            Initialize();
            instance = this;
        }
    }

    private void Initialize() {
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

            this.client.SetHost(socket, ep);

            this.client.OnPacketReceive = (Packet recvPacket) => {
                lock (this.receivePacketQueueLock)
                {
                    this.receivedPacketQueue.Enqueue(recvPacket);
                }
            };

            this.client.OnDisconnect = () => {
                lock (this.receivePacketQueueLock)
                {
                    this.receivedPacketQueue.Enqueue(FB_DISCONNECT);
                }
            };

            lock (this.receivePacketQueueLock)
            {
                this.receivedPacketQueue.Enqueue(FB_CONNECT_SUCCESS);
            }

            this.client.RunReceive();
        };
    }

    private void FixedUpdate()
    {
        if (receivedPacketQueue.Count > 0 && onPacketReceive != null)
        {
            lock (receivePacketQueueLock)
            {
                Queue<Packet>.Enumerator iter = receivedPacketQueue.GetEnumerator();
                while (iter.MoveNext())
                {
                    onPacketReceive.Invoke(new ReadonlyPacket(iter.Current));
                }
                receivedPacketQueue.Clear();
            }
        }

        if (client.IsEnable)
        {

            ushort[] sendSuccessPayloadSeqNums = client.GetSuccessPayloadSeqNums();
            ushort[] sendFailedPayloadSeqNums = client.GetLossPayloadSeqNums();

            for (int i = 0; i < sendSuccessPayloadSeqNums.Length; i++)
            {
                this.deliverNotifyTable[sendSuccessPayloadSeqNums[i]]?.Invoke(true, sendSuccessPayloadSeqNums[i]);
                this.deliverNotifyTable[sendSuccessPayloadSeqNums[i]] = null;
            }

            for (int j = 0; j < sendFailedPayloadSeqNums.Length; j++)
            {
                this.deliverNotifyTable[sendFailedPayloadSeqNums[j]]?.Invoke(false, sendFailedPayloadSeqNums[j]);
                this.deliverNotifyTable[sendFailedPayloadSeqNums[j]] = null;
            }

            client.SendPendingAcks();
        }
    }

    public DeliverStatistics GetDeliverStatistics() {
        return this.client.GetDeliverStatistics();
    }

    public void CancelWait() {
        Debug.Log("Cancel Wait");
        try
        {
            connector.CancelWait();
        }
        catch (Exception e) {
            LogView.Log(e.Message + " " + e.StackTrace);
        }
    }

    public void ConnectWait() {
        try
        {
            connector.ConnectWait(Config.PORT);
        }
        catch (Exception e) {
            LogView.Log(e.Message + " " + e.StackTrace);
        }
    }

    public void ConnectTo(string ip){
        try
        {
            connector.ConnectTo(IPAddress.Parse(ip), Config.PORT);
        }
        catch (Exception e) {
            LogView.Log(e.Message + " " + e.StackTrace);
        }
    }

    public void SendRaw(Packet packet) {
        this.client.SendRaw(packet);
    }

    public ushort SendDeliverNotify(Packet packet, Action<bool, ushort> onDeliverNotify)
    {
        try
        {
            ushort seq = this.client.SendDeliverNotify(packet);
            if (this.deliverNotifyTable.ContainsKey(seq))
            {
                deliverNotifyTable[seq] = onDeliverNotify;
            }
            else {
                deliverNotifyTable.Add(seq, onDeliverNotify);
            }
            return seq;
        }
        catch (Exception e)
        {
            LogView.Log(e.Message + " " + e.StackTrace);
        }
        return 0;
    }

    public void SendReliable(Packet packet) {
        this.SendDeliverNotify(packet, (bool flag, ushort seq) =>
        {
            if (!flag) {
                this.SendReliable(packet);
            }
        });   
    }

    public void Disconnect(bool immediately)
    {
        this.deliverNotifyTable.Clear();
        this.client.Disconnect(immediately);
    }
}
