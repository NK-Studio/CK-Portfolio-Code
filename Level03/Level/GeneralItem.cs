using System;
using Character.Presenter;
using Dummy.Scripts;
using Enemy.UI;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Utility;

namespace Level
{
    public abstract class GeneralItem : MonoBehaviour, IItem
    {
        public enum DropMode
        {
            InPlace,
            Parabola,
        }
        
        [SerializeField, ReadOnly, BoxGroup("상태")] 
        private bool _dropping = false;
        [SerializeField, ReadOnly, BoxGroup("상태")] 
        private DropMode _mode;

        [SerializeField, BoxGroup("드랍 공통")]
        private float _dropHeight = 5f;
        [SerializeField, BoxGroup("드랍 공통")]
        private float _dropDuration = 1f;
        
        [SerializeField, BoxGroup("제자리 드랍")] 
        private AnimationCurve _dropInPlaceCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 1f, 1f),
            new Keyframe(0.5f, 1f, 0f, 0f),
            new Keyframe(1f, 0f, -1f, -1f)
        );
        
        [SerializeField, BoxGroup("포물선 드랍"), ReadOnly]
        private ParabolaByMaximumHeightGenerator _dropParabola;

        private Vector3 _spawnPosition;
        private Vector3 _targetPosition;
        private float _time;
        public void Initialize(Vector3 spawnPosition)
        {
            _dropping = true;
            _time = 0f;

            var targetPosition = _targetPosition = IItem.GetPlayerAvailablePosition(_spawnPosition = spawnPosition);
            // 수평 위치가 차이가 있으면: 포물선 사용
            if ((targetPosition - spawnPosition).Copy(y: 0f).sqrMagnitude >= Vector3.kEpsilon)
            {
                _mode = DropMode.Parabola;
                _dropParabola.Start = spawnPosition;
                _dropParabola.End = targetPosition;
                _dropParabola.HighestFromLeap = _dropHeight - (targetPosition.y - spawnPosition.y);
                _dropParabola.Generate();
            }
            // 수평 위치가 차이가 없으면: 커브 수직 낙하운동
            else
            {
                _mode = DropMode.InPlace;
            }

            transform.forward = (-Camera.main.transform.forward).Copy(y: 0f).normalized;
        }

        protected virtual void Update()
        {
            if (!_dropping)
            {
                return;
            }
            if (_time > _dropDuration)
            {
                _dropping = false;
                transform.position = _targetPosition;
                return;
            }
            _time += Time.deltaTime;

            var normalizedTime = _time / _dropDuration;
            switch (_mode)
            {
                case DropMode.InPlace:
                {
                    var y = _targetPosition.y + _dropInPlaceCurve.Evaluate(normalizedTime) * _dropHeight;
                    transform.position = _targetPosition.Copy(y: y);
                    break;
                }
                case DropMode.Parabola:
                {
                    var position = _dropParabola.Parabola.GetPosition(normalizedTime);
                    transform.position = position;
                    break;
                }
            }
        }

        public bool CanBeNearestItem(PlayerPresenter player)
        {
            var result = !_dropping && CanBeSelected(player);
            if (result && InteractImmediatelyIfCanBeSelected(player))
            {
                Interact(player);
                return false;
            }
            return result;
        }

        protected virtual bool CanBeSelected(PlayerPresenter player) => true;
        protected virtual bool InteractImmediatelyIfCanBeSelected(PlayerPresenter player) => false;
        public abstract void Interact(PlayerPresenter player);
        
        private InteractionUI _guide;
        public virtual void OnStartNearestItem()
        {
            // Debug.Log($"Nearest Item {name} Start");
            _guide = InteractionUIPool.Instance.Get();
            _guide.Target = this;
        }

        public virtual void OnEndNearestItem()
        {
            // Debug.Log($"Nearest Item {name} End");
            if (_guide)
            {
                _guide.Release();
            }
        }

        [field: SerializeField, BoxGroup("디버깅")]
        private Color _startColor = Color.green;
        [field: SerializeField, BoxGroup("디버깅")]
        private Color _endColor = Color.cyan;
        [field: SerializeField, BoxGroup("디버깅")]
        private float _verticalLineAlpha = 0.75f;
        private void OnDrawGizmos()
        {
            var parabola = _dropParabola.Parabola;
            if (!parabola.Valid)
            {
                return;
            }

            var startColor = _startColor;
            var endColor = _endColor;
            Vector3 v = parabola.Start;
            Vector3 endPosition;
            endPosition = parabola.GetPosition(1f);
            for (float f = 0; f <= 1f; f += 0.05f)
            {
                var p = parabola.GetPosition(f);
                Gizmos.color = Color.Lerp(startColor, endColor, f);
                Gizmos.DrawLine(v, p);
                Gizmos.color = Gizmos.color.Copy(a: _verticalLineAlpha);
                Gizmos.DrawLine(p, p.Copy(y: endPosition.y));
                v = p;
            }
            Gizmos.color = endColor;
            Gizmos.DrawLine(v, endPosition);

            var horizontalOne = new Vector3(1f, 0f, 1f);
            Gizmos.DrawLine(endPosition + horizontalOne, endPosition - horizontalOne);
            Gizmos.DrawLine(endPosition + horizontalOne.Copy(x: -1f), endPosition - horizontalOne.Copy(x: -1f));
        }
    }
}