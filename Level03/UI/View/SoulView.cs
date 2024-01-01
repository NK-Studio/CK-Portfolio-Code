using UnityEngine;
using UnityEngine.UI;

public class SoulView : MonoBehaviour
{
    [SerializeField] private Image _front;
    [SerializeField] private bool _invert = true;

    public void ChangeThreshold(float threshold)
    {
        if (_front)
            _front.fillAmount = _invert ? 1 - threshold : threshold;
        else
            DebugX.LogWarning("_front가 비어있습니다.");

    }
}