using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Config {

    public static readonly int MAX_SESSION_BUFFER_SIZE = 1024;
    public static readonly int MAX_HEARTBEAT_PERIOD = 8000;
    public static readonly short PORT = 26578;

    public static readonly int ACK_TIMER_VALUE = 50; //ms

    public static readonly int PACKET_DROP_SIMULATION_RATIO = 10; //100개의 패킷중 몇개의 패킷을 드랍할지 결정하는 비율 (디버그환경에서만 해당 시뮬레이션이 동작함.)
}

