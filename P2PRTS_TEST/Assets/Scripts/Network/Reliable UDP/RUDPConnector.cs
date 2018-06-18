using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Net;
using System;
using System.Net.Sockets;

/// <summary>
/// </summary>
public class RUDPConnector {

    public enum CONNECTOR_STATE {
        IDLE,
        WAIT_ACK,
        WAIT_SYN,
        WAIT_SYN_ACK,
        CONNECTED
    }

    static readonly int CONNECTION_TIMEOUT = 5000;

    readonly RUDPPayload SYN = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.SYN);
    readonly RUDPPayload SYN_ACK = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.SYN_ACK);
    readonly RUDPPayload ACK = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.ACK);

    CONNECTOR_STATE state;

    Thread connectThread = null;
    bool connectThreadAbortFlag = false;

    EndPoint remoteEndPoint = null;

    Socket socket = null;

    byte[] recvBuffer;
    byte[] sendBuffer;

    public Action OnWaitStart { get; set; }
    public Action OnConnectStart { get; set; }
    public Action OnWaitCancel { get; set; }
    public Action OnConnectFail { get; set; }
    public Action<Socket, EndPoint> OnConnectSuccess { get; set; }

    public RUDPConnector() {
        // 받는쪽에서 Parsing을 할 수 있게 빈 패킷을 붙여줌.
        SYN.Packet = new Packet(Packet.HEADER.RUDP, 8);
        ACK.Packet = new Packet(Packet.HEADER.RUDP, 8);
        SYN_ACK.Packet = new Packet(Packet.HEADER.RUDP, 8);

        this.sendBuffer = new byte[SYN.Size];
        this.recvBuffer = new byte[SYN.Size];
    }

    public void ConnectTo(IPAddress ip, int port) {
        if (state != CONNECTOR_STATE.IDLE) { throw new Exception("This Peer Not Idle State"); }
        state = CONNECTOR_STATE.WAIT_SYN_ACK;

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, CONNECTION_TIMEOUT);

        remoteEndPoint = new IPEndPoint(ip, port);

        byte[] synPayloadBytes = SYN.ToBytes();

        Array.Clear(sendBuffer, 0, sendBuffer.Length);
        //SYN 패킷 전송
        Array.Copy(synPayloadBytes, sendBuffer, synPayloadBytes.Length);
        socket.SendTo(sendBuffer, remoteEndPoint);

        Debug.Log("Connect Start" + "  " + remoteEndPoint.ToString());
        OnConnectStart?.Invoke();

        connectThread = new Thread(ThreadMainConnect);
        connectThread.Start();
    }

    public void ConnectWait(int port) {

        if (state != CONNECTOR_STATE.IDLE) { throw new Exception("This Peer Not Idle State"); }
        state = CONNECTOR_STATE.WAIT_SYN;

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        socket.Bind(remoteEndPoint);

        OnWaitStart?.Invoke();

        connectThreadAbortFlag = false;
        connectThread = new Thread(ThreadMainConnect);
        connectThread.Start();
    }

    public void CancelWait() {
        Debug.Log("대기 상태 취소");
        ResetConnector();
        OnWaitCancel?.Invoke();
    }

    private void ResetConnector() {
        try
        {
            socket?.Close();

            connectThreadAbortFlag = true;
            connectThread = null;

            remoteEndPoint = null;
            socket = null;

            Array.Clear(this.sendBuffer, 0, sendBuffer.Length);
            Array.Clear(this.recvBuffer, 0, recvBuffer.Length);

            state = CONNECTOR_STATE.IDLE;
        }
        catch (Exception e) {
            Debug.Log(e.GetType().Name + " " + e.Message + " " + e.StackTrace);
        }
    }

    private void ThreadMainConnect()
    {
        while (true)
        {
            if (connectThreadAbortFlag) {
                connectThreadAbortFlag = false;
                break;
            }

            Array.Clear(recvBuffer, 0, recvBuffer.Length);

            int byteTransferred = 0;

            try
            {
                byteTransferred = socket.ReceiveFrom(recvBuffer, SocketFlags.None, ref remoteEndPoint);
            } catch (Exception e) {
                Debug.Log(e.GetType().Name + " " + e.Message + " " + e.StackTrace);
                OnConnectFail();
                ResetConnector();
                continue;
            }

            Queue<RUDPPayload> payloads = RUDPPayload.BytesToPayloads(recvBuffer, 0, byteTransferred);

            if (payloads.Count == 1)
            {
                RUDPPayload payload = payloads.Dequeue();

                if (payload != null)
                {
                    switch (payload.Tag)
                    {
                        case RUDPPayload.PAYLOAD_TAG.SYN:
                            {
                                ProcessSYN();
                                break;
                            }
                        case RUDPPayload.PAYLOAD_TAG.SYN_ACK:
                            {
                                ProcessSYN_ACK();
                                break;
                            }
                        case RUDPPayload.PAYLOAD_TAG.ACK:
                            {
                                ProcessACK();
                                break;
                            }
                    }
                }
            }
        }
    }

    private void ProcessSYN()
    {
        if (state == CONNECTOR_STATE.WAIT_SYN)
        {

            state = CONNECTOR_STATE.WAIT_ACK;

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, CONNECTION_TIMEOUT);

            Array.Clear(sendBuffer, 0, sendBuffer.Length);
            //SYN_ACK 패킷 전송

            byte[] synAckPacketBytes = SYN_ACK.ToBytes();
            Array.Copy(synAckPacketBytes, sendBuffer, synAckPacketBytes.Length);
            socket.SendTo(sendBuffer, remoteEndPoint);
        }
        else {
            Debug.Log("SYN 대기 중 아닌데 SYN 패킷 들어옴.");
        }
    }

    private void ProcessSYN_ACK() {
        if (state == CONNECTOR_STATE.WAIT_SYN_ACK)
        {

            Array.Clear(sendBuffer, 0, sendBuffer.Length);

            byte[] ackPacketBytes = ACK.ToBytes();
            Array.Copy(ackPacketBytes, sendBuffer, ackPacketBytes.Length);
            socket.SendTo(sendBuffer, remoteEndPoint);

            state = CONNECTOR_STATE.CONNECTED;

            //연결완료
            this.OnConnectSuccess(this.socket ,this.remoteEndPoint);
            this.socket = null;
            this.remoteEndPoint = null;
            ResetConnector();
        }
        else
        {
            Debug.Log("SYN_ACK 대기 중 아닌데 SYN_ACK 패킷 들어옴.");
        }
    }

    private void ProcessACK()
    {
        if (state == CONNECTOR_STATE.WAIT_ACK)
        {
            state = CONNECTOR_STATE.CONNECTED;
            OnConnectSuccess(this.socket, remoteEndPoint);
            this.socket = null;
            this.remoteEndPoint = null;
            ResetConnector();
        }
        else {
            Debug.Log("ACK 대기중 아닌데 ACK 패킷 들어옴.");
        }
    }
}
