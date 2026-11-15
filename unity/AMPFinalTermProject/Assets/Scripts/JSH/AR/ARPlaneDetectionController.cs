using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace RoomFitAR.AR
{
    /// <summary>
    /// [SFR-004 / SFR-005] ARCore 기반 바닥면(HorizontalUp) 및 벽면(Vertical) 평면 감지 제어기.
    /// - ARPlaneManager의 감지 모드를 Horizontal | Vertical로 설정하고 감지된 평면을 분류한다.
    /// - 벽면 평면에 물리 콜라이더(MeshCollider)를 자동 동기화하여 OBB 벽면 침범 판정(SFR-005)을 지원한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARPlaneDetectionController : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField]
        [Tooltip("XR Origin에 부착된 ARPlaneManager (비워두면 씬에서 자동 탐색)")]
        private ARPlaneManager planeManager;

        [Header("Detection Settings")]
        [SerializeField]
        [Tooltip("바닥(Horizontal)과 벽면(Vertical) 동시 감지 활성화 여부")]
        private PlaneDetectionMode targetDetectionMode = PlaneDetectionMode.Horizontal | PlaneDetectionMode.Vertical;

        [SerializeField]
        [Tooltip("벽면(Vertical) 평면에 OBB 충돌 판정용 MeshCollider를 자동으로 부착 및 갱신할지 여부")]
        private bool attachColliderToVerticalWalls = true;

        [Header("Plane Visualization")]
        [SerializeField]
        [Tooltip("수직 벽면(Vertical) 시각적 메쉬 표시 여부 (비활성화해도 충돌용 MeshCollider는 유지됨)")]
        private bool showVerticalWallVisuals = false;

        [SerializeField]
        [Tooltip("천장(HorizontalDown) 평면 숨김 여부")]
        private bool hideCeilingPlanes = true;

        [SerializeField]
        [Tooltip("바닥(HorizontalUp) 평면의 반투명 채우기 색상")]
        private Color floorPlaneFillColor = new Color(0.20f, 0.78f, 0.95f, 0.08f);

        [SerializeField]
        [Tooltip("바닥(HorizontalUp) 평면의 외곽선 색상")]
        private Color floorPlaneBorderColor = new Color(0.20f, 0.85f, 1.0f, 0.35f);

        [Header("Floor Height Filtering")]
        [SerializeField]
        [Tooltip("가장 낮은 유효 바닥 평면 대비 이 높이(m) 이상 떠 있는 수평 평면은 바닥 후보 및 시각화에서 제외")]
        private float elevatedPlaneHideThreshold = 0.14f;

        [SerializeField]
        [Tooltip("카메라 위치 대비 최소 이 높이(m) 아래에 있는 수평 평면만 바닥으로 간주")]
        private float minDepthBelowCamera = 0.15f;

        private Material runtimeFloorPlaneMaterial;

        private readonly List<ARPlane> horizontalFloorPlanes = new();
        private readonly List<ARPlane> verticalWallPlanes = new();
        private bool arePlanesVisible = true;
        private bool hasAutoFloorReference;
        private float autoFloorReferenceY;

        public IReadOnlyList<ARPlane> HorizontalFloorPlanes => horizontalFloorPlanes;
        public IReadOnlyList<ARPlane> VerticalWallPlanes => verticalWallPlanes;
        public bool HasDetectedFloor => horizontalFloorPlanes.Count > 0;
        public bool HasDetectedWall => verticalWallPlanes.Count > 0;
        public bool IsFloorCalibrated { get; private set; }
        public float CalibratedFloorY { get; private set; }
        public bool HasFloorReference => IsFloorCalibrated || hasAutoFloorReference;
        public float CurrentFloorReferenceY => IsFloorCalibrated ? CalibratedFloorY : autoFloorReferenceY;
        public ARPlane LowestFloorPlane { get; private set; }
        public float ElevatedPlaneHideThreshold => elevatedPlaneHideThreshold;
        public float MinDepthBelowCamera => minDepthBelowCamera;

        public event Action<ARPlane> FloorPlaneDetected;
        public event Action<ARPlane> WallPlaneDetected;
        public event Action<float> CalibratedFloorYChanged;
        public event Action FloorCalibrationCleared;

        private void Awake()
        {
            if (planeManager == null)
            {
                planeManager = FindFirstObjectByType<ARPlaneManager>();
            }
        }

        private void OnEnable()
        {
            if (planeManager == null)
            {
                Debug.LogWarning("[ARPlaneDetectionController] 씬에서 ARPlaneManager를 찾을 수 없습니다.");
                return;
            }

            planeManager.requestedDetectionMode = targetDetectionMode;
            planeManager.trackablesChanged.AddListener(OnTrackablesChanged);
            RefreshAllTrackedPlanes();
        }

        private void OnDisable()
        {
            if (planeManager != null)
            {
                planeManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
            }
        }

        /// <summary>
        /// 바닥 및 벽면 평면 감지 활성/비활성 전환.
        /// </summary>
        public void SetPlaneDetectionEnabled(bool isEnabled)
        {
            if (planeManager == null)
            {
                return;
            }

            planeManager.requestedDetectionMode = isEnabled ? targetDetectionMode : PlaneDetectionMode.None;
        }

        /// <summary>
        /// 감지된 모든 AR 평면의 시각적 렌더러(MeshRenderer / LineRenderer) 표시 여부를 제어한다.
        /// (충돌 판정용 Collider는 유지하면서 시각적인 그리드만 숨길 때 사용)
        /// </summary>
        public void SetPlaneVisualsVisible(bool visible)
        {
            arePlanesVisible = visible;
            if (planeManager == null)
            {
                return;
            }

            foreach (ARPlane plane in planeManager.trackables)
            {
                ApplyPlaneVisibility(plane, ShouldShowPlane(plane));
            }
        }

        /// <summary>
        /// 사용자가 눈으로 확인한 실제 바닥 높이(Y)를 기준 바닥으로 수동 확정한다.
        /// </summary>
        public void ConfirmCalibratedFloor(float floorY, ARPlane sourcePlane = null)
        {
            IsFloorCalibrated = true;
            CalibratedFloorY = floorY;
            if (sourcePlane != null)
            {
                LowestFloorPlane = sourcePlane;
            }

            RefreshHorizontalFloorVisibility();
            CalibratedFloorYChanged?.Invoke(CalibratedFloorY);
        }

        /// <summary>
        /// 확정된 바닥 기준 높이를 미세 조정(예: ±2cm)한다.
        /// </summary>
        public void AdjustCalibratedFloorY(float deltaMeters)
        {
            if (!IsFloorCalibrated)
            {
                if (!hasAutoFloorReference)
                {
                    return;
                }

                IsFloorCalibrated = true;
                CalibratedFloorY = autoFloorReferenceY;
            }

            CalibratedFloorY += deltaMeters;
            RefreshHorizontalFloorVisibility();
            CalibratedFloorYChanged?.Invoke(CalibratedFloorY);
        }

        /// <summary>
        /// 수동 확정된 바닥 높이를 초기화하고 바닥 탐색 단계로 되돌린다.
        /// </summary>
        public void ClearFloorCalibration()
        {
            IsFloorCalibrated = false;
            CalibratedFloorY = 0f;
            UpdateFloorReference();
            RefreshHorizontalFloorVisibility();
            FloorCalibrationCleared?.Invoke();
        }

        /// <summary>
        /// 특정 ARPlane이 수평 바닥면(HorizontalUp)인지 판별한다.
        /// </summary>
        public static bool IsHorizontalFloorPlane(ARPlane plane)
        {
            if (plane == null)
            {
                return false;
            }

            return plane.alignment == PlaneAlignment.HorizontalUp;
        }

        /// <summary>
        /// 특정 ARPlane이 수직 벽면(Vertical)인지 판별한다.
        /// </summary>
        public static bool IsVerticalWallPlane(ARPlane plane)
        {
            if (plane == null)
            {
                return false;
            }

            return plane.alignment == PlaneAlignment.Vertical;
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
        {
            foreach (ARPlane addedPlane in eventArgs.added)
            {
                RegisterOrUpdatePlane(addedPlane, isNewlyAdded: true);
            }

            foreach (ARPlane updatedPlane in eventArgs.updated)
            {
                RegisterOrUpdatePlane(updatedPlane, isNewlyAdded: false);
            }

            foreach (KeyValuePair<TrackableId, ARPlane> removedPair in eventArgs.removed)
            {
                ARPlane removedPlane = removedPair.Value;
                if (removedPlane != null)
                {
                    horizontalFloorPlanes.Remove(removedPlane);
                    verticalWallPlanes.Remove(removedPlane);
                }
            }

            UpdateFloorReference();
        }

        private void RefreshAllTrackedPlanes()
        {
            horizontalFloorPlanes.Clear();
            verticalWallPlanes.Clear();

            if (planeManager == null)
            {
                UpdateFloorReference();
                return;
            }

            foreach (ARPlane plane in planeManager.trackables)
            {
                RegisterOrUpdatePlane(plane, isNewlyAdded: false);
            }

            UpdateFloorReference();
        }

        private void RegisterOrUpdatePlane(ARPlane plane, bool isNewlyAdded)
        {
            if (plane == null)
            {
                return;
            }

            // 다른 큰 평면에 병합(Subsumed)되었거나 트래킹이 유실된 평면 정리
            if (plane.subsumedBy != null || plane.trackingState == TrackingState.None)
            {
                horizontalFloorPlanes.Remove(plane);
                verticalWallPlanes.Remove(plane);
                return;
            }

            ApplyPlaneStyle(plane);
            ApplyPlaneVisibility(plane, ShouldShowPlane(plane));

            if (IsHorizontalFloorPlane(plane))
            {
                if (!horizontalFloorPlanes.Contains(plane))
                {
                    horizontalFloorPlanes.Add(plane);
                    if (isNewlyAdded)
                    {
                        FloorPlaneDetected?.Invoke(plane);
                    }
                }
            }
            else if (IsVerticalWallPlane(plane))
            {
                if (!verticalWallPlanes.Contains(plane))
                {
                    verticalWallPlanes.Add(plane);
                    if (isNewlyAdded)
                    {
                        WallPlaneDetected?.Invoke(plane);
                    }
                }

                if (attachColliderToVerticalWalls)
                {
                    EnsurePlaneMeshCollider(plane);
                }
            }
        }

        private static void EnsurePlaneMeshCollider(ARPlane plane)
        {
            MeshFilter meshFilter = plane.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

            MeshCollider meshCollider = plane.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = plane.gameObject.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        private void UpdateFloorReference()
        {
            bool previousHasRef = HasFloorReference;
            float previousRefY = CurrentFloorReferenceY;

            float minValidY = float.MaxValue;
            Camera cam = Camera.main;

            for (int i = horizontalFloorPlanes.Count - 1; i >= 0; i--)
            {
                ARPlane plane = horizontalFloorPlanes[i];
                if (plane == null || plane.subsumedBy != null || plane.trackingState == TrackingState.None)
                {
                    horizontalFloorPlanes.RemoveAt(i);
                    continue;
                }

                float planeY = plane.center.y;
                if (cam != null && planeY >= cam.transform.position.y - minDepthBelowCamera)
                {
                    continue;
                }

                if (planeY < minValidY)
                {
                    minValidY = planeY;
                }
            }

            ARPlane bestReferencePlane = null;
            float bestArea = -1f;

            if (minValidY < float.MaxValue)
            {
                const float floorBandTolerance = 0.05f;
                for (int i = 0; i < horizontalFloorPlanes.Count; i++)
                {
                    ARPlane plane = horizontalFloorPlanes[i];
                    float planeY = plane.center.y;
                    if (cam != null && planeY >= cam.transform.position.y - minDepthBelowCamera)
                    {
                        continue;
                    }

                    // 수동 확정 상태에서는 확정 높이 ±elevatedPlaneHideThreshold 안의 가장 넓은 바닥 평면을 앵커 참조용으로 유지
                    bool inBand = IsFloorCalibrated
                        ? Mathf.Abs(planeY - CalibratedFloorY) <= elevatedPlaneHideThreshold
                        : planeY <= minValidY + floorBandTolerance;

                    if (inBand)
                    {
                        float area = plane.size.x * plane.size.y;
                        if (bestReferencePlane == null || area > bestArea)
                        {
                            bestReferencePlane = plane;
                            bestArea = area;
                        }
                    }
                }
            }

            hasAutoFloorReference = bestReferencePlane != null;
            autoFloorReferenceY = bestReferencePlane != null ? bestReferencePlane.center.y : 0f;
            if (bestReferencePlane != null || !IsFloorCalibrated)
            {
                LowestFloorPlane = bestReferencePlane;
            }

            if (previousHasRef != HasFloorReference ||
                (HasFloorReference && Mathf.Abs(previousRefY - CurrentFloorReferenceY) > 0.01f))
            {
                RefreshHorizontalFloorVisibility();
            }
        }

        private void RefreshHorizontalFloorVisibility()
        {
            for (int i = 0; i < horizontalFloorPlanes.Count; i++)
            {
                ARPlane floorPlane = horizontalFloorPlanes[i];
                ApplyPlaneVisibility(floorPlane, ShouldShowPlane(floorPlane));
            }
        }

        private bool ShouldShowPlane(ARPlane plane)
        {
            if (!arePlanesVisible || plane == null)
            {
                return false;
            }

            if (plane.alignment == PlaneAlignment.Vertical)
            {
                return showVerticalWallVisuals;
            }

            if (hideCeilingPlanes && plane.alignment == PlaneAlignment.HorizontalDown)
            {
                return false;
            }

            if (plane.alignment != PlaneAlignment.HorizontalUp)
            {
                return false;
            }

            Camera cam = Camera.main;
            if (cam != null && plane.center.y >= cam.transform.position.y - minDepthBelowCamera)
            {
                return false;
            }

            if (HasFloorReference && plane.center.y > CurrentFloorReferenceY + elevatedPlaneHideThreshold)
            {
                return false;
            }

            return true;
        }

        private void ApplyPlaneStyle(ARPlane plane)
        {
            if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp)
            {
                return;
            }

            Material floorMat = GetOrCreateFloorPlaneMaterial();
            MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
            if (meshRenderer != null && floorMat != null)
            {
                meshRenderer.sharedMaterial = floorMat;
            }

            LineRenderer lineRenderer = plane.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.startWidth = 0.004f;
                lineRenderer.endWidth = 0.004f;
                lineRenderer.startColor = floorPlaneBorderColor;
                lineRenderer.endColor = floorPlaneBorderColor;
            }
        }

        private Material GetOrCreateFloorPlaneMaterial()
        {
            if (runtimeFloorPlaneMaterial != null)
            {
                return runtimeFloorPlaneMaterial;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                return null;
            }

            runtimeFloorPlaneMaterial = new Material(shader);
            runtimeFloorPlaneMaterial.name = "RoomFit_FloorPlane_Subtle";
            runtimeFloorPlaneMaterial.color = floorPlaneFillColor;
            return runtimeFloorPlaneMaterial;
        }

        private static void ApplyPlaneVisibility(ARPlane plane, bool visible)
        {
            if (plane == null)
            {
                return;
            }

            ARPlaneMeshVisualizer visualizer = plane.GetComponent<ARPlaneMeshVisualizer>();
            if (visualizer != null)
            {
                visualizer.enabled = visible;
            }

            MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = visible;
            }

            LineRenderer lineRenderer = plane.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.enabled = visible;
            }
        }
    }
}
