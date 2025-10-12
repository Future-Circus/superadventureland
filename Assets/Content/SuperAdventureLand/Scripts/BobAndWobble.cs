namespace SuperAdventureLand.Scripts
{
    using System;
    using UnityEngine;
    using UnityEngine.Animations;

    public class BobAndWobble : MonoBehaviour
    {
        public static float offset;

        public float bobHeight = 0.5f; // Height of the bob
        public float bobSpeed = 2.0f;  // Speed of the bob

        public float wobbleAngle = 10.0f; // Angle of the wobble
        public float wobbleSpeed = 3.0f;  // Speed of the wobble

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float _offset;
        private Rigidbody rb;

        public Axis axis = Axis.Y;

        void Start()
        {
            initialPosition = transform.localPosition;
            initialRotation = transform.localRotation;
            offset += Mathf.Deg2Rad*60f;
            _offset = offset;
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            Bob();
            if (wobbleAngle > 0 && wobbleSpeed > 0)
                Wobble();
        }

        void Bob()
        {
            if (axis == Axis.X) {
                float newY = initialPosition.x + Mathf.Sin(_offset+Time.time * bobSpeed) * bobHeight;
                SetPosition(new Vector3(newY, initialPosition.y, initialPosition.z));
            } else if (axis == Axis.Y) {
                float newY = initialPosition.y + Mathf.Sin(_offset+Time.time * bobSpeed) * bobHeight;
                SetPosition(new Vector3(initialPosition.x, newY, initialPosition.z));
            } else if (axis == Axis.Z) {
                float newY = initialPosition.z + Mathf.Sin(_offset+Time.time * bobSpeed) * bobHeight;
                SetPosition(new Vector3(initialPosition.x, initialPosition.y, newY));
            }
        }

        void Wobble()
        {
            float wobbleX = Mathf.Sin(_offset+Time.time * wobbleSpeed) * wobbleAngle;
            float wobbleZ = Mathf.Cos(_offset+Time.time * wobbleSpeed) * wobbleAngle;
            Quaternion wobbleRotation = Quaternion.Euler(wobbleX, initialRotation.eulerAngles.y, wobbleZ);
            SetRotation(initialRotation * wobbleRotation);
        }
        public void SetPosition(Vector3 position) {
            transform.localPosition = position;
        }
        public void SetRotation(Quaternion rotation) {
           transform.localRotation = rotation;
        }
    }
}
