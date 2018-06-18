using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace UnitTest
{
    [TestClass]
    public class PacketTest
    {
        [TestMethod]
        public void PacketTestMethod()
        {
            int dataInt32 = 1234567;
            string dataString = "If you see this, you will turn into great one. 한글도잘돼";
            float dataSingle = 3.14f;
            byte[] somebytes = Encoding.Unicode.GetBytes("아는 형님");

            Packet packet = new Packet(Packet.HEADER.MESSAGE);

            packet.Push(dataInt32);
            packet.Push(dataString);
            packet.Push(dataSingle);
            packet.Push(somebytes);

            //Packet -> Bytes -> Packet 변환 테스트
            Packet testFromBytes = Packet.BytesToPacket(packet.Data, packet.SizeIncludedFixedArea);

            // Push Pop 테스트
            byte[] popSomeBytes = packet.Pop_Bytes();
            float popSingle = packet.Pop_Float();
            string popString = packet.Pop_String();
            int popInt = packet.Pop_Int32();

            Assert.IsTrue(popSomeBytes.SequenceEqual(somebytes));
            Assert.AreEqual(popSingle, dataSingle);
            Assert.AreEqual(popString, dataString);
            Assert.AreEqual(popInt, dataInt32);

            //Packet 복사테스트
            Packet clone = packet.Clone();
            Assert.IsTrue(clone.Data.SequenceEqual(packet.Data.Take(packet.SizeIncludedFixedArea)));
        }

        [TestMethod]
        public void RUDPPayloadTest() {
            RUDPPayload payload1 = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.RAW, 20);
            RUDPPayload payload2 = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.ACK, 21);
            RUDPPayload payload3 = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.DELIVERY_NOFIFY, 22);

            Packet test1 = new Packet(Packet.HEADER.RUDP);
            test1.Push(23);
            test1.Push("If you see this, you turn into great one.");
            Packet test2 = new Packet(Packet.HEADER.MESSAGE);
            test2.Push(34);
            test2.Push("I will not give up, my life is totally mine");

            // 아무 패킷이 안붙을때 붙는 빈 더미 패킷
            Packet test3 = new Packet(Packet.HEADER.RUDP, 8);

            payload1.Packet = test1;
            payload2.Packet = test2;
            payload3.Packet = test3;

            byte[] allbytes = new byte[payload1.Size + payload2.Size + payload3.Size];

            Buffer.BlockCopy(payload1.ToBytes(), 0, allbytes, 0, payload1.Size);
            Buffer.BlockCopy(payload2.ToBytes(), 0, allbytes, payload1.Size, payload2.Size);
            Buffer.BlockCopy(payload3.ToBytes(), 0, allbytes, payload1.Size + payload2.Size, payload3.Size);

            Queue<RUDPPayload> queue = RUDPPayload.BytesToPayloads(allbytes, 0, allbytes.Length);

            RUDPPayload popPayload1 = queue.Dequeue();
            RUDPPayload popPayload2 = queue.Dequeue();
            RUDPPayload popPayload3 = queue.Dequeue();

            Assert.IsTrue(popPayload3.PayloadHeader.SequenceEqual(payload3.PayloadHeader));
            Assert.IsTrue(payload3.Packet.Data.Take(payload3.Packet.SizeIncludedFixedArea).SequenceEqual(popPayload3.Packet.Data));

            Assert.IsTrue(popPayload2.PayloadHeader.SequenceEqual(payload2.PayloadHeader));
            Assert.IsTrue(payload2.Packet.Data.Take(payload2.Packet.SizeIncludedFixedArea).SequenceEqual(popPayload2.Packet.Data));

            Assert.IsTrue(popPayload1.PayloadHeader.SequenceEqual(payload1.PayloadHeader));
            Assert.IsTrue(payload1.Packet.Data.Take(payload1.Packet.SizeIncludedFixedArea).SequenceEqual(popPayload1.Packet.Data));
        }
    }
}
