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
        [Tooltip("수직 벽면(Vertical) 시각적 메쉬 표시 여부 (비활성화해도 충돌용 BoxCollider는 유지됨)")]
        private bool showVerticalWallVisuals = true;

        [SerializeField]
        [Tooltip("천장(HorizontalDown) 평면 숨김 여부")]
        private bool hideCeilingPlanes = true;

        [SerializeField]
        [Tooltip("바닥(HorizontalUp) 평면의 반투명 채우기 색상")]
        private Color floorPlaneFillColor = new Color(0.20f, 0.78f, 0.95f, 0.08f);

        [SerializeField]
        [Tooltip("바닥(HorizontalUp) 평면의 외곽선 색상")]
        private Color floorPlaneBorderColor = new Color(0.20f, 0.85f, 1.0f, 0.35f);

        [SerializeField]
        [Tooltip("수직 벽면(Vertical) 평면의 반투명 채우기 색상")]
        private Color wallPlaneFillColor = new Color(1.0f, 0.55f, 0.20f, 0.08f);

        [SerializeField]
        [Tooltip("수직 벽면(Vertical) 평면의 외곽선 색상")]
        private Color wallPlaneBorderColor = new Color(1.0f, 0.60f, 0.20f, 0.45f);

        [Header("Vertical Wall Collider (SFR-005)")]
        [SerializeField]
        [Tooltip("벽면 앞쪽(실내 방향) 콜라이더 여유 두께(m)")]
        private float wallColliderFrontMargin = 0.01f;

        [SerializeField]
        [Tooltip("벽면 뒤쪽(벽 너머)으로 확장할 충돌 볼륨 깊이(m) — 가구를 벽 너머로 밀어도 충돌 상태 유지")]
        private float wallColliderBackDepth = 0.50f;

        [Header("Floor Height Filtering")]
        [SerializeField]
        [Tooltip("가장 낮은 유효 바닥 평면 대비 이 높이(m) 이상 떠 있는 수평 평면은 바닥 후보 및 시각화에서 제외")]
        private float elevatedPlaneHideThreshold = 0.14f;

        [SerializeField]
        [Tooltip("카메라 위치 대비 최소 이 높이(m) 아래에 있는 수평 평면만 바닥으로 간주")]
        private float minDepthBelowCamera = 0.15f;

        private Material runtimeFloorPlaneMaterial;
        private Material runtimeWallPlaneMaterial;

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
        public float FloorHeightOffsetY { get; private set; }
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
            FloorHeightOffsetY = 0f;
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
                FloorHeightOffsetY = 0f;
            }

            FloorHeightOffsetY += deltaMeters;
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
            FloorHeightOffsetY = 0f;
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
                DisableWallColliders(plane);
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
                    EnsureVerticalWallCollider(plane);
                }
            }
        }

        private void EnsureVerticalWallCollider(ARPlane plane)
        {
            // 1) 기존 MeshFilter 메시가 준비된 경우 MeshCollider도 동기화
            MeshFilter meshFilter = plane.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                MeshCollider meshCollider = plane.GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    meshCollider = plane.gameObject.AddComponent<MeshCollider>();
                }

                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.enabled = true;
            }

            // 2) ARPlaneMeshVisualizer 활성 여부나 0두께 평면 통과와 무관하게 항상 동작하는 두께 있는 BoxCollider 동기화
            BoxCollider boxCollider = plane.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = plane.gameObject.AddComponent<BoxCollider>();
            }

            float sizeX = Mathf.Max(plane.size.x, 0.10f);
            float sizeZ = Mathf.Max(plane.size.y, 0.10f);
            float front = Mathf.Max(0.005f, wallColliderFrontMargin);
            float back = Mathf.Max(0.05f, wallColliderBackDepth);

            // ARPlane 로컬 +Y는 평면 법선. 카메라 쪽을 실내(+normalSign)로 판별하여 벽 뒤쪽(-normalSign)으로 충돌 볼륨 확장
            float normalSign = 1f;
            Camera cam = Camera.main;
            if (cam != null)
            {
                float dot = Vector3.Dot(plane.transform.up, cam.transform.position - plane.center);
                normalSign = dot >= 0f ? 1f : -1f;
            }

            float centerY = normalSign * (front - back) * 0.5f;
            float sizeY = front + back;

            Vector2 centerInPlane = plane.centerInPlaneSpace;
            boxCollider.center = new Vector3(centerInPlane.x, centerY, centerInPlane.y);
            boxCollider.size = new Vector3(sizeX, sizeY, sizeZ);
            boxCollider.isTrigger = true;
            boxCollider.enabled = true;
        }

        private static void DisableWallColliders(ARPlane plane)
        {
            if (plane == null)
            {
                return;
            }

            BoxCollider boxCollider = plane.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }

            MeshCollider meshCollider = plane.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshCollider.enabled = false;
            }
        }

        private void UpdateFloorReference()
        {
            bool previousHasRef = HasFloorReference;
            float previousRefY = CurrentFloorReferenceY;

            Camera cam = Camera.main;

            for (int i = horizontalFloorPlanes.Count - 1; i >= 0; i--)
            {
                ARPlane plane = horizontalFloorPlanes[i];
                if (plane == null || plane.subsumedBy != null || plane.trackingState == TrackingState.None)
                {
                    horizontalFloorPlanes.RemoveAt(i);
                }
            }

            ARPlane bestReferencePlane = null;
            float bestScore = -1f;
            const float clusterBandTolerance = 0.08f;

            for (int i = 0; i < horizontalFloorPlanes.Count; i++)
            {
                ARPlane plane = horizontalFloorPlanes[i];
                float planeY = plane.center.y;
                if (cam != null && planeY >= cam.transform.position.y - minDepthBelowCamera)
                {
                    continue;
                }

                if (IsFloorCalibrated)
                {
                    // 수동 확정 상태에서는 확정 높이 ±elevatedPlaneHideThreshold 안의 가장 넓은 바닥 평면을 앵커 참조용으로 유지
                    if (Mathf.Abs(planeY - CalibratedFloorY) > elevatedPlaneHideThreshold)
                    {
                        continue;
                    }

                    float area = plane.size.x * plane.size.y;
                    if (bestReferencePlane == null || area > bestScore)
                    {
                        bestReferencePlane = plane;
                        bestScore = area;
                    }
                }
                else
                {
                    // 확정 전에는 작은 지하 노이즈 평면에 끌려 내려가지 않도록,
                    // 비슷한 높이대(±8cm)의 누적 면적이 가장 넓은 주력 바닥 평면을 우선 선택
                    float clusterArea = 0f;
                    for (int j = 0; j < horizontalFloorPlanes.Count; j++)
                    {
                        ARPlane other = horizontalFloorPlanes[j];
                        if (cam != null && other.center.y >= cam.transform.position.y - minDepthBelowCamera)
                        {
                            continue;
                        }

                        if (Mathf.Abs(other.center.y - planeY) <= clusterBandTolerance)
                        {
                            clusterArea += other.size.x * other.size.y;
                        }
                    }

                    float ownArea = plane.size.x * plane.size.y;
                    float score = clusterArea + ownArea * 0.25f;
                    if (bestReferencePlane == null || score > bestScore)
                    {
                        bestReferencePlane = plane;
                        bestScore = score;
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

            // 바닥 높이 확정(2단계) 이후에만 확정 높이에서 크게 벗어난 오차 평면을 숨기고,
            // 확정 전(1단계)에는 감지된 바닥 평면을 모두 표시해 넓은 바닥이 잘리지 않도록 한다.
            if (IsFloorCalibrated && Mathf.Abs(plane.center.y - CalibratedFloorY) > elevatedPlaneHideThreshold)
            {
                return false;
            }

            return true;
        }

        private void ApplyPlaneStyle(ARPlane plane)
        {
            if (plane == null)
            {
                return;
            }

            Material targetMat;
            Color borderColor;

            if (plane.alignment == PlaneAlignment.HorizontalUp)
            {
                targetMat = GetOrCreateFloorPlaneMaterial();
                borderColor = floorPlaneBorderColor;
            }
            else if (plane.alignment == PlaneAlignment.Vertical)
            {
                targetMat = GetOrCreateWallPlaneMaterial();
                borderColor = wallPlaneBorderColor;
            }
            else
            {
                return;
            }

            MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
            if (meshRenderer != null && targetMat != null)
            {
                meshRenderer.sharedMaterial = targetMat;
            }

            LineRenderer lineRenderer = plane.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.startWidth = 0.004f;
                lineRenderer.endWidth = 0.004f;
                lineRenderer.startColor = borderColor;
                lineRenderer.endColor = borderColor;
            }
        }

        private Material GetOrCreateFloorPlaneMaterial()
        {
            if (runtimeFloorPlaneMaterial != null)
            {
                return runtimeFloorPlaneMaterial;
            }

            runtimeFloorPlaneMaterial = CreateSubtlePlaneMaterial("RoomFit_FloorPlane_Subtle", floorPlaneFillColor);
            return runtimeFloorPlaneMaterial;
        }

        private Material GetOrCreateWallPlaneMaterial()
        {
            if (runtimeWallPlaneMaterial != null)
            {
                return runtimeWallPlaneMaterial;
            }

            runtimeWallPlaneMaterial = CreateSubtlePlaneMaterial("RoomFit_WallPlane_Subtle", wallPlaneFillColor);
            return runtimeWallPlaneMaterial;
        }

        private static Material CreateSubtlePlaneMaterial(string materialName, Color fillColor)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (shader == null)
            {
                return null;
            }

            Material mat = new Material(shader);
            mat.name = materialName;
            mat.color = fillColor;
            return mat;
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
