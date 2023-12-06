using System;
using System.Threading;
using AutoManager;
using Cysharp.Threading.Tasks;
using FMODUnity;
using Managers;
using Sirenix.OdinInspector;
using Spine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Event = Spine.Event;

namespace Animation.CutScene
{
    public class CutSceneManager : MonoBehaviour
    {
        private enum CutSceneType
        {
            Starting,
            Ending,
        }

        [Title("스타트 컷씬")] [SerializeField] private SkeletonGraphic[] StartCutScene;

        [Title("엔딩 컷씬")] [SerializeField] private SkeletonGraphic[] EndingCutScene;

        [Title("애니메이션 컷씬")] [SerializeField] private CutSceneType AnimationType;
        [SerializeField] EventReference NextPageClip;

        [Title("이동할 씬")] public string NextScene;

        [Title("텍스트")]
        public TMP_Text SpaceBarText;
        
        public bool PlayOnAwake = true;

        [Title("사운드")] 
        public EventReference BGM;
        public EventReference[] SFX;

        private CancellationTokenSource _cancellationTokenSource;

        private bool _isEnd;

        private AsyncOperation _endCredit;

        private int _index;
        private float _currentTime;
        private float _endTime;

        private string _nextText = "스페이스바로 넘기기";
        private string _skipText = "스페이스바로 스킵";
        
        private void Awake()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private void Start()
        {
 
            if (AnimationType == CutSceneType.Starting)
            {
                Manager.Get<AudioManager>().ChangeBGMWithPlay(BGM);

                StartCutScene[0].AnimationState.Event += AnimationStateOnEvent;
                StartCutScene[1].AnimationState.Event += AnimationStateOnEvent;
                StartCutScene[2].AnimationState.Event += AnimationStateOnEvent;
                StartCutScene[3].AnimationState.Event += AnimationStateOnEvent;
                StartCutScene[4].AnimationState.Event += AnimationStateOnEvent;

                StartCutScene[_index].gameObject.SetActive(true);
            }
            else if (AnimationType == CutSceneType.Ending)
            {
                Time.timeScale = 1f;
                GameObject whiteCanvasObject = GameObject.Find("WhiteFadeCanvas");

                if (whiteCanvasObject)
                {
                    WhiteFadeManager whiteFadeManager = whiteCanvasObject.GetComponent<WhiteFadeManager>();

                    whiteFadeManager.TestFadeOut();
                    DeleteWhiteCanvasTask().Forget();
                }
                
                Manager.Get<GameManager>().ResetGame();

                Manager.Get<AudioManager>().ChangeBGMWithPlay(BGM);
                _endCredit = SceneManager.LoadSceneAsync("Credit");
                _endCredit.allowSceneActivation = false;

                EndingCutScene[_index].gameObject.SetActive(true);
            }
        }

        // 두지


        private async UniTaskVoid DeleteWhiteCanvasTask()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: this.GetCancellationTokenOnDestroy());
            DeleteWhiteCanvas();
        }

        private void AnimationStateOnEvent(TrackEntry trackentry, Event e)
        {
            if (e.Data.Name == "Magic")
                Manager.Get<AudioManager>().PlayOneShot(SFX[0]);

            else if (e.Data.Name == "Fall")
                Manager.Get<AudioManager>().PlayOneShot(SFX[1]);

            else if (e.Data.Name == "HelpMe")
                Manager.Get<AudioManager>().PlayOneShot(SFX[2]);

            else if (e.Data.Name == "Faint")
                Manager.Get<AudioManager>().PlayOneShot(SFX[3]);

            else if (e.Data.Name == "Dung01")
                Manager.Get<AudioManager>().PlayOneShot(SFX[4]);

            else if (e.Data.Name == "Dung02")
                Manager.Get<AudioManager>().PlayOneShot(SFX[5]);

            else if (e.Data.Name == "Yea")
                Manager.Get<AudioManager>().PlayOneShot(SFX[6]);

            else if (e.Data.Name == "Whoosh01")
                Manager.Get<AudioManager>().PlayOneShot(SFX[7]);

            else if (e.Data.Name == "Whoosh02")
                Manager.Get<AudioManager>().PlayOneShot(SFX[8]);

            else if (e.Data.Name == "Whoosh03")
                Manager.Get<AudioManager>().PlayOneShot(SFX[9]);
        }

        private void Update()
        {
            if(AnimationType == CutSceneType.Starting)
            {
                _currentTime = StartCutScene[_index].AnimationState.GetCurrent(0).AnimationTime;
                _endTime = StartCutScene[_index].AnimationState.GetCurrent(0).AnimationEnd;

            }
            else if (AnimationType == CutSceneType.Ending)
            {
                _currentTime = EndingCutScene[_index].AnimationState.GetCurrent(0).AnimationTime;
                _endTime = EndingCutScene[_index].AnimationState.GetCurrent(0).AnimationEnd;
            }

            if (_currentTime > _endTime-1) 
                SpaceBarText.text = _nextText;
            else
                SpaceBarText.text = _skipText;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                CutSceneSkip();
            }
        }

        private void CutSceneSkip()
        {
            if (AnimationType == CutSceneType.Starting)
            {
                if(_currentTime < _endTime)
                {
                    StartCutScene[_index].AnimationState.Update(_endTime);
                }
                else if(_index != 4)
                {
                    StartCutScene[_index].gameObject.SetActive(false);
                    _index++;
                    StartCutScene[_index].gameObject.SetActive(true);
                    Manager.Get<AudioManager>().PlayOneShot(NextPageClip);
                }
                else if(_index == 4)
                {
                    DeleteWhiteCanvas();

                    if (!_isEnd)
                        OnNext();
                }
            }
            else if(AnimationType == CutSceneType.Ending)
            {
                if (_currentTime < _endTime)
                {
                    EndingCutScene[_index].AnimationState.Update(_endTime);
                }
                else if (_index < 4)
                {
                    EndingCutScene[_index].gameObject.SetActive(false);
                    _index++;
                    EndingCutScene[_index].gameObject.SetActive(true);
                    Manager.Get<AudioManager>().PlayOneShot(NextPageClip);
                }
                else if (_index == 4)
                {
                    DeleteWhiteCanvas();

                    if (!_isEnd)
                        OnNext();
                }
            }
        }
        
        /// <summary>
        /// 컷 씬이 끝나면 동작합니다.
        /// </summary>
        private void OnNext()
        {
            if (AnimationType == CutSceneType.Starting)
            {
                Manager.Get<GameManager>().NextSceneInfo.NextScene = NextScene;
                SceneManager.LoadScene("Loading");
            }
            else if (AnimationType == CutSceneType.Ending)
                _endCredit.allowSceneActivation = true;
        }

        /// <summary>
        /// 화이트 캔버스 삭제
        /// </summary>
        private void DeleteWhiteCanvas()
        {
            GameObject whiteFind = GameObject.Find("WhiteFadeCanvas");
            if (whiteFind)
                Destroy(whiteFind);
        }

        private void OnDestroy()
        {
            DeleteToken();
        }

        private void DeleteToken()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}