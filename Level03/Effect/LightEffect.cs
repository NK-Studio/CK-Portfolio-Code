using UnityEngine;

public class LightEffect : MonoBehaviour
{
    [Tooltip("최소 밝기")]
    public float MinIntensity = 0.5f;
    
    [Tooltip("최대 발기")]
    public float MaxIntensity = 2.0f;
    
    [Tooltip("깜박이는 속도")]
    public float BlinkSpeed = 1.0f;
    
    private Light _light;

    private void Start()
    {
        _light = GetComponent<Light>();    
    }
    
    private void Update()
    {
        // Mathf.PingPong 함수는 입력값을 0과 두 번째 파라미터 사이로 반복시킵니다. 
        // 이렇게 함으로써 인텐시티가 최소값과 최대값 사이를 주기적으로 왔다갔다하게 됩니다.
        float intensity = Mathf.PingPong(Time.time * BlinkSpeed, MaxIntensity - MinIntensity) + MinIntensity;
        _light.intensity = intensity;
    }
}
