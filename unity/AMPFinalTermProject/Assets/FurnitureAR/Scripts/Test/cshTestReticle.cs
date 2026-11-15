using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// [테스트용] 화면 가운데에서 바닥 평면을 찾아 레티클을 표시하고, 배치 전 가구가 레티클을 따라오게 함
// 정식 SFR-004(레티클, ARAnchor, 제스처)는 AR 담당이 구현하면 이 스크립트를 빼면 됨
public class cshTestReticle : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public Transform reticle;              // 바닥에 표시할 원판 (cshFurnitureSpawner.reticle 에도 같은 것을 연결)

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        // 화면 가운데(+ 표시)에서 현실 공간으로 광선을 쏘아 인식된 평면에 맞는지 검사
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        bool found = false;

        if (raycastManager != null && raycastManager.Raycast(center, hits, TrackableType.PlaneWithinPolygon))
        {
            // 벽면에 맞은 것은 건너뛰고 바닥(위를 향한 수평면)만 사용
            foreach (ARRaycastHit hit in hits)
            {
                ARPlane plane = hit.trackable as ARPlane;
                if (plane != null && plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    reticle.position = hit.pose.position;
                    found = true;
                    break;
                }
            }
        }

        reticle.gameObject.SetActive(found);

        // 아직 "배치하기" 전인 가구는 레티클 위치를 따라다님 (회전은 테스트 패널의 회전 버튼으로)
        cshFurnitureSpawner spawner = cshFurnitureSpawner.instance;
        if (found && spawner != null && spawner.currentFurniture != null)
        {
            spawner.currentFurniture.transform.position = reticle.position;
        }
    }
}
