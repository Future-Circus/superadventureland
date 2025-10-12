namespace SuperAdventureLand.Scripts
{
    using System;
    using UnityEngine;
    using Random = UnityEngine.Random;

    public class SpawnFlyingPengo : MonoBehaviour
    {
        public float speed = 0.1f;
        private MovingPlatformBehaviour movingPlatformBehaviour;

        private Vector3 destination;
        private float startTime = -1;
        private bool isFlying = true;
        private bool isFlyingBack = false;
        private Vector3 orgPos;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            movingPlatformBehaviour = GetComponent<MovingPlatformBehaviour>();
            GameObject worldOffset = GameObject.Find("WorldOffset");
            if (worldOffset) {
                destination = worldOffset.transform.TransformPoint(movingPlatformBehaviour.waypoints[0]);
            } else {
                destination = movingPlatformBehaviour.waypoints[0];
            }
            movingPlatformBehaviour.enabled = false;
        }

        void Start () {
            orgPos = transform.position;
            speed = Random.Range(speed*0.8f,speed*1.2f);
        }

        // Update is called once per frame
        void Update()
        {
            if (!isFlying)
            {
                return;
            }
            if (startTime < 0)
            {
                startTime = Time.time;
            }
            float t = (Time.time-startTime) * speed;
            float eT = EaseInOut(t);
            Vector3 newPos = Vector3.Lerp(orgPos, destination, eT);
            transform.position = newPos + new Vector3(0,Mathf.Sin(eT*Mathf.PI)*10f,0);
            if (t >= 1)
            {
                isFlying = false;
                if (!isFlyingBack) {
                    movingPlatformBehaviour.enabled = true;
                } else {
                    Destroy(gameObject);
                }
            }
        }

        float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }

        public void FlyAway () {
            speed = 0.1f;
            startTime = Time.time;
            isFlying = true;
            isFlyingBack = true;
            movingPlatformBehaviour.enabled = false;
            destination = orgPos;
            orgPos = transform.position;
        }

        public void DisableFlying () {
            isFlying = false;
            movingPlatformBehaviour.enabled = false;
        }
    }
}
