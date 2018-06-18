using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILogic : MonoBehaviour {

    [SerializeField]
    GameObject landingScreen;

    [SerializeField]
    GameObject makeGameScreen;

    [SerializeField]
    GameObject joinGameScreen;

    public void OpenJoinGameScreen() {
        landingScreen.SetActive(false);
        makeGameScreen.SetActive(false);
        joinGameScreen.SetActive(true);
    }

    public void OpenMakeGameScreen() {
        landingScreen.SetActive(false);
        makeGameScreen.SetActive(true);
        joinGameScreen.SetActive(false);
    }

    public void OpenLandingPage() {
        landingScreen.SetActive(true);
        makeGameScreen.SetActive(false);
        joinGameScreen.SetActive(false);
    }
}
