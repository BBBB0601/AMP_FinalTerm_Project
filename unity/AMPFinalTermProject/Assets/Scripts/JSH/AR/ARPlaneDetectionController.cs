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

        private readonly List<ARPlane> horizontalFloorPlanes = new();
        private readonly List<ARPlane> verticalWallPlanes = new();
        private bool arePlanesVisible = true;

        public IReadOnlyList<ARPlane> HorizontalFloorPlanes => horizontalFloorPlanes;
        public IReadOnlyList<ARPlane> VerticalWallPlanes => verticalWallPlanes;
        public bool HasDetectedFloor => horizontalFloorPlanes.Count > 0;
        public bool HasDetectedWall => verticalWallPlanes.Count > 0;

        public event Action<ARPlane> FloorPlaneDetected;
        public event Action<ARPlane> WallPlaneDetected;

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
                ApplyPlaneVisibility(plane, visible);
            }
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
        }

        private void RefreshAllTrackedPlanes()
        {
            horizontalFloorPlanes.Clear();
            verticalWallPlanes.Clear();

            if (planeManager == null)
            {
                return;
            }

            foreach (ARPlane plane in planeManager.trackables)
            {
                RegisterOrUpdatePlane(plane, isNewlyAdded: false);
            }
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

            ApplyPlaneVisibility(plane, arePlanesVisible);

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

        private static void ApplyPlaneVisibility(ARPlane plane, bool visible)
        {
            if (plane == null)
            {
                return;
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
