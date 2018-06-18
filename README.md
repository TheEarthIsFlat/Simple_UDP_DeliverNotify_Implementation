# Simple_UDP_DeliverNotify_Implementation
간단한 UDP 배달 통지 모듈 개발

#개발
Unity3D와 mono C#으로 개발되었습니다.


# 설명(Deliver Notify)
배달 통지 모듈(RUDPClient의 SendDeliverNotify 함수 호출로 사용)
- UDP 패킷에 Sequence 번호를 부여
- Sequence 번호가 부여된 패킷을 InflightPayloads에 저장
- 패킷을 전송
- 패킷을 받는 쪽에서 주기적으로(Config에 50ms로 설정됨) 받은 패킷들의 Sequence번호를 Ack패킷에 담아서 전송
- ACK확인이 된 패킷들은 전송이 성공된 것으로 판단하여 콜백 호출

연결
- TCP의 Three way handshake 를 모방하여 연결을 구현했습니다.
- 연결과정은 쓰레드를 이용한 동기통신으로 구현되었습니다.

디버그
- 테스트프로젝트가 포함되어있습니다. 여기에 Payload와 Packet, DeliverNotify에 관한 코드를 테스트하는 스크립트가 포함되어있습니다.
- 디버그 모드로 해당 프로젝트를 빌드하면 특정비율(Config에 20으로 정의되어 있습니다) 만큼의 패킷을 드랍하도록(보내지않도록) 구현되어있습니다. 실제 통신환경에서 패킷이 드랍되는 것을 간단하게 재현할 목적입니다.

# 참조
- DeliverNotify 로 보내진 Payload간의 순서는 보장됩니다. 
- Ack자체가 유실될 가능성이 있으므로, 실제로는 보내졌으나 전송 실패 Callback이 호출될 가능성이 있습니다. 만약 고유성이 보장되어야 할 데이터라면 이들의 고유함을 보장할 책임은 상위 모듈에 있다는 것을 참조해주십시오.
- 원래는 연결에 비동기 통신을 이용하려 했지만, UDP 통신에서 원격종단점의 정보를 비동기 통신으로 얻어올려면 ReceiveMessageFromAsync 함수를 이용해야하는데 monoC# 은 이를 구현해두지 않았습니다. 그래서 쓰레드를 이용한 동기통신으로 구현하였습니다.
- 패킷의 드랍 비율을 체크해보면 Config에 정의된 비율보다 좀 더 드랍되는데 이것은 확인응답 Ack 패킷또한 드랍시키기 때문입니다.
