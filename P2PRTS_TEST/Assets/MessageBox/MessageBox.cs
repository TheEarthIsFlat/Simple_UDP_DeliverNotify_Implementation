using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageBox {

    public static void Show(string title, string massage, string button1Text, Action onButton1) { }

    public static void Show(string title, string message, string button1Text, string button2Text, Action onButton1, Action onButton2) { }

    public static void Show(string title, string message, string button1Text, string button2Text, string button3Text, Action onButton1, Action onButton2, Action onButton3) { }
}
