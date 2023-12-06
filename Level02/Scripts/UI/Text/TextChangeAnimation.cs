using System;
using Sirenix.OdinInspector;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace UITweenAnimation
{
    [AddComponentMenu("UI/Tween/UI Text Change Animation", 0)]
    public class TextChangeAnimation : MonoBehaviour
    {
        //임시용 컴포넌트
        [Title("타겟")] public TMP_Text target;

        [Title("간격")] [Tooltip("글자가 전환되는 간격 시간"), Min(0.1f)]
        public float interval = 0.3f;
        
        [Title("내용")]
        [DisableInPlayMode] public string[] textList;

        private int _index;
        private IDisposable _disposables;

        public void Start()
        {
            //없을 시 리턴
            if (textList.Length == 0)
                return;

            _disposables = Observable
                .Interval(TimeSpan.FromSeconds(interval))
                .Subscribe(ChangeText);

            this.UpdateAsObservable()
                .Select(_ => interval) //매 프레임마다 interval을 방출
                .DistinctUntilChanged() //이전 값과 다른 값만 방출
                .Subscribe(ChangeDuration) //적용
                .AddTo(this);
        }

        private void ChangeText(long obj)
        {
            _index += 1;

            //_index가 textList의 길이를 넘어가면 0으로 초기화
            if (_index >= textList.Length)
                _index = 0;

            //전환
            target.text = textList[_index];
        }
        
        private void ChangeDuration(float _)
        {
            //기존에 등록한 옵저버를 해제합니다.
            _disposables.Dispose();

            _disposables = Observable
                .Interval(TimeSpan.FromSeconds(interval))
                .Subscribe(ChangeText);
        }
        
        private void OnDestroy()
        {
            _disposables?.Dispose();
        }
    }
}