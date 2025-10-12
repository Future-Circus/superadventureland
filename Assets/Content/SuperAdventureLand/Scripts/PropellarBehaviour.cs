namespace SuperAdventureLand.Scripts
{
    using System.Collections;
    using UnityEngine;

    public class PropellarBehaviour : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        GameObject propellar;
        bool propellarMoving = false;

        Vector3? startScale;
        float startTime;

        void Start()
        {
            propellar = this.gameObject;
        }

        void LateUpdate() {
            if (propellarMoving) {
                propellar.transform.localPosition += Vector3.up * 2f * Time.deltaTime;
                propellar.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 10) * 10);
                if (startScale != null) {
                    propellar.transform.localScale = Vector3.Lerp((Vector3)startScale, Vector3.zero, (Time.time - startTime)*2.5f);
                }
            }
        }

        IEnumerator ScalePropellar () {
            yield return new WaitForSeconds(0.4f);
            startScale = propellar.transform.localScale;
            startTime = Time.time;
        }

        public void DisconnectPropellar () {
            propellarMoving = true;
            propellar.transform.parent = null;
            propellar.GetComponent<Animator>().speed = 4;
            StartCoroutine(ScalePropellar());
            Destroy(propellar, 0.8f);
        }
    }
}
