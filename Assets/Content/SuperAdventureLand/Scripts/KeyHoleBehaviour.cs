namespace SuperAdventureLand.Scripts
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.Events;

    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    public class KeyHoleBehaviour : MonoBehaviour
    {
        public Transform scaleAnchor;
        public Transform coinSpawnPoint;
        public ParticleSystem openEffect;
        public ParticleSystem closeEffect;

        private enum KeyHoleState {
            LOCKED,
            UNLOCKED,
            GROW,
            REWARD,
            COMPLETE,
            DONE
        }

        private KeyHoleState state = KeyHoleState.LOCKED;

        private int rewardCount = 10;
        private bool canSpawn = true;
        private bool playunlock = false;

        public void Unlock()
        {
            openEffect.Play();
            state = KeyHoleState.UNLOCKED;
        }

        public void Update()
        {
            //do an animation where keyhole scales along the z axis from 1 to 10
            if (state == KeyHoleState.UNLOCKED)
            {
                if (!playunlock) {
                    "keyunlock".PlaySFX(transform.position);
                    playunlock = true;
                }
                scaleAnchor.transform.localScale = Vector3.MoveTowards(scaleAnchor.transform.localScale, new Vector3(1, 1, 150), Time.deltaTime*60);
                if (scaleAnchor.transform.localScale.z >= 149f) {
                    state = KeyHoleState.GROW;
                }
            } else if (state == KeyHoleState.GROW)
            {
                scaleAnchor.transform.localScale = Vector3.MoveTowards(scaleAnchor.transform.localScale, new Vector3(2, 2, 150), Time.deltaTime*10);
                if (scaleAnchor.transform.localScale.x >= 1.95f) {
                    state = KeyHoleState.REWARD;
                }
            } else if (state == KeyHoleState.REWARD)
            {
                if (!canSpawn)
                    return;
                SpawnCoin();
                Invoke("SpawnReady",0.1f);
                canSpawn = false;
                rewardCount--;
                if (rewardCount <= 0) {
                    state = KeyHoleState.COMPLETE;
                }
            } else if (state == KeyHoleState.COMPLETE)
            {
                scaleAnchor.transform.localScale = Vector3.MoveTowards(scaleAnchor.transform.localScale, Vector3.zero, Time.deltaTime*140);
                if (scaleAnchor.transform.localScale.x <= 0.01f) {
                    state = KeyHoleState.DONE;
                    SunManager.Instance?.OnEvent("key-hole-completed");
                }
            } else if (state == KeyHoleState.DONE)
            {

            }
        }

        public void SpawnCoin()
        {
            "coin".PlaySFX(transform.position);

            "E_COIN".GetAsset<GameObject>(coinPrefab => {
                GameObject coin = Instantiate(coinPrefab, coinSpawnPoint.position, Quaternion.identity, transform.FindRoot());
                coin.GetComponent<Coin>().dp_isStatic = false;

                float baseForceMultiplier = 2.5f;
                float minY = 1f;

                Vector3 direction = coinSpawnPoint.forward;
                Vector3 xzForce = direction * baseForceMultiplier;
                Vector3 calculatedForce =  xzForce + Vector3.up * minY;

                coin.GetComponent<Coin>().PopUpItem(calculatedForce);
            }, error => {
                Debug.LogError($"Failed to load coin: {error}");
            });
        }

        public void SpawnReady () {
            canSpawn = true;
        }

    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(KeyHoleBehaviour), true)]
    public class KeyHoleBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();  // Draws the default inspector

            KeyHoleBehaviour pengoBehaviour = (KeyHoleBehaviour)target;

            if (GUILayout.Button("Unlock"))
            {
                pengoBehaviour.Unlock();
            }
        }
    }
    #endif
}
