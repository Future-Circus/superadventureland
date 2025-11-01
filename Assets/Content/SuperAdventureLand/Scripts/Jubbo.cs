namespace SuperAdventureLand
{
    using System;
    using UnityEngine;
    using Random = UnityEngine.Random; // Required for Random.Range
    public class Jubbo : StandardCreature
    {
        public AudioClip jubboGruntSfx;
        public AudioClip slapSfx;
        public override void OnValidate()
        {
            #if UNITY_EDITOR
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (jubboGruntSfx == null) {
                jubboGruntSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Content/SuperAdventureLand/Audio/jubbo_grunt.mp3");
            }
            if (slapSfx == null) {
                slapSfx = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Content/SuperAdventureLand/Audio/slap.wav");
            }
            base.OnValidate();
            #endif
        }
        public override void ExecuteState()
        {
             switch (state) {
                case CreatureState.IDLE:
                    base.ExecuteState();
                    break;
                case CreatureState.HIT:
                    jubboGruntSfx.PlaySFX(transform.position, 1f, Random.Range(0.8f, 1.2f));
                    base.ExecuteState();
                    break;
                case CreatureState.KICK:
                    slapSfx.PlaySFX(lastCollision.collisionPoint, 1f, Random.Range(0.9f, 1.1f));
                    jubboGruntSfx.PlaySFX(transform.position, 1f, Random.Range(0.8f, 1.2f));
                    agent.enabled = false;
                    animator.enabled = false;
                    rb.useGravity = true;
                    rb.isKinematic = false;
                    rb.linearDamping = 0.0f;
                    rb.constraints = RigidbodyConstraints.None;
                    rb.freezeRotation = false;
                    if (lastCollision != null && lastCollision.tag == "Kicker") {
                        if (!rb.LaunchAtLayer(1 << LayerMask.NameToLayer("Enemy"),20f, 1 << LayerMask.NameToLayer("Entity") | 1 << LayerMask.NameToLayer("Level"),10f))
                        {
                            Vector3 hitDirection = (transform.position - lastCollision.collisionPoint).normalized;
                            Vector3 hittedForce = hitDirection * hitSettings.directionForce;

                            hittedForce.y = Math.Max(hitSettings.upwardForce, hittedForce.y);
                            rb.AddForce(hittedForce, ForceMode.Impulse);

                            Vector3 randomTorque = new Vector3(
                                Random.Range(-1f, 1f),
                                Random.Range(-1f, 1f),
                                Random.Range(-1f, 1f)
                            ) * hitSettings.angularForce;
                            rb.AddTorque(randomTorque, ForceMode.Impulse);
                        }
                        gameObject.BecomeProjectile();
                    } else {
                        Vector3 launchVelocity = Vector3.up * 10;
                        rb.AddTorque(new Vector3(Random.Range(0, 2) * 2 - 1,Random.Range(0, 2) * 2 - 1,Random.Range(0, 2) * 2 - 1) * launchVelocity.magnitude*4, ForceMode.Impulse);
                        rb.AddForce(launchVelocity, ForceMode.Impulse);
                    }
                    break;
                case CreatureState.LAUNCH:
                    animator.enabled = false;
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }

        public override void GroundHit(CollisionWrapper collision) {
            if (state == CreatureState.LAUNCHING) {
                animator.enabled = true;
                rb.linearDamping = 0.9f;
                rb.angularDamping = 0.8f;
                rb.freezeRotation = true;
                Vector3 velocity = rb.linearVelocity;
                velocity.y = 0; // Project onto horizontal plane
                if (velocity.magnitude > 0.1f) {
                    float targetYRotation = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
                    rb.MoveRotation(Quaternion.Euler(0, targetYRotation, 0));
                } else {
                    rb.MoveRotation(Quaternion.Euler(0, rb.rotation.eulerAngles.y, 0));
                }
                SetState(CreatureState.IDLE);
            } else {
                base.GroundHit(collision);
            }
        }
    }
}
