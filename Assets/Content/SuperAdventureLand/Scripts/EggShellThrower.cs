namespace SuperAdventureLand
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class EggShellThrower : MonoBehaviour
    {
        //egg shell1
        public GameObject eggShell1;
        //egg shell2
        public GameObject eggShell2;

        public void Start () {
            //add rigidbody to eggshell1
            Rigidbody rb = eggShell1.AddComponent<Rigidbody>();
            rb.angularDamping = 0.8f;
            rb.linearDamping = 0.8f;
            eggShell1.AddComponent<SphereCollider>();
            // add rigidbody to eggshell2
            Rigidbody rb2 = eggShell2.AddComponent<Rigidbody>();
            rb2.angularDamping = 0.8f;
            rb2.linearDamping = 0.8f;
            eggShell2.AddComponent<SphereCollider>();
            var f = 2;
            var r = 10;

            //apply force and torque to throw the egg shell randomly
            rb.AddForce(Random.Range(-f, f), Random.Range(f*3, f*4), Random.Range(-f, f), ForceMode.Impulse);
            rb.AddTorque(Random.Range(-r, r), Random.Range(-r, r), Random.Range(-r, r), ForceMode.Impulse);

            rb2.AddForce(Random.Range(-f, f), Random.Range(f*2, f*3), Random.Range(-f, f), ForceMode.Impulse);
            rb2.AddTorque(Random.Range(-r, r), Random.Range(-r, r), Random.Range(-r, r), ForceMode.Impulse);

            Destroy(eggShell1, 2);
            Destroy(eggShell2, 2);
        }
    }
}
