using Sirenix.OdinInspector;
using UnityEngine;

namespace Animation
{
    public enum RotationDirection
    {
        Left,
        Right
    }

    public class ItemAnimation : MonoBehaviour
    {
        [Title("위치")]
        //속도를 변경하려면 이것을 조정하십시오
        public float moveSpeed = 5f;

        //높이를 변경하려면 이것을 조정하십시오.
        public float height = 0.5f;

        [Title("회전")] public float rotationSpeed = 5f;
        public RotationDirection leftDirection;

        private const float PositionThreshold = 0.01f;
        private const float RotationThreshold = 100f;
        
        private void Update()
        {
            Move();
            Rotation();
        }

        private void Move()
        {
            //객체의 현재 위치를 가져와 변수에 넣어 나중에 더 적은 코드로 액세스할 수 있습니다.
            Vector3 pos = transform.localPosition;

            //새로운 Y 위치가 무엇인지 계산
            float newY = Mathf.Sin(Time.time * moveSpeed);

            //객체의 Y를 새로 계산된 Y로 설정
            transform.localPosition = new Vector3(pos.x, newY, pos.z) * (height * PositionThreshold);
        }

        private void Rotation()
        {
            transform.rotation = leftDirection == RotationDirection.Left
                ? Quaternion.Euler(0, Time.time * rotationSpeed * RotationThreshold, 0)
                : Quaternion.Euler(0, Time.time * -rotationSpeed * RotationThreshold, 0);
        }
    }
}