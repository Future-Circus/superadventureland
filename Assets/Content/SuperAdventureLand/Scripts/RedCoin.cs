namespace SuperAdventureLand
{
    using UnityEngine;
    #if UNITY_EDITOR
    using UnityEditor;
    #endif
    public class RedCoin : Item
    {
        public static int redcoinCount;
        public override void OnValidate()
        {
            #if UNITY_EDITOR
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (dp_collectFx == null)
            {
                dp_collectFx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Content/SuperAdventureLand/VFX/FX_RedCoinParticle.prefab");
            }
            if (dp_collectSfx == null)
            {
                dp_collectSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Content/SuperAdventureLand/Audio/redcoin.mp3");
            }
            base.OnValidate();
            #endif
        }
        public override void ExecuteState()
        {
            switch (state) {
                case ItemState.COLLECT:
                    SunManager.Instance?.OnEvent("red-coin-hunt");
                    "redcoin".PlaySFX(transform.position, 1f, 1f+redcoinCount/12f);
                    if (DreamBand.Instance != null && DreamBand.Instance is SA_DreamBand saDreamBand)
                        saDreamBand.CollectCoin();

                    redcoinCount++;
                    base.ExecuteState();
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }
    }
}
