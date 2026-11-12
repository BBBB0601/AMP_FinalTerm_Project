using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using RoomFitAR.Bridge;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace RoomFitAR.AR
{
    /// <summary>
    /// [SFR-004 / QUR-001] ARAnchor 기반 가상 가구 배치, 바닥 고정 및 멀티터치 제스처 조작 컨트롤러.
    /// - 1손가락 터치/드래그: 바닥 평면(HorizontalUp) 히트테스트 기반 수평 이동 및 스냅
    /// - 2손가락 트위스트(Twist): Y축 수직 기준 회전 조작
    /// - 조작 완료 시 ARAnchorManager.AttachAnchor를 통해 물리 평면에 고정하여 카메라 이동 시 드리프트를 억제한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARAnchorPlacementManipulator : MonoBehaviour
    {
        [Header("AR & Bridge References")]
        [SerializeField]
        [Tooltip("바닥 조준 레티클 컴포넌트 (비워두면 씬에서 자동 탐색)")]
        private ARPlacementReticle placementReticle;

        [SerializeField]
        [Tooltip("XR Origin의 ARAnchorManager (비워두면 씬에서 자동 탐색)")]
        private ARAnchorManager anchorManager;

        [SerializeField]
        [Tooltip("Android-Unity IPC 브릿지 매니저 (비워두면 씬에서 자동 탐색)")]
        private ARBridgeManager bridgeManager;

        [Header("Placement & Gesture Settings")]
        [SerializeField]
        [Tooltip("Android에서 가구 치수 JSON 수신 시 현재 조준 위치에 즉시 자동 배치할지 여부")]
        private bool autoPlaceOnDataReceived = true;

        [SerializeField]
        [Tooltip("2손가락 트위스트 회전 감도 배율")]
        private float twistRotationSensitivity = 1.0f;

        [SerializeField]
        [Tooltip("1손가락 드래그 수평 이동 시 위치 보간 속도")]
        private float dragMoveSmoothSpeed = 25.0f;

        [SerializeField]
        [Tooltip("터치 탭과 드래그를 구분하기 위한 최소 픽셀 이동 임계값")]
        private float dragPixelThreshold = 10.0f;

        [Header("Fallback Preview Settings (Phase 1 Standalone)")]
        [SerializeField]
        [Tooltip("절차적 메시 생성기(파트 B) 미연결 시 사용할 기본 가구 설정")]
        private FurnitureConfigData currentFurnitureConfig = new();

        /// <summary>
        /// Phase 2 통합 시 김기현 학우의 절차적 3D 큐브 생성기(SFR-003)를 주입하기 위한 팩토리 델리게이트.
        /// 설정되지 않은 경우 내장된 1:1 스케일 프리뷰 박스 생성기가 자동 동작한다.
        /// </summary>
        public Func<FurnitureConfigData, GameObject> CustomFurnitureFactory { get; set; }

        public GameObject PlacedFurnitureInstance { get; private set; }
        public ARAnchor CurrentBoundAnchor { get; private set; }
        public bool IsPlacementLocked { get; private set; }
        public bool HasPlacedFurniture => PlacedFurnitureInstance != null;

        public event Action<GameObject, FurnitureConfigData> FurniturePlacedOrUpdated;
        public event Action<Pose> FurniturePoseChanged;
        public event Action FurnitureRemoved;

        private ARPlane lastHitFloorPlane;
        private Vector3 targetDragPosition;
        private bool isDraggingSingleFinger;
        private bool isTwistingTwoFingers;
        private Vector2 initialTouchScreenPos;
        private Vector2 previousTwistVector;

        private void Awake()
        {
            if (placementReticle == null)
            {
                placementReticle = FindFirstObjectByType<ARPlacementReticle>();
            }

            if (anchorManager == null)
            {
                anchorManager = FindFirstObjectByType<ARAnchorManager>();
            }

            if (bridgeManager == null)
            {
                bridgeManager = FindFirstObjectByType<ARBridgeManager>();
            }
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();

            if (bridgeManager != null)
            {
                bridgeManager.FurnitureDataReceived += OnFurnitureDataReceived;
                bridgeManager.PlacementLockToggled += SetPlacementLocked;
                bridgeManager.ResetPlacementRequested += ResetPlacedFurniture;
            }
        }

        private void OnDisable()
        {
            if (bridgeManager != null)
            {
                bridgeManager.FurnitureDataReceived -= OnFurnitureDataReceived;
                bridgeManager.PlacementLockToggled -= SetPlacementLocked;
                bridgeManager.ResetPlacementRequested -= ResetPlacedFurniture;
            }

            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            if (IsPlacementLocked)
            {
                return;
            }

            if (Touch.activeTouches.Count > 0)
            {
                HandleTouchInput();
            }
#if UNITY_EDITOR
            else
            {
                HandleEditorMouseFallback();
            }
#endif

            if (isDraggingSingleFinger && PlacedFurnitureInstance != null)
            {
                float t = Mathf.Clamp01(Time.deltaTime * dragMoveSmoothSpeed);
                PlacedFurnitureInstance.transform.position = Vector3.Lerp(
                    PlacedFurnitureInstance.transform.position,
                    targetDragPosition,
                    t);

                FurniturePoseChanged?.Invoke(new Pose(
                    PlacedFurnitureInstance.transform.position,
                    PlacedFurnitureInstance.transform.rotation));
            }
        }

        /// <summary>
        /// [SCR-002] 가구 배치 고정(Lock) / 재배치 허용(Unlock) 상태를 전환한다.
        /// 고정 시 조준 레티클을 숨기고 현재 위치에 ARAnchor를 확실히 바인딩한다.
        /// </summary>
        public void SetPlacementLocked(bool locked)
        {
            IsPlacementLocked = locked;
            isDraggingSingleFinger = false;
            isTwistingTwoFingers = false;

            if (placementReticle != null)
            {
                placementReticle.SetReticleSuppressed(locked || HasPlacedFurniture);
            }

            if (locked && HasPlacedFurniture)
            {
                BindToARAnchor(lastHitFloorPlane);
            }
        }

        /// <summary>
        /// 현재 조준된 레티클 위치에 가구를 즉시 배치하거나 치수를 갱신한다.
        /// </summary>
        public bool TryPlaceOrUpdateAtReticle(FurnitureConfigData config)
        {
            if (config != null)
            {
                currentFurnitureConfig = config;
            }

            if (HasPlacedFurniture)
            {
                RebuildFurnitureGeometry(currentFurnitureConfig);
                return true;
            }

            if (placementReticle == null || !placementReticle.HasValidHit)
            {
                Debug.LogWarning("[ARAnchorPlacementManipulator] 아직 유효한 바닥 평면이 조준되지 않아 탭 시점에 배치를 대기합니다.");
                return false;
            }

            SpawnAndAnchorFurniture(placementReticle.CurrentHitPose, placementReticle.CurrentHitPlane);
            return true;
        }

        /// <summary>
        /// 배치된 가상 가구와 ARAnchor를 제거하고 초기 조준 상태로 복귀한다.
        /// </summary>
        public void ResetPlacedFurniture()
        {
            isDraggingSingleFinger = false;
            isTwistingTwoFingers = false;

            if (PlacedFurnitureInstance != null)
            {
                Destroy(PlacedFurnitureInstance);
                PlacedFurnitureInstance = null;
            }

            DetachAndDestroyCurrentAnchor();

            if (placementReticle != null)
            {
                placementReticle.SetReticleSuppressed(false);
            }

            FurnitureRemoved?.Invoke();
        }

        private void OnFurnitureDataReceived(FurnitureConfigData config)
        {
            currentFurnitureConfig = config;

            if (HasPlacedFurniture)
            {
                RebuildFurnitureGeometry(currentFurnitureConfig);
            }
            else if (autoPlaceOnDataReceived && placementReticle != null && placementReticle.HasValidHit)
            {
                SpawnAndAnchorFurniture(placementReticle.CurrentHitPose, placementReticle.CurrentHitPlane);
            }
        }

        private void HandleTouchInput()
        {
            var touches = Touch.activeTouches;

            // 1. 두 손가락 트위스트 회전 (Y축 회전)
            if (touches.Count >= 2)
            {
                if (isDraggingSingleFinger)
                {
                    FinishSingleFingerDrag();
                }

                if (!HasPlacedFurniture)
                {
                    return;
                }

                Touch touch0 = touches[0];
                Touch touch1 = touches[1];
                Vector2 currentVector = touch1.screenPosition - touch0.screenPosition;

                if (!isTwistingTwoFingers ||
                    touch0.phase == TouchPhase.Began ||
                    touch1.phase == TouchPhase.Began)
                {
                    isTwistingTwoFingers = true;
                    previousTwistVector = currentVector;
                    DetachFromAnchorPreservingWorldTransform();
                    return;
                }

                if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
                {
                    if (previousTwistVector.sqrMagnitude > 1.0f && currentVector.sqrMagnitude > 1.0f)
                    {
                        float deltaAngle = Vector2.SignedAngle(previousTwistVector, currentVector);
                        PlacedFurnitureInstance.transform.Rotate(0f, -deltaAngle * twistRotationSensitivity, 0f, Space.World);

                        FurniturePoseChanged?.Invoke(new Pose(
                            PlacedFurnitureInstance.transform.position,
                            PlacedFurnitureInstance.transform.rotation));
                    }

                    previousTwistVector = currentVector;
                }

                if (touch0.phase == TouchPhase.Ended || touch0.phase == TouchPhase.Canceled ||
                    touch1.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Canceled)
                {
                    isTwistingTwoFingers = false;
                    BindToARAnchor(lastHitFloorPlane);
                }

                return;
            }

            // 2. 한 손가락 탭 배치 또는 드래그 수평 이동
            if (touches.Count == 1)
            {
                if (isTwistingTwoFingers)
                {
                    isTwistingTwoFingers = false;
                    BindToARAnchor(lastHitFloorPlane);
                    return;
                }

                Touch touch = touches[0];

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        initialTouchScreenPos = touch.screenPosition;
                        if (!HasPlacedFurniture)
                        {
                            if (placementReticle != null && placementReticle.HasValidHit)
                            {
                                SpawnAndAnchorFurniture(placementReticle.CurrentHitPose, placementReticle.CurrentHitPlane);
                            }
                        }
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (!HasPlacedFurniture || placementReticle == null)
                        {
                            break;
                        }

                        if (!isDraggingSingleFinger)
                        {
                            float movedDist = Vector2.Distance(initialTouchScreenPos, touch.screenPosition);
                            if (movedDist >= dragPixelThreshold)
                            {
                                isDraggingSingleFinger = true;
                                targetDragPosition = PlacedFurnitureInstance.transform.position;
                                DetachFromAnchorPreservingWorldTransform();
                            }
                        }

                        if (isDraggingSingleFinger &&
                            placementReticle.TryRaycastFloorAtScreenPoint(touch.screenPosition, out Pose hitPose, out ARPlane hitPlane))
                        {
                            targetDragPosition = hitPose.position;
                            if (hitPlane != null)
                            {
                                lastHitFloorPlane = hitPlane;
                            }
                        }
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (isDraggingSingleFinger)
                        {
                            FinishSingleFingerDrag();
                        }
                        break;
                }
            }
        }

#if UNITY_EDITOR
        private void HandleEditorMouseFallback()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || placementReticle == null)
            {
                return;
            }

            Vector2 mousePos = mouse.position.ReadValue();

            // 에디터 좌클릭: 배치 및 수평 드래그 이동
            if (mouse.leftButton.wasPressedThisFrame)
            {
                initialTouchScreenPos = mousePos;
                if (!HasPlacedFurniture && placementReticle.HasValidHit)
                {
                    SpawnAndAnchorFurniture(placementReticle.CurrentHitPose, placementReticle.CurrentHitPlane);
                }
            }
            else if (mouse.leftButton.isPressed && HasPlacedFurniture)
            {
                if (!isDraggingSingleFinger && Vector2.Distance(initialTouchScreenPos, mousePos) >= dragPixelThreshold)
                {
                    isDraggingSingleFinger = true;
                    targetDragPosition = PlacedFurnitureInstance.transform.position;
                    DetachFromAnchorPreservingWorldTransform();
                }

                if (isDraggingSingleFinger &&
                    placementReticle.TryRaycastFloorAtScreenPoint(mousePos, out Pose hitPose, out ARPlane hitPlane))
                {
                    targetDragPosition = hitPose.position;
                    if (hitPlane != null)
                    {
                        lastHitFloorPlane = hitPlane;
                    }
                }
            }
            else if (mouse.leftButton.wasReleasedThisFrame && isDraggingSingleFinger)
            {
                FinishSingleFingerDrag();
            }

            // 에디터 우클릭 드래그: Y축 회전 테스트
            if (mouse.rightButton.isPressed && HasPlacedFurniture)
            {
                float deltaX = mouse.delta.ReadValue().x;
                if (Mathf.Abs(deltaX) > 0.01f)
                {
                    PlacedFurnitureInstance.transform.Rotate(0f, deltaX * 0.4f * twistRotationSensitivity, 0f, Space.World);
                    FurniturePoseChanged?.Invoke(new Pose(
                        PlacedFurnitureInstance.transform.position,
                        PlacedFurnitureInstance.transform.rotation));
                }
            }
        }
