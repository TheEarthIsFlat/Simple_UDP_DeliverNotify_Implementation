using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

public class ReadonlyPacket {

    public static readonly int HEADER_SIZE = sizeof(Packet.HEADER);

    private readonly Packet original;

    int cursorDefaultValue;
    int localCursor;

    public ReadonlyPacket(Packet packet) {
        original = packet;
        localCursor = cursorDefaultValue = packet.SizeIncludedFixedArea;
    }

    public Packet.HEADER Header {
        get {
            return original.Head;
        }
    }

    public int Size {
        get {
            return original.SizeExceptFixedArea;
        }
    }

    public void ResetCursor() {
        this.localCursor = cursorDefaultValue;
    }

    public Int32 Pop_Int32() {
        if (localCursor < HEADER_SIZE + sizeof(Int32) + sizeof(Int32))
        {
            throw new Exception("PacketDataIsNull");
        }
        localCursor -= sizeof(Int32);
        return BitConverter.ToInt32(this.original.Data, localCursor);
    }

    public UInt32 Pop_UInt32() {
        if (localCursor < HEADER_SIZE + sizeof(Int32) + sizeof(UInt32))
        {
            throw new Exception("PacketDataIsNull");
        }
        localCursor -= sizeof(UInt32);
        return BitConverter.ToUInt32(this.original.Data, localCursor);
    }

    public float Pop_Float() {
        if (localCursor < HEADER_SIZE + sizeof(Int32) + sizeof(float))
        {
            throw new Exception("PacketDataIsNull");
        }
        localCursor -= sizeof(float);
        return BitConverter.ToSingle(this.original.Data, localCursor);
    }

    public bool Pop_Bool() {
        if (localCursor < HEADER_SIZE + sizeof(Int32) + sizeof(bool))
        {
            throw new Exception("PacketDataIsNull");
        }
        localCursor -= sizeof(bool);
        return BitConverter.ToBoolean(this.original.Data, localCursor);
    }

    public string Pop_String() {
        int length = this.Pop_Int32();

        if (localCursor < HEADER_SIZE + sizeof(Int32) + length)
        {
            throw new Exception("PacketDataIsNull");
        }
        localCursor -= length;
        return Encoding.Unicode.GetString(this.original.Data, localCursor, length);
    }
}
