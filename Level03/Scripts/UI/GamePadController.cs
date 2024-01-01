using System;
using DebuggingEssentials;
using Doozy.Runtime.UIManager.Components;
using Doozy.Runtime.UIManager.Containers;
using Managers;
using ManagerX;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 게임 패드 모드라면, 화면에 특정 UI를 띄우고, 특정 UI에 포커스를 주는 역할을 수행
/// </summary>
public class GamePadController : MonoBehaviour
{
    public UIView UIView;
    public UIButton Button;

    public EventSystem EventSystem;

    private void Start()
    {
        IsGamepadObservable.Where(isGamepad => !isGamepad).Subscribe(_ =>
        {
            if (UIView)
                UIView.AutoSelectAfterShow = false;

            if (EventSystem)
                EventSystem.SetSelectedGameObject(null);
            
        }).AddTo(this);

        IsGamepadObservable.Where(isGamepad => isGamepad).Subscribe(_ =>
            {
                if (UIView)
                    UIView.AutoSelectAfterShow = true;

                if (Button)
                    Button.Select();
            })
            .AddTo(this);
    }

    private IObservable<bool> IsGamepadObservable => this.UpdateAsObservable()
        .ObserveEveryValueChanged(_ => AutoManager.Get<InputManager>().CurrentController == ControllerType.Gamepad);
}