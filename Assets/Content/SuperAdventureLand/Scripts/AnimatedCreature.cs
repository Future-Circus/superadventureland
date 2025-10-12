namespace SuperAdventureLand.Scripts
{
    using UnityEngine;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;
    #if UNITY_EDITOR
    using UnityEditor;
    using UnityEditorInternal;

    [CustomEditor(typeof(AnimatedCreature), true)]
    public class AnimatedCreatureEditor : CreatureEditor
    {
        public override void OnEnable() {
            showAnimationStates = true;
            showAnimationEvents = true;
            base.OnEnable();
        }
    }
    #endif

    public class AnimatedCreature : Creature
    {
        public override void ToggleRagdoll(bool enable)
        {
            if (animator != null) {
                animator.enabled = !enable;
            }
            base.ToggleRagdoll(enable);
        }
    }
}
