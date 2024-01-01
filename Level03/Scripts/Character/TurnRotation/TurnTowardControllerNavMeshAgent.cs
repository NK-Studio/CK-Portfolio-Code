using System;
using Character.Model;
using Character.View;
using Managers;
using Settings;
using UnityEngine;
using UnityEngine.AI;

namespace Character.TurnRotation
{
    //이 스크립트는 게임 오브젝트를 대상 컨트롤러의 속도 방향으로 돌립니다.
    public class TurnTowardControllerNavMeshAgent : MonoBehaviour
    {
        public PlayerModel PlayerModel;
        public PlayerView PlayerView;
        [SerializeField] private NavMeshAgent _agent;

        private Transform _tr;
        private CharacterSettings _characterSettings;

        private void Awake()
        {
            _tr = transform;
        }

        //Setup;
        private void Start()
        {
            _characterSettings = GameManager.Instance.Settings;
        }

        private void Update()
        {
            if (!PlayerModel.IsStop)
                TurnRotation();
        }

        private void TurnRotation()
        {
            var controllerType = PlayerView.GetInput().GetControllerType();

            switch (controllerType)
            {
                case ControllerType.KeyboardMouse:
                    if (_agent.desiredVelocity.sqrMagnitude >= 0.01f)
                    {
                        Vector3 direction = _agent.desiredVelocity.normalized;

                        Quaternion targetAngle = Quaternion.LookRotation(direction);
                        targetAngle.x = 0;
                        targetAngle.z = 0;

                        transform.rotation = Quaternion.Slerp(transform.rotation, targetAngle,
                            Time.deltaTime * _characterSettings.GFXRotateSpeed);
                    }

                    break;
                case ControllerType.KeyboardWASD:
                case ControllerType.Gamepad:
                    
                    Vector2 axis = PlayerView.GetInput().MoveAxis.Value;
                    Vector3 velocity = _agent.desiredVelocity;

                    bool isMoving = velocity.sqrMagnitude >= 0.001f;
                    bool isControlling = false && axis.sqrMagnitude >= 0.001f;
                    
                    if (isControlling || isMoving)
                    {
                        Vector3 direction = isControlling ? new Vector3(axis.x, 0, axis.y) : velocity;
                        direction.y = 0f;
                        direction.Normalize();
                        Quaternion startRotation = transform.rotation;
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        Quaternion endRotation = Quaternion.Euler(new Vector3(0, targetRotation.eulerAngles.y, 0));

                        Vector3 currentDirection = transform.forward;
                        currentDirection.y = 0f;
                        currentDirection.Normalize();
                        
                        const float angleNormalizer = 1f / 180f;
                        // dot 쓰려고 했는데 생각해보니까 dot 결과물은 cos라서 linear하지 않음 ㅋㅋ;
                        // 그래서 그냥 angle -> acos 써서 [0º ~ 180º] 얻어오기
                        float angle = Vector3.Angle(currentDirection, direction);
                        // 나누기 180해서 [0 ~ 1]로 정규화
                        float multiplier = _characterSettings.GFXRotateCurve.Evaluate(angle * angleNormalizer);
                        float rotateSpeed = _characterSettings.GFXRotateSpeed 
                            * multiplier 
                            * Time.deltaTime;
                        // DebugX.Log($"dot: {angle:F3}, multiplier: {multiplier:F3}, rotateSpeed: {rotateSpeed:F3}");
                        transform.rotation = Quaternion.RotateTowards(startRotation, endRotation, rotateSpeed);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetRotationAs(Transform t) => SetRotation(t.forward);
        /// <summary>
        /// degree(도)값 방향으로 회전 시킵니다.
        /// </summary>
        /// <param name="direction"></param>
        public void SetRotation(Vector3 direction)
        {
            direction.Normalize();
            _tr.localRotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        /// <summary>
        /// degree(도)값 방향으로 회전 시킵니다.
        /// </summary>
        /// <param name="angle"></param>
        public void SetRotation(float angle)
        {
            _tr.localRotation = Quaternion.Euler(0f, angle, 0f);
        }

        /// <summary>
        /// degree(도)값 방향으로 회전 시킵니다.
        /// </summary>
        /// <param name="direction"></param>
        public void SetRotationBody(Vector3 direction)
        {
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            rotation.x = 0;
            rotation.z = 0;
            _tr.rotation = rotation;
        }

        /// <summary>
        /// 앞을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetForward() => _tr.forward;
        public Vector3 GetRight() => _tr.right;

        /// <summary>
        /// 오일러 앵글을 반환합니다.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetEulerAngle() => _tr.eulerAngles;

        public Quaternion GetRotation() => _tr.rotation;
    }
}