using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Android가 보내는 FurnitureConfig JSON과 같은 모양 (SFR-001, SFR-002)
// 치수는 Android에서 cm → m 로 바꿔서 보냄
// 예) {"name":"단독 책상","width":1.2,"depth":0.6,"height":0.75}
[System.Serializable]
public class cshFurnitureConfig
{
    public string name;
    public float width;
    public float depth;
    public float height;
}

// Android ↔ Unity 사이에서 가구를 만들고 관리함
// 제안서(SFR-002) 호출 방식: UnityPlayer.UnitySendMessage("ARBridgeManager", "OnReceiveFurnitureData", json)
// → 이 스크립트를 "ARBridgeManager" 오브젝트에 붙이거나,
//   브릿지 스크립트에서 cshFurnitureSpawner.instance.OnReceiveFurnitureData(json) 을 호출
public class cshFurnitureSpawner : MonoBehaviour
{
    public static cshFurnitureSpawner instance;  // 다른 스크립트에서 cshFurnitureSpawner.instance로 접근

    [Header("구성 요소")]
    public GameObject furniturePrefab;   // cshFurniture가 붙은 박스 프리팹
    public Transform cameraTransform;    // AR 카메라 (XR Origin 안의 Main Camera)
    public Transform reticle;            // 바닥 조준 레티클 (SFR-004, AR 담당). 있으면 이 위치에 놓음

    [Header("레티클이 없을 때 임시 위치")]
    public float distance = 1.5f;        // 카메라 앞 몇 m에 놓을지
    public float eyeHeight = 1.4f;       // 카메라(눈높이)에서 바닥까지 대략 몇 m인지

    [Header("동선 가이드 (SFR-006)")]
    public bool showGuide = true;        // Android "동선 가이드" 스위치 상태

    [Header("Android로 알림 (SFR-005)")]
    // 브릿지 담당이 여기에 Android OnCollisionStateChanged(boolean isColliding) 호출을 연결
    public UnityEvent<bool> onCollisionStateChanged;

    public List<cshFurniture> furnitures = new List<cshFurniture>();  // 지금까지 만든 가구들
    public cshFurniture currentFurniture;  // 아직 "배치하기" 전인, 지금 옮기고 있는 가구
    public bool isAnyColliding = false;    // 가구 중 하나라도 겹쳐 있는지

    void Awake()
    {
        instance = this;
    }

    // ---------- Android → Unity (UnitySendMessage는 string 하나만 넘길 수 있음) ----------

    // 치수 확정 시 호출. 배치 전인 가구가 있으면 그 가구의 치수를 바꾸고, 없으면 새로 만듦
    public void OnReceiveFurnitureData(string json)
    {
        cshFurnitureConfig config = ReadJson(json);
        if (config == null)
        {
            return;
        }

        if (currentFurniture != null)
        {
            currentFurniture.furnitureName = config.name;
            currentFurniture.SetSize(config.width, config.depth, config.height);
            return;
        }

        GameObject go = Instantiate(furniturePrefab, transform);
        go.name = "Furniture_" + config.name;
        PlaceAtStart(go.transform);

        cshFurniture furniture = go.GetComponent<cshFurniture>();
        furniture.furnitureName = config.name;
        furniture.SetSize(config.width, config.depth, config.height);
        furniture.ShowGuide(showGuide);

        furnitures.Add(furniture);
        currentFurniture = furniture;
    }

    // "배치하기" 버튼. 지금 가구를 고정하고, 다음에 받은 치수는 새 가구로 만듦
    // (ARAnchor로 고정하는 부분은 AR 담당이 이 가구에 붙임)
    public void OnPlaceFurniture(string unused)
    {
        if (currentFurniture != null)
        {
            currentFurniture.isPlaced = true;
            currentFurniture = null;
        }
    }

