using AutoManager;
using FMOD.Studio;
using FMODUnity;
using Managers;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public enum Stage
{
    None,
    Stage1 = 3,
    Stage2 = 4,
    Stage3 = 1,
    Intro = 2,
    Home = 2,
    Loading
}

[ManagerDefaultPrefab("AudioManager")]
public class AudioManager : Manager
{
    #region Public

    [BoxGroup("Audio Emitter")] public StudioEventEmitter BGMEmitter;
    [BoxGroup("Audio Emitter")] public StudioEventEmitter SFXEmitter;
    [BoxGroup("Music")] public EventReference[] bgmClip;
    [BoxGroup("Bank")] public string[] Bus;

    #endregion

    #region Private

    private Stage _preStage = Stage.None;
    private Bus _bgmBus;
    private Bus _sfxBus;

    #endregion

    [FormerlySerializedAs("NextBossRoom")] public bool NoStopBGM;

    private void Awake()
    {
        _bgmBus = RuntimeManager.GetBus(Bus[0]);
        _sfxBus = RuntimeManager.GetBus(Bus[1]);
    }

    private void Start()
    {
        //씬이 전환될 때, 자동으로 BGM이 바뀌도록 합니다. 
        SceneManager.activeSceneChanged += OnSceneChanged;
        OnSceneChanged(SceneManager.GetSceneByName("NULL"), SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    //씬이 변경되면 감지해서 알맞게 사운드를 재생한다.
    private void OnSceneChanged(Scene arg0, Scene arg1)
    {
        //현재 씬 이름이 : Demo1이라면,
        if (CompareScene(arg1, "Intro"))
            ChangeBGMWithPlay(Stage.Intro);
        else if (CompareScene(arg1, "Home"))
            ChangeBGMWithPlay(Stage.Home);
        else if (CompareScene(arg1, "Loading"))
        {
            //BGM 멈추기가 false일 때만 로딩에서 BGM을 멈춥니다. 
            if (!NoStopBGM) 
                StopBGM(true);
        }
        else if (CompareScene(arg1, "Stage_1"))
        {
            ChangeBGMWithPlay(Stage.Stage1);
            SetParameterByBGM("Stage", 0f);
        }
        else if (CompareScene(arg1, "Stage_2"))
        {
            ChangeBGMWithPlay(Stage.Stage2);
            SetParameterByBGM("Stage", 1f);
        }
        else if (CompareScene(arg1, "Stage_3")) 
            ChangeBGMWithPlay(Stage.Stage3);
    }

    /// <summary>
    /// 씬 이름이 맞나 확인
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="sceneName"></param>
    /// <returns></returns>
    public bool CompareScene(Scene scene, string sceneName)
    {
        return scene.name.Equals(sceneName);
    }

    /// <summary>
    /// BGM 사운드 이벤트를 변경하는데 사용합니다.
    /// </summary>
    public void ChangeBGM(Stage stage)
    {
        int index = (int)stage;
        BGMEmitter.ChangeEvent(bgmClip[index]);
    }


    /// <summary>
    /// BGM 사운드 이벤트를 변경하는데 사용합니다.
    /// </summary>
    public void ChangeBGM(EventReference clip)
    {
        BGMEmitter.ChangeEvent(clip);
    }

    /// <summary>
    /// BGM 사운드 이벤트를 변경하는데 사용합니다.
    /// </summary>
    public void ChangeBGMWithPlay(Stage stage)
    {
        int index = (int)stage;

        int preIndex = (int)_preStage;
        int nextIndex = (int)stage;

        bool isSame = preIndex == nextIndex;

        //재생해야하는 BGM이 동일하다면 False
        if (isSame)
            return;

        //이전 스테이지에 현재 스테이지를 대입합니다.
        _preStage = stage;

        BGMEmitter.ChangeEvent(bgmClip[index]);
        PlayBGM();
    }

    /// <summary>
    /// BGM 사운드 이벤트를 변경하는데 사용합니다.
    /// </summary>
    public void ChangeBGMWithPlay(EventReference clip)
    {
        _preStage = Stage.None;
        BGMEmitter.ChangeEvent(clip);
        PlayBGM();
    }

    /// <summary>
    /// 볼륨의 파라미터를 변경합니다.
    /// </summary>
    /// <param name="valueName"></param>
    /// <param name="value"></param>
    public void SetParameterByBGM(string valueName, float value)
    {
        BGMEmitter.SetParameter(valueName, value);
    }

    /// <summary>
    /// 효과음을 변경합니다.
    /// </summary>
    /// <param name="path">재생할 효과음 경로</param>
    public void ChangeSFX(EventReference path) => SFXEmitter.ChangeEvent(path);

    /// <summary>
    /// 사운드를 재생하게 해줍니다.
    /// </summary>
    public void PlayBGM() => BGMEmitter.Play();

    /// <summary>
    /// 효과음을 재생하게 해줍니다.
    /// </summary>
    public void PlaySFX() => SFXEmitter.Play();

    /// <summary>
    /// 배경음악이 일시정지 되었는지 반환합니다.
    /// </summary>
    /// <returns></returns>
    public bool IsPlayingBGM() => BGMEmitter.IsPlaying();

    /// <summary>
    /// 사운드를 정지합니다.
    /// </summary>
    /// <param name="fadeOut">true이면 페이드를 합니다.</param>
    public void StopBGM(bool fadeOut = false)
    {
        BGMEmitter.AllowFadeout = fadeOut;
        BGMEmitter.Stop();
    }

    /// <summary>
    /// 사운드를 일시정지하거나, 다시 재생합니다.
    /// </summary>
    /// <param name="pause">true면 정지하고, false면 다시 재생합니다.</param>
    public void SetPauseBGM(bool pause) => BGMEmitter.SetPause(pause);

    public float GetBGMVolume() {
        _bgmBus.getVolume(out var volume);
        return volume;
    }

    /// <summary>
    /// BGM의 볼륨을 조절합니다.
    /// </summary>
    /// <param name="value">0~1사이의 값, 0이면 뮤트됩니다.</param>
    public void SetBGMVolume(float value) => _bgmBus.setVolume(value);

    public float GetSFXVolume() {
        _sfxBus.getVolume(out var volume);
        return volume;
    }
    /// <summary>
    /// SFX의 볼륨을 조절합니다.
    /// </summary>
    /// <param name="value">0~1사이의 값, 0이면 뮤트됩니다.</param>
    public void SetSFXVolume(float value) => _sfxBus.setVolume(value);

    /// <summary>
    /// 인스턴스를 내부에서 만들어서 효과음을 재생하고, 즉시 파괴합니다.
    /// </summary>
    /// <param name="path">재생할 효과음 경로</param>
    /// <param name="position">해당 위치에서 소리를 재생합니다.</param>
    public void PlayOneShot(EventReference path, Vector3 position = default)
    {
        RuntimeManager.PlayOneShot(path, position);
    }

    #region Parameter

    public void Reset()
    {
        BGMEmitter.SetParameter("Fast", 0);
        BGMEmitter.SetParameter("Death", 1);
        BGMEmitter.SetParameter("Stage", 0);
    }

    /// <summary>
    /// 베경 음악을 종료합니다.
    /// </summary>
    public void TriggerDeath()
    {
        bool isDemo01Scene = Manager.Get<GameManager>().CompareSceneName("Demo01");
        bool isDemo02Scene = Manager.Get<GameManager>().CompareSceneName("Demo02");

        //데모 01에서는 파라미터를 통해 사운드의 끝을 표현합니다.
        if (isDemo01Scene)
            BGMEmitter.SetParameter("Death", 0);

        //데모 02에서는 사운드를 페이드하여 정지합니다.
        if (isDemo02Scene)
            StopBGM(true);
    }

    #endregion
}