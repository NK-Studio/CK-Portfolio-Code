using System.Collections.Generic;
using UnityEngine;

namespace Platform
{
    //이 스크립트는 이 게임 오브젝트에 연결된 트리거 충돌기에 들어가거나 나가는 모든 컨트롤러를 추적합니다.
    //'MovingPlatform'에서 그 위에 서 있는 컨트롤러를 감지하고 이동하는 데 사용됩니다.
    public class TriggerArea : MonoBehaviour
    {
        public List<Rigidbody> rigidbodiesInTriggerArea = new();

        //방금 트리거에 들어간 콜라이더에 리지드바디가 연결되어 있는지 확인하고 목록에 추가합니다.
        private void OnTriggerEnter(Collider col)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null) 
                rigidbodiesInTriggerArea.Add(rb);
        }

        //방금 방아쇠를 당긴 콜라이더에 리지드바디가 부착되어 있는지 확인하고 목록에서 제거합니다.
        private void OnTriggerExit(Collider col)
        {
            Rigidbody rb = col.attachedRigidbody;
            if (rb != null) 
                rigidbodiesInTriggerArea.Remove(rb);
        }
    }
}