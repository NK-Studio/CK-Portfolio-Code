using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class BuildInformationUI : MonoBehaviour
{

    private TMP_Text _text;

    private void Start()
    {
        _text = GetComponent<TMP_Text>();

        _text.text = GetBuildInformation();
    }

    private string GetBuildInformation()
    {
#if UNITY_EDITOR
        return $"{Application.version} - Unity Editor";
#else
        return $"{Application.version} - #{BuildNumber.BuildNumberValue}";
#endif
    }
}
