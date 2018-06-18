using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RUDPPayload
{

    public static readonly int PAYLOAD_HEADER_SIZE = sizeof(byte) + sizeof(ushort);

    public enum PAYLOAD_TAG : byte
    {
        WRONG = 0,
        SYN,
        ACK,
        SYN_ACK,
        DISCONNECT,
        RAW,
        DELIVERY_NOFIFY
    }

    // 페이로드 헤더 구조
    // 첫번째 바이트 : 페이로드 태그
    // 두번째 바이트 : 시퀀스 넘버
    byte[] payloadHeader = new byte[PAYLOAD_HEADER_SIZE];
    public Packet Packet { get; set; }

    public PAYLOAD_TAG Tag
    {
        get { return (PAYLOAD_TAG)payloadHeader[0]; }
        set { Array.Copy(BitConverter.GetBytes((byte)value), 0, payloadHeader, 0, sizeof(PAYLOAD_TAG)); }
    }

    public ushort SequenceNumber
    {
        get { return BitConverter.ToUInt16(payloadHeader, 1); }
        set { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, payloadHeader, 1, sizeof(ushort)); }
    }

    public byte[] PayloadHeader
    {
        get { return payloadHeader; }
    }

    public int Size
    {
        get
        {
            int packetsize = 0;
            if (this.Packet != null) { packetsize = Packet.SizeIncludedFixedArea; }
            return PAYLOAD_HEADER_SIZE + packetsize;
        }
    }

    public RUDPPayload(PAYLOAD_TAG tag, ushort seqNum = 0)
    {
        this.Tag = tag;
        this.SequenceNumber = seqNum;
    }

    public byte[] ToBytes()
    {

        byte[] result;


        if (Packet != null)
        {
            result = new byte[PAYLOAD_HEADER_SIZE + this.Packet.SizeIncludedFixedArea];

            Buffer.BlockCopy(payloadHeader, 0, result, 0, PAYLOAD_HEADER_SIZE); //헤더 복사
            Buffer.BlockCopy(this.Packet.Data, 0, result, PAYLOAD_HEADER_SIZE, this.Packet.SizeIncludedFixedArea);

            return result;
        }
        else
        {
            result = new byte[PAYLOAD_HEADER_SIZE];

            Buffer.BlockCopy(this.payloadHeader, 0, result, 0, PAYLOAD_HEADER_SIZE);

            return result;

        }
    }

    /// <summary>
    /// 바이트 배열을 복수의 페이로드로 파싱
    /// </summary>
    /// <param name="bytes">변환할 바이트 배열</param>
    /// <param name="offset">오프셋</param>
    /// <param name="length">변환할 바이트의 총 길이</param>
    /// <returns></returns>
    public static Queue<RUDPPayload> BytesToPayloads(byte[] bytes, int offset, int length)
    {
        Queue<RUDPPayload> result = new Queue<RUDPPayload>(10);
        int cursor = 0;

        while ((length - cursor) >= (RUDPPayload.PAYLOAD_HEADER_SIZE + Packet.FIXED_AREA_SIZE))
        {
            RUDPPayload payload = new RUDPPayload((PAYLOAD_TAG)bytes[cursor + offset], BitConverter.ToUInt16(bytes, cursor + offset + 1));
            int packetSize = Packet.FIXED_AREA_SIZE + BitConverter.ToInt32(bytes, offset + RUDPPayload.PAYLOAD_HEADER_SIZE + cursor + sizeof(Packet.HEADER));
            Packet packet = Packet.BytesToPacket(bytes, packetSize, offset + RUDPPayload.PAYLOAD_HEADER_SIZE + cursor);
            payload.Packet = packet;
            cursor += payload.Size;
            result.Enqueue(payload);
        }

        return result;
    }
}
