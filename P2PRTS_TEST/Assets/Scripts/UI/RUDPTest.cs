using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RUDPTest : UnityPeerPacketHandle {

    [SerializeField]
    Text label;

    protected override void OnEnable()
    {
        base.OnEnable();
        StartCoroutine("CheckNetworkStatistics");
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopCoroutine("CheckNetworkStatistics");
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < 5; i++) {
            Packet test = new Packet(Packet.HEADER.RUDP, 8);
            RUDPUnityManager.Instance.SendDeliverNotify(test, null);
        }
    }

    IEnumerator CheckNetworkStatistics() {
        while (true) {
            DeliverStatistics statistics = RUDPUnityManager.Instance.GetDeliverStatistics();
            statistics.LossRate =  (float)statistics.FailedPayloadCount / (float)statistics.AllPayloadCount;
            string str = String.Format(
                "Sending Payloads : {0} , Success : {1} , Loss : {2} \n Loss Rate : {3}%",
                statistics.AllPayloadCount, statistics.SuccessPayloadCount, statistics.FailedPayloadCount, statistics.LossRate);

            this.label.text = str;

            yield return new WaitForSecondsRealtime(0.33f);
        }
    }

    public void OnDisconnectButton() {
        RUDPUnityManager.Instance.Disconnect(false);
    }

    protected override void OnMessage(ReadonlyPacket packet)
    {
        switch (packet.Header) {
            case Packet.HEADER.FB_DISCONNECT: {
                    this.gameObject.SetActive(false);
                    break;
            }
        }
    }
}
