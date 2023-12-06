using System;
using Character.USystem.Hook.Model;
using Character.USystem.Hook.View;
using Settings;
using Sirenix.OdinInspector;
using UITweenAnimation;
using UnityEngine;
using Zenject;

namespace Installer
{
    public class GameModeInstaller : MonoInstaller
    {
        [SerializeField, ValidateInput("@offScreenManager != null", "OFF Screen 매니저를 연결해야합니다.")]
        private OffScreenSystemManager offScreenManager;

        [SerializeField, ValidateInput("@crossHair != null", "크로스 헤어 UI를 연결해야합니다.")]
        private GameObject crossHair;

        [SerializeField, ValidateInput("@DeadUI != null", "사망 UI 연결해야합니다.")]
        private UIView DeadUI;

        [SerializeField, ValidateInput("@hookSystemView != null", "젤리 핸드 연결해야합니다.")]
        private HookSystemView hookSystemView;

        [SerializeField, ValidateInput("@hookSystemModel != null", "젤리 핸드 연결해야합니다.")]
        private HookSystemModel hookSystemModel;

        public override void InstallBindings()
        {
            Container.Bind<OffScreenSystemManager>().FromInstance(offScreenManager);
            Container.Bind<GameObject>().WithId("CROSS-HAIR").FromInstance(crossHair);
            Container.Bind<HookSystemView>().FromInstance(hookSystemView);
            Container.Bind<HookSystemModel>().FromInstance(hookSystemModel);
            Container.Bind<UIView>().WithId("DeadUI").FromInstance(DeadUI);
        }

        [Button("Auto Binding", ButtonSizes.Large)]
        private void AutoBinding()
        {
            if (GameObject.Find("Jelly Hand").TryGetComponent(out HookSystemView hookView))
                hookSystemView = hookView;

            if (GameObject.Find("Jelly Hand").TryGetComponent(out HookSystemModel hookModel))
                hookSystemModel = hookModel;

            crossHair = GameObject.Find("CrossHair");
            
            if (GameObject.Find("View-DeadUI").TryGetComponent(out UIView deadUI))
                DeadUI = deadUI;

            if (GameObject.Find("OFFScreenManager").TryGetComponent(out OffScreenSystemManager offScreenSystemManager))
                offScreenManager = offScreenSystemManager;
        }
    }
}