namespace SuperAdventureLand.Scripts
{
    public class Coin : Item
    {
        public override void Start()
        {
            dp_collectFx = "FX_CoinParticle";
            dp_collectSfx = "coin";
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
