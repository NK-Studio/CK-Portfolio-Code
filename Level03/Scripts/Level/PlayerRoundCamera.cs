using Character.Presenter;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Splines;
using Utility;

namespace Level
{
    [ExecuteInEditMode]
    public class PlayerRoundCamera : MonoBehaviour
    {
        [LabelText("플레이어 위치 트랜스폼")]
        public Transform PlayerTransform;
        [LabelText("카메라 경로 스플라인")]
        public SplineContainer Spline;
        [LabelText("기본 카메라 회전")]
        public Quaternion DefaultTargetRotation = Quaternion.Euler(-40f, 0f, 0f);
        [LabelText("트랜스폼 적용 대상")]
        public Transform TargetObject;
        [LabelText("카메라 오브젝트")]
        public GameObject CameraObject;
        [LabelText("씬 원점")]
        public Transform CircularCenter;
        [LabelText("Ray 방식 사용 여부")]
        public bool UseRay = true;
        private void Start()
        {
            if (!PlayerTransform)
            {
                PlayerTransform = FindAnyObjectByType<PlayerPresenter>()?.transform;
            }
        }

        private void Update()
        {
            if(UseRay){
                if(!CircularCenter || !Spline || Spline.Spline == null) return;
                var spline = Spline.Spline;
                var origin = PlayerTransform.position;
                var circularOrigin = CircularCenter.position;
                var ray = new Ray(origin, (origin - circularOrigin).Copy(y: 0f).normalized);
                DebugX.DrawLine(origin, circularOrigin, Color.yellow);
                var nearest = GetCircularPosition(spline, ray, out var t);

                if (t <= 0f || t >= 1f)
                {
                    CameraObject.gameObject.SetActive(false);
                    return;
                }
                CameraObject.gameObject.SetActive(true);
                
                DebugX.DrawLine(origin, nearest, Color.green);
                if(!TargetObject) return;
                // var rotation = Quaternion.LookRotation(-ray.direction);
                var rotation = Quaternion.LookRotation(-GetRightVector(spline, t));
                TargetObject.SetPositionAndRotation(nearest, rotation * DefaultTargetRotation);
                DebugX.DrawLine(nearest, nearest + TargetObject.forward, Color.cyan);
            }
            else
            {
                if (!Spline || Spline.Spline == null) return;
                var spline = Spline.Spline;
                var origin = PlayerTransform.position;
                var nearest = GetNearestPosition(spline, origin, out var t);

                if (t <= 0f || t >= 1f)
                {
                    CameraObject.gameObject.SetActive(false);
                    return;
                }
                CameraObject.gameObject.SetActive(true);

                DebugX.DrawLine(origin, nearest, Color.green);
                if (!TargetObject) return;
                var rotation = Quaternion.LookRotation((PlayerTransform.position - nearest).Copy(y: 0f).normalized);
                TargetObject.SetPositionAndRotation(nearest, rotation * DefaultTargetRotation);
                DebugX.DrawLine(nearest, nearest + TargetObject.forward, Color.cyan);
            }
        }

        private Vector3 GetNearestPosition(Spline spline, Vector3 from, out float t)
        {
            SplineUtility.GetNearestPoint(spline, from, out var nearest, out t);
            return nearest;
        }

        private Vector3 GetCircularPosition(Spline spline, Ray ray, out float t)
        {
            SplineUtility.GetNearestPoint(spline, ray, out var nearest, out t);
            return nearest;
        }

        private Vector3 GetRightVector(Spline spline, float t)
        {
            spline.Evaluate(t, out var position, out var tangent, out var _);
            return Vector3.Cross(Vector3.up, tangent).normalized;
        }
    }
}