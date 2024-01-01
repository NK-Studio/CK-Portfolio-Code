namespace Enemy.UI
{
    public class InteractionUIPool : GameObjectPool<InteractionUI>
    {
        public static InteractionUIPool Instance { get; private set; }
        
        private void Start()
        {
            if (Instance)
            {
                Destroy(Instance);
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}