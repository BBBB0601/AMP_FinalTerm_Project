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
                raycastManager.Raycast(screenPoint, raycastHits, TrackableType.PlaneWithinPolygon | TrackableType.PlaneEstimated))
            {
                for (int i = 0; i < raycastHits.Count; i++)
                {
                    ARRaycastHit hit = raycastHits[i];
                    ARPlane plane = ResolvePlane(hit);

                    if (plane != null && plane.alignment != PlaneAlignment.HorizontalUp)
                    {
                        continue;
                    }

                    Vector3 forward = CalculateCameraHorizontalForward();
                    hitPose = new Pose(hit.pose.position, Quaternion.LookRotation(forward, Vector3.up));
                    hitPlane = plane;
                    return true;
                }
            }

#if UNITY_EDITOR
            if (enableEditorFallbackFloor && arCamera != null)
            {
                Ray ray = arCamera.ScreenPointToRay(screenPoint);
                Plane virtualFloor = new Plane(Vector3.up, Vector3.zero);
                if (virtualFloor.Raycast(ray, out float enterDistance) && enterDistance <= 20.0f)
                {
                    Vector3 hitPoint = ray.GetPoint(enterDistance);
                    Vector3 forward = CalculateCameraHorizontalForward();
                    hitPose = new Pose(hitPoint, Quaternion.LookRotation(forward, Vector3.up));
                    return true;
                }
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
