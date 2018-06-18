using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

public partial class Packet
{
    public static readonly int HEADER_SIZE = sizeof(HEADER);
    public static readonly int FIXED_AREA_SIZE = HEADER_SIZE + sizeof(int);

    byte[] data;

    public byte[] Data
    {
        get { return data; }
    }

    public HEADER Head
    {
        get
        {
            return (HEADER)BitConverter.ToInt32(data, 0); // 처음부터 4바이트 까지는 헤더
        }
        set {
            Buffer.BlockCopy(BitConverter.GetBytes((Int32)value), 0, data, 0, sizeof(Int32));
        }
    }

    public int SizeExceptFixedArea {
        get {
            return BitConverter.ToInt32(data, sizeof(Int32));
        }
        private set {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, data, HEADER_SIZE, sizeof(Int32));
        }
    }
        
    public int SizeIncludedFixedArea {
        get {
            return SizeExceptFixedArea + FIXED_AREA_SIZE;
        } private set {
            this.SizeExceptFixedArea = value - FIXED_AREA_SIZE;
        }
    }

    /// <summary>
    /// 헤더 값을 가진 빈 패킷을 생성합니다.
    /// 패킷에는 Int32, float, bool, string 데이터를 넣을 수 있으며, 최대 받을 수 있는 용량은 미리 정의된 세션마다 할당하는 버퍼의 최대 사이즈 입니다.
    /// capacity 만큼 버퍼를 할당하며, 버퍼를 초과 할때 마다 버퍼의 양을 두배로 늘려서 처리합니다. 또한 미리 정의된 세션당 버퍼 값을 넘어갈때에는 예외를 던집니다.
    /// </summary>
    /// <param name="head"></param>
    /// <param name="capacity"></param>
    public Packet(HEADER head, int capacity = 32)
    {
        if (capacity > Config.MAX_SESSION_BUFFER_SIZE)
        {
            throw new Exception("Packet pre-Allocate Memory over the max Session buffer size");
        }
        else
        {
            data = new byte[capacity];
        }
        SizeIncludedFixedArea = HEADER_SIZE + sizeof(Int32);
        Head = head;
    }

    public void ReAllocate(int capacity) {
        if (capacity > Config.MAX_SESSION_BUFFER_SIZE)
        {
            throw new Exception("Packet pre-Allocate Memory over the max Session buffer size");
        }
        byte[] newBytes = new byte[capacity];

        Buffer.BlockCopy(this.data, 0, newBytes, 0, this.SizeIncludedFixedArea);

        this.data = newBytes;
    }

    public override string ToString()
    {
        return string.Format("HEAD : {0} | SIZE : {1} ", Head.ToString(), SizeExceptFixedArea);
    }

    /// <summary>
    /// 바이트 배열을 하나의 패킷으로 파싱
    /// </summary>
    /// <param name="bytes">변환 대상이 될 바이트 배열</param>
    /// <param name="packetLength">변환할 패킷의 총 길이(헤더포함)</param>
    /// <param name="offset">오프셋 값</param>
    /// <returns></returns>
    public static Packet BytesToPacket(byte[] bytes, int packetLength, int offset = 0) {                                              
        Packet packet = new Packet(HEADER.__WRONG, packetLength); //데이터 사이즈 + 헤더사이즈 + 사이즈 변수 사이즈 만큼 공간확보
        Buffer.BlockCopy(bytes, offset, packet.Data, 0, packetLength);
        packet.SizeIncludedFixedArea = packetLength;
        return packet;
    }

    public void Clear() {
        Array.Clear(data, 0, data.Length);
        Head = HEADER.__WRONG;
        SizeIncludedFixedArea = HEADER_SIZE + sizeof(Int32);
    }

    public void Push(UInt16 value) {
        if (SizeIncludedFixedArea + sizeof(UInt16) > this.Data.Length)
        { //초과된 사이즈가 버퍼 최대 값을 넘기면 할당 실패
            this.ReAllocate(SizeIncludedFixedArea * 2);
        }
        Buffer.BlockCopy(BitConverter.GetBytes(value), 0, this.data, SizeIncludedFixedArea, sizeof(UInt16));
        SizeExceptFixedArea += sizeof(UInt16);
    }

    public void Push(Int32 value)
    {
        if (SizeIncludedFixedArea + sizeof(Int32) > this.Data.Length)
        { //초과된 사이즈가 버퍼 최대 값을 넘기면 할당 실패
            this.ReAllocate(SizeIncludedFixedArea * 2);
        }
        Buffer.BlockCopy(BitConverter.GetBytes(value), 0, this.data, SizeIncludedFixedArea, sizeof(Int32));
        SizeExceptFixedArea += sizeof(Int32);
    }

    public void Push(UInt32 value)
    {
        if (SizeIncludedFixedArea + sizeof(UInt32) > this.Data.Length)
        { //초과된 사이즈가 버퍼 최대 값을 넘기면 할당 실패
            this.ReAllocate(SizeIncludedFixedArea * 2);
        }
        Buffer.BlockCopy(BitConverter.GetBytes(value), 0, this.data, SizeIncludedFixedArea, sizeof(UInt32));
        SizeExceptFixedArea += sizeof(UInt32);
    }

    public void Push(float value)
    {
        if (SizeIncludedFixedArea + sizeof(float) > this.Data.Length)
        { //초과된 사이즈가 버퍼 최대 값을 넘기면 할당 실패
            this.ReAllocate(SizeIncludedFixedArea * 2);
        }
        Buffer.BlockCopy(BitConverter.GetBytes(value), 0, this.data, SizeIncludedFixedArea, sizeof(float));
        SizeExceptFixedArea += sizeof(float);
    }

    public void Push(bool value)
    {
        if (SizeIncludedFixedArea + sizeof(bool) > this.Data.Length)
        { //초과된 사이즈가 버퍼 최대 값을 넘기면 할당 실패
            this.ReAllocate(SizeIncludedFixedArea * 2);
        }
        Buffer.BlockCopy(BitConverter.GetBytes(value), 0, this.data, SizeIncludedFixedArea, sizeof(bool));
        SizeExceptFixedArea += sizeof(bool);
    }

    public void Push(string value)
    {
        byte[] temp = Encoding.Unicode.GetBytes(value);

        if (SizeIncludedFixedArea + temp.Length > this.Data.Length)
        { //초과된 사이즈가 버퍼 최대 값을 넘기면 할당 실패
            this.ReAllocate(Math.Max(SizeIncludedFixedArea * 2, SizeIncludedFixedArea + temp.Length));
        }
        Buffer.BlockCopy(temp, 0, this.data, SizeIncludedFixedArea, temp.Length);
        SizeExceptFixedArea += temp.Length;
        this.Push(temp.Length);
    }

    public void Push(byte[] bytes) {
        if (SizeIncludedFixedArea + bytes.Length > this.Data.Length)
        {
            this.ReAllocate(Math.Max(SizeIncludedFixedArea * 2, SizeIncludedFixedArea + bytes.Length));
        }
        Buffer.BlockCopy(bytes, 0, this.data, SizeIncludedFixedArea, bytes.Length);
        SizeExceptFixedArea += bytes.Length;
        this.Push(bytes.Length);
    }

    public UInt16 Pop_UInt16() {
        if (SizeExceptFixedArea < +sizeof(UInt16))
        {
            throw new Exception("PacketDataIsNull");
        }
        SizeExceptFixedArea -= sizeof(UInt16);
        return BitConverter.ToUInt16(this.data, SizeIncludedFixedArea);
    }

    public Int32 Pop_Int32()
    {
        if (SizeExceptFixedArea < + sizeof(Int32))
        {
            throw new Exception("PacketDataIsNull");
        }
        SizeExceptFixedArea -= sizeof(Int32);
        return BitConverter.ToInt32(this.data, SizeIncludedFixedArea);
    }

    public UInt32 Pop_UInt32() {
        if (SizeExceptFixedArea < sizeof(UInt32))
        {
            throw new Exception("PacketDataIsNull");
        }
        SizeExceptFixedArea -= sizeof(UInt32);
        return BitConverter.ToUInt32(this.data, SizeIncludedFixedArea);
    }

    public float Pop_Float()
    {
        if (SizeExceptFixedArea < sizeof(float))
        {
            throw new Exception("PacketDataIsNull");
        }
        SizeExceptFixedArea -= sizeof(float);
        return BitConverter.ToSingle(this.data, SizeIncludedFixedArea);
    }
    public bool Pop_Bool()
    {
        if (SizeExceptFixedArea < sizeof(bool))
        {
            throw new Exception("PacketDataIsNull");
        }
        SizeExceptFixedArea -= sizeof(bool);
        return BitConverter.ToBoolean(this.data, SizeIncludedFixedArea);
    }
    public string Pop_String()
    {
        int length = this.Pop_Int32();

        if (SizeExceptFixedArea < length)
        {
            throw new Exception("PacketDataIsNull");
        }
        SizeExceptFixedArea -= length;
        return Encoding.Unicode.GetString(this.data, SizeIncludedFixedArea, length);
    }

    public byte[] Pop_Bytes() {

        int length = this.Pop_Int32();

        byte[] bytes = new byte[length];

        if (SizeExceptFixedArea < length) {
            throw new Exception("PacketDataIsNull");
        }
        SizeExceptFixedArea -= length;
        Buffer.BlockCopy(this.data, SizeIncludedFixedArea, bytes, 0, length);
        return bytes;
    }

    public Packet Clone()
    {
        Packet result = new Packet(this.Head, this.SizeIncludedFixedArea);

        Buffer.BlockCopy(this.data, 0, result.data, 0, this.SizeIncludedFixedArea);

        return result;
    }
}


