namespace SuperAdventureLand.Scripts
{
    using UnityEngine;
    using Text = TMPro.TMP_Text;

    public class SA_DreamBand : DreamBand
    {
        public Text coinText;
        public Text sunText;
        public ParticleSystem collectParticle;
        private int coinCount = 0;
        private int sunCount = 0;
            public override void ExecuteState()
        {
            switch (state) {
                case DreamBandState.COLLECT:
                    collectParticle.Play();
                    coinCount++;
                    coinText.text = coinCount.ToString();
                    break;
                case DreamBandState.COLLECTING:
                    SetState(DreamBandState.PLAY);
                    break;
                case DreamBandState.ACHIEVEMENT:
                    sunCount++;
                    sunText.text = sunCount.ToString();
                    break;
                case DreamBandState.ACHIEVEMENTING:
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }
        public void CollectCoin() {
            SetState(DreamBandState.COLLECT);
        }

        public void CollectSun(bool showEffect = true) {
            if (showEffect) {
                collectParticle.Play();
            }
            SetState(DreamBandState.ACHIEVEMENT);
        }
    }
}
