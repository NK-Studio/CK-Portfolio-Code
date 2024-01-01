using System;
using System.Collections.Generic;
using Character.Presenter;
using Damage;
using Micosmo.SensorToolkit;
using RayFire;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Enemy.Behavior.Boss
{
    public class BossRoomGround : MonoBehaviour
    {
        [Serializable]
        public struct GroundSettings
        {
            public List<NavMeshObstacle> Obstacles;
            public List<RayfireRigid> Rigids;
            public List<RangeSensor> PlayerKillRanges;
            public List<GameObject> ObjectsToEnable;
            public List<GameObject> ObjectsToDisable;
            public UnityEvent Events;
            public float Wait;

            public void Execute(out float wait)
            {
                Events?.Invoke();
                foreach (var obj in ObjectsToEnable)
                    if(obj) obj.SetActive(true);
                foreach (var obj in ObjectsToDisable)
                    if(obj) obj.SetActive(false);
                
                wait = Wait;
                // Rigid 활성화 (무너짐)
                foreach (var rigid in Rigids)
                {
                    if(!rigid) continue;
                    Debug.Log($"ExplodeCurrentAndNext() - rigid {rigid.name} start");
                    try
                    {
                        rigid.Initialize();
                        rigid.Fade();
                        if (rigid.TryGetComponent(out RayfireBomb bomb))
                        {
                            Debug.Log($"RayfireBomb {bomb.name} Explode", bomb);
                            bomb.Explode(0f);
                        }

                        if (rigid.objectType == ObjectType.MeshRoot)
                        {
                            foreach (var fragment in rigid.fragments)
                            {
                                fragment.gameObject.tag = "FallingGround";
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                        continue;
                    }

                    Debug.Log($"ExplodeCurrentAndNext() - rigid {rigid.name} end");
                }

                Debug.Log("ExplodeCurrentAndNext() Rigid End");
                foreach (var obstacle in Obstacles)
                {
                    if(!obstacle) continue;
                    // 장애물 활성화 (못 감)
                    obstacle.gameObject.SetActive(true);
                }

                Debug.Log("ExplodeCurrentAndNext() Obstacle End");
                // 범위 내 플레이어 즉사
                foreach (var range in PlayerKillRanges)
                {
                    if(!range) continue;
                    range.Pulse();
                    foreach (var obj in range.Detections)
                    {
                        if (obj.CompareTag("Player") && obj.TryGetComponent(out PlayerPresenter player))
                        {
                            player.Health = 0f;
                        }else if (obj.CompareTag("Enemy") && obj.TryGetComponent(out Monster m))
                        {
                            if (m.IsFreeze)
                            {
                                m.OnFreezeBreak();
                            }
                            else
                            {
                                m.Damage(EnemyDamageInfo.Get(m.Health, range.gameObject));
                            }
                        }
                    }
                }

                Debug.Log("ExplodeCurrentAndNext() PlayerKillRanges End");
            }
        }

        public GroundSettings DeadlyStrikeSettings;

        public void ExecuteDeadlyStrike()
        {
            DeadlyStrikeSettings.Execute(out _);
        }
        
        public List<GroundSettings> Rigids;

        public int Index = 0;

        public bool ExplodeCurrentAndNext(out float wait)
        {
            if (Index >= Rigids.Count)
            {
                wait = 0;
                return false;
            }

            Debug.Log("ExplodeCurrentAndNext()");
            var settings = Rigids[Index];
            settings.Execute(out wait);
            
            ++Index;

            Debug.Log("ExplodeCurrentAndNext() return true");
            return true;
        }
    }
}