using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public struct DeliverStatistics {
    public int AllPayloadCount { get; set; }
    public int SuccessPayloadCount { get; set; }
    public int FailedPayloadCount { get; set; }
    public float LossRate { get; set; }
    public float SuccessRate { get; set; }
}

/// <summary>
/// 확인 응답의 범위를 나타내는 구조체, StartNumber(시작) , Count(StartNum을 포함한 총 Ack의 갯수)
/// </summary>
public class AckRange {
    public ushort StartNum { get; set; }
    public ushort AckCount { get; set; }

    public AckRange(ushort start, ushort ackCount) {
        this.StartNum = start;
        this.AckCount = ackCount;
    }

    public bool CheckContinous(ushort newAck) {

        ushort temp = StartNum;
        temp += AckCount;

        if (newAck == temp)
        {
            return true;
        }
        else {
            return false;
        }
    }
}

public class InFlightPayload {
    public static readonly int benchmark = ((sizeof(ushort) * 8) - 1) * ((sizeof(ushort) * 8) - 1);

    public RUDPPayload Payload { get; set; }
}

public class RUDPDeliverConfirmModule {

    private ushort nextExpectIncomeSeqNum = 0;

    private List<AckRange> pendingAcks = new List<AckRange>(5);

    private object pendingAcksLock = new object();

    public void Reset(ushort expectedSeqNum) {

        nextExpectIncomeSeqNum = expectedSeqNum;

        lock (pendingAcksLock) {
            pendingAcks.Clear();
        }

        //UnityEngine.Debug.Log("Call Deliver Confirmer Reset" + expectedSeqNum.ToString());
    }

    // 들어온 시퀀스 번호 : seq
    // seq > 0 또는 seq < -benchmark -> 처리안함.
    // seq < 0 또는 seq >  benchmark -> 처리함.
    public bool JudgeIncomePayload(RUDPPayload payload) {

        int distance = nextExpectIncomeSeqNum - payload.SequenceNumber;

        if (distance > 0 || distance < -InFlightPayload.benchmark)
        {
            if (distance > InFlightPayload.benchmark) {
                nextExpectIncomeSeqNum = payload.SequenceNumber;
                nextExpectIncomeSeqNum++;

                this.AddPendingAck(payload.SequenceNumber);
                return true;
            }
            return false;
        }
        else {
            nextExpectIncomeSeqNum = payload.SequenceNumber;
            nextExpectIncomeSeqNum++;

            this.AddPendingAck(payload.SequenceNumber);
            return true;
        }
    }

    public bool IsPendingAcksExist() {
        lock (pendingAcksLock) {
            return (pendingAcks.Count > 0);
        }
    }

    public Queue<AckRange> PopPendingAckRanges() {
        Queue<AckRange> result = null;
        lock (pendingAcksLock)
        {
            result = new Queue<AckRange>(this.pendingAcks);
            this.pendingAcks.Clear();
        }
        return result;
    }

    private void AddPendingAck(ushort newAck) {
        lock (pendingAcksLock)
        {
            if (pendingAcks.Count == 0 || !pendingAcks[pendingAcks.Count - 1].CheckContinous(newAck))
            {
                pendingAcks.Add(new AckRange(newAck, 1));
            }
            else
            {
                pendingAcks[pendingAcks.Count-1].AckCount++;
            }
        }
    }
}

public class RUDPDeliverTrackingModule {

    private ushort nextOutgoingSeqNum = 0;

    private DeliverStatistics statistics = new DeliverStatistics();
    private Queue<InFlightPayload> inflightPayloads = new Queue<InFlightPayload>(sizeof(ushort)); //sequence번호만큼 미리 초기화
    private object inflightPayloadsLock = new object();


    private Queue<ushort> successPayloadSeqNums = new Queue<ushort>(10);
    private object successPayloadSeqNumsLock = new object();
    private Queue<ushort> lossPayloadSeqNums = new Queue<ushort>(10);
    private object lossPayloadSeqNumsLock = new object();


    public void Reset(ushort startSeqNum) {

        this.nextOutgoingSeqNum = startSeqNum;

        statistics = new DeliverStatistics();

        lock (this.inflightPayloadsLock) {
            this.inflightPayloads.Clear();
        }

        lock (this.lossPayloadSeqNumsLock) {
            this.lossPayloadSeqNums.Clear();
        }

        lock (this.successPayloadSeqNumsLock) {
            this.successPayloadSeqNums.Clear();
        }

        //UnityEngine.Debug.Log("Call Deliver Tracker Reset" + startSeqNum.ToString());
    }

    /// <summary>
    /// Payload에 Sequence번호를 쓰고 현재 시간을 sendingTime에 기록하고 Inflight패킷에 등록함.
    /// </summary>
    /// <param name="payload"></param>
    /// <returns></returns>
    public ushort PrepareToDeliver(RUDPPayload payload) {

        InFlightPayload p = new InFlightPayload();
        p.Payload = payload;

        lock (inflightPayloadsLock)
        {
            p.Payload.SequenceNumber = nextOutgoingSeqNum++;
            this.inflightPayloads.Enqueue(p);
            statistics.AllPayloadCount++;
        }
        return payload.SequenceNumber;
    }

