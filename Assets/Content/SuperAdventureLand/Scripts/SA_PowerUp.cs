namespace SuperAdventureLand.Scripts
{
    using UnityEngine;

    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(SA_PowerUp),true)]
    public class SA_PowerUpEditor : StandardEntityEditor<SA_PowerUpState>
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
    #endif
    public enum SA_PowerUpState {
        START,
        IDLE,
        IDLING,
        ACTIVATE,
        ACTIVATING,
        WAIT,
        WAITING,
        USE,
        USING,
        DEACTIVATE,
        DEACTIVATING,
        KEY_ANIM,
        KEY_ANIMING
    }

    [System.Serializable]
    public class PowerUpConfig
    {
        public string powerupId;
        public string powerupName;
        public float powerupDuration = 60f;
        public float powerupCooldown = 0.4f;
    }

    public class SA_PowerUp : StandardEntity<SA_PowerUpState>
    {
        [SerializeField]
        public PowerUpConfig powerupConfig;

        private float startTime = 0f;

        public override void ExecuteState()
        {
            switch (state)
            {
                case SA_PowerUpState.START:
                    gameObject.HideVisual();
                    if (mainCollider) {
                        mainCollider.enabled = false;
                    }
                    break;
                case SA_PowerUpState.IDLE:
                    break;
                case SA_PowerUpState.IDLING:
                    break;
                case SA_PowerUpState.ACTIVATE:
                    gameObject.UnHideVisual();
                    if (mainCollider) {
                        mainCollider.enabled = true;
                    }
                    startTime = Time.time;
                    break;
                case SA_PowerUpState.ACTIVATING:
                    SetState(SA_PowerUpState.WAIT);
                    break;
                case SA_PowerUpState.WAIT:
                    break;
                case SA_PowerUpState.WAITING:
                    if (Time.time - startTime > powerupConfig.powerupDuration)
                    {
                        SetState(SA_PowerUpState.DEACTIVATE);
                    }
                    if (PLAYER_INPUT) {
                        SetState(SA_PowerUpState.USE);
                    }
                    break;
                case SA_PowerUpState.USE:
                    //something happen here
                    break;
                case SA_PowerUpState.USING:
                    if (timeSinceStateChange > powerupConfig.powerupCooldown) {
                        SetState(SA_PowerUpState.WAIT);
                    }
                    break;
                case SA_PowerUpState.DEACTIVATE:
                    gameObject.HideVisual();
                    if (mainCollider) {
                        mainCollider.enabled = false;
                    }
                    SetState(SA_PowerUpState.IDLE);
                    break;
                case SA_PowerUpState.DEACTIVATING:
                    break;
            }
        }

        public virtual bool PLAYER_INPUT {
            get {
                return true;
            }
        }

        public virtual bool isActive {
            get {
                return  state != SA_PowerUpState.IDLE && state != SA_PowerUpState.IDLING && state != SA_PowerUpState.START && state != SA_PowerUpState.DEACTIVATE && state != SA_PowerUpState.DEACTIVATING;
            }
        }
    }
}
