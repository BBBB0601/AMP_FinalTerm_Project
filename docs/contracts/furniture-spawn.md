# Android ↔ Unity: 가구 체적 메시지 규격

제안서 SFR-002 기준. Unity 쪽 수신 오브젝트는 씬의 GameObject **`ARBridgeManager`** 이다.
`cshFurnitureSpawner`를 이 오브젝트에 붙이거나, 브릿지 스크립트에서 `cshFurnitureSpawner.instance`의 같은 이름 메서드를 호출한다.

## Android → Unity

```kotlin
UnityPlayer.UnitySendMessage("ARBridgeManager", "OnReceiveFurnitureData", json)  // 치수 확정
UnityPlayer.UnitySendMessage("ARBridgeManager", "OnPlaceFurniture", "")           // "배치하기"
UnityPlayer.UnitySendMessage("ARBridgeManager", "OnToggleClearanceGuide", "true") // 동선 가이드 On/Off
UnityPlayer.UnitySendMessage("ARBridgeManager", "OnCancelFurniture", "")          // 배치 전 가구 취소
UnityPlayer.UnitySendMessage("ARBridgeManager", "OnResetFurniture", "")           // AR 배치 초기화
```

### `OnReceiveFurnitureData` payload (FurnitureConfig)

```json
{ "name": "단독 책상", "width": 1.2, "depth": 0.6, "height": 0.75 }
```

| 필드 | 타입 | 필수 | 설명 |
|---|---|---|---|
| `name` | string |  | 가구 이름. 없으면 "가구" |
| `width` | number | ✔ | 가로(W), **m** → Unity X축 |
| `depth` | number | ✔ | 깊이(D), **m** → Unity Z축 |
| `height` | number | ✔ | 높이(H), **m** → Unity Y축 |

- 단위는 **m**이다 (SFR-001: Android가 cm → m로 바꿔서 보냄). 각 축은 **0.1 ~ 3.0m**(10 ~ 300cm)만 받고, 범위를 벗어나면 Unity가 `Debug.Log`를 남기고 무시한다.
- "배치하기" 전에 다시 보내면 새로 만들지 않고 **지금 가구의 치수만 바꾼다**. "배치하기" 뒤에 보내면 새 가구를 만든다.
- 박스의 피벗은 **바닥면 가운데**이고, 앞면(-Z)이 카메라를 향한다. 레티클이 연결돼 있으면 레티클 위치에 놓는다.

## Unity → Android

- `OnCollisionStateChanged(boolean isColliding)`: 가구 중 하나라도 벽면이나 다른 가구와 겹치기 시작하거나 겹침이 풀릴 때 한 번 호출한다.
- Unity 쪽에서는 `cshFurnitureSpawner.onCollisionStateChanged` (UnityEvent&lt;bool&gt;)로 나온다. 브릿지 담당이 여기에 Android 콜백 호출을 연결한다.
