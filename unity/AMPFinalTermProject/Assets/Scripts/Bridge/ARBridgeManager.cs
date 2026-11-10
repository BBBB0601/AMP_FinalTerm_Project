using System;
using UnityEngine;
using UnityEngine.Events;

namespace RoomFitAR.Bridge
{
    /// <summary>
    /// [SFR-002 / INR-001 / PER-001] Android Native와 Unity AR 런타임 간의 양방향 IPC 브릿지 매니저.
    /// - Android -> Unity: UnityPlayer.UnitySendMessage("ARBridgeManager", "OnReceiveFurnitureData", jsonString)
    /// - Unity -> Android: JNI currentActivity.Call("OnCollisionStateChanged", isColliding) 및 JSON 피드백 전달
    /// </summary>
    [DisallowMultipleComponent]
    public class ARBridgeManager : MonoBehaviour
    {
        public const string RequiredGameObjectName = "ARBridgeManager";

        public static ARBridgeManager Instance { get; private set; }

        [Header("Debug & Editor Test Config")]
        [SerializeField]
        [Tooltip("에디터 단독 테스트용 샘플 JSON 문자열 (우클릭 컨텍스트 메뉴로 전송 테스트 가능)")]
        private string testFurnitureJson = "{\"name\":\"원목 단독 책상\",\"width\":1.20,\"depth\":0.60,\"height\":0.75,\"unit\":\"meter\"}";

        [Header("Unity Events (Inspector Wiring)")]
        public UnityEvent<FurnitureConfigData> onFurnitureConfigReceived = new();
        public UnityEvent<bool> onClearanceGuideToggled = new();
        public UnityEvent<bool> onPlacementLockToggled = new();
        public UnityEvent onResetPlacementRequested = new();

        public event Action<FurnitureConfigData> FurnitureDataReceived;
        public event Action<bool> ClearanceGuideToggled;
        public event Action<bool> PlacementLockToggled;
        public event Action ResetPlacementRequested;

        public FurnitureConfigData LatestFurnitureConfig { get; private set; }
        public bool IsLastStateColliding { get; private set; }
        public string LastCollisionTarget { get; private set; } = CollisionEventData.TargetNone;

        private bool hasSentInitialCollisionState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (gameObject.name != RequiredGameObjectName)
            {
                Debug.LogWarning($"[ARBridgeManager] UnitySendMessage 수신 보장을 위해 오브젝트 이름을 '{gameObject.name}'에서 '{RequiredGameObjectName}'(으)로 변경합니다.");
                gameObject.name = RequiredGameObjectName;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #region Android -> Unity 수신 메서드 (UnitySendMessage Target)

        /// <summary>
        /// [SFR-002] Android Native에서 가구 치수 JSON을 전송할 때 호출되는 공식 엔트리 포인트.
        /// 호출 규격: UnityPlayer.UnitySendMessage("ARBridgeManager", "OnReceiveFurnitureData", jsonString)
        /// </summary>
        public void OnReceiveFurnitureData(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                Debug.LogError("[ARBridgeManager] 수신된 JSON 문자열이 비어 있습니다.");
                return;
            }

            try
            {
                FurnitureConfigData config = JsonUtility.FromJson<FurnitureConfigData>(jsonString);
                if (config == null)
                {
                    Debug.LogError($"[ARBridgeManager] JSON 역직렬화 결과가 null입니다. 원본: {jsonString}");
                    return;
                }

                config.NormalizeToMetersIfNeeded();

                if (!config.Validate(out string validationError))
                {
                    Debug.LogError($"[ARBridgeManager] 가구 치수 데이터 검증 실패: {validationError} | 원본: {jsonString}");
                    return;
                }

                LatestFurnitureConfig = config;
                Debug.Log($"[ARBridgeManager] 가구 데이터 수신 완료: {config.name} ({config.width:F2}m x {config.depth:F2}m x {config.height:F2}m)");

                FurnitureDataReceived?.Invoke(config);
                onFurnitureConfigReceived?.Invoke(config);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ARBridgeManager] JSON 파싱 중 예외 발생: {ex.Message}\n원본 JSON: {jsonString}");
            }
        }

        /// <summary>
        /// [SFR-006 / SCR-002] Android 하단 툴바에서 60cm 통행 동선 가이드라인 표시 On/Off 토글 시 호출.
        /// 호출 규격: UnityPlayer.UnitySendMessage("ARBridgeManager", "OnToggleClearanceGuide", "true" or "false")
        /// </summary>
        public void OnToggleClearanceGuide(string stateString)
        {
            bool isVisible = ParseBooleanString(stateString, true);
            Debug.Log($"[ARBridgeManager] 60cm 동선 가이드라인 표시 상태 변경: {isVisible}");

            ClearanceGuideToggled?.Invoke(isVisible);
            onClearanceGuideToggled?.Invoke(isVisible);
        }

