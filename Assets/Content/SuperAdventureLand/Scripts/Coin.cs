using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace SuperAdventureLand
{
    public class Coin : Item
    {
        public override void OnValidate()
        {
            #if UNITY_EDITOR
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (dp_collectFx == null)
            {
                dp_collectFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/SuperAdventureLand/VFX/FX_CoinParticle.prefab");
            }
            if (dp_collectSfx == null)
            {
                dp_collectSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Content/SuperAdventureLand/Audio/coin.wav");
            }
            base.OnValidate();
            #endif
        }
        public override void Start()
        {
            base.Start();
        }
        public override void ExecuteState() {
            switch (state) {
                case ItemState.COLLECT:

                    SunManager.Instance?.OnEvent("coin-collected");

                    if (DreamBand.Instance != null && DreamBand.Instance is SA_DreamBand saDreamBand)
                        saDreamBand.CollectCoin();

                    base.ExecuteState();
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }
    }
}
