using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogView : MonoBehaviour {

    [SerializeField]
    Text logText;

    static LogView instance = null;

    private void Awake()
    {
        if (instance == null)
        {
            Object.DontDestroyOnLoad(this);
            instance = this;
        }
        else {
            Destroy(this);
        }
    }

    public static void Log(string msg) {
        instance.logText.text = msg;
    }
}
