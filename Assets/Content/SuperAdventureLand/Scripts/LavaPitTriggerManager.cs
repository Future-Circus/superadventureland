namespace SuperAdventureLand.Scripts
{
    using System.Collections.Generic;
    using UnityEngine;

    public class LavaPitTriggerManager : MonoBehaviour
    {
        private Vector3 startPosition;

        public void Enter() {
            startPosition = Camera.main.transform.position;
        }

        public void Stay() {

        }

        public void Exit() {
            Vector3 exitPosition = Camera.main.transform.position;
            Vector3 directionToExit = exitPosition - startPosition;
            float dotProduct = Vector3.Dot(directionToExit.normalized, transform.forward);

            if (dotProduct < 0) {
                Debug.Log("Exit position is on the opposite side of the lava pit.");
                SunManager.Instance?.OnEvent("lava-pit-exit");
                enabled = false;
            } else {
                Debug.Log("Exit position is not on the opposite side of the lava pit.");
            }
        }

    }
}
