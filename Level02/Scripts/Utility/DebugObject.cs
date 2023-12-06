using UnityEngine;

namespace Utility
{
    public class DebugObject : MonoBehaviour
    {
        private static DebugObject _instance;
        public static DebugObject Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DebugObject>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null) 
                _instance = this;
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
    }
}
