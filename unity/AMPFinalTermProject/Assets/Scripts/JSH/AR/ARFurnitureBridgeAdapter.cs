using System.Collections.Generic;
using UnityEngine;
using RoomFitAR.Bridge;

namespace RoomFitAR.AR
{
    /// <summary>
    /// [Phase 2 통합 어댑터] 진승호(ARAnchorPlacementManipulator / ARBridgeManager)와
    /// 김기현(Furniture.prefab / cshFurniture / cshCollisionChecker) 모듈을 결합한다.
    /// - CustomFurnitureFactory 주입: Furniture.prefab 기반 절차적 메시·모서리선·TMP 치수 라벨·전면 60cm 가이드 생성
    /// - ClearanceGuideToggled 수신: 배치된 모든 cshFurniture의 전면 60cm 동선 가이드 On/Off 동기화
    /// - cshCollisionChecker 충돌 이벤트 집계: 벽면(WALL) / 타 가구(FURNITURE) 간섭 상태를 ARBridgeManager로 전달
    /// </summary>
    [DisallowMultipleComponent]
    public class ARFurnitureBridgeAdapter : MonoBehaviour
    {
        [Header("Module References")]
        [SerializeField]
        [Tooltip("김기현 파트 절차적 가구 프리팹 (Assets/FurnitureAR/Prefabs/Furniture.prefab)")]
        private GameObject furniturePrefab;

        [SerializeField]
        [Tooltip("AR 배치·앵커·제스처 컨트롤러 (비워두면 씬에서 자동 탐색)")]
        private ARAnchorPlacementManipulator placementManipulator;

        [SerializeField]
        [Tooltip("Android-Unity IPC 브릿지 매니저 (비워두면 씬에서 자동 탐색)")]
        private ARBridgeManager bridgeManager;

        [Header("Clearance Guide (SFR-006)")]
        [SerializeField]
        [Tooltip("가구 생성 시 전면 60cm 통행 동선 가이드 초기 표시 여부")]
        private bool showClearanceGuideByDefault = true;

        private readonly List<cshFurniture> trackedFurnitures = new();
        private bool isClearanceGuideVisible;

        private void Awake()
        {
            isClearanceGuideVisible = showClearanceGuideByDefault;

            if (placementManipulator == null)
            {
                placementManipulator = FindFirstObjectByType<ARAnchorPlacementManipulator>();
            }

            if (bridgeManager == null)
            {
                bridgeManager = FindFirstObjectByType<ARBridgeManager>();
            }

            if (placementManipulator != null)
            {
                placementManipulator.CustomFurnitureFactory = CreateFurnitureFromPrefab;
            }
            else
            {
                Debug.LogError("[ARFurnitureBridgeAdapter] ARAnchorPlacementManipulator를 찾을 수 없습니다.");
            }
        }

        private void OnEnable()
        {
            if (bridgeManager != null)
            {
                bridgeManager.ClearanceGuideToggled += HandleClearanceGuideToggled;
            }

            if (placementManipulator != null)
            {
                placementManipulator.FurnitureCommitted += HandleFurnitureCommitted;
                placementManipulator.FurnitureRemoved += HandleFurnitureRemoved;
            }

            cshCollisionChecker.AnyCollisionStateChanged += HandleAnyCollisionStateChanged;
        }

        private void OnDisable()
        {
            if (bridgeManager != null)
            {
                bridgeManager.ClearanceGuideToggled -= HandleClearanceGuideToggled;
            }

            if (placementManipulator != null)
            {
                placementManipulator.FurnitureCommitted -= HandleFurnitureCommitted;
                placementManipulator.FurnitureRemoved -= HandleFurnitureRemoved;
            }

            cshCollisionChecker.AnyCollisionStateChanged -= HandleAnyCollisionStateChanged;
        }

        private GameObject CreateFurnitureFromPrefab(FurnitureConfigData config)
        {
            if (furniturePrefab == null)
            {
                Debug.LogWarning("[ARFurnitureBridgeAdapter] furniturePrefab이 할당되지 않아 기본 프리뷰 박스로 폴백합니다.");
                return null;
            }

            GameObject instance = Instantiate(furniturePrefab);
            string displayName = config != null && !string.IsNullOrWhiteSpace(config.name)
                ? config.name.Trim()
                : "가구";
            instance.name = $"Furniture_{displayName}";

            cshFurniture furniture = instance.GetComponent<cshFurniture>();
            if (furniture == null)
            {
                furniture = instance.AddComponent<cshFurniture>();
            }

            float width = config != null ? config.width : 1.20f;
            float depth = config != null ? config.depth : 0.60f;
            float height = config != null ? config.height : 0.75f;

            furniture.furnitureName = displayName;
            furniture.isPlaced = false;
            furniture.SetSize(width, depth, height);
            furniture.ShowGuide(isClearanceGuideVisible);

            cshCollisionChecker checker = instance.GetComponent<cshCollisionChecker>();
            if (checker == null)
            {
                checker = instance.AddComponent<cshCollisionChecker>();
            }
            checker.furniture = furniture;

            PruneDestroyedFurnitures();
            trackedFurnitures.Add(furniture);

            return instance;
        }

        private void HandleClearanceGuideToggled(bool visible)
        {
            isClearanceGuideVisible = visible;
            PruneDestroyedFurnitures();

            for (int i = 0; i < trackedFurnitures.Count; i++)
            {
                trackedFurnitures[i].ShowGuide(visible);
            }
        }

        private void HandleFurnitureCommitted(GameObject committedObject)
        {
            if (committedObject == null)
            {
                return;
            }

            cshFurniture furniture = committedObject.GetComponent<cshFurniture>();
            if (furniture != null)
            {
                furniture.isPlaced = true;
            }
        }

        private void HandleFurnitureRemoved()
        {
            PruneDestroyedFurnitures();
            ReportAggregateCollisionState();
        }

        private void HandleAnyCollisionStateChanged(cshFurniture changedFurniture, bool isColliding, string targetType)
        {
            PruneDestroyedFurnitures();
            ReportAggregateCollisionState();
        }

        private void ReportAggregateCollisionState()
        {
            if (bridgeManager == null)
            {
                return;
            }

            bool anyWallCollision = false;
            bool anyFurnitureCollision = false;

            for (int i = 0; i < trackedFurnitures.Count; i++)
            {
                cshFurniture furniture = trackedFurnitures[i];
                if (furniture == null || !furniture.isColliding)
                {
                    continue;
                }

                cshCollisionChecker checker = furniture.GetComponent<cshCollisionChecker>();
                string target = checker != null ? checker.lastCollisionTarget : cshCollisionChecker.TargetWall;
                if (target == cshCollisionChecker.TargetWall)
                {
                    anyWallCollision = true;
                }
                else
                {
                    anyFurnitureCollision = true;
                }
            }

            if (anyWallCollision)
            {
                bridgeManager.ReportCollisionState(true, CollisionEventData.TargetWall, "벽면 침범 발생");
            }
            else if (anyFurnitureCollision)
            {
                bridgeManager.ReportCollisionState(true, CollisionEventData.TargetFurniture, "기존 가구와 간섭 발생");
            }
            else
            {
                bridgeManager.ReportCollisionState(false, CollisionEventData.TargetNone, "배치 가능 영역");
            }
        }

        private void PruneDestroyedFurnitures()
        {
            for (int i = trackedFurnitures.Count - 1; i >= 0; i--)
            {
                if (trackedFurnitures[i] == null)
                {
                    trackedFurnitures.RemoveAt(i);
                }
            }
        }
    }
}
