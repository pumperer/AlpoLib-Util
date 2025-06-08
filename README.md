# AlpoLib Util

## Sequential State Machine
 - 순차적으로 해야 하는 일들을 정의하는 시스템
 - 예를 들어, 로비 진입시 해야 하는 일들을 순차적으로 등록하고 하나씩 검사한다.

## RechargeTimer
 - 쿨타임 가지고 일정 시간마다 특정량이 증가하는 것을 관리하는 타이머
 - 충전 쿨타임을 가지는 하트, 에너지 재화
 - 사용 쿨타임을 가지는 ... 뭐가 있지??

## Application Pause Listener
 - 앱의 pause 상태를 감지한다.
 - 이 곳에 이벤트를 등록해서 사용한다.

## Cached UI Behaviour
 - rect transform 을 cache 하고 있다.

## Coroutine Task Manager
 - 글로벌하게 관리하고 동작하는 코루틴 매니저이다.
 
## Date Change Broadcaster
 - 날짜 변경을 감지하고 알려준다.
 
## Dont Destroy GameObject
 - DontDestroy 객체를 관리한다.
 - 앱을 재시작해야하는 로직을 태울 때, 이곳에서 삭제하거나 한다.
 
## Game State Manager
 - 로컬에 데이터을 저장한다.
 - Generic 방식으로 키는 클래스 이다.
 
## Task Scheduler
 - 알람, 스탑워치, 주기적 호출 태스크
 
## Throttle Action
 - 지연된 호출 기능이다.
 - 지연이 끝나기 전에 다시 호출이 들어가면 재지연된다.
 
# Type Helper
 - Assembly 에서 클래스 Type 찾는 기능이다.