    // "동선 가이드" 스위치. "true" / "false"
    public void OnToggleClearanceGuide(string onOff)
    {
        showGuide = onOff == "true" || onOff == "1";
        foreach (cshFurniture furniture in furnitures)
        {
            furniture.ShowGuide(showGuide);
        }
    }

    // 배치 전인 가구만 지움 (배치 취소)
    public void OnCancelFurniture(string unused)
    {
        if (currentFurniture != null)
        {
            RemoveFurniture(currentFurniture);
            currentFurniture = null;
        }
    }

    // "AR 배치 초기화": 모든 가구 지움
    public void OnResetFurniture(string unused)
    {
        foreach (cshFurniture furniture in furnitures)
        {
            Destroy(furniture.gameObject);
        }
        furnitures.Clear();
        currentFurniture = null;
        UpdateCollisionState();
    }

    // ---------- Unity → Android ----------

    // cshFurniture에서 충돌 상태가 바뀔 때 호출
    public void OnFurnitureCollisionChanged(cshFurniture furniture)
    {
        UpdateCollisionState();
    }

    // 가구 전체 중 하나라도 겹치는지 다시 계산해서, 바뀌었을 때만 Android에 알림
    void UpdateCollisionState()
    {
        bool colliding = false;
        foreach (cshFurniture furniture in furnitures)
        {
            if (furniture != null && furniture.isColliding)
            {
                colliding = true;
            }
        }

        if (colliding != isAnyColliding)
        {
            isAnyColliding = colliding;
            Debug.Log("OnCollisionStateChanged: " + isAnyColliding);
            if (onCollisionStateChanged != null)
            {
                onCollisionStateChanged.Invoke(isAnyColliding);
            }
        }
    }

    // ---------- 내부 ----------

    void RemoveFurniture(cshFurniture furniture)
    {
        furnitures.Remove(furniture);
        Destroy(furniture.gameObject);
        UpdateCollisionState();
    }

    // JSON을 읽고 값이 올바른지 검사. 잘못됐으면 null
    public static cshFurnitureConfig ReadJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("빈 메시지");
            return null;
        }

        cshFurnitureConfig config;
        try
        {
            config = JsonUtility.FromJson<cshFurnitureConfig>(json);
        }
        catch (System.ArgumentException)
        {
            Debug.Log("JSON 형식이 아님: " + json);
            return null;
        }

        if (config == null)
        {
            Debug.Log("JSON 형식이 아님: " + json);
            return null;
        }

        if (cshFurniture.IsValidSize(config.width, config.depth, config.height) == false)
        {
            // 흔한 실수: cm 그대로 보낸 경우 (예: 120)
            if (config.width > cshFurniture.maxSize || config.depth > cshFurniture.maxSize || config.height > cshFurniture.maxSize)
            {
                Debug.Log("치수가 3m를 넘음. cm가 아니라 m로 보내야 함: " + json);
            }
            else
            {
                Debug.Log("치수가 범위(0.1~3.0m)를 벗어남: " + json);
            }
            return null;
        }

        if (string.IsNullOrEmpty(config.name))
        {
            config.name = "가구";
        }
        return config;
    }

    // 레티클이 있으면 그 위치, 없으면 카메라 앞 바닥쯤. 가구 앞면(-Z)이 사용자를 바라보게 함
    void PlaceAtStart(Transform target)
    {
        if (cameraTransform == null)
        {
            target.position = transform.position;
            return;
        }

        Vector3 forward = cameraTransform.forward;
        forward.y = 0.0f;  // 위아래로 기울지 않게
        forward.Normalize();

        Vector3 pos;
        if (reticle != null && reticle.gameObject.activeInHierarchy)
        {
            pos = reticle.position;
        }
        else
        {
            pos = cameraTransform.position + forward * distance;
            pos.y = cameraTransform.position.y - eyeHeight;
        }
        target.position = pos;

        // LookAt은 +Z를 목표 쪽으로 돌림 → 카메라 반대쪽을 보게 하면 앞면(-Z)이 사용자를 향함
        target.LookAt(pos + forward);
    }
}
