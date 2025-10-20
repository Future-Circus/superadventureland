namespace SuperAdventureLand
{
    using UnityEngine;

    public class RedCoin : Item
    {
        public static int redcoinCount;
        public override void Start()
        {
            dp_collectFx = "FX_RedCoinParticle";
            dp_collectSfx = "redcoin";
            base.Start();
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
