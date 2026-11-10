using System;
using UnityEngine;

namespace RoomFitAR.Bridge
{
    /// <summary>
    /// [SFR-002 / INR-001] Android Native -> Unity AR 가구 치수 전송 JSON 데이터 규격.
    /// docs/contracts/furniture_config_schema.json 명세와 필드명 및 단위를 1:1로 일치시킨다.
    /// </summary>
    [Serializable]
    public class FurnitureConfigData
    {
        public const float MinDimensionMeters = 0.10f;
        public const float MaxDimensionMeters = 3.00f;
        public const string ExpectedUnit = "meter";

        public string name = " 기본 가구";
        public float width = 1.20f;
        public float depth = 0.60f;
        public float height = 0.75f;
        public string unit = ExpectedUnit;

        /// <summary>
        /// Unity 월드 좌표계(1 unit = 1 meter) 기준 크기 벡터 반환 (X: 가로 폭, Y: 높이, Z: 깊이).
        /// </summary>
        public Vector3 ToSizeVector3()
        {
            return new Vector3(width, height, depth);
        }

        /// <summary>
        /// 만약 실수로 cm 단위(10 ~ 300) 수치가 전달되었을 경우 미터(m) 단위로 안전하게 보정한다.
        /// </summary>
        public void NormalizeToMetersIfNeeded()
        {
            bool looksLikeCentimeters =
                width > MaxDimensionMeters ||
                depth > MaxDimensionMeters ||
                height > MaxDimensionMeters ||
                string.Equals(unit, "cm", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(unit, "centimeter", StringComparison.OrdinalIgnoreCase);

            if (looksLikeCentimeters)
            {
                Debug.LogWarning($"[FurnitureConfigData] 치수 데이터가 cm 단위로 감지되어 미터(m) 단위로 자동 변환합니다: ({width}, {depth}, {height})");
                width /= 100.0f;
                depth /= 100.0f;
                height /= 100.0f;
                unit = ExpectedUnit;
            }
        }

        /// <summary>
        /// 스키마 제약 조건(0.10m <= W, D, H <= 3.00m, unit == "meter") 유효성을 검증한다.
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "가구 명칭(name)이 비어 있습니다.";
                return false;
            }

            if (!string.Equals(unit, ExpectedUnit, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = $"단위 필드(unit)가 '{ExpectedUnit}'가 아닙니다. 입력값: '{unit}'";
                return false;
            }

            if (width < MinDimensionMeters || width > MaxDimensionMeters ||
                depth < MinDimensionMeters || depth > MaxDimensionMeters ||
                height < MinDimensionMeters || height > MaxDimensionMeters)
            {
                errorMessage = $"가구 치수(W:{width:F2}m, D:{depth:F2}m, H:{height:F2}m)가 허용 범위({MinDimensionMeters:F2}m ~ {MaxDimensionMeters:F2}m)를 벗어났습니다.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// [SFR-002 / SFR-005] Unity AR -> Android Native 물리 간섭 및 충돌 상태 피드백 JSON 규격.
    /// docs/contracts/collision_event_schema.json 명세와 1:1로 일치시킨다.
    /// </summary>
    [Serializable]
    public class CollisionEventData
    {
        public const string TargetWall = "WALL";
        public const string TargetFurniture = "FURNITURE";
        public const string TargetNone = "NONE";

        public bool isColliding;
        public string target = TargetNone;
        public string message = string.Empty;

        public CollisionEventData()
        {
        }

        public CollisionEventData(bool isColliding, string target, string message)
        {
            this.isColliding = isColliding;
            this.target = string.IsNullOrEmpty(target) ? (isColliding ? TargetWall : TargetNone) : target;
            this.message = message ?? string.Empty;
        }

        public static CollisionEventData CreateSafe()
        {
            return new CollisionEventData(false, TargetNone, "배치 가능 영역");
        }

        public static CollisionEventData CreateWallCollision(string customMessage = "벽면 침범 발생")
        {
            return new CollisionEventData(true, TargetWall, customMessage);
        }

        public static CollisionEventData CreateFurnitureCollision(string customMessage = "기존 가구와 간섭 발생")
        {
            return new CollisionEventData(true, TargetFurniture, customMessage);
        }
    }
}
