using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplicationValueSetting : MonoBehaviour {

    void Awake() {
        Application.runInBackground = true;
    }
}
