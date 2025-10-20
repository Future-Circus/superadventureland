namespace SuperAdventureLand
{
    using System.Collections.Generic;
    using UnityEngine;

    public class SpawnCrib : MonoBehaviour
    {
        Dictionary<GameObject,GameObject> children;

        float spawnIterationDelay = 1f;
        float startTime = 0f;

        void Start () {
            children = new Dictionary<GameObject, GameObject>();
            foreach (Transform child in transform) {
                children.Add(child.gameObject, null);
                child.gameObject.SetActive(false);
            }
        }

        public void Spawn () {
            //iterate and find first missing child
            foreach (var child in children) {
                if (child.Value == null || child.Value.IsDestroyed()) {
                    if (Vector3.Distance(Camera.main.transform.position, child.Key.transform.position) < 1f)
                        continue;

                    var ogObj = child.Key;
                    children[child.Key] = Instantiate(ogObj, ogObj.transform.position, ogObj.transform.rotation, transform);
                    children[child.Key].SetActive(true);
                    break;
                }
            }
        }

        public void Update () {
            if (children.Count == 0)
                return;

            if (Time.time - startTime >= spawnIterationDelay) {
                Spawn();
                startTime = Time.time;
            }
        }
    }
}