        /// <summary>
        /// [SFR-004 / SCR-002] Android 툴바에서 가구 배치 고정(Lock) / 재배치(Unlock) 전환 시 호출.
        /// 호출 규격: UnityPlayer.UnitySendMessage("ARBridgeManager", "OnSetPlacementLock", "true" or "false")
        /// </summary>
        public void OnSetPlacementLock(string lockStateString)
        {
            bool isLocked = ParseBooleanString(lockStateString, false);
            Debug.Log($"[ARBridgeManager] 가구 배치 잠금 상태 변경: {isLocked}");

            PlacementLockToggled?.Invoke(isLocked);
            onPlacementLockToggled?.Invoke(isLocked);
        }

        /// <summary>
        /// [SCR-002] Android에서 현재 배치된 가구를 초기화하고 다시 조준 모드로 돌아갈 때 호출.
        /// 호출 규격: UnityPlayer.UnitySendMessage("ARBridgeManager", "OnResetPlacement", "")
        /// </summary>
        public void OnResetPlacement(string unused = "")
        {
            Debug.Log("[ARBridgeManager] 가구 배치 초기화 요청 수신");

            ResetPlacementRequested?.Invoke();
            onResetPlacementRequested?.Invoke();
        }

        #endregion

        #region Unity -> Android 역방향 송신 메서드 (JNI Callback)

        /// <summary>
        /// [SFR-002 / SFR-005] OBB 충돌 판정 모듈(김기현 학우 파트)에서 충돌 상태가 변할 때 호출하는 헬퍼 메서드.
        /// 상태가 이전 프레임과 동일하면 불필요한 JNI 호출을 생략하여 PER-001(16ms 이내 지연)을 보장한다.
        /// </summary>
        public void ReportCollisionState(bool isColliding, string targetType = CollisionEventData.TargetWall, string message = "")
        {
            string normalizedTarget = isColliding
                ? (string.IsNullOrEmpty(targetType) ? CollisionEventData.TargetWall : targetType)
                : CollisionEventData.TargetNone;

            if (hasSentInitialCollisionState &&
                IsLastStateColliding == isColliding &&
                string.Equals(LastCollisionTarget, normalizedTarget, StringComparison.Ordinal))
            {
                return;
            }

            string resolvedMessage = message;
            if (string.IsNullOrEmpty(resolvedMessage))
            {
                resolvedMessage = isColliding
                    ? (normalizedTarget == CollisionEventData.TargetFurniture ? "기존 가구와 간섭 발생" : "벽면 침범 발생")
                    : "배치 가능 영역";
            }

            CollisionEventData eventData = new CollisionEventData(isColliding, normalizedTarget, resolvedMessage);
            SendCollisionEventToAndroid(eventData);
        }

        /// <summary>
        /// [SFR-002] CollisionEventData 객체를 직렬화하고 Android Native 콜백(OnCollisionStateChanged)을 호출한다.
        /// </summary>
        public void SendCollisionEventToAndroid(CollisionEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            hasSentInitialCollisionState = true;
            IsLastStateColliding = eventData.isColliding;
            LastCollisionTarget = eventData.target;

            string jsonPayload = JsonUtility.ToJson(eventData);

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (currentActivity != null)
                    {
                        // 로드맵 합의 규격 1: boolean 기반 즉시 콜백
                        currentActivity.Call("OnCollisionStateChanged", eventData.isColliding);

                        // 로드맵 합의 규격 2: 상세 JSON 페이로드 콜백 (안드로이드 Activity에 오버로드가 정의된 경우 호출)
                        try
                        {
                            currentActivity.Call("OnCollisionEventReceived", jsonPayload);
                        }
                        catch (Exception)
                        {
                            // Android 측에 단일 boolean 콜백만 구현된 경우 무시
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ARBridgeManager] Android JNI 콜백 호출 실패: {ex.Message} | Payload: {jsonPayload}");
            }
#else
            Debug.Log($"[ARBridgeManager -> Android Mock JNI] OnCollisionStateChanged({eventData.isColliding}) | JSON: {jsonPayload}");
#endif
        }

        #endregion

        #region Editor Context Menu & Utility

        [ContextMenu("Test: Send Sample Furniture JSON")]
        public void TestSendSampleFurnitureJson()
        {
            OnReceiveFurnitureData(testFurnitureJson);
        }

        [ContextMenu("Test: Report Wall Collision (True)")]
        public void TestReportWallCollision()
        {
            ReportCollisionState(true, CollisionEventData.TargetWall, "벽면 침범 발생");
        }

        [ContextMenu("Test: Report Collision Cleared (False)")]
        public void TestReportCollisionCleared()
        {
            ReportCollisionState(false, CollisionEventData.TargetNone, "배치 가능 영역");
        }

        private static bool ParseBooleanString(string rawValue, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultValue;
            }

            string trimmed = rawValue.Trim();
            if (string.Equals(trimmed, "1", StringComparison.Ordinal) ||
                string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "on", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(trimmed, "0", StringComparison.Ordinal) ||
                string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "off", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return defaultValue;
        }

        #endregion
    }
}
