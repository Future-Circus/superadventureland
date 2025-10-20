namespace SuperAdventureLand
{
    using System.Collections;
    using UnityEngine;

    public class KeyItem : Coin
    {
        private bool isCollected = false;
        private bool isActivated = false;
        private bool isInLock = false;
        public AudioClip collectSfx;

        private GameObject KeyHole;

        public AudioClip unlockSfx;

        private Transform attachmentPoint;

        // public override void CollectCoin()
        // {
        //     GetComponent<AudioSource>().PlayOneShot(collectSfx);
        //     Instantiate(coinParticle, transform.position, Quaternion.identity);
        //     isCollected = true;
        //     GetComponent<Collider>().enabled = false;
        //     if (TryGetComponent<Rigidbody>(out Rigidbody rb)) {
        //         rb.isKinematic = true;
        //         rb.useGravity = false;
        //         rb.constraints = RigidbodyConstraints.None;
        //     }
        //     //attachmentPoint = other.transform;

        //     transform.SetParent(attachmentPoint,true);
        //     transform.localPosition = new Vector3(-2.77f,0f,0f);
        //     transform.localRotation = Quaternion.identity;
        // }

        public override void Update()
        {
            if (KeyHole == null) {
                KeyHole = GameObject.Find("KeyHoleSlot");
            }

            if (isActivated) {

                if (isInLock) {
                    return;
                }

                transform.position = Vector3.MoveTowards(transform.position, KeyHole.transform.position, Time.deltaTime*3f);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, KeyHole.transform.rotation, Time.deltaTime*200f);

                if (transform.position.Distance(KeyHole.transform.position) <= 0.1f && Quaternion.Angle(transform.rotation, KeyHole.transform.rotation) <= 0.1f) {
                    if (!isInLock) {
                        transform.SetParent(null,true);
                        transform.rotation = KeyHole.transform.rotation;
                        StartCoroutine(Turn());
                        isInLock = true;
                    }
                }

            } else if (isCollected) {
                //slow follow camera.main with y offset of 0.5 down
                // Vector3 targetPosition = Camera.main.transform.position + Camera.main.transform.forward * -1f;
                // targetPosition.y -= 1f;
                // transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime);
                // CoinSpin();

                if (KeyHole && transform.position.Distance(KeyHole.transform.position) <= 1f) {
                    isActivated = true;
                }

            } else {
                base.Update();
            }
        }

        private IEnumerator Turn() {
            yield return new WaitForSeconds(0.5f);

            if (unlockSfx) {
                GetComponent<AudioSource>().PlayOneShot(unlockSfx);
            }
            //lerp so key turns 90 degrees around its forward axis over 1 second
            float time = 0;
            while (time < 1f) {
                time += Time.deltaTime*2;
                transform.rotation = Quaternion.Lerp(transform.rotation, KeyHole.transform.rotation * Quaternion.Euler(90, 0, 0), time);
                yield return null;
            }

            yield return new WaitForSeconds(0.2f);

            "FX_SteamPuff".SpawnAsset(transform.position + KeyHole.transform.right * 0.3f, Quaternion.LookRotation(transform.position + KeyHole.transform.right * 0.3f - Camera.main.transform.position));

            transform.GetChild(0).gameObject.SetActive(false);

            yield return new WaitForSeconds(0.5f);

            KeyHole.GetComponentInParent<KeyHoleBehaviour>().Unlock();

            yield return new WaitForSeconds(1f);

            Destroy(gameObject);
        }
    }
}
