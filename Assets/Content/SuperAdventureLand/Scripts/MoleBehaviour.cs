namespace SuperAdventureLand.Scripts
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.AI;
    using UnityEngine.Rendering;
    using Random = UnityEngine.Random; // Required for Random.Range

    public class MoleBehaviour : StandardCreature
    {
        private static MoleBehaviour activeMole;
        public ParticleSystem digEffect;
        public ParticleSystem attackEffect;
        public Collider attackCollider;
        public Transform dirtMound;
        private Vector3 dirtMoundScale = new Vector3(1, 1, 1);
        public override void Start()
        {
            base.Start();
            if(attackCollider != null) attackCollider.enabled = false;
            if(digEffect != null) digEffect.Stop();
            if(attackEffect != null) attackEffect.Stop();
            if(mainCollider != null) mainCollider.enabled = false;
        }
        public override void ExecuteState()
        {
            switch(state)
            {
                case CreatureState.START:
                    agent.updatePosition = false;
                    agent.updateRotation = false;
                    base.ExecuteState();
                    break;
                case CreatureState.IDLE:
                    base.ExecuteState();
                    agent.enabled = true;
                    dirtMoundScale = Vector3.one;
                    if(digEffect != null) digEffect.Stop();
                    if (activeMole == this) activeMole = null;
                    break;
                case CreatureState.IDLING:
                    if (waypoints.Length > 1) {
                        if (StateCondition()) {
                            base.ExecuteState();
                        }
                    } else {
                        if (IdleCondition()) {
                            SetState(CreatureState.ATTACK);
                        }
                        if (TargetCondition(CreatureState.IDLING)) {
                            if (StateCondition()) {
                                SetState(CreatureState.TARGET);
                            }
                        }
                    }
                    break;
                case CreatureState.TURN:
                    agent.isStopped = true; // stop moving while turning
                    if(digEffect != null) digEffect.Stop();
                    break;
                case CreatureState.TURNING:
                    if (StateCondition()) {
                        if (waypoints.Length > 0 && Random.Range(0, 100) < 50) {
                            SetState(CreatureState.MOVE);
                        } else {
                            SetState(CreatureState.ATTACK);
                        }
                    }
                    break;
                case CreatureState.TARGET:
                    if (!agentEnabled || !navigationReady) {
                        SetState(CreatureState.IDLE);
                        break;
                    }
                    agent.updatePosition = true;
                    agent.updateRotation = true;
                    agent.isStopped = false;
                    if(digEffect != null) digEffect.Play();
                    activeMole = this;
                    TargetPlayer();
                    break;
                case CreatureState.TARGETING:
                    if (TargetCondition(CreatureState.TARGETING)) {
                        TargetPlayer();
                        if(AttackCondition(CreatureState.TARGETING))
                        {
                            SetState(CreatureState.ATTACK);
                        }
                    } else {
                        NearestWaypoint();
                        SetState(CreatureState.MOVE);
                    }

                    break;
                case CreatureState.MOVE:
                    if (!agentEnabled || !navigationReady) {
                        SetState(CreatureState.IDLE);
                        break;
                    }
                    EnableAgent();
                    base.ExecuteState();
                    if(digEffect != null) digEffect.Play();
                    break;
                case CreatureState.ATTACK:
                    base.ExecuteState();
                    digEffect.Stop();
                    break;
                 case CreatureState.RUN:
                    if (!agentEnabled || !navigationReady) {
                        SetState(CreatureState.IDLE);
                        break;
                    }
                    EnableAgent();
                    NearestWaypoint();
                    if(digEffect != null) digEffect.Play();
                    break;
                case CreatureState.RUNNING:
                    if (agentEnabled && agent.remainingDistance < 0.05f) {
                        SetState(CreatureState.IDLE);
                    }
                    break;
                case CreatureState.FLY:
                    base.ExecuteState();
                    dirtMoundScale = Vector3.zero;
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
            DirtMoundController();
        }
        public override bool StateCondition() {
            switch(state)
            {
                case CreatureState.IDLING:
                    return agentEnabled && playerInRange;
                case CreatureState.TURNING:
                    return base.StateCondition() && animator.GetCurrentAnimatorStateInfo(0).IsName("Mole-Peek") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f;
                default:
                    return base.StateCondition();
            }
        }
        public void TargetPlayer() {
            Transform player = Camera.main.transform;
            Vector3 direction = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);

            var playerXZ = new Vector3(player.position.x, 0, player.position.z);
            var moleXZ = new Vector3(transform.position.x, 0, transform.position.z);
            var dir = (playerXZ - moleXZ).normalized;
            agent.SetDestination(player.position - dir * 1f);
        }
        public void EnableAgent() {
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }
        public bool IdleCondition() {
            return animator.GetCurrentAnimatorStateInfo(0).IsName("Mole-Idle") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f;
        }
        public override bool TargetCondition(CreatureState _state) {
            return base.TargetCondition(_state) && (activeMole == null || activeMole == this);
        }
        public bool AttackCondition(CreatureState _state) {
            return (activeMole == null || activeMole == this) && agent != null && agent.remainingDistance < 0.05f;
        }
        public void Animator_StartAttack () {
            attackEffect.Play();
            mainCollider.enabled = true;
            if(attackCollider != null)
            {
                attackCollider.enabled = true;
                attackCollider.GetComponent<Rigidbody>().isKinematic = false;
            }
        }
        public void Animator_DisableCollider() {
            mainCollider.enabled = false;
        }
        public void Animator_EnableCollider() {
            mainCollider.enabled = true;
        }
        public void Animator_EndAttack () {
            mainCollider.enabled = false;
            if(attackCollider != null)
            {
                attackCollider.enabled = false;
            }
            SetState(CreatureState.RUN);
        }
        public void DirtMoundController () {
            dirtMound.localScale = Vector3.Lerp(dirtMound.localScale, dirtMoundScale, Time.deltaTime * 5f);
        }
    }
}
