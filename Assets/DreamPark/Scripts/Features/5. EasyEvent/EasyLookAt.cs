namespace DreamPark.Easy
{
    #if UNITY_EDITOR
    using UnityEditor;
    #endif
    using UnityEngine;

    public class EasyLookAt : EasyEvent
    {
        public Transform target;
        public Vector3 rotationOffset = Vector3.zero;
        public float speed = -1f;
        public bool rotateX = true;
        public bool rotateY = true;
        public bool rotateZ = true;
        public void Update() {
            if (!isEnabled) {
                return;
            }
            if (target == null) {
                target = Camera.main.transform;
            }
            Vector3 targetPosition = target.position;
            if (target != null) {
                if (rotateX && rotateY && rotateZ && speed <= 0f && rotationOffset == Vector3.zero) {
                    transform.LookAt(targetPosition);
                } else {
                    Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position) * Quaternion.Euler(rotationOffset);
                    if (speed > 0f) {
                        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, speed * Time.deltaTime);
                    } else {
                        transform.rotation = targetRotation;
                    }
                }
            }
        }
    }

}
