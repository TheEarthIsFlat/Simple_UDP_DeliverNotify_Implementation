using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;

public class JoinGame : UnityPeerPacketHandle {

    bool isConnecting = false;

    [SerializeField]
    RUDPTest rudpTest;

    [SerializeField]
    UILogic uiLogic;

    [SerializeField]
    InputField ipInput;

    [SerializeField]
    InputField nicknameInput;

    [SerializeField]
    Dropdown tribeSelector;

    [SerializeField]
    Text messageLabel;

    [SerializeField]
    Button joinButton;

    [SerializeField]
    Button backButton;

    private void Start()
    {
        joinButton.onClick.AddListener(JoinGameCurrentOption);
    }


    public void JoinGameCurrentOption()
    {
        IPAddress ip;
        if (IPAddress.TryParse(ipInput.text, out ip))
        {
            RUDPUnityManager.Instance.ConnectTo(ip.ToString());
        }
        else {
            Debug.Log("Wrong IP");
        }
    }

    void OnConnectStart() {
        //옵션들 비활성화
        isConnecting = true;
        ipInput.interactable = false;
        nicknameInput.interactable = false;
        tribeSelector.interactable = false;
        joinButton.interactable = false;
        messageLabel.text = "연결 시도 중...";
    }

    void ReturnInitialState() {
        isConnecting = false;
        ipInput.interactable = true;
        nicknameInput.interactable = true;
        tribeSelector.interactable = true;
        joinButton.interactable = true;
        messageLabel.text = "게임 참가";
    }

    public void OnBackButton()
    {
        if (isConnecting)
        {
            
        }
        else {
            uiLogic.OpenLandingPage();
        }
    }

    protected override void OnMessage(ReadonlyPacket packet)
    {
        switch (packet.Header) {
            case Packet.HEADER.FB_CONNECT_FAILED: {
                    ReturnInitialState();
                    break;
                }
            case Packet.HEADER.FB_CONNECT_START: {
                    OnConnectStart();
                    break;
                }
            case Packet.HEADER.FB_CONNECT_SUCCESS: {
                    rudpTest.gameObject.SetActive(true);
                    break;
                }
            case Packet.HEADER.FB_DISCONNECT: {
                    ReturnInitialState();
                    break;
                }
            case Packet.HEADER.MESSAGE:
                {
                    this.messageLabel.text = packet.Pop_String();
                    break;
                }
        }
    }
}
