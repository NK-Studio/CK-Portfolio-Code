using System.Collections.Generic;
using UITweenAnimation;
using UnityEngine;
using UnityEngine.InputSystem;

public class BossTutorialManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> _images;

    private int _imageCount;
    private int _imageIndex;

    private void OnEnable()
    {
        _imageCount = _images.Count;
        _imageIndex = 0;
        UpdateImage();
        Time.timeScale = 0f;
    }

    private void UpdateImage()
    {
        // 현재 index의 이미지만 키고, 나머지는 끔
        for (int i = 0; i < _imageCount; i++)
        {
            _images[i].SetActive(i == _imageIndex);
        }
    }

    private void Update()
    {
        // ESC 누른 중에는 스킵 안 됨
        // 이미 다 지나간 경우..?는 안 됨
        if (UIController.Instance.IsPause || _imageIndex >= _imageCount)
        {
            return;
        }

        // 아무튼 살아있는 동안에는 강제로 0으로 만듬
        // TODO 맞나?
        Time.timeScale = 0f;
        // Space 또는 → 누르면 다음 이미지로
        if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            NextImage();
        // ← 누르면 이전 이미지로
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame) 
            PreviousImage();
    }

    private void PreviousImage()
    {
        if (_imageIndex <= 0) return;
        --_imageIndex;
        UpdateImage();
    }

    private void NextImage()
    {
        // 다음 이미지로 넘김
        ++_imageIndex;
        // 만약 전부 본 경우, 보스 튜토리얼 끝
        if (_imageIndex >= _imageCount)
        {
            gameObject.SetActive(false);
            return;
        }

        UpdateImage();
    }

    private void OnDisable()
    {
        // 사라질 때 만약 일시정지 중이 아니면 Scale 1로 만들기
        if (!UIController.Instance.IsPause)
        {
            Time.timeScale = 1f;
        }
    }
}