
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class RUDPClient {

    Socket socket;
    EndPoint remoteEndPoint;

    RUDPDeliverTrackingModule deliverTrackingModule = new RUDPDeliverTrackingModule();
    RUDPDeliverConfirmModule deliverConfirmModule = new RUDPDeliverConfirmModule();

    RUDPSender sender;
    RUDPReceiver receiver;

    bool isDisable = true;

    public bool IsDisable { get { return isDisable; } }
    public bool IsEnable { get { return !isDisable; } }

    public Action<Packet> OnPacketReceive { get; set; }
    public Action OnDisconnect { get; set; }

    public RUDPClient() {
        sender = new RUDPSender();
        receiver = new RUDPReceiver();
        receiver.OnReceivePayloads = this.OnReceivePayloads;
    }

    public void RunReceive() {
        receiver.RunReceive();
    }

    public void Disconnect(bool immediately) {
        isDisable = true;
        receiver.Disconnect();
        sender.Disconnect(false, immediately);
        this.OnDisconnect?.Invoke();
    }

    public DeliverStatistics GetDeliverStatistics() {
        return deliverTrackingModule.GetDeliverStatistics();
    }

    private void Reset() {
        
        try {
            Debug.Log("클라이언트 초기화 됨.");
            isDisable = false;

            this.deliverConfirmModule.Reset(0);
            this.deliverTrackingModule.Reset(0);

            this.socket = null;
            this.remoteEndPoint = null;
        }
        catch (Exception e) {
            Debug.Log(e.GetType().Name + " " + e.Message + " " + e.StackTrace);
        }
        
    }

    public void SetHost(Socket connectedSocket, EndPoint remoteEp)
    {
        if (isDisable)
        {
            Reset();
            this.socket = connectedSocket;
            this.remoteEndPoint = remoteEp;

            sender.SetHost(this.socket, this.remoteEndPoint);
            receiver.SetHost(this.socket, this.remoteEndPoint);
        }
        else {
            throw new Exception("AleadyRunningClient");
        }
    }

    public ushort SendDeliverNotify(Packet p) {

        if (isDisable) { throw new Exception("RUDPClientExpired"); }

        RUDPPayload payload = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.DELIVERY_NOFIFY);
        payload.Packet = p;

        ushort resultSeqNum = deliverTrackingModule.PrepareToDeliver(payload);

        sender.SendPayload(payload);;

        return resultSeqNum;
    }

    public void SendRaw(Packet p) {

        if (isDisable) { return; }

        RUDPPayload payload = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.RAW);
        payload.Packet = p;

        sender.SendPayload(payload);
    }

    private void SendAck(Packet p) {
        if (isDisable) { return; }

        RUDPPayload payload = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.ACK);
        payload.Packet = p;

        sender.SendPayload(payload);
    }

    public ushort[] GetSuccessPayloadSeqNums() {
        return this.deliverTrackingModule.GetAndClearSuccessPayloadSeqNums();
    }

    public ushort[] GetLossPayloadSeqNums() {
        return this.deliverTrackingModule.GetAndClearLossPayloadSeqNums();
    }

    public void SendPendingAcks() {
        if (deliverConfirmModule.IsPendingAcksExist())
        {
            Queue<AckRange> acks = deliverConfirmModule.PopPendingAckRanges();

            var acksIter = acks.GetEnumerator();

            while (acksIter.MoveNext())
            {
                RUDPPayload acksPayload = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.ACK);
                Packet acksPacket = new Packet(Packet.HEADER.RUDP);

                acksPacket.Push(acksIter.Current.StartNum);
                acksPacket.Push(acksIter.Current.AckCount);
                acksPayload.Packet = acksPacket;
                this.SendAck(acksPacket);
                //Debug.Log(string.Format("Ack 보냄 StartNum : {0}, Count : {1}", acksIter.Current.StartNum, acksIter.Current.AckCount));
            }
        }
    }

    private void OnReceivePayloads(Queue<RUDPPayload> payloads) {

        var iter = payloads.GetEnumerator();

        while (iter.MoveNext()) {
            if (iter.Current.Packet != null)
            {
                switch (iter.Current.Tag) {
                    case RUDPPayload.PAYLOAD_TAG.RAW: {
                            OnPacketReceive(iter.Current.Packet);
                            break;
                        }
                    case RUDPPayload.PAYLOAD_TAG.DELIVERY_NOFIFY: {
                            //Debug.Log(string.Format("DeliverNotify 패킷 받음 {0}", iter.Current.SequenceNumber));
                            if (this.deliverConfirmModule.JudgeIncomePayload(iter.Current))
                            {
                                OnPacketReceive(iter.Current.Packet);
                            }
                            break;
                        }
                    case RUDPPayload.PAYLOAD_TAG.ACK: {
                            AckRange currentAck = new AckRange(0, iter.Current.Packet.Pop_UInt16());
                            currentAck.StartNum = iter.Current.Packet.Pop_UInt16();
                            //Debug.Log(string.Format("Ack 받음 StartNum : {0}, Count : {1}", currentAck.StartNum, currentAck.AckCount));
                            deliverTrackingModule.ProcessAckRange(currentAck);
                            break;
                        }
                    case RUDPPayload.PAYLOAD_TAG.DISCONNECT: {
                            isDisable = true;
                            this.receiver.Disconnect();
                            this.sender.Disconnect(true, true);
                            OnDisconnect.Invoke();
                            break;
                        }
                }
            } 
        }
    }
}
