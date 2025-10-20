namespace SuperAdventureLand
{
    using UnityEngine;

    public class BotBehaviour : MonoBehaviour
    {

        //look at target
        public Transform target;
        //look at speed
        public float lookAtSpeed = 1.0f;

        void Start()
        {
            //target main camera
            if (target == null)
                target = Camera.main.transform;
        }

        // Update is called once per frame
        void Update()
        {
            //look at target with lerp
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(target.position - transform.position), lookAtSpeed * Time.deltaTime);
        }
    }
}
