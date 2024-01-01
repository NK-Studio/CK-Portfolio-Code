using Character.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class ImageChanger : MonoBehaviour
{
    private PlayerModel _playerModel;
    public Image CurrentImage;
    public Sprite OnImage;
    public Sprite OffImage;
    void Start()
    {
        _playerModel = FindObjectOfType<PlayerModel>();
        _playerModel.CurrentBattleAreaObservable.Subscribe(newArea =>
        {
            if (newArea != null)
            {
                ChangeOnImage();
            }
            else
            {
                ChangeOffImage();
            }
        }).AddTo(this);
        //CurrentImage = GetComponent<Image>();
    }

    public void ChangeOnImage()
    {
        CurrentImage.sprite = OnImage;
    }

    public void ChangeOffImage()
    {
        CurrentImage.sprite = OffImage;
    }
}
