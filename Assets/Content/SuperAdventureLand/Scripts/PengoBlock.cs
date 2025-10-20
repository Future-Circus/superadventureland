namespace SuperAdventureLand
{
    using System;
    using System.Collections;
    using UnityEngine;

    public class PengoBlock : MonoBehaviour
    {
        public GameObject spawnPrefab;
        public Transform SpawnPoint;
        public bool autoSpawn = true;

        void Awake () {
            spawnPrefab.SetActive(false);
        }

        void Start () {
            if (autoSpawn) {
                Spawn();
            }
        }

        public void Spawn () {

            GameObject spawned = Instantiate(spawnPrefab, SpawnPoint.position, SpawnPoint.rotation, transform.FindRoot());
            spawned.SetActive(true);
        }

        public IEnumerator SpawnSignal (float delay) {
            yield return new WaitForSeconds(delay);
            float startTime = Time.time;
            float duration = 1f;
            while (Time.time-startTime < duration) {
                var t = (Time.time - startTime) / duration;
                transform.localScale = Vector3.one + Vector3.one * (0.4f * t);
                transform.localRotation = Quaternion.Euler(Mathf.Sin(t*10)*(t*10), Mathf.Cos(t*10)*(t*10), Mathf.Sin(-t*10)*(t*10));
                yield return null;
            }
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }

        public void SpawnDelayed (float delay) {
            StartCoroutine(SpawnSignal(delay-1f));
            Invoke("Spawn", delay);
        }
    }
}
