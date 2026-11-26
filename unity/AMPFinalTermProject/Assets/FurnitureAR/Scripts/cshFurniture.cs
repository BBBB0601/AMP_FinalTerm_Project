using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class cshFurniture : MonoBehaviour
{
    public string furnitureName;   // 이름 

    // 치수는 m 단위 (Android가 cm → m 로 바꾼 FurnitureConfig를 보냄, SFR-001)
    public float width = 1.2f;     // 가로 W → X축
    public float depth = 0.6f;     // 깊이 D → Z축
    public float height = 0.75f;   // 높이 H → Y축

    public const float minSize = 0.1f;          // 10cm  (SFR-001 입력 범위)
    public const float maxSize = 3.0f;          // 300cm
    public const float clearanceDepth = 0.6f;   // 통행 동선 60cm (SFR-006)

    [Header("구성 요소 (비어 있으면 실행 중에 만듦)")]
    public MeshFilter bodyFilter;    // 반투명 박스 (이 오브젝트)
    public MeshFilter edgeFilter;    // 모서리 윤곽선 (자식 Edges)
    public BoxCollider boxCollider;  // 충돌 영역
    public MeshFilter guideFilter;   // 전면 바닥 동선 가이드 (자식 ClearanceGuide)

    [Header("동선 가이드 (SFR-006)")]
    public bool guideAllSides = false;       // false: 앞면만 W x 60cm (보고서 사양) / true: 사방 60cm (팀 합의 시)
    public Material guideGridMaterial;       // 노란 점선 격자 머티리얼 (ClearanceGuide.mat). 비어 있으면 단색 노랑

    [Header("치수 라벨 (SFR-003)")]
    public TextMeshPro widthLabel;   // 가로 치수 (윗면 앞 모서리 위, 예: 120cm)
    public TextMeshPro heightLabel;  // 높이 치수 (앞 왼쪽 세로 모서리 옆, 예: 75cm)
    public float labelGap = 0.03f;   // 모서리에서 라벨까지 3cm 띄움

    [Header("색")]
    public Color safeColor = new Color(0.0f, 0.85f, 0.8f, 0.35f);       // 안전: 청록 반투명
    public Color collidingColor = new Color(1.0f, 0.1f, 0.1f, 0.6f);    // 충돌: 빨강
    public Color guideColor = new Color(1.0f, 0.85f, 0.0f, 0.4f);       // 동선: 노랑

    public bool isPlaced = false;     // "배치하기"로 고정됐는지
    public bool isColliding = false;  // 지금 벽/다른 가구와 겹쳐 있는지 (cshCollisionChecker가 판정)

    private Mesh bodyMesh;
    private Mesh edgeMesh;
    private Mesh guideMesh;
    private Material bodyMaterial;
    private Material guideMaterial;

    // 치수를 바꾸고 박스를 다시 만듦. 잘못된 치수면 false (원래 모양 유지)
    public bool SetSize(float w, float d, float h)
    {
        if (IsValidSize(w, d, h) == false)
        {
            Debug.Log("잘못된 치수(m): " + w + " x " + d + " x " + h + " (0.1 ~ 3.0m만 가능)");
            return false;
        }

        width = w;
        depth = d;
        height = h;

        MakeBox();
        MakeCollider();
        MakeGuide();
        MakeLabels();
        return true;
    }

    public static bool IsValidSize(float w, float d, float h)
    {
        return IsValidAxis(w) && IsValidAxis(d) && IsValidAxis(h);
    }

    static bool IsValidAxis(float m)
    {
        // NaN은 어떤 비교를 해도 false라서 이 검사로 함께 걸러짐
        return m >= minSize && m <= maxSize;
    }

    // ---------- 3D 체적 메시 ----------

    void MakeBox()
    {
        float x = width * 0.5f;   // 가로의 절반
        float z = depth * 0.5f;   // 깊이의 절반
        float y = height;         // 바닥이 0이라 높이 그대로

        // 정점 8개. 피벗(0,0,0)이 바닥면 가운데라서 AR 바닥에 그대로 놓을 수 있음
        //      7-------6
        //     /|      /|      윗면 4,5,6,7
        //    4-------5 |
        //    | 3-----|-2      바닥 0,1,2,3
        //    |/      |/       앞면(-Z, 사용자 쪽)이 0,1,5,4
        //    0-------1
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-x, 0.0f, -z),
            new Vector3(x, 0.0f, -z),
            new Vector3(x, 0.0f, z),
            new Vector3(-x, 0.0f, z),
            new Vector3(-x, y, -z),
            new Vector3(x, y, -z),
            new Vector3(x, y, z),
            new Vector3(-x, y, z),
        };

        // 면 6개 x 삼각형 2개. 바깥에서 봤을 때 시계 방향으로 이어야 보임
        int[] triangles = new int[]
        {
            0, 4, 5,  0, 5, 1,  // 앞 (-Z)
            1, 5, 6,  1, 6, 2,  // 오른쪽 (+X)
            2, 6, 7,  2, 7, 3,  // 뒤 (+Z)
            3, 7, 4,  3, 4, 0,  // 왼쪽 (-X)
            4, 7, 6,  4, 6, 5,  // 위 (+Y)
            1, 2, 3,  1, 3, 0,  // 아래 (-Y)
        };

        // 처음 한 번만 new Mesh, 이후에는 같은 메시를 고쳐 씀 (치수 바꿀 때마다 새로 만들면 메모리 낭비)
        if (bodyMesh == null)
        {
            bodyMesh = new Mesh();
            bodyMesh.name = "FurnitureBox";
        }
        bodyMesh.Clear();
        bodyMesh.vertices = vertices;
        bodyMesh.triangles = triangles;
        bodyMesh.RecalculateNormals();
        bodyMesh.RecalculateBounds();

        if (bodyFilter == null)
        {
            bodyFilter = GetComponent<MeshFilter>();
        }
        bodyFilter.sharedMesh = bodyMesh;

        // 반투명 머티리얼은 가구마다 따로 복사해서 써야 한 가구만 빨갛게 바꿀 수 있음
        MeshRenderer bodyRenderer = bodyFilter.GetComponent<MeshRenderer>();
        if (bodyMaterial == null && bodyRenderer.sharedMaterial != null)
        {
            bodyMaterial = new Material(bodyRenderer.sharedMaterial);
            bodyRenderer.sharedMaterial = bodyMaterial;
        }
        UpdateColor();

        // 모서리 12개를 선으로 (같은 정점 8개를 2개씩 이어서 선 하나)
        if (edgeFilter != null)
        {
            int[] lines = new int[]
            {
                0, 1, 1, 2, 2, 3, 3, 0,  // 바닥 4개
                4, 5, 5, 6, 6, 7, 7, 4,  // 윗면 4개
                0, 4, 1, 5, 2, 6, 3, 7,  // 기둥 4개
            };

            if (edgeMesh == null)
            {
                edgeMesh = new Mesh();
                edgeMesh.name = "FurnitureEdges";
            }
            edgeMesh.Clear();
            edgeMesh.vertices = vertices;
            edgeMesh.SetIndices(lines, MeshTopology.Lines, 0);
            edgeMesh.RecalculateBounds();
            edgeFilter.sharedMesh = edgeMesh;
        }
    }

    // 충돌 영역을 박스 크기에 맞춤. 없으면 붙임
    void MakeCollider()
    {
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }
        boxCollider.size = new Vector3(width, height, depth);
        boxCollider.center = new Vector3(0.0f, height * 0.5f, 0.0f);
        boxCollider.isTrigger = true;  // 물리로 밀려나지 않고 겹침만 검사
    }

    // ---------- SFR-006: 통행 동선 가이드 (앞면 W x 60cm, 또는 사방 60cm) ----------

    // 가이드 메시를 만들 때 사각형 정보를 모아 두는 목록
    private List<Vector3> guideVertices = new List<Vector3>();
    private List<Vector2> guideUv = new List<Vector2>();
    private List<Vector2> guideSize = new List<Vector2>();
    private List<Color> guideEdges = new List<Color>();
    private List<int> guideTriangles = new List<int>();

    void MakeGuide()
    {
        if (guideFilter == null)
        {
            GameObject guide = new GameObject("ClearanceGuide");
            guide.transform.SetParent(transform, false);
            guideFilter = guide.AddComponent<MeshFilter>();
            MeshRenderer guideRenderer = guide.AddComponent<MeshRenderer>();
            guideRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            guideRenderer.receiveShadows = false;

            if (guideGridMaterial != null)
            {
                // 노란 점선 격자 셰이더 (모든 가구가 같은 머티리얼을 함께 씀)
                guideRenderer.sharedMaterial = guideGridMaterial;
            }
            else if (bodyMaterial != null)
            {
                // 격자 머티리얼이 없으면 박스와 같은 반투명 셰이더를 노란색으로 복사해서 사용
                guideMaterial = new Material(bodyMaterial);
                guideMaterial.color = guideColor;
                guideRenderer.sharedMaterial = guideMaterial;
            }
        }

        float x = width * 0.5f;
        float z = depth * 0.5f;
        float c = clearanceDepth;  // 60cm

        guideVertices.Clear();
        guideUv.Clear();
        guideSize.Clear();
        guideEdges.Clear();
        guideTriangles.Clear();

        if (guideAllSides == false)
        {
            // 앞면(사용자 쪽, -Z)만: 가로 W x 60cm, 테두리 4변 모두 점선
            AddGuideRect(-x, x, -z - c, -z, true, true, true, true);
        }
        else
        {
            // 사방 60cm: 앞/뒤 띠는 모서리까지 포함해 넓게, 좌/우 띠는 그 사이만
            // 띠끼리 맞닿는 변은 테두리를 그리지 않음 (바깥 둘레와 가구 쪽 변만 점선)
            AddGuideRect(-x - c, x + c, -z - c, -z, true, true, true, true);   // 앞
            AddGuideRect(-x - c, x + c, z, z + c, true, true, true, true);     // 뒤
            AddGuideRect(-x - c, -x, -z, z, true, true, false, false);         // 왼쪽
            AddGuideRect(x, x + c, -z, z, true, true, false, false);           // 오른쪽
        }

        if (guideMesh == null)
        {
            guideMesh = new Mesh();
            guideMesh.name = "ClearanceGuide";
        }
        guideMesh.Clear();
        guideMesh.SetVertices(guideVertices);
        guideMesh.SetUVs(0, guideUv);       // 사각형 안에서의 위치 (m)
        guideMesh.SetUVs(1, guideSize);     // 사각형 크기 (m)
        guideMesh.SetColors(guideEdges);    // 점선 테두리를 그릴 변
        guideMesh.SetTriangles(guideTriangles, 0);
        guideMesh.RecalculateNormals();
        guideMesh.RecalculateBounds();
        guideFilter.sharedMesh = guideMesh;
    }

    // 바닥에 사각형 하나 추가 (xMin~xMax, zMin~zMax). left/right/near/far: 그 변에 점선 테두리를 그릴지
    void AddGuideRect(float xMin, float xMax, float zMin, float zMax, bool left, bool right, bool near, bool far)
    {
        float y = 0.005f;  // 바닥과 겹쳐 깜빡이지 않게 5mm 띄움
        float w = xMax - xMin;
        float d = zMax - zMin;
        int start = guideVertices.Count;

        guideVertices.Add(new Vector3(xMin, y, zMin));
        guideVertices.Add(new Vector3(xMin, y, zMax));
        guideVertices.Add(new Vector3(xMax, y, zMax));
        guideVertices.Add(new Vector3(xMax, y, zMin));

        guideUv.Add(new Vector2(0.0f, 0.0f));
        guideUv.Add(new Vector2(0.0f, d));
        guideUv.Add(new Vector2(w, d));
        guideUv.Add(new Vector2(w, 0.0f));

        // r=왼쪽, g=오른쪽, b=아래(zMin), a=위(zMax)
        Color edges = new Color(left ? 1.0f : 0.0f, right ? 1.0f : 0.0f, near ? 1.0f : 0.0f, far ? 1.0f : 0.0f);
        for (int i = 0; i < 4; i++)
        {
            guideSize.Add(new Vector2(w, d));
            guideEdges.Add(edges);
        }

        // 위에서 봤을 때 시계 방향
        guideTriangles.Add(start + 0);
        guideTriangles.Add(start + 1);
        guideTriangles.Add(start + 2);
        guideTriangles.Add(start + 0);
        guideTriangles.Add(start + 2);
        guideTriangles.Add(start + 3);
    }

    // Android "동선 가이드" 스위치로 켜고 끔
    public void ShowGuide(bool show)
    {
        if (guideFilter != null)
        {
            guideFilter.gameObject.SetActive(show);
        }
    }

    // ---------- 치수 라벨 ----------

    // 가로/높이 모서리에 실제 치수(cm)를 표시. 라벨은 cshLookCamera로 항상 카메라를 바라봄
    void MakeLabels()
    {
        float x = width * 0.5f;
        float z = -depth * 0.5f;   // 앞면(사용자 쪽)

        if (widthLabel != null)
        {
            widthLabel.text = ToCmText(width);
            // 윗면 앞 모서리 가운데 바로 위 (기준점을 글자 아래쪽 가운데로 → 글자가 모서리 위로 올라감)
            widthLabel.rectTransform.pivot = new Vector2(0.5f, 0.0f);
            widthLabel.transform.localPosition = new Vector3(0.0f, height + labelGap, z);
        }

        if (heightLabel != null)
        {
            heightLabel.text = ToCmText(height);
            // 앞 왼쪽 세로 모서리 가운데 옆 (기준점을 글자 오른쪽 끝으로 → 글자가 모서리 바깥쪽으로 뻗음)
            heightLabel.rectTransform.pivot = new Vector2(1.0f, 0.5f);
            heightLabel.transform.localPosition = new Vector3(-x - labelGap, height * 0.5f, z);
        }
    }

    // 0.75 → "75cm" (Android 입력이 cm 정수라 반올림해서 표시)
    public static string ToCmText(float meters)
    {
        return Mathf.RoundToInt(meters * 100.0f) + "cm";
    }

    // 치수 라벨 켜고 끄기
    public void ShowLabels(bool show)
    {
        if (widthLabel != null)
        {
            widthLabel.gameObject.SetActive(show);
        }
        if (heightLabel != null)
        {
            heightLabel.gameObject.SetActive(show);
        }
    }

    // ---------- 충돌 표시 (판정은 cshCollisionChecker) ----------

    // 충돌 상태를 바꾸고 색을 바꿈 (안전: 청록 / 충돌: 빨강)
    public void SetColliding(bool colliding)
    {
        isColliding = colliding;
        UpdateColor();
    }

    void UpdateColor()
    {
        if (bodyMaterial == null)
        {
            return;
        }

        if (isColliding)
        {
            bodyMaterial.color = collidingColor;
        }
        else
        {
            bodyMaterial.color = safeColor;
        }
    }

    // 직접 만든 메시/머티리얼은 오브젝트를 지워도 남아 있으므로 같이 지움
    void OnDestroy()
    {
        DestroyIfExists(bodyMesh);
        DestroyIfExists(edgeMesh);
        DestroyIfExists(guideMesh);
        DestroyIfExists(bodyMaterial);
        DestroyIfExists(guideMaterial);
    }

    void DestroyIfExists(Object target)
    {
        if (target != null)
        {
            Destroy(target);
        }
    }
}