    public ushort[] GetAndClearSuccessPayloadSeqNums() {

        ushort[] result;

        lock (successPayloadSeqNumsLock) {
            result = this.successPayloadSeqNums.ToArray();
            this.successPayloadSeqNums.Clear();
        }

        return result;
    }

    public ushort[] GetAndClearLossPayloadSeqNums() {
        ushort[] result;

        lock (lossPayloadSeqNumsLock) {
            result = this.lossPayloadSeqNums.ToArray();
            this.lossPayloadSeqNums.Clear();
        }

        return result;
    }

    // AckRange 만큼의 Ack신호를 처리함. 주의점 : 절대로 타임아웃 패킷 정리와 동시에 수행되어서는 안됨.
    public void ProcessAckRange(AckRange range) {

        ushort currentInflightSeqNum = 0;
        int distance = 0;

        lock (inflightPayloadsLock)
        {
            currentInflightSeqNum = this.inflightPayloads.Peek().Payload.SequenceNumber;
            distance = currentInflightSeqNum - range.StartNum;
        }

        
        if (distance == 0) { // 0이면 현재 inflight와 ack가 일치하는것임
            for (int i = 0; i < range.AckCount; i++)
            {
                ushort seq = 0;
                lock (inflightPayloadsLock)
                {
                    seq = inflightPayloads.Dequeue().Payload.SequenceNumber;
                }
                statistics.SuccessPayloadCount++;
                lock (this.successPayloadSeqNumsLock)
                {
                    this.successPayloadSeqNums.Enqueue(seq);
                }
            }
        }
        else if (distance > 0 || distance < -InFlightPayload.benchmark)
        {// Ack가 현재 Inflight보다 작음. 처리하지 않음
            if (distance > InFlightPayload.benchmark)
            {  // 오버플로우 구간인지 검사하고 오버플로우 구간이면 처리하도록 함.
               // ack값이 기대보다 큼 중간에 패킷이 손실된것으로 판단, ack값 이전까지의 패킷을 손실로 처리
                ushort currentSeq = 0;
                lock (this.inflightPayloadsLock)
                {
                    currentSeq = this.inflightPayloads.Peek().Payload.SequenceNumber;
                }

                while (currentSeq != range.StartNum)
                {
                    lock (inflightPayloadsLock)
                    {
                        lock (this.lossPayloadSeqNumsLock)
                        {
                            this.lossPayloadSeqNums.Enqueue(inflightPayloads.Dequeue().Payload.SequenceNumber);
                        }
                    }
                    statistics.FailedPayloadCount++;
                    currentSeq++;
                }

                // ack에 해당하는 패킷들을 전송 성공처리 시켜줘야함.
                for (int i = 0; i < range.AckCount; i++)
                {
                    ushort seq = 0;
                    lock (inflightPayloadsLock)
                    {
                        seq = inflightPayloads.Dequeue().Payload.SequenceNumber;
                    }
                    statistics.SuccessPayloadCount++;
                    lock (this.successPayloadSeqNumsLock)
                    {
                        this.successPayloadSeqNums.Enqueue(seq);
                    }
                }
            }
            else { //오버플로우도 아니니까 현재 Inflight보다 작은 것으로 판단됨, 처리하지않도록함.
                // ack신호를 무시하되, range 내에 무시해선 안될 ack가있는지 확인
                ushort end = range.StartNum;
                end += range.AckCount;
                for (ushort currentAck = range.StartNum; currentAck != end; currentAck++)
                {
                    lock (inflightPayloadsLock)
                    {
                        if (currentAck == this.inflightPayloads.Peek().Payload.SequenceNumber)
                        {
                            lock (this.successPayloadSeqNumsLock)
                            {
                                this.successPayloadSeqNums.Enqueue(this.inflightPayloads.Dequeue().Payload.SequenceNumber);
                            }
                            statistics.SuccessPayloadCount++;
                        }
                    }
                }
            }
        }
        else
        {
            // ack값이 기대보다 큼 중간에 패킷이 손실된것으로 판단, ack값 이전까지의 패킷을 손실로 처리
            ushort currentSeq = 0;
            lock (this.inflightPayloadsLock) {
                currentSeq = this.inflightPayloads.Peek().Payload.SequenceNumber;
            }

            while (currentSeq != range.StartNum) {
                lock (inflightPayloadsLock) {
                    lock (this.lossPayloadSeqNumsLock) {
                        this.lossPayloadSeqNums.Enqueue(inflightPayloads.Dequeue().Payload.SequenceNumber);
                    }
                }
                statistics.FailedPayloadCount++;
                currentSeq++;
            }

            // ack에 해당하는 패킷들을 전송 성공처리 시켜줘야함.
            for (int i = 0; i < range.AckCount; i++)
            {
                ushort seq = 0;
                lock (inflightPayloadsLock)
                {
                    seq = inflightPayloads.Dequeue().Payload.SequenceNumber;
                }
                statistics.SuccessPayloadCount++;
                lock (this.successPayloadSeqNumsLock)
                {
                    this.successPayloadSeqNums.Enqueue(seq);
                }
            }
        }
    }

    public DeliverStatistics GetDeliverStatistics() {
        return this.statistics;
    }
}
