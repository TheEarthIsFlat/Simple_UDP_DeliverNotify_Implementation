
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class RUDPSender {

    // 패킷이 달려있지않은 페이로드를 보내면 페이로드들을 Parsing할때 예외가 발생하기때문에
    // 패킷이 없는 페이로드들에는 해당 패킷을 붙여서 보냄.
    static readonly Packet EMPTY_PACKET = new Packet(Packet.HEADER.RUDP, 8);

    int currentSendingCount = 0;

    Socket socket;
    EndPoint remoteEndPoint;

    SocketAsyncEventArgs sendEventArgs;

    Queue<RUDPPayload> sendPayloadQueue = new Queue<RUDPPayload>(10);
    object sendPayloadQueueLock = new object();

    bool flagDisconnect = false;
    bool flagDisconnectImmediately = false;
    
    public RUDPSender() {
        sendEventArgs = new SocketAsyncEventArgs();
        sendEventArgs.SetBuffer(new byte[Config.MAX_SESSION_BUFFER_SIZE], 0, Config.MAX_SESSION_BUFFER_SIZE);
        sendEventArgs.Completed += OnSendPayload;
    }


    public void SetHost(Socket connectedSocket, EndPoint remoteEp)
    {
        Debug.Log("새로운 Endpoint가 Client에 SetHost 됨." + remoteEp);

        flagDisconnect = false;
        flagDisconnectImmediately = false;

        this.socket = connectedSocket;
        this.remoteEndPoint = remoteEp;

        sendEventArgs.RemoteEndPoint = this.remoteEndPoint;
    }

    /// <summary>
    /// </summary>
    /// <param name="aleadyRemoteDisconnect">
    /// 이미 원격종단점의 연결이 끊겨있으면 해당 인자를 True, 
    /// 상대방이 먼저 끊었을때 패킷보내는 작업 없이 소켓을 정리하기 위해 사용함</param>
    /// <param name="immediately">
    /// 현재 남은 패킷을 다 보내고 종료할거면 False 패킷을 모두 폐기하고 종료할거면 True
    /// </param>
    public void Disconnect(bool aleadyRemoteDisconnect, bool immediately) {

        if (aleadyRemoteDisconnect) {
            socket.Close();
            lock (this.sendPayloadQueueLock) {
                sendPayloadQueue.Clear();
            }
            return;
        }

        if (flagDisconnect) { return; }
        this.flagDisconnectImmediately = immediately;

        if (immediately) {
            lock (this.sendPayloadQueueLock) {
                this.sendPayloadQueue.Clear();
            }
        }

        RUDPPayload disconnectPayload = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.DISCONNECT);
        disconnectPayload.Packet = new Packet(Packet.HEADER.RUDP, 8);
        this.SendPayload(disconnectPayload);
        this.flagDisconnect = true;
    }

    public void SendPayload(RUDPPayload payload)
    {
        if (flagDisconnect) { return; }

#if DEBUG //DEBUG 로 빌드될때 패킷 유실을 흉내내기 위해 Config에 정의된 확률에 따라 패킷을 보내지 않음.(Queue에 추가시키지않음)
        System.Random random = new System.Random();

        if (random.Next() % 100 < Config.PACKET_DROP_SIMULATION_RATIO) { //이런식으로 구현하면 실제로는 정의한 비율대로 동작안함..
            return;                                                      //패킷을 많이 보내니까 그냥 편의상 이렇게 해둠.
                                                                         // Ack패킷 까지 드랍시키기 때문에 실제로 정한 확률 이상으로 드랍되게됨.(받아도 Ack를 제대로 못보내니까)
                                                                         // Ack가 실제 네트워크에선 드랍되기도 하기때문에 그냥 이대로 놔두기로함.
        }
#endif

        int count = 0;

        lock (sendPayloadQueueLock) {
            sendPayloadQueue.Enqueue(payload);
            count = sendPayloadQueue.Count;
        }

        if (count == 1)
        {
            StartSend();
        }
    }

    private void StartSend()
    {
        int sendingPayloadsLength = 0;
        this.currentSendingCount = 0;

        lock (sendPayloadQueueLock)
        {
            var iter = sendPayloadQueue.GetEnumerator();

            while (iter.MoveNext())
            {
                // 패킷을 채울 수 있는 만큼 채운다.
                if (sendingPayloadsLength + iter.Current.Size > Config.MAX_SESSION_BUFFER_SIZE)
                {
                    break;
                }
                else
                {
                    if (iter.Current.Packet == null) {
                        iter.Current.Packet = EMPTY_PACKET;
                    }
                    // 헤더 복사
                    Buffer.BlockCopy(iter.Current.PayloadHeader, 0, sendEventArgs.Buffer, sendEventArgs.Offset + sendingPayloadsLength, RUDPPayload.PAYLOAD_HEADER_SIZE);
                    // 패킷 부분 복사
                    Buffer.BlockCopy(iter.Current.Packet.Data, 0, sendEventArgs.Buffer, sendEventArgs.Offset + sendingPayloadsLength + RUDPPayload.PAYLOAD_HEADER_SIZE, iter.Current.Packet.SizeIncludedFixedArea);
                    sendingPayloadsLength += iter.Current.Size;
                    currentSendingCount++;
                }
            }
        }

        sendEventArgs.SetBuffer(sendEventArgs.Offset, sendingPayloadsLength);

        if (!socket.SendToAsync(sendEventArgs))
        {
            OnSendPayload(null, sendEventArgs);
        }
    }

    private void OnSendPayload(object sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            lock (sendPayloadQueueLock)
            {
                if (!flagDisconnectImmediately)
                {
                    for (int i = 0; i < this.currentSendingCount; i++)
                    {
                        sendPayloadQueue.Dequeue();
                    }
                }

                if (sendPayloadQueue.Count > 0)
                {
                    StartSend();
                }
                else if (flagDisconnect)
                {
                    this.socket.Close();
                    this.socket = null;
                }
            }
        }
        else
        {
            Debug.Log("패킷 전송에 문제가 생겼습니다. " + e.SocketError.ToString());
        }
    }
}
