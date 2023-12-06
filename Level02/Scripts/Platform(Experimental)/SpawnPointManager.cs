using System;
using AutoManager;
using Character.Controllers;
using Character.View;
using Managers;
using Platform_Experimental_;
using Sirenix.OdinInspector;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    [Title("옵션"), Tooltip("데미지를 받지 않습니다.")]
    public bool IsAttackPlayerAfterDontDamage;

    [Title("세이브 포인트"), ReadOnly, SerializeField]
    private int index = -1;

    [SerializeField] private SavePoint[] worldSavePoints;

    [Title("옵션")]
    [SerializeField] private bool UseWaterSlashEffect;

    [ShowIf("@UseWaterSlashEffect")]
    public float SpawnY;

    private void Awake()
    {
        index = -1;
    }

    public int GetIndex()
    {
        return index;
    }
    
    public void SetIndex(int index)
    {
        this.index = index;
    }

    [Button(ButtonSizes.Large), PropertySpace(20), ContextMenu("AutoBind")]
    private void AutoBind()
    {
        worldSavePoints = gameObject.GetComponentsInChildren<SavePoint>();
    }

    public void ReSpawnPlayer()
    {
        TakeAttackPlayer();
        Respawn(true);
    }

    private void TakeAttackPlayer()
    {
        if (!IsAttackPlayerAfterDontDamage)
            Manager.Get<GameManager>().HP -= 1;
    }

    public void Respawn(bool applyEffect = false)
    {
        PlayerController playerController = FindObjectOfType<PlayerController>();
        PlayerView playerView = FindObjectOfType<PlayerView>();

        foreach (SavePoint savePoint in worldSavePoints)
        {
            if (savePoint.Index == index)
            {
                Transform spawnTransform = savePoint.GetSpawnPoint();

                if (applyEffect)
                    if (UseWaterSlashEffect)
                        playerView.OnWatterSlash(SpawnY, false);


                playerController.SetPosition(spawnTransform.position);
            }
        }
    }
}