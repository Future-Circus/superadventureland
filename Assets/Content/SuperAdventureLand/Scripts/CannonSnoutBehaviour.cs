namespace SuperAdventureLand.Scripts
{
    using UnityEngine;
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(CannonSnoutBehaviour))]
    public class CannonSnoutBehaviourEditor : CreatureEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            CannonSnoutBehaviour cannonSnoutBehaviour = (CannonSnoutBehaviour)target;
            if (GUILayout.Button("Start Battle"))
            {
                cannonSnoutBehaviour.SetState(CreatureState.TAUNT);
            }
        }
    }
    #endif
    public class CannonSnoutBehaviour : Creature
    {
        public ShipBehaviour shipBehaviour;
        public Transform headBone;
        public Transform cannonSnout;
        public ParticleSystem muzzleFlashParticle;
        public GameObject bulletPrefab;
        public ParticleSystem appearParticle;
        public LineRenderer rope;
        public Transform cannonSnoutHand;
        public Transform cannonSnoutFoot;
        private SpringJoint joint;
        public MusicArea musicArea;
        public AudioClip introMusic;
        public AudioClip battleMusic;
        public AudioClip victoryMusic;
        public AudioSource cannonFireAudio;
        public override void ExecuteState()
        {
            switch (state) {
                case CreatureState.START:
                    rope.enabled = false;
                    SetState(CreatureState.IDLE);
                    break;
                case CreatureState.IDLE:
                    animator.enabled = true;
                    animator.SetBool("isAttacking", false);
                    animator.SetBool("isAiming", false);
                    animator.SetBool("isTaunting", false);
                    SetState(CreatureState.IDLING);
                    break;
                case CreatureState.TAUNT:
                    animator.SetBool("isTaunting", true);
                    musicArea.SwapAudioClip(introMusic,0.01f);
                    break;
                case CreatureState.TARGET:
                    animator.SetBool("isAiming", true);
                    this.Wait(2f, () => {
                        SetState(CreatureState.ATTACK);
                    });
                    break;
                case CreatureState.ATTACK:
                    SetState(CreatureState.ATTACKING);
                    animator.SetBool("isAttacking", true);
                    muzzleFlashParticle.Play();
                    GameObject bullet = Instantiate(bulletPrefab, cannonSnout.position, cannonSnout.rotation);
                    Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
                    bulletRb.AddForce(cannonSnout.forward * 30f * bulletRb.mass, ForceMode.Impulse);
                    cannonFireAudio.Play();
                    this.Wait(1f, () => {
                        SetState(CreatureState.IDLE);
                    });
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }
        void LateUpdate()
        {
            if (rope && rope.enabled) {
                rope.SetPosition(0, rope.transform.position);
                rope.SetPosition(1, cannonSnoutHand.position);
                rope.SetPosition(2, cannonSnoutFoot.position);
                if (joint && state != CreatureState.DYING) {
                    joint.spring += Time.deltaTime * 10f;

                    if (joint.spring >= 80f || transform.position.y >= rope.transform.position.y) {
                        Anchor();
                    };
                }
            } else {
                if (state == CreatureState.TARGETING) {
                    animator.enabled = false;
                    Vector3 directionToPlayer = (Camera.main.transform.position - headBone.position).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer) * Quaternion.Euler(-90, 0, 180); // Adjust this as needed
                    headBone.rotation = Quaternion.Slerp(headBone.rotation, targetRotation, Time.deltaTime * 2f);
                }
            }
        }
        public void Attack() {
            SetState(CreatureState.TARGET);
        }
        public void Attach(float delay = 1f) {
            rope.enabled = true;
            rope.GetComponentInChildren<ParticleSystem>().Play();
            cannonSnoutHand.GetComponent<AudioSource>().Play();

            this.Wait(delay, () => {
                joint = gameObject.AddComponent<SpringJoint>();
                joint.connectedBody = rope.GetComponent<Rigidbody>();

                joint.damper = 5f;
                joint.autoConfigureConnectedAnchor = false;
                joint.anchor = Vector3.zero;
                joint.connectedAnchor = Vector3.zero;

                var rb = GetComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.None;
                rb.useGravity = true;
                rb.isKinematic = false;

                animator.SetBool("isRoping",true);
                animator.SetBool("isTaunting", false);
            });
        }
        public void Anchor() {
            rope.enabled = false;
            var rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.useGravity = false;
            rb.isKinematic = true;
            Destroy(joint);
            transform.SetParent(shipBehaviour.cannonsnoutAnchor,true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one*1.4f;
            animator.SetBool("isRoping",false);
            animator.SetBool("isTaunting", false);
            animator.enabled = true;
            appearParticle.Play();
        }
        public void Crash () {
            musicArea.SwapAudioClip(victoryMusic,1f);
            transform.SetParent(shipBehaviour.transform.parent,true);
            Attach(0.01f);
            var rb = GetComponent<Rigidbody>();
            rb.AddForce(shipBehaviour.transform.forward * 30f, ForceMode.Impulse);
            transform.localScale = Vector3.one*1f;
            SetState(CreatureState.DIE);
        }
        public void Animator_TauntEnd() {
            SetState(CreatureState.IDLE);
            shipBehaviour.StartBattle();
            musicArea.SwapAudioClip(battleMusic,0.5f);
            musicArea.transform.localScale = Vector3.one*150;
            Attach(0f);
        }
        public void StartBattle() {
            if (state != CreatureState.IDLE && state != CreatureState.IDLING) return;
            if (shipBehaviour && shipBehaviour.state != CreatureState.IDLE && shipBehaviour.state != CreatureState.IDLING) return;
            SetState(CreatureState.TAUNT);
        }
    }
}
