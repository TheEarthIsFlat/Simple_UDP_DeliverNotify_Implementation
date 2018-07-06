using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MakeGame : UnityPeerPacketHandle {

    bool isWaiting = false;

    [SerializeField]
    RUDPTest rudpTest;

    [SerializeField]
    UILogic uiLogic;

    [SerializeField]
    InputField nicknameInput;

    [SerializeField]
    Dropdown mapSelector;

    [SerializeField]
    Dropdown tribeSelector;

    [SerializeField]
    Button confirmButton;

    [SerializeField]
    Text confirmButtonText;

    [SerializeField]
    Text messageLabel;


    protected override void OnEnable()
    {
        base.OnEnable();
        this.confirmButton.onClick.AddListener(ConfirmGameCurrentOption);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        this.confirmButton.onClick.RemoveListener(ConfirmGameCurrentOption);
    }

    public void OnBackButton() {
        if (isWaiting)
        {

        }
        else {
            uiLogic.OpenLandingPage();
        }
    }

    public void ConfirmGameCurrentOption() {
        RUDPUnityManager.Instance.ConnectWait();
    }

    public void CancelWaiting() {
        RUDPUnityManager.Instance.CancelWait();
    }

    private void OnAcceptStart() {

        if (isWaiting) return;

        isWaiting = true;

        // 옵션 요소들 끄기
        nicknameInput.interactable = false;
        mapSelector.interactable = false;
        tribeSelector.interactable = false;

        //버튼 텍스트 바꿈
        confirmButtonText.text = "취소";

        //버튼 콜백 바꿈
        confirmButton.onClick.RemoveListener(ConfirmGameCurrentOption);
        confirmButton.onClick.AddListener(CancelWaiting);

        //상태 레이블 변경
        messageLabel.text = "다른 유저를 기다리는중...";
    }

    private void ReturnInitialState() {

        if (!isWaiting) return;

        isWaiting = false;

        // 옵션 요소들 끄기
        nicknameInput.interactable = true;
        mapSelector.interactable = true;
        tribeSelector.interactable = true;

        //버튼 텍스트 바꿈
        confirmButtonText.text = "확정";

        //버튼 콜백 바꿈
        confirmButton.onClick.RemoveListener(CancelWaiting);
        confirmButton.onClick.AddListener(ConfirmGameCurrentOption);

        //상태 레이블 변경
        messageLabel.text = "게임 생성";
    }

    protected override void OnMessage(ReadonlyPacket packet)
    {
        switch (packet.Header) {
            case Packet.HEADER.FB_ACCEPT_START: {
                    Debug.Log("ACCEPT START");
                    OnAcceptStart();
                    break;
                }
            case Packet.HEADER.FB_CONNECT_SUCCESS: {
                    this.rudpTest.gameObject.SetActive(true);
                    Debug.Log("CONNECT SUCCESS");
                    break;
                }
            case Packet.HEADER.FB_CONNECT_FAILED: {
                    Debug.Log("CONNECT FAILED");
                    ReturnInitialState();
                    break;
                }
            case Packet.HEADER.FB_ACCEPT_CANCEL: {
                    Debug.Log("ACCEPT CANCEL");
                    ReturnInitialState();
                    break;
                }
            case Packet.HEADER.FB_DISCONNECT: {
                    Debug.Log("DISCONNECT");
                    ReturnInitialState();
                    break;
                }
            case Packet.HEADER.MESSAGE: {
                    string msg = packet.Pop_String();
                    Debug.Log(msg);
                    this.messageLabel.text = msg;
                    break;
                }
                
        }
    }
}
