using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace RoomFitAR.AR
{
    /// <summary>
    /// [SFR-004] 화면 중앙에서 바닥 평면으로 실시간 AR Raycast를 발사하여 조준점(Reticle)을 렌더링하는 컴포넌트.
    /// - 수평 바닥면(HorizontalUp)만 필터링하여 바닥 스냅 좌표(CurrentHitPose)를 갱신한다.
    /// - 별도의 레티클 프리팹이 할당되지 않은 경우 런타임에 기본 바닥 마커를 자동 생성한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARPlacementReticle : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField]
        [Tooltip("XR Origin의 ARRaycastManager (비워두면 자동 탐색)")]
        private ARRaycastManager raycastManager;

        [SerializeField]
        [Tooltip("ARPlane 조회용 ARPlaneManager (비워두면 자동 탐색)")]
        private ARPlaneManager planeManager;

        [SerializeField]
        [Tooltip("AR 메인 카메라 (비워두면 Camera.main 자동 할당)")]
        private Camera arCamera;

        [SerializeField]
        [Tooltip("기준 바닥 높이 참조용 ARPlaneDetectionController (비워두면 자동 탐색)")]
        private ARPlaneDetectionController planeDetectionController;

        [Header("Floor Projection Settings")]
        [SerializeField]
        [Tooltip("실제 바닥 평면이 인식된 상태에서 평면 틈새/공백 구간을 기준 바닥 높이로 투영 허용할지 여부")]
        private bool enableVirtualFloorGapProjection = true;

        [SerializeField]
        [Tooltip("가상 바닥 틈새 투영 시 허용되는 최대 레이 거리 (단위: m)")]
        private float maxVirtualFloorDistance = 4.5f;

        [Header("Reticle Visual Settings")]
        [SerializeField]
        [Tooltip("조준점 표시용 오브젝트 또는 프리팹 (비워두면 런타임에 기본 마커 자동 생성)")]
        private GameObject reticleVisualObject;

        [SerializeField]
        [Tooltip("바닥면과의 Z-Fighting 깜빡임 방지를 위한 수직 오프셋 (단위: m)")]
        private float surfaceVerticalOffset = 0.005f;

        [SerializeField]
        [Tooltip("조준점 위치 보간 속도 (높을수록 즉각 반응)")]
        private float poseLerpSpeed = 20.0f;

        [SerializeField]
        [Tooltip("Unity 에디터에서 AR 트래킹이 없을 때 Y=0 가상 바닥 평면으로 히트테스트를 대체할지 여부")]
        private bool enableEditorFallbackFloor = true;

        private readonly List<ARRaycastHit> raycastHits = new();
        private bool isReticleSuppressed;

        public bool HasValidHit { get; private set; }
        public Pose CurrentHitPose { get; private set; }
        public ARPlane CurrentHitPlane { get; private set; }
        public TrackableId CurrentHitTrackableId { get; private set; }

        private void Awake()
        {
            if (raycastManager == null)
            {
                raycastManager = FindFirstObjectByType<ARRaycastManager>();
            }

            if (planeManager == null)
            {
                planeManager = FindFirstObjectByType<ARPlaneManager>();
            }

            if (arCamera == null)
            {
                arCamera = Camera.main;
            }

            if (planeDetectionController == null)
            {
                planeDetectionController = FindFirstObjectByType<ARPlaneDetectionController>();
            }

            EnsureReticleVisual();
            SetReticleObjectActive(false);
        }

        private void Update()
        {
            if (arCamera == null)
            {
                arCamera = Camera.main;
                if (arCamera == null)
                {
                    return;
                }
            }

            UpdateCenterScreenHitTest();
            UpdateReticleTransform();
        }

        /// <summary>
        /// 가구 배치가 완료되었거나 고정 모드일 때 조준 레티클 표시를 숨기거나 다시 활성화한다.
        /// </summary>
        public void SetReticleSuppressed(bool suppressed)
        {
            isReticleSuppressed = suppressed;
            if (isReticleSuppressed)
            {
                SetReticleObjectActive(false);
            }
        }

        /// <summary>
        /// 임의의 화면 터치 좌표(Screen Point)에서 수평 바닥면 AR Raycast를 수행한다.
        /// (1손가락 드래그 수평 이동 시 ARAnchorPlacementManipulator에서 호출)
        /// </summary>
        public bool TryRaycastFloorAtScreenPoint(Vector2 screenPoint, out Pose hitPose, out ARPlane hitPlane)
        {
            hitPose = default;
            hitPlane = null;

            if (raycastManager != null &&
                raycastManager.Raycast(screenPoint, raycastHits, TrackableType.PlaneWithinPolygon))
            {
                bool isCalibrated = planeDetectionController != null && planeDetectionController.IsFloorCalibrated;
                float calibratedY = planeDetectionController != null ? planeDetectionController.CalibratedFloorY : 0f;
                float heightOffsetY = planeDetectionController != null ? planeDetectionController.FloorHeightOffsetY : 0f;
                float bandThreshold = planeDetectionController != null
                    ? Mathf.Max(0.35f, planeDetectionController.ElevatedPlaneHideThreshold)
                    : 0.35f;
                float minDepthBelowCam = planeDetectionController != null
                    ? planeDetectionController.MinDepthBelowCamera
                    : 0.15f;

                bool foundBestHit = false;
                ARRaycastHit bestFloorHit = default;
                ARPlane bestFloorPlane = null;
                float bestArea = -1f;

                for (int i = 0; i < raycastHits.Count; i++)
                {
                    ARRaycastHit hit = raycastHits[i];
                    ARPlane plane = ResolvePlane(hit);

                    if (!IsValidFloorHit(hit, plane, isCalibrated, calibratedY, bandThreshold, minDepthBelowCam))
                    {
                        continue;
                    }

                    // 작은 지하 오차 조각 대신, 광선이 맞은 유효 바닥 평면 중 가장 넓은 평면을 우선 선택
                    float area = plane.size.x * plane.size.y;
                    if (!foundBestHit || area > bestArea)
                    {
                        foundBestHit = true;
                        bestFloorHit = hit;
                        bestFloorPlane = plane;
                        bestArea = area;
                    }
                }

                if (foundBestHit)
                {
                    // 다른 공간으로 이동해도 실제 그 자리의 AR 바닥 평면 높이에 밀착하도록
                    // 상수 CalibratedFloorY로 강제 덮어쓰지 않고 실제 히트 높이 + 사용자 미세 오프셋 적용
                    Vector3 hitPos = bestFloorHit.pose.position;
                    if (isCalibrated)
                    {
                        hitPos.y += heightOffsetY;
                    }

                    Vector3 forward = CalculateCameraHorizontalForward();
                    hitPose = new Pose(hitPos, Quaternion.LookRotation(forward, Vector3.up));
                    hitPlane = bestFloorPlane;
                    return true;
                }
            }

            // 사용자가 바닥 높이를 수동 확정한 상태에서만 무늬 없는 문앞/틈새 공백 구간을 확정 바닥 높이로 투영
            if (enableVirtualFloorGapProjection &&
                arCamera != null &&
                planeDetectionController != null &&
                planeDetectionController.IsFloorCalibrated)
            {
                Ray ray = arCamera.ScreenPointToRay(screenPoint);
                Plane calibratedFloor = new Plane(Vector3.up, new Vector3(0f, planeDetectionController.CalibratedFloorY, 0f));

                if (calibratedFloor.Raycast(ray, out float enterDistance) &&
                    enterDistance >= 0.35f &&
                    enterDistance <= maxVirtualFloorDistance)
                {
                    Vector3 hitPoint = ray.GetPoint(enterDistance);
                    Vector3 forward = CalculateCameraHorizontalForward();
                    hitPose = new Pose(hitPoint, Quaternion.LookRotation(forward, Vector3.up));
                    hitPlane = planeDetectionController.LowestFloorPlane;
                    return true;
                }
            }

#if UNITY_EDITOR
            if (enableEditorFallbackFloor && arCamera != null)
            {
                Ray ray = arCamera.ScreenPointToRay(screenPoint);
                float floorY = arCamera.transform.position.y > 0.2f ? 0.0f : arCamera.transform.position.y - 0.8f;
                Plane virtualFloor = new Plane(Vector3.up, new Vector3(0f, floorY, 0f));

                if (virtualFloor.Raycast(ray, out float enterDistance) && enterDistance > 0.1f && enterDistance <= 20.0f)
                {
                    Vector3 hitPoint = ray.GetPoint(enterDistance);
                    Vector3 forward = CalculateCameraHorizontalForward();
                    hitPose = new Pose(hitPoint, Quaternion.LookRotation(forward, Vector3.up));
                    return true;
                }

                // 에디터에서 카메라가 수평(X=0도)을 바라보고 있어 바닥과 광선이 평행할 때 전방 2.2m 바닥을 기본 조준
                Vector3 camForward = CalculateCameraHorizontalForward();
                Vector3 defaultFloorPoint = arCamera.transform.position + camForward * 2.2f;
                defaultFloorPoint.y = floorY;
                hitPose = new Pose(defaultFloorPoint, Quaternion.LookRotation(camForward, Vector3.up));
                return true;
            }
#endif

            return false;
        }

        private void UpdateCenterScreenHitTest()
        {
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            if (TryRaycastFloorAtScreenPoint(screenCenter, out Pose floorPose, out ARPlane floorPlane))
            {
                HasValidHit = true;
                CurrentHitPose = floorPose;
                CurrentHitPlane = floorPlane;
                CurrentHitTrackableId = floorPlane != null ? floorPlane.trackableId : TrackableId.invalidId;
            }
            else
            {
                HasValidHit = false;
                CurrentHitPlane = null;
                CurrentHitTrackableId = TrackableId.invalidId;
            }
        }

        private void UpdateReticleTransform()
        {
            if (reticleVisualObject == null)
            {
                return;
            }

            bool shouldShow = HasValidHit && !isReticleSuppressed;
            SetReticleObjectActive(shouldShow);

            if (!shouldShow)
            {
                return;
            }

            Vector3 targetPosition = CurrentHitPose.position + Vector3.up * surfaceVerticalOffset;
            Quaternion targetRotation = CurrentHitPose.rotation;

            Transform visualTransform = reticleVisualObject.transform;
            float t = Mathf.Clamp01(Time.deltaTime * poseLerpSpeed);
            visualTransform.position = Vector3.Lerp(visualTransform.position, targetPosition, t);
            visualTransform.rotation = Quaternion.Slerp(visualTransform.rotation, targetRotation, t);
        }

        private bool IsValidFloorHit(
            ARRaycastHit hit,
            ARPlane plane,
            bool isCalibrated,
            float calibratedY,
            float bandThreshold,
            float minDepthBelowCam)
        {
            if (plane == null ||
                plane.alignment != PlaneAlignment.HorizontalUp ||
                plane.subsumedBy != null ||
                plane.trackingState == TrackingState.None)
            {
                return false;
            }

            if (arCamera != null && hit.pose.position.y >= arCamera.transform.position.y - minDepthBelowCam)
            {
                return false;
            }

            if (isCalibrated && Mathf.Abs(hit.pose.position.y - calibratedY) > bandThreshold)
            {
                return false;
            }

            return true;
        }

        private ARPlane ResolvePlane(ARRaycastHit hit)
        {
            if (hit.trackable is ARPlane directPlane)
            {
                return directPlane;
            }

            if (planeManager != null && hit.trackableId != TrackableId.invalidId)
            {
                return planeManager.GetPlane(hit.trackableId);
            }

            return null;
        }

        private Vector3 CalculateCameraHorizontalForward()
        {
            if (arCamera == null)
            {
                return Vector3.forward;
            }

            Vector3 projected = Vector3.ProjectOnPlane(arCamera.transform.forward, Vector3.up);
            if (projected.sqrMagnitude < 0.0001f)
            {
                projected = Vector3.ProjectOnPlane(arCamera.transform.up, Vector3.up);
            }

            return projected.sqrMagnitude > 0.0001f ? projected.normalized : Vector3.forward;
        }

        private void EnsureReticleVisual()
        {
            if (reticleVisualObject != null)
            {
                if (reticleVisualObject.scene.rootCount == 0)
                {
                    reticleVisualObject = Instantiate(reticleVisualObject, transform);
                    reticleVisualObject.name = "ARPlacementReticle_Visual";
                }
                return;
            }

            // 프리팹이 할당되지 않은 경우 기본 원판형 조준 마커를 동적 생성 (Fallback)
            GameObject root = new GameObject("ARPlacementReticle_DefaultVisual");
            root.transform.SetParent(transform, false);

            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "ReticleDisc";
            disc.transform.SetParent(root.transform, false);
            disc.transform.localScale = new Vector3(0.25f, 0.002f, 0.25f);

            Collider discCollider = disc.GetComponent<Collider>();
            if (discCollider != null)
            {
                Destroy(discCollider);
            }

            Renderer discRenderer = disc.GetComponent<Renderer>();
            if (discRenderer != null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Unlit");
                }

                if (shader != null)
                {
                    Material mat = new Material(shader);
                    mat.color = new Color(0.0f, 0.9f, 1.0f, 0.55f);
                    discRenderer.sharedMaterial = mat;
                }
            }

            reticleVisualObject = root;
        }

        private void SetReticleObjectActive(bool active)
        {
            if (reticleVisualObject != null && reticleVisualObject.activeSelf != active)
            {
                reticleVisualObject.SetActive(active);
            }
        }
    }
}
