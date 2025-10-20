namespace SuperAdventureLand
{
    using System.Linq;
    using UnityEngine;

    public class PowerUpBlock : Block {

        public string powerupName;
        public bool singleUse = false;

        public override void ExecuteState() {
            switch (state) {
                case BlockState.HIT:
                    var PowerUpManager = FindFirstObjectByType<PowerUpManager>();
                    if (PowerUpManager != null) {
                        PowerUpManager.SetPowerUp(powerupName);
                    }
                    "FX_PowerUp".GetAsset<GameObject>(powerupPrefab => {
                        Instantiate(powerupPrefab, transform.position, Quaternion.identity);
                    }, error => {
                        Debug.LogError($"Failed to load powerup: {error}");
                    });
                    "powerup".PlaySFX(transform.position, 0.6f, Random.Range(0.8f, 1.2f));
                    if (singleUse) {
                        SetState(BlockState.DESTROY);
                    }
                    break;
                case BlockState.HITTING:
                    if (timeSinceStateChange > 0.4f) {
                        SetState(BlockState.IDLE);
                    }
                    break;
                case BlockState.ACTIVATE:
                    break;
                case BlockState.ACTIVATING:
                    SetState(BlockState.IDLE);
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }

        public override bool isActivated {
            get {
                return state == BlockState.HIT || state == BlockState.HITTING;
            }

        }
    }
}
