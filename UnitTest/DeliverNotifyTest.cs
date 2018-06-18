using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace UnitTest
{
    [TestClass]
    public class DeliverNotifyTest
    {
        Packet emptyPacket = new Packet(Packet.HEADER.RUDP, 8);


        // Confirm 모듈 테스트
        // 받아야할 패킷보다 이전것이 올때(예상 시퀀스 번호보다 낮음)
        // 해당패킷은 처리하지않는다. False Return
        [TestMethod]
        public void ConfirmModuleTest()
        {
            RUDPDeliverConfirmModule confirmModule = new RUDPDeliverConfirmModule();

            confirmModule.Reset(ushort.MaxValue - 3); // 시퀀스 번호 최대값에 근접시키기(오버플로우 테스트 위해)

            // 시퀀스 번호가 낮을때 False Return
            RUDPPayload payload1 = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.DELIVERY_NOFIFY, ushort.MaxValue - 4);
            payload1.Packet = emptyPacket;

            bool result = confirmModule.JudgeIncomePayload(payload1);

            Assert.IsTrue(result == false);

            // 시퀀스 번호가 같을때 True Return
            RUDPPayload payload2 = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.DELIVERY_NOFIFY, ushort.MaxValue - 3);
            payload2.Packet = emptyPacket;

            bool result2 = confirmModule.JudgeIncomePayload(payload2);

            Assert.IsTrue(result2 == true);

            //시퀀스 번호가 높을때 (위에 테스트때문에 현재 ConfirmModule의 다음예상시퀀스는 ushort.MaxValue-2 임
            RUDPPayload payload3 = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.DELIVERY_NOFIFY, ushort.MaxValue-1);
            payload3.Packet = emptyPacket;

            bool result3 = confirmModule.JudgeIncomePayload(payload3);

            Assert.IsTrue(result3 == true);

            //시퀀스 번호 오버플로우 구간일때(true 리턴해야함)
            RUDPPayload payload4 = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.DELIVERY_NOFIFY, 200);
            RUDPPayload payload5 = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.DELIVERY_NOFIFY, 201);
            RUDPPayload payload6 = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.DELIVERY_NOFIFY, 202);
            RUDPPayload payload7 = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.DELIVERY_NOFIFY, 203);

            payload4.Packet = emptyPacket;
            payload5.Packet = emptyPacket;
            payload6.Packet = emptyPacket;
            payload7.Packet = emptyPacket;

            bool result4 = confirmModule.JudgeIncomePayload(payload4);

            Assert.IsTrue(result4 == true);

            // 밑에 AckRange검사용
            confirmModule.JudgeIncomePayload(payload5);
            confirmModule.JudgeIncomePayload(payload6);
            confirmModule.JudgeIncomePayload(payload7);


            //AckRange 가 잘 나오는지 검사
            Queue<AckRange> acks = confirmModule.PopPendingAckRanges();

            AckRange ack1 = acks.Dequeue();
            Assert.IsTrue(ack1.StartNum == (ushort.MaxValue - 3) && ack1.AckCount == 1);

            AckRange ack2 = acks.Dequeue();
            Assert.IsTrue(ack2.StartNum == (ushort.MaxValue - 1) && ack2.AckCount == 1);

            AckRange ack3 = acks.Dequeue();
            Assert.IsTrue(ack3.StartNum == 200 && ack3.AckCount == 4);
        }

        [TestMethod]
        public void TrackingModuleTest() {
            RUDPDeliverTrackingModule trackingModule = new RUDPDeliverTrackingModule();

            ushort seed = 65520;
            ushort payloadsCount = 0;

            trackingModule.Reset(65520);

            for (ushort i = seed; i != 20; i++) {
                RUDPPayload newPayload = new RUDPPayload(RUDPPayload.PAYLOAD_TAG.DELIVERY_NOFIFY, 0);
                newPayload.Packet = this.emptyPacket;
                trackingModule.PrepareToDeliver(newPayload);
                payloadsCount++;
            }

            // 몇가지 Seq를 뺀 Ack를 만들어서 trackingModule에서 Ack를 처리하게 해야함
            // 그리고 결과를 처음 빠트린 Ack와 정상 ACk와 일치하는지 검증

            ushort halfPoint = seed;
            halfPoint += (ushort)(payloadsCount / 2);

            AckRange ackR = new AckRange(halfPoint, (ushort)(payloadsCount/2));

            trackingModule.ProcessAckRange(ackR);

            ushort[] success = trackingModule.GetAndClearSuccessPayloadSeqNums();
            ushort[] fail = trackingModule.GetAndClearLossPayloadSeqNums();

            Assert.IsTrue(ackR.AckCount == success.Length);
            Assert.IsTrue((payloadsCount - ackR.AckCount) == fail.Length);

            for (int i = 0; i < ackR.AckCount; i++) { //성공한 Ack
                Assert.IsTrue((ackR.StartNum + i) == success[i]);
            }

            ushort start = seed;
            for (ushort i = 0; i < (payloadsCount - ackR.AckCount); i++) { //실패한 Ack
                Assert.IsTrue((ushort)start == fail[i]);
                start++;
            }
        }
    }
}
