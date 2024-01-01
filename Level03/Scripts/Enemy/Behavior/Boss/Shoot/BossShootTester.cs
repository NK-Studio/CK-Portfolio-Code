using System.Collections.Generic;
using Character.Presenter;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Enemy.Behavior.Boss
{
    public class BossShootTester : MonoBehaviour
    {
        public List<BossShootSettings> SettingsList = new();
        public BossShootSettings Settings;
        
        private BossAquus _boss;
        private PlayerPresenter _player;
        private int _index = -1;
        
        private void Start()
        {
            UpdateSettingsByIndex();
            _boss = FindAnyObjectByType<BossAquus>();
            _player = FindAnyObjectByType<PlayerPresenter>();
        }

        private void UpdateSettingsByIndex()
        {
            if(SettingsList.Count <= 0) return;
            _index = (_index + 1) % SettingsList.Count;
            Settings = SettingsList[_index];
        }
        
        private void Update()
        {
            if (Keyboard.current[Key.R].wasPressedThisFrame)
            {
                UpdateSettingsByIndex();
            }
            if (Keyboard.current[Key.T].wasPressedThisFrame)
            {
                Settings.Shoot(_boss, transform.position + Vector3.up, _player.transform, destroyCancellationToken);
            }
        }

        private void OnDrawGizmos()
        {
            if(!Settings) return;
            Settings.OnDrawGizmoDebug(transform);

        }

        private void OnGUI()
        {
            if(!Settings) return;
            Settings.OnGUIDebug(transform);
        }
    }
}