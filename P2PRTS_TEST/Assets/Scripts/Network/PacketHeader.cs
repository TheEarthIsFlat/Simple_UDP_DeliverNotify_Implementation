using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


public partial class Packet
{
    public enum HEADER : Int32
    {
        __FEED_BACK = 100, // 피드백 패킷(원격엔드포인트에서 수신되거나 전송하는게 아닌
                            // 네트워크 io과정에서 생긴 이벤트를 유니티 쓰레드에서 처리하기 위한 패킷
                            // 패킷을 제작하고 바로 ReceiveQueue로 집어넣음, 그러면 유니티스레드에서 해당 패킷을 읽음.
        FB_CONNECT_SUCCESS, 
        FB_CONNECT_FAILED,
        FB_DISCONNECT,
        FB_ACCEPT_START,
        FB_ACCEPT_CANCEL,
        FB_CONNECT_START,


        _DEFAULT = 1000,

        __HEART_BEAT,

        // RUDP 페이로드에 필요한 데이터를 담는 패킷
        RUDP,

        MESSAGE,

        __WRONG,

        __END
    }


    public static HEADER Int32ToHead(Int32 num) {
        if (Enum.IsDefined(typeof(HEADER), num))
        {
            return (HEADER)num;
        }
        else {
            return HEADER.__WRONG;
        }
    }
}

