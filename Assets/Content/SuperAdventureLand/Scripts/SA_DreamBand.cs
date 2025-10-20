namespace SuperAdventureLand
{
    using UnityEngine;
    using Text = TMPro.TMP_Text;

    public class SA_DreamBand : DreamBand
    {
        public Text coinText;
        public Text sunText;
        public ParticleSystem collectParticle;
        public ParticleSystem punishParticle;
        public float coinSpawnDistance = 1f;
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
                    Debug.Log("Collect coin, " + coinCount + " coins");
                    break;
                case DreamBandState.COLLECTING:
                    SetState(DreamBandState.PLAY);
                    break;
                case DreamBandState.INJURE:
                    int punishCoinCount = Mathf.Min(1, coinCount);
                    punishParticle.Play();
                    coinCount -= punishCoinCount;
                    coinText.text = coinCount.ToString();
                    "sizzle".PlaySFX(transform.position, 1f, 1f);
                    Debug.Log("Injure player, punish " + punishCoinCount + " coins" + " remaining coins: " + coinCount);
                    for (int i = 0; i < punishCoinCount; i++)
                    {
                        "E_COIN".GetAsset<GameObject>(coinPrefab =>
                        {
                            Vector3 randomPos = Camera.main.transform.position + Camera.main.transform.forward * coinSpawnDistance;
                            GameObject coin = Instantiate(coinPrefab, randomPos, Quaternion.identity);
                            coin.GetComponent<Coin>().dp_canSplash = true;
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
