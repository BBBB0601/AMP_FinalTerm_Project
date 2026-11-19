// 참고 강의: Chap3 LookAt, Chap2 Rotate
using UnityEngine;

public class cshLookCamera : MonoBehaviour
{
    void Update()
    {
        if (Camera.main == null)
        {
            return;
        }

        Vector3 target = Camera.main.transform.position;
        target.y = transform.position.y;  // 좌우로만 회전 (위아래로 기울지 않게)

        transform.LookAt(target);
        // 글자는 뒷면에서 보면 뒤집혀 보이므로 반 바퀴 돌려서 앞면이 카메라를 향하게 함
        transform.Rotate(0.0f, 180.0f, 0.0f);
    }
}
