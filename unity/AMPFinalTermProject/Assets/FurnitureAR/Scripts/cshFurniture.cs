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

    // ---------- SFR-006: 전면 동선 가이드 (가로 W x 60cm) ----------

    void MakeGuide()
    {
        if (guideFilter == null)
        {
            GameObject guide = new GameObject("ClearanceGuide");
            guide.transform.SetParent(transform, false);
            guideFilter = guide.AddComponent<MeshFilter>();
            MeshRenderer guideRenderer = guide.AddComponent<MeshRenderer>();
            guideRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // 박스와 같은 반투명 셰이더를 노란색으로 복사해서 사용
            if (bodyMaterial != null)
            {
                guideMaterial = new Material(bodyMaterial);
                guideMaterial.color = guideColor;
                guideRenderer.sharedMaterial = guideMaterial;
            }
        }

        float x = width * 0.5f;
        float zNear = -depth * 0.5f;              // 가구 앞면
        float zFar = zNear - clearanceDepth;      // 앞면에서 60cm 앞
        float y = 0.005f;                         // 바닥과 겹쳐 깜빡이지 않게 5mm 띄움

        if (guideMesh == null)
        {
            guideMesh = new Mesh();
            guideMesh.name = "ClearanceGuide";
        }
        guideMesh.Clear();
        guideMesh.vertices = new Vector3[]
        {
            new Vector3(-x, y, zFar),
            new Vector3(-x, y, zNear),
            new Vector3(x, y, zNear),
            new Vector3(x, y, zFar),
        };
        guideMesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };  // 위에서 봤을 때 시계 방향
        guideMesh.RecalculateNormals();
        guideMesh.RecalculateBounds();
        guideFilter.sharedMesh = guideMesh;
    }

    // Android "동선 가이드" 스위치로 켜고 끔
    public void ShowGuide(bool show)
    {
        if (guideFilter != null)
        {
            guideFilter.gameObject.SetActive(show);
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
