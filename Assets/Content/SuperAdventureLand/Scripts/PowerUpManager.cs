namespace SuperAdventureLand.Scripts
{
    using System.Linq;
    using UnityEngine;
    public class PowerUpManager : MonoBehaviour
    {
        public SA_PowerUp[] powerups;
        public void SetPowerUp(string powerupIdentifier)
        {
            var powerup = powerups.FirstOrDefault(p => p.powerupConfig.powerupId == powerupIdentifier);
            if (powerup == null) {
                powerup = powerups.FirstOrDefault(p => p.powerupConfig.powerupName == powerupIdentifier);
            }
            if (powerup != null) {
                SetPowerUp(powerup);
            } else {
                Debug.LogError($"PowerUpManager: PowerUp {powerupIdentifier} not found");
            }
        }

        public void SetPowerUp(SA_PowerUp powerup)
        {
            foreach (var p in powerups) {
                if (p != powerup && p.isActive) {
                    p.SetState(SA_PowerUpState.DEACTIVATE);
                }
            }
            if (powerup != null) {
                powerup.SetState(SA_PowerUpState.ACTIVATE);
            }
        }
    }
}
