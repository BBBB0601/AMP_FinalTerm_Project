using RoomFitAR.Bridge;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RoomFitAR.AR
{
    /// <summary>
    /// [SFR-004] 1회 바닥 높이 확정(캘리브레이션) 및 실시간 ±2cm 미세 높이 보정 HUD 컨트롤러.
    /// - 시작 시 발 앞 실제 바닥 평면을 조준한 뒤 [여기가 바닥] 버튼으로 기준 높이(CalibratedFloorY)를 잠근다.
    /// - 확정 이후에는 [-2cm] / [+2cm] 미세 보정, 가구 배치 초기화, 바닥 재설정 기능을 제공한다.
    /// [SCR-002] Android 네이티브 툴바(RoomFitARActivity) 안에서 실행되면 이 Unity HUD는 화면에서 숨기고,
    /// 같은 기능을 네이티브 버튼(OnConfirmFloor / OnAdjustFloor / OnRecalibrateFloor)으로 받아 처리하며
    /// 진행 상태는 ARBridgeManager.ReportCalibrationState로 Android에 보낸다 (UI 겹침 방지).
    /// 에디터·단독 Unity 빌드에서는 기존처럼 Unity HUD를 그대로 표시한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARCalibrationHudController : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField]
        private ARPlaneDetectionController planeDetectionController;

        [SerializeField]
        private ARPlacementReticle placementReticle;

        [SerializeField]
        private ARAnchorPlacementManipulator placementManipulator;

        [SerializeField]
        [Tooltip("Android 네이티브 툴바 연동용 브릿지 (비워두면 씬에서 자동 탐색)")]
        private ARBridgeManager bridgeManager;

        [Header("Calibration Settings")]
        [SerializeField]
        [Tooltip("미세 보정 버튼 1회 탭당 높이 이동량 (단위: m)")]
        private float fineStepMeters = 0.02f;

        private Canvas hudCanvas;
        private Text statusText;
        private GameObject calibrationPanel;
        private Button confirmFloorButton;
        private Image confirmFloorButtonBg;
        private Text confirmFloorButtonLabel;
        private GameObject adjustmentPanel;
        private Button commitFurnitureButton;
        private Button resetFurnitureButton;
        private Font runtimeFont;

        private void Awake()
        {
            if (planeDetectionController == null)
            {
                planeDetectionController = FindFirstObjectByType<ARPlaneDetectionController>();
            }

            if (placementReticle == null)
            {
                placementReticle = FindFirstObjectByType<ARPlacementReticle>();
            }

            if (placementManipulator == null)
            {
                placementManipulator = FindFirstObjectByType<ARAnchorPlacementManipulator>();
            }

            if (bridgeManager == null)
            {
                bridgeManager = FindFirstObjectByType<ARBridgeManager>();
            }

            EnsureEventSystem();
            BuildHudCanvas();
        }

        private void OnEnable()
        {
            if (bridgeManager != null)
            {
                bridgeManager.ConfirmFloorRequested += OnConfirmFloorClicked;
                bridgeManager.AdjustFloorRequested += OnAdjustFloorRequested;
                bridgeManager.RecalibrateFloorRequested += OnRecalibrateFloorClicked;
            }
        }

        private void OnDisable()
        {
            if (bridgeManager != null)
            {
                bridgeManager.ConfirmFloorRequested -= OnConfirmFloorClicked;
                bridgeManager.AdjustFloorRequested -= OnAdjustFloorRequested;
                bridgeManager.RecalibrateFloorRequested -= OnRecalibrateFloorClicked;
            }
        }

        private void Start()
        {
            // ARBridgeManager.Awake에서 네이티브 호스트 감지가 끝난 뒤 적용
            ApplyNativeToolbarMode(IsNativeToolbarHost);
        }

        private void Update()
        {
            RefreshHudState();
        }

        private bool IsNativeToolbarHost => bridgeManager != null && bridgeManager.IsNativeToolbarHost;

        /// <summary>
        /// [SCR-002] 네이티브 툴바가 있으면 Unity HUD를 숨겨 겹침을 없앤다 (기능은 브릿지 이벤트로 계속 동작).
        /// </summary>
        private void ApplyNativeToolbarMode(bool isNative)
        {
            if (hudCanvas != null)
            {
                hudCanvas.gameObject.SetActive(!isNative);
            }
        }

        private void OnAdjustFloorRequested(float deltaMeters)
        {
            if (planeDetectionController != null && planeDetectionController.IsFloorCalibrated)
            {
                planeDetectionController.AdjustCalibratedFloorY(deltaMeters);
            }
        }

        private void OnConfirmFloorClicked()
        {
            if (planeDetectionController == null || placementReticle == null || !placementReticle.HasValidHit)
            {
                return;
            }

            planeDetectionController.ConfirmCalibratedFloor(
                placementReticle.CurrentHitPose.position.y,
                placementReticle.CurrentHitPlane);
        }

        private void OnLowerFloorClicked()
        {
            planeDetectionController?.AdjustCalibratedFloorY(-fineStepMeters);
        }

        private void OnRaiseFloorClicked()
        {
            planeDetectionController?.AdjustCalibratedFloorY(fineStepMeters);
        }

        private void OnCommitFurnitureClicked()
        {
            placementManipulator?.CommitCurrentFurniture();
        }

        private void OnResetFurnitureClicked()
        {
            placementManipulator?.ResetPlacedFurniture();
        }

        private void OnRecalibrateFloorClicked()
        {
            placementManipulator?.ResetPlacedFurniture();
            planeDetectionController?.ClearFloorCalibration();
        }

        private void RefreshHudState()
        {
            if (planeDetectionController == null || placementReticle == null)
            {
                return;
            }

            bool isCalibrated = planeDetectionController.IsFloorCalibrated;
            bool hasFloorHit = placementReticle.HasValidHit;
            bool hasActive = placementManipulator != null && placementManipulator.HasPlacedFurniture;
            bool hasAny = placementManipulator != null && placementManipulator.HasAnyFurniture;
            int committedCount = placementManipulator != null ? placementManipulator.CommittedFurnitureCount : 0;

            string status;
            if (!isCalibrated)
            {
                status = hasFloorHit
                    ? "1단계: 원이 실제 바닥에 닿았다면 아래 버튼으로 확정하세요"
                    : "1단계: 발 앞 1m 실제 바닥을 비추며 살짝 움직여 주세요";
            }
            else
            {
                status = hasActive
                    ? $"드래그: 이동 | 2손가락: 회전 | [위치 확정]으로 고정 (확정: {committedCount}개)"
                    : $"2단계: 바닥을 탭해 가구를 배치하세요 (확정: {committedCount}개)";
            }

            // [SCR-002] 네이티브 툴바 모드: Unity HUD는 숨겨져 있으므로 상태만 Android로 보낸다 (바뀔 때만 전송)
            if (IsNativeToolbarHost)
            {
                bridgeManager.ReportCalibrationState(isCalibrated, hasFloorHit, hasActive, committedCount, status);
                return;
            }

            if (calibrationPanel != null)
            {
                calibrationPanel.SetActive(!isCalibrated);
            }

            if (adjustmentPanel != null)
            {
                adjustmentPanel.SetActive(isCalibrated);
            }

            if (statusText != null)
            {
                statusText.text = status;
            }

            if (!isCalibrated)
            {
                if (confirmFloorButton != null)
                {
                    confirmFloorButton.interactable = hasFloorHit;
                }

                if (confirmFloorButtonBg != null)
                {
                    confirmFloorButtonBg.color = hasFloorHit
                        ? new Color(0.08f, 0.72f, 0.65f, 0.95f)
                        : new Color(0.28f, 0.32f, 0.36f, 0.85f);
                }

                if (confirmFloorButtonLabel != null)
                {
                    confirmFloorButtonLabel.text = hasFloorHit
                        ? "여기가 바닥 (높이 확정)"
                        : "발 앞 바닥을 비춰주세요...";
                }
            }
            else
            {
                if (commitFurnitureButton != null)
                {
                    commitFurnitureButton.interactable = hasActive;
                }

                if (resetFurnitureButton != null)
                {
                    resetFurnitureButton.interactable = hasAny;
                }
            }
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildHudCanvas()
        {
            runtimeFont = ResolveUiFont();

            GameObject canvasObj = new GameObject("RoomFit_CalibrationHUD");
            canvasObj.transform.SetParent(transform, false);

            hudCanvas = canvasObj.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2400f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            BuildTopStatusBanner(canvasObj.transform);
            BuildCalibrationPanel(canvasObj.transform);
            BuildAdjustmentPanel(canvasObj.transform);
        }

        private void BuildTopStatusBanner(Transform parent)
        {
            GameObject banner = new GameObject("TopStatusBanner");
            banner.transform.SetParent(parent, false);

            RectTransform rect = banner.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 1f);
            rect.anchorMax = new Vector2(0.95f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -120f);
            rect.sizeDelta = new Vector2(0f, 120f);

            Image bg = banner.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.09f, 0.12f, 0.78f);
            bg.raycastTarget = false;

            statusText = CreateTextChild(banner.transform, "StatusText", string.Empty, 32, TextAnchor.MiddleCenter);
            statusText.raycastTarget = false;
            RectTransform textRect = statusText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(28f, 12f);
            textRect.offsetMax = new Vector2(-28f, -12f);
        }

        private void BuildCalibrationPanel(Transform parent)
        {
            calibrationPanel = new GameObject("CalibrationPanel");
            calibrationPanel.transform.SetParent(parent, false);

            RectTransform panelRect = calibrationPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0f);
            panelRect.anchorMax = new Vector2(0.92f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 110f);
            panelRect.sizeDelta = new Vector2(0f, 140f);

            confirmFloorButton = CreateButton(
                calibrationPanel.transform,
                "ConfirmFloorButton",
                "발 앞 바닥을 비춰주세요...",
                36,
                new Color(0.28f, 0.32f, 0.36f, 0.85f),
                OnConfirmFloorClicked,
                out confirmFloorButtonBg,
                out confirmFloorButtonLabel);

            RectTransform btnRect = confirmFloorButton.GetComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
        }

        private void BuildAdjustmentPanel(Transform parent)
        {
            adjustmentPanel = new GameObject("AdjustmentPanel");
            adjustmentPanel.transform.SetParent(parent, false);

            RectTransform panelRect = adjustmentPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.04f, 0f);
            panelRect.anchorMax = new Vector2(0.96f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 100f);
            panelRect.sizeDelta = new Vector2(0f, 120f);

            HorizontalLayoutGroup layout = adjustmentPanel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            Color stepColor = new Color(0.16f, 0.22f, 0.28f, 0.90f);
            Color commitColor = new Color(0.08f, 0.62f, 0.50f, 0.92f);
            Color actionColor = new Color(0.10f, 0.45f, 0.62f, 0.92f);
            Color resetColor = new Color(0.55f, 0.22f, 0.22f, 0.92f);

            CreateButton(adjustmentPanel.transform, "BtnLower2cm", "높이 -2cm", 26, stepColor, OnLowerFloorClicked, out _, out _);
            CreateButton(adjustmentPanel.transform, "BtnRaise2cm", "높이 +2cm", 26, stepColor, OnRaiseFloorClicked, out _, out _);
            commitFurnitureButton = CreateButton(adjustmentPanel.transform, "BtnCommitFurniture", "위치 확정", 26, commitColor, OnCommitFurnitureClicked, out _, out _);
            resetFurnitureButton = CreateButton(adjustmentPanel.transform, "BtnResetFurniture", "전체 초기화", 26, actionColor, OnResetFurnitureClicked, out _, out _);
            CreateButton(adjustmentPanel.transform, "BtnRecalibrate", "바닥 재설정", 26, resetColor, OnRecalibrateFloorClicked, out _, out _);
        }

        private Button CreateButton(
            Transform parent,
            string name,
            string labelText,
            int fontSize,
            Color backgroundColor,
            UnityEngine.Events.UnityAction onClick,
            out Image backgroundImage,
            out Text labelComponent)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            backgroundImage = btnObj.AddComponent<Image>();
            backgroundImage.color = backgroundColor;

            Button button = btnObj.AddComponent<Button>();
            button.targetGraphic = backgroundImage;
            button.onClick.AddListener(onClick);

            labelComponent = CreateTextChild(btnObj.transform, "Label", labelText, fontSize, TextAnchor.MiddleCenter);
            RectTransform textRect = labelComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 6f);
            textRect.offsetMax = new Vector2(-8f, -6f);

            return button;
        }

        private Text CreateTextChild(Transform parent, string name, string content, int fontSize, TextAnchor alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            Text text = textObj.AddComponent<Text>();
            text.font = runtimeFont;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Font ResolveUiFont()
        {
            try
            {
                Font osFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Noto Sans CJK KR", "SamsungOneKorean", "Malgun Gothic", "sans-serif" },
                    32);
                if (osFont != null)
                {
                    return osFont;
                }
            }
            catch
            {
                // 시스템 동적 폰트 로드 실패 시 내장 폰트로 폴백
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
