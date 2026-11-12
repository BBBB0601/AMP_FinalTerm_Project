using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace RoomFitAR.AR
{
    /// <summary>
    /// [PER-001 / COR-001] AR 뷰 렌더링 프레임율(30 FPS 이상) 유지 및 메모리 점유율(400MB 이하) 방어 가드.
    /// UaaL(Unity as a Library) 하이브리드 구동 시 OOM(Out Of Memory) 방지와 리소스 정리를 담당한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARRuntimePerformanceGuard : MonoBehaviour
    {
        [Header("Frame Rate Settings (PER-001)")]
        [SerializeField]
        [Tooltip("목표 렌더링 프레임율 (최소 30 FPS 이상 유지 보장)")]
        private int targetFrameRate = 60;

        [SerializeField]
        [Tooltip("성능 경고 판단 기준 최소 FPS")]
        private float minAcceptableFps = 30.0f;

        [Header("Memory Guard Settings (COR-001)")]
        [SerializeField]
        [Tooltip("메모리 자동 정리(UnloadUnusedAssets)를 트리거할 임계치 (단위: MB, 상한 400MB 기준)")]
        private float memoryCleanupThresholdMB = 350.0f;

        [SerializeField]
        [Tooltip("메모리 점검 주기 (단위: 초)")]
        private float checkIntervalSeconds = 5.0f;

        [SerializeField]
        [Tooltip("PC 에디터 환경에서는 에디터 자체 메모리(약 400MB+)가 포함되므로 모바일 기기에서만 자동 정리를 수행할지 여부")]
        private bool enforceMemoryGuardInEditor = false;

        public float CurrentSmoothedFps { get; private set; } = 60.0f;
        public float AllocatedMemoryMB { get; private set; }
        public bool IsMeetingFpsTarget => CurrentSmoothedFps >= minAcceptableFps;

        private float checkTimer;
        private bool isCleaningMemory;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime > 0.0001f)
            {
                float instantFps = 1.0f / deltaTime;
                CurrentSmoothedFps = Mathf.Lerp(CurrentSmoothedFps, instantFps, 0.1f);
            }

            checkTimer += deltaTime;
            if (checkTimer >= checkIntervalSeconds)
            {
                checkTimer = 0f;
                EvaluateSystemResources();
            }
        }

        /// <summary>
        /// UaaL 환경에서 AR 화면이 백그라운드로 전환되거나 언로드되기 직전 호출하여 미사용 메모리를 즉시 반환한다.
        /// </summary>
        public void ReleaseUnusedResourcesNow()
        {
            if (isCleaningMemory)
            {
                return;
            }

            isCleaningMemory = true;
            AsyncOperation unloadOp = Resources.UnloadUnusedAssets();
            if (unloadOp != null)
            {
                unloadOp.completed += _ =>
                {
                    GC.Collect();
                    isCleaningMemory = false;
                };
            }
            else
            {
                GC.Collect();
                isCleaningMemory = false;
            }
        }

        private void EvaluateSystemResources()
        {
            long allocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
            AllocatedMemoryMB = allocatedBytes / (1024f * 1024f);

            if (Application.isEditor && !enforceMemoryGuardInEditor)
            {
                return;
            }

            if (AllocatedMemoryMB >= memoryCleanupThresholdMB)
            {
                Debug.LogWarning($"[ARRuntimePerformanceGuard] 메모리 점유율({AllocatedMemoryMB:F1}MB)이 임계치({memoryCleanupThresholdMB:F1}MB)에 도달하여 미사용 리소스를 정리합니다.");
                ReleaseUnusedResourcesNow();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                ReleaseUnusedResourcesNow();
            }
        }
    }
}
