namespace SuperAdventureLand.Scripts
{
    using System;
    using UnityEngine;

    public class RandomSwing : MonoBehaviour
    {
        public float speed = 1.0f;
        public float amplitude = 1.0f;
        public float offset = 0.0f;

        // Update is called once per frame

        private Vector3 startRot;

        void Start () {
            startRot = transform.eulerAngles;
        }

        void Update()
        {
            float x = Mathf.Sin(Time.time * speed + offset) * amplitude;
            float z = Mathf.Cos(Time.time * speed + offset) * amplitude;

            Vector3 targetEulerAngles = startRot + new Vector3(x, 0, z);

            transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, targetEulerAngles, Time.deltaTime * speed);
        }
        public void Pause()
        {
            enabled = false;
        }
        public void Resume(Vector3 prevRot)
        {
            enabled = true;
            startRot = prevRot;
        }
    }
}
