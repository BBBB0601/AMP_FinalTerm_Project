using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// 가구 한 개의 충돌 판정 (cshFurniture와 같은 오브젝트에 붙임)
// 회전까지 반영한 상자(OBB)로 벽면 / 다른 가구와 겹치는지 매 프레임 검사
public class cshCollisionChecker : MonoBehaviour
{
    public const string TargetNone = "NONE";
    public const string TargetWall = "WALL";
    public const string TargetFurniture = "FURNITURE";

    public static event Action<cshFurniture, bool, string> AnyCollisionStateChanged;

    public cshFurniture furniture;    // 비어 있으면 같은 오브젝트에서 찾음
    public float tolerance = 0.01f;   // 1cm 이내로 맞닿은 것은 충돌로 보지 않음 (바닥, 옆 가구에 딱 붙인 경우)
    public string lastCollisionTarget = TargetNone;

    public event Action<bool, string> CollisionStateChanged;

    void Awake()
    {
        if (furniture == null)
        {
            furniture = GetComponent<cshFurniture>();
        }
    }

    void Update()
    {
        if (furniture == null)
        {
            return;
        }

        bool colliding = CheckCollision(out string collisionTarget);
        if (colliding != furniture.isColliding || !string.Equals(collisionTarget, lastCollisionTarget, StringComparison.Ordinal))
        {
            lastCollisionTarget = collisionTarget;

            // 색 바꾸기는 cshFurniture가 담당
            furniture.SetColliding(colliding);

            // 전체 충돌 상태는 Spawner 또는 통합 어댑터가 모아서 Android로 알림
            if (cshFurnitureSpawner.instance != null)
            {
                cshFurnitureSpawner.instance.OnFurnitureCollisionChanged(furniture);
            }

            CollisionStateChanged?.Invoke(colliding, collisionTarget);
            AnyCollisionStateChanged?.Invoke(furniture, colliding, collisionTarget);
        }
    }

    // 가구의 회전까지 반영한 상자(OBB)와 겹치는 벽면 / 다른 가구가 있는지 검사
    public bool CheckCollision()
    {
        return CheckCollision(out _);
    }

    public bool CheckCollision(out string collisionTarget)
    {
        collisionTarget = TargetNone;

        if (furniture == null)
        {
            furniture = GetComponent<cshFurniture>();
        }

        if (furniture == null)
        {
            return false;
        }

        float width = furniture.width;
        float depth = furniture.depth;
        float height = furniture.height;

        // 바닥이나 옆 가구에 딱 붙은 것까지 충돌로 보지 않도록 사방을 tolerance만큼 줄임
        Vector3 half = new Vector3(
            Mathf.Max(0.005f, width * 0.5f - tolerance),
            Mathf.Max(0.005f, height * 0.5f - tolerance),
            Mathf.Max(0.005f, depth * 0.5f - tolerance));
        Vector3 center = transform.TransformPoint(new Vector3(0.0f, height * 0.5f, 0.0f));

        // 다른 가구의 BoxCollider는 Is Trigger라서 Collide로 함께 검사
        Collider[] hits = Physics.OverlapBox(center, half, transform.rotation, ~0, QueryTriggerInteraction.Collide);

        bool hitWall = false;
        bool hitFurniture = false;

        foreach (Collider hit in hits)
        {
            // 다른 가구
            cshFurniture other = hit.GetComponentInParent<cshFurniture>();
            if (other != null && other != furniture)
            {
                hitFurniture = true;
                continue;
            }

            // ARCore가 인식한 벽면 (바닥면은 가구가 올라가 있으므로 제외)
            ARPlane plane = hit.GetComponentInParent<ARPlane>();
            if (plane != null && plane.alignment == PlaneAlignment.Vertical)
            {
                hitWall = true;
            }
        }

        if (hitWall)
        {
            collisionTarget = TargetWall;
            return true;
        }

        if (hitFurniture)
        {
            collisionTarget = TargetFurniture;
            return true;
        }

        return false;
    }
}
