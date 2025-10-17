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
            switch (state)
            {
                case DreamBandState.COLLECT:
                    collectParticle.Play();
                    coinCount++;
                    coinText.text = coinCount.ToString();
                    break;
                case DreamBandState.COLLECTING:
                    SetState(DreamBandState.PLAY);
                    break;
                case DreamBandState.INJURE:
                    int punishCoinCount = Mathf.Min(3, coinCount);
                    coinCount -= punishCoinCount;
                    coinText.text = coinCount.ToString();
                    for (int i = 0; i < punishCoinCount; i++)
                    {
                        "E_COIN".GetAsset<GameObject>(coinPrefab =>
                        {
                            Vector3 randomPos = transform.position + Random.insideUnitSphere * 0.1f;
                            GameObject coin = Instantiate(coinPrefab, randomPos, Quaternion.identity, transform.FindRoot());
                            coin.GetComponent<Coin>().dp_isStatic = false;
                        }, error =>
                        {
                            Debug.LogError($"Failed to load coin: {error}");
                        });
                    }
                    break;
                case DreamBandState.INJURING:
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

        public void CollectCoin()
        {
            SetState(DreamBandState.COLLECT);
        }

        public void CollectSun(bool showEffect = true)
        {
            if (showEffect)
            {
                collectParticle.Play();
            }
            SetState(DreamBandState.ACHIEVEMENT);
        }

        public void InjurePlayer()
        {
            SetState(DreamBandState.INJURE);
        }
    }
}
