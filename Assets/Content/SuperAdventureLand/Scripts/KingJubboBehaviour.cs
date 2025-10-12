namespace SuperAdventureLand.Scripts
{
    using System;
    using System.Collections;
    using UnityEngine;

    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(KingJubboBehaviour))]
    public class KingJubboBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            KingJubboBehaviour kingJubbo = (KingJubboBehaviour)target;
            if (GUILayout.Button("Hit"))
            {
                if (kingJubbo.currentState != KingJubboBehaviour.KingJubboStates.HIT && kingJubbo.currentState != KingJubboBehaviour.KingJubboStates.HITTED && kingJubbo.currentState != KingJubboBehaviour.KingJubboStates.KILL && kingJubbo.currentState != KingJubboBehaviour.KingJubboStates.KILLING)
                    kingJubbo.currentState = KingJubboBehaviour.KingJubboStates.HIT;
            }
        }
    }
    #endif

    public class KingJubboBehaviour : MonoBehaviour
    {
        public Animator animator;
        public Transform muzzle;
        public GameObject projectilePrefab;
        public ParticleSystem spitParticle;
        public ParticleSystem hitParticle;
        public ParticleSystem deathParticle;
        public AudioClip victoryMusic;
        public AudioSource victoryMusicSwap;
        public AudioSource injuredAudioSource;
        public AudioSource defeatAudioSource;
        public GameObject pear;

        private int hp = 3;

        public Transform rigT;
        private Vector3 rigOffset;

        private Vector3 ogRot;

       public enum KingJubboStates {
            IDLE,
            IDLING,
            ATTACK,
            ATTACKING,
            HIT,
            HITTED,
            KILL,
            KILLING
        }
        public KingJubboStates currentState = KingJubboStates.IDLE;
        private Coroutine StateCoroutine;

        private Quaternion shootRotation;

        private void Start()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            ogRot = transform.localRotation.eulerAngles;
        }

        public void Update () {
            if (animator == null)
                return;

            switch (currentState)
            {
                case KingJubboStates.IDLE:
                    animator.SetBool("isAttacking", false);
                    animator.SetBool("isRecovering", false);
                    currentState = KingJubboStates.IDLING;
                    if ( StateCoroutine != null )
                        StopCoroutine(StateCoroutine);

                    StateCoroutine = StartCoroutine(Wait(UnityEngine.Random.Range(1f,2f), () => {
                        currentState = KingJubboStates.ATTACK;
                    }));
                    break;
                case KingJubboStates.IDLING:
                    break;
                case KingJubboStates.ATTACK:
                    animator.SetBool("isAttacking", true);
                    animator.SetBool("isRecovering", false);
                    currentState = KingJubboStates.ATTACKING;
                    shootRotation = Quaternion.Euler(new Vector3(0, UnityEngine.Random.Range(ogRot.y-40f,ogRot.y+40f), 0));
                    break;
                case KingJubboStates.ATTACKING:
                    transform.localRotation = Quaternion.Slerp(transform.localRotation, shootRotation, Time.deltaTime * 2f);
                    break;
                case KingJubboStates.HIT:
                    animator.SetBool("isAttacking", false);
                    animator.SetBool("isRecovering", true);
                    if (hitParticle != null)
                    {
                        hitParticle.Play();
                    }
                    hp--;
                    injuredAudioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
                    injuredAudioSource.Play();
                    if (hp <= 0)
                    {
                        currentState = KingJubboStates.KILL;
                    }
                    else
                    {
                        currentState = KingJubboStates.HITTED;
                    }
                    break;
                case KingJubboStates.HITTED:
                    break;
                case KingJubboStates.KILL:
                    animator.enabled = false;
                    currentState = KingJubboStates.KILLING;
                    if ( StateCoroutine != null )
                        StopCoroutine(StateCoroutine);
                    StateCoroutine = StartCoroutine(Wait(2.4f, Explode));
                    rigOffset = rigT.localPosition;
                    defeatAudioSource.Play();
                    break;
                case KingJubboStates.KILLING:
                    transform.position += Vector3.up * Time.deltaTime * 0.5f;
                    transform.Rotate(Vector3.up * Time.deltaTime * 5f);
                    transform.Rotate(Vector3.right * Time.deltaTime * 10f);
                    rigT.ShakeEngine(0.2f,rigOffset);

                    break;
            }
        }

        private IEnumerator Wait (float duration, Action action) {
            yield return new WaitForSeconds(duration);
            action.Invoke();
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("ActiveHit"))
            {
                if (currentState != KingJubboStates.HIT && currentState != KingJubboStates.HITTED && currentState != KingJubboStates.KILL && currentState != KingJubboStates.KILLING) {
                    currentState = KingJubboStates.HIT;
                }
            } else if (collision.gameObject.CompareTag("Trap")) {
                if (currentState != KingJubboStates.HIT && currentState != KingJubboStates.HITTED && currentState != KingJubboStates.KILL && currentState != KingJubboStates.KILLING) {
                    currentState = KingJubboStates.KILL;
                }
            }
        }

        public void Animation_Shoot () {

            if (spitParticle != null)
            {
                spitParticle.Play();
            }

            if (projectilePrefab == null)
                return;

            "mole_jump".PlaySFX(transform.position, 1f, UnityEngine.Random.Range(0.8f, 1.2f));
            GameObject projectile = Instantiate(projectilePrefab, muzzle.transform.position, muzzle.transform.rotation);
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(muzzle.transform.forward * 100f);
                //add fake torque
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 10f);
            }
        }

        public void Animation_AttackEnd () {
            currentState = KingJubboStates.IDLE;
        }

        public void Animation_RecoveryEnd () {
            currentState = KingJubboStates.IDLE;
        }

        public void Explode () {
            if (deathParticle != null)
            {
                deathParticle.transform.SetParent(null,true);
                deathParticle.Play();
            }

            for (var i = 0; i < 3; i++) {
                //get random position in sphere
                Vector3 randomPosition = UnityEngine.Random.insideUnitSphere * 0.2f;
                //random rotation
                Quaternion randomRotation = UnityEngine.Random.rotation;
                GameObject pear = Instantiate(this.pear, transform.position + randomPosition, randomRotation);
                Rigidbody rb = pear.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.constraints = RigidbodyConstraints.None;
                    //launch up
                    rb.AddForce(Vector3.up * UnityEngine.Random.Range(100f,200f), ForceMode.Impulse);
                }
            }

            if (victoryMusic && victoryMusicSwap) {
                victoryMusicSwap.clip = victoryMusic;
                victoryMusicSwap.Play();
            }
            defeatAudioSource.transform.SetParent(null, true);

            SunManager.Instance?.OnEvent("king-jubbo-defeated");

            Destroy(gameObject);
        }
    }
}
