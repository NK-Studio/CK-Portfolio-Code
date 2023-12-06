using System;
using AutoManager;
using Character.Controllers;
using Character.Core;
using FMODUnity;
using Managers;
using UnityEngine;
using Utility;


//이 스크립트는 캐릭터 이동 속도 및 이벤트에 따라 발자국, 점프 및 착지 오디오 클립과 같은 오디오 신호를 처리하고 재생합니다. 
public class AudioControl : MonoBehaviour
{
    private PlayerController _controller;
    private Mover _mover;
    private Transform _tr;

    //이동 거리가 이 값에 도달할 때마다 발자국이 재생됩니다('useAnimationBasedFootsteps'가 'true'로 설정된 경우).
    public float FootstepDistance = 0.2f;
    private float _currentFootstepDistance;
    private EventReference _footStep;

    private bool _isCutSceneMode = false;

    //Setup;
    private void Awake()
    {
        //Get component references;
        _controller = GetComponent<PlayerController>();
        _mover = GetComponent<Mover>();
        _tr = transform;
    }

    private void Start()
    {
        _footStep = Manager.Get<GameManager>().characterSettings.SFXClips[0];
    }

    //Update;
    private void Update()
    {
        //컨트롤러 속도 가져오기
        Vector3 velocity = _controller.GetVelocity();

        //수평 속도를 계산합니다.
        Vector3 horizontalVelocity = VectorMath.RemoveDotVector(velocity, _tr.up);

        FootStepUpdate(horizontalVelocity.magnitude);
    }

    private void FootStepUpdate(float movementSpeed)
    {
        const float speedThreshold = 0.05f;
        if (!_isCutSceneMode)
        {
            _currentFootstepDistance += Time.deltaTime * movementSpeed;

            //특정 거리를 이동한 경우 발걸음 오디오 클립을 재생합니다.
            if (_currentFootstepDistance > FootstepDistance)
            {
                //무버가 땅에 닿고 있꼬, 이동 속도가 임계값보다 높은 경우에만 발자국 소리를 재생합니다.
                if (_mover.IsGrounded() && movementSpeed > speedThreshold)
                    PlayFootstepSound();

                _currentFootstepDistance = 0f;
            }
        }
        //컷씬이 되면 자동 이동 시스템
        else
        {
            _currentFootstepDistance += Time.deltaTime * 9;

            //특정 거리를 이동한 경우 발걸음 오디오 클립을 재생합니다.
            if (_currentFootstepDistance > FootstepDistance)
            {
                //무버가 땅에 닿고 있꼬, 이동 속도가 임계값보다 높은 경우에만 발자국 소리를 재생합니다.
                if (_mover.IsGrounded())
                    PlayFootstepSound();   
                
                _currentFootstepDistance = 0f;
            }
        }
    }

    private void PlayFootstepSound()
    {
        Manager.Get<AudioManager>().PlayOneShot(_footStep, transform.position);
    }

    public void SetActiveCutScene(bool active)
    {
        if (active) 
            _currentFootstepDistance = FootstepDistance;
        
        _isCutSceneMode = active;
    }
}