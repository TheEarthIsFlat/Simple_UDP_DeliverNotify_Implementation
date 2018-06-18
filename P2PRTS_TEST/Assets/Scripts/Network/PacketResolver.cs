using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;


public class PacketResolver
{

    byte[] currentData = new byte[Config.MAX_SESSION_BUFFER_SIZE * 4];

    int endIndex = 0;
    int startIndex = 0;

    public void Clear()
    {
        endIndex = 0;
        startIndex = 0;
        Array.Clear(currentData, 0, currentData.Length);
    }

    public Stack<Packet> Resolve(byte[] buffer, int offset, int transferred)
    {
        Array.Copy(buffer, offset, currentData, endIndex, transferred);

        endIndex += transferred;

        Stack<Packet> resultPacketStack = new Stack<Packet>(currentData.Length / 8);

        while (endIndex - startIndex >= Packet.HEADER_SIZE + sizeof(Int32))
        { //8바이트 이상의 데이터가 들어있을때(패킷하나를 완성할수있는 최소조건
            Int32 size = BitConverter.ToInt32(currentData, Packet.HEADER_SIZE + startIndex); //사이즈 변환
            int overBytesLength = endIndex - (size + Packet.HEADER_SIZE + sizeof(Int32)); //패킷사이즈 보다 추가로 받은 바이트 수

            if (overBytesLength < 0) { break; } //사이즈에 적힌양보다 데이터가 적다면 루프 탈출

            if (size > (Config.MAX_SESSION_BUFFER_SIZE - (Packet.HEADER_SIZE + sizeof(Int32))))//사이즈가 패킷의 최대 한계보다 크다면(뭔가이상한거임)
            {
                throw new Exception("Transferred Size Parameter over max session buffer size");
            }
            else
            {
                Packet packet = new Packet(Packet.Int32ToHead(BitConverter.ToInt32(currentData, startIndex)), size);

                if (size > 0)
                {
                    byte[] body = new byte[size];

                    Array.Copy(currentData, startIndex + Packet.HEADER_SIZE + sizeof(Int32), body, 0, size);

                    packet.Push(body);
                }

                startIndex += packet.SizeIncludedFixedArea;
                resultPacketStack.Push(packet);
            }
        }

        int remainBytesSize = endIndex - startIndex;
        remainBytesSize = remainBytesSize < 0 ? 0 : remainBytesSize;
        if (remainBytesSize > 0)
        {
            byte[] temp = new byte[remainBytesSize];

            Array.Copy(currentData, startIndex, temp, 0, remainBytesSize);
            Array.Clear(currentData, 0, currentData.Length);
            Array.Copy(temp, currentData, temp.Length);
        }

        startIndex = 0;
        endIndex = remainBytesSize;

        return resultPacketStack;
    }
}

