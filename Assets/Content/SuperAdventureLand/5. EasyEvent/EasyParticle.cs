namespace SuperAdventureLand {
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
    public class EasyParticle : DreamPark.Easy.EasyParticle
    {
        #if UNITY_EDITOR
        public void OnValidate()
        {
            if (particleEffect == null)
            {
                particleEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/SuperAdventureLand/VFX/FX_SteamCloud.prefab");
            }
        }
        #endif
    }
}