#endif

        private void FinishSingleFingerDrag()
        {
            isDraggingSingleFinger = false;
            if (PlacedFurnitureInstance != null)
            {
                PlacedFurnitureInstance.transform.position = targetDragPosition;
                BindToARAnchor(lastHitFloorPlane);
            }
        }

        private void SpawnAndAnchorFurniture(Pose spawnPose, ARPlane targetPlane)
        {
            lastHitFloorPlane = targetPlane;
            targetDragPosition = spawnPose.position;

            PlacedFurnitureInstance = CreateFurnitureInstance(currentFurnitureConfig);
            PlacedFurnitureInstance.transform.SetPositionAndRotation(spawnPose.position, spawnPose.rotation);

            BindToARAnchor(targetPlane);

            if (placementReticle != null)
            {
                placementReticle.SetReticleSuppressed(true);
            }

            FurniturePlacedOrUpdated?.Invoke(PlacedFurnitureInstance, currentFurnitureConfig);
            FurniturePoseChanged?.Invoke(spawnPose);
        }

        private void RebuildFurnitureGeometry(FurnitureConfigData config)
        {
            if (PlacedFurnitureInstance == null)
            {
                return;
            }

            Pose preservedPose = new Pose(
                PlacedFurnitureInstance.transform.position,
                PlacedFurnitureInstance.transform.rotation);

            Destroy(PlacedFurnitureInstance);
            PlacedFurnitureInstance = CreateFurnitureInstance(config);
            PlacedFurnitureInstance.transform.SetPositionAndRotation(preservedPose.position, preservedPose.rotation);

            BindToARAnchor(lastHitFloorPlane);
            FurniturePlacedOrUpdated?.Invoke(PlacedFurnitureInstance, config);
        }

        private GameObject CreateFurnitureInstance(FurnitureConfigData config)
        {
            if (CustomFurnitureFactory != null)
            {
                GameObject customObj = CustomFurnitureFactory.Invoke(config);
                if (customObj != null)
                {
                    return customObj;
                }
            }

            // Phase 1 단독 구동용 1:1 미터 스케일 바운딩 박스 생성 (바닥 중앙 피벗 정렬)
            Vector3 size = config != null ? config.ToSizeVector3() : new Vector3(1.20f, 0.75f, 0.60f);
            string furnitureName = config != null && !string.IsNullOrEmpty(config.name) ? config.name : "FurniturePreview";

            GameObject root = new GameObject($"PlacedFurniture_{furnitureName}");

            GameObject volumeBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            volumeBox.name = "BoundingVolumeMesh";
            volumeBox.transform.SetParent(root.transform, false);
            volumeBox.transform.localScale = size;
            volumeBox.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);

            Renderer boxRenderer = volumeBox.GetComponent<Renderer>();
            if (boxRenderer != null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Lit");
                }

                if (shader != null)
                {
                    Material previewMaterial = new Material(shader);
                    previewMaterial.color = new Color(0.0f, 0.85f, 0.75f, 0.45f);
                    boxRenderer.sharedMaterial = previewMaterial;
                }
            }

            return root;
        }

        /// <summary>
        /// [SFR-004 / QUR-001] 가구 오브젝트를 ARAnchor에 바인딩하여 카메라 이동 시 월드 좌표 드리프트를 방지한다.
        /// 에디터 등 ARAnchor 서브시스템이 구동되지 않는 환경에서는 불필요한 에러 방지를 위해 스킵한다.
        /// </summary>
        private async void BindToARAnchor(ARPlane plane)
        {
            if (PlacedFurnitureInstance == null)
            {
                return;
            }

            DetachFromAnchorPreservingWorldTransform();

            // 에디터 Play 모드 등 실제 ARAnchor 서브시스템이 실행 중이지 않을 때는 월드 위치만 유지하고 종료
            if (anchorManager == null || !anchorManager.enabled ||
                anchorManager.subsystem == null || !anchorManager.subsystem.running)
            {
                return;
            }

            Pose currentPose = new Pose(
                PlacedFurnitureInstance.transform.position,
                PlacedFurnitureInstance.transform.rotation);

            if (plane != null && plane.subsumedBy == null)
            {
                try
                {
                    CurrentBoundAnchor = anchorManager.AttachAnchor(plane, currentPose);
                }
                catch (Exception)
                {
                    CurrentBoundAnchor = null;
                }
            }

            if (CurrentBoundAnchor == null)
            {
                var result = await anchorManager.TryAddAnchorAsync(currentPose);
                if (result.status.IsSuccess())
                {
                    CurrentBoundAnchor = result.value;
                }
            }

            if (CurrentBoundAnchor != null && PlacedFurnitureInstance != null)
            {
                PlacedFurnitureInstance.transform.SetParent(CurrentBoundAnchor.transform, true);
            }
        }

        private void DetachFromAnchorPreservingWorldTransform()
        {
            if (PlacedFurnitureInstance != null && PlacedFurnitureInstance.transform.parent != null)
            {
                PlacedFurnitureInstance.transform.SetParent(null, true);
            }

            DetachAndDestroyCurrentAnchor();
        }

        private void DetachAndDestroyCurrentAnchor()
        {
            if (CurrentBoundAnchor == null)
            {
                return;
            }

            if (PlacedFurnitureInstance != null && CurrentBoundAnchor.gameObject == PlacedFurnitureInstance)
            {
                Destroy(CurrentBoundAnchor);
            }
            else
            {
                Destroy(CurrentBoundAnchor.gameObject);
            }

            CurrentBoundAnchor = null;
        }
    }
}
