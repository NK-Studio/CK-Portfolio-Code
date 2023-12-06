using System;
using Animation;
using AutoManager;
using SaveLoadSystem;
using UnityEngine;

namespace Scenes.CutScene.Stage2
{
    public class Stage2PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private Transform[] SpawnPoint;

        public Transform Player;

        public GameObject[] FakeWalls;

        private Gate _gate;

        private void Awake()
        {
            _gate = FindObjectOfType<Gate>();
        }

        private void Start()
        {
            Vector3 candyInfo = Manager.Get<DataManager>().Load("Stage2Data", Vector3.zero);

            if (candyInfo == Vector3.zero)
            {
                Player.position = SpawnPoint[0].position;
            }
            else
            {
                FakeWalls[0].SetActive(false);
                Player.position = SpawnPoint[1].position;

                if (candyInfo.x > 0)
                {
                    FakeWalls[1].SetActive(false);
                    _gate.SetRedCandyKeyOnStand();
                }
                
                if (candyInfo.y > 0)
                {
                    _gate.SetYellowKeyCandyKeyOnStand();
                }
                
                if (candyInfo.z > 0)
                {
                    _gate.SetGreenCandyKeyOnStand();
                }
            }
        }

        /// <summary>
        /// 보스 BGM을 재생한다.
        /// </summary>
        public void OnTriggerBossMusic()
        {
            Manager.Get<AudioManager>().ChangeBGMWithPlay(Stage.Stage3);
        }
    }
};