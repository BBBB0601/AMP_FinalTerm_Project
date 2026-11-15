using UnityEngine;

// [테스트용] Android 앱 대신 버튼으로 브릿지 메시지를 보내 보는 패널
// Android의 UnityPlayer.UnitySendMessage("ARBridgeManager", 메서드, 문자열)와 똑같이
// 오브젝트 이름으로 찾아서 SendMessage를 보냄 → 이름이나 메서드가 틀리면 여기서 바로 드러남
public class cshBridgeTestPanel : MonoBehaviour
{
    public string bridgeObjectName = "ARBridgeManager";
    public float moveStep = 0.1f;     // 이동 버튼 한 번에 10cm
    public float rotateStep = 15.0f;  // 회전 버튼 한 번에 15도

    // 직접 입력 (cm, Android 입력 폼과 같음)
    private string nameText = "단독 책상";
    private string widthText = "120";
    private string depthText = "60";
    private string heightText = "75";

    private bool guideOn = true;
    private bool isColliding = false;
    private string lastMessage = "";

    void Start()
    {
        // Android 쪽 OnCollisionStateChanged(boolean) 대신 여기서 받아서 경고 배너로 표시
        if (cshFurnitureSpawner.instance != null)
        {
            cshFurnitureSpawner.instance.onCollisionStateChanged.AddListener(OnCollisionStateChanged);
        }
    }

    void OnCollisionStateChanged(bool colliding)
    {
        isColliding = colliding;
    }

    // Android가 하는 것처럼 오브젝트 이름 + 메서드 이름으로 보냄
    void Send(string method, string value)
    {
        GameObject bridge = GameObject.Find(bridgeObjectName);
        if (bridge == null)
        {
            lastMessage = "'" + bridgeObjectName + "' 오브젝트가 씬에 없음";
            Debug.Log(lastMessage);
            return;
        }

        bridge.SendMessage(method, value, SendMessageOptions.RequireReceiver);
        lastMessage = method + "(" + value + ")";
    }

    // cm로 받은 치수를 Android처럼 m로 바꿔서 FurnitureConfig JSON으로 보냄 (SFR-001)
    void SendFurniture(string furnitureName, float wCm, float dCm, float hCm)
    {
        cshFurnitureConfig config = new cshFurnitureConfig();
        config.name = furnitureName;
        config.width = wCm / 100.0f;
        config.depth = dCm / 100.0f;
        config.height = hCm / 100.0f;
        Send("OnReceiveFurnitureData", JsonUtility.ToJson(config));
    }

    void OnGUI()
    {
        // 폰 화면에서도 누르기 쉽게 화면 높이에 맞춰 키움
        float scale = Mathf.Max(1.0f, Screen.height / 900.0f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1.0f));

        // 충돌 경고 배너 (Android 상단 "벽면 침범 발생" 배너 대신)
        if (isColliding)
        {
            GUI.color = new Color(1.0f, 0.3f, 0.3f);
            GUI.Box(new Rect(10, 10, 300, 30), "[경고] 간섭 발생: 벽면/다른 가구와 겹침");
            GUI.color = Color.white;
        }

        GUILayout.BeginArea(new Rect(10, 50, 300, 700), GUI.skin.box);
        GUILayout.Label("브릿지 테스트 (Android 대신)");

        // 프리셋 (UI 설계서 SCR-001)
        if (GUILayout.Button("단독 책상 120×60×75")) SendFurniture("단독 책상", 120, 60, 75);
        if (GUILayout.Button("3단 서랍장 80×40×90")) SendFurniture("3단 서랍장", 80, 40, 90);
        if (GUILayout.Button("소형 냉장고 47×50×85")) SendFurniture("소형 냉장고", 47, 50, 85);

        GUILayout.Space(6);
        GUILayout.Label("직접 입력 (이름, W, D, H cm)");
        nameText = GUILayout.TextField(nameText);
        GUILayout.BeginHorizontal();
        widthText = GUILayout.TextField(widthText);
        depthText = GUILayout.TextField(depthText);
        heightText = GUILayout.TextField(heightText);
        GUILayout.EndHorizontal();
        if (GUILayout.Button("치수 보내기"))
        {
            float w, d, h;
            if (float.TryParse(widthText, out w) && float.TryParse(depthText, out d) && float.TryParse(heightText, out h))
            {
                SendFurniture(nameText, w, d, h);
            }
            else
            {
                lastMessage = "숫자를 입력하세요";
            }
        }

        GUILayout.Space(6);
        if (GUILayout.Button("배치하기")) Send("OnPlaceFurniture", "");
        if (GUILayout.Button("동선 가이드 " + (guideOn ? "끄기" : "켜기")))
        {
            guideOn = !guideOn;
            Send("OnToggleClearanceGuide", guideOn ? "true" : "false");
        }
        if (GUILayout.Button("배치 취소")) Send("OnCancelFurniture", "");
        if (GUILayout.Button("전체 초기화")) Send("OnResetFurniture", "");

        // 배치 전 가구 옮기기/돌리기 (제스처 SFR-004 구현 전 임시)
        GUILayout.Space(6);
        GUILayout.Label("배치 전 가구 이동 / 회전");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("왼쪽")) Move(Vector3.left);
        if (GUILayout.Button("오른쪽")) Move(Vector3.right);
        if (GUILayout.Button("가까이")) Move(Vector3.back);
        if (GUILayout.Button("멀리")) Move(Vector3.forward);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("왼쪽 " + rotateStep + "도")) Rotate(-rotateStep);
        if (GUILayout.Button("오른쪽 " + rotateStep + "도")) Rotate(rotateStep);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.Label("보낸 메시지: " + lastMessage);
        if (cshFurnitureSpawner.instance != null)
        {
            GUILayout.Label("가구 수: " + cshFurnitureSpawner.instance.furnitures.Count);
        }
        GUILayout.EndArea();
    }

    // 카메라 기준 방향으로 이동 (위아래는 그대로)
    void Move(Vector3 direction)
    {
        cshFurniture furniture = GetCurrent();
        if (furniture == null)
        {
            return;
        }

        Transform cam = Camera.main.transform;
        Vector3 move = cam.TransformDirection(direction);
        move.y = 0.0f;
        move.Normalize();
        furniture.transform.position += move * moveStep;
    }

    void Rotate(float angle)
    {
        cshFurniture furniture = GetCurrent();
        if (furniture != null)
        {
            furniture.transform.Rotate(0.0f, angle, 0.0f);
        }
    }

    cshFurniture GetCurrent()
    {
        if (cshFurnitureSpawner.instance == null || cshFurnitureSpawner.instance.currentFurniture == null)
        {
            lastMessage = "배치 전인 가구가 없음";
            return null;
        }
        return cshFurnitureSpawner.instance.currentFurniture;
    }
}
