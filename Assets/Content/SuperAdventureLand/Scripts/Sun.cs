namespace SuperAdventureLand
{
    using System;
    using UnityEngine;

    #if UNITY_EDITOR
    using UnityEditor;
    using System.Threading.Tasks;

    [CustomEditor(typeof(Sun), true)]
    public class SunEditor : StandardEntityEditor<SunState>
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Unlock"))
            {
                ((Sun)target).SetState(SunState.UNLOCK);
            }
        }
    }
    #endif

    public enum SunState
    {
        START,
        IDLE,
        IDLING,
        UNLOCK,
        UNLOCKING,
        UNLOCK_2,
        UNLOCK_2ING,
        UNLOCK_3,
        UNLOCK_3ING,
        HIDE,
        HIDING,
        REVEAL,
        REVEALING,
        DELAY_REVEAL,
        DELAY_REVEALING
    }

    [System.Serializable]
    public class SunConfig
    {
        public string title;
        public string description;
        public string achievementId;
        public bool isUnlocked
        {
            get
            {
                return !String.IsNullOrEmpty(title) && PlayerPrefs.GetInt("sun_" + title.Replace(" ", "").ToLower(), 0) == 1;
            }
            set
            {
                PlayerPrefs.SetInt("sun_" + title.Replace(" ", "").ToLower(), value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
        public string eventId;
        public int eventTotal = 1;
        public bool unlockedThisSession = false;
        public int eventCount
        {
            get
            {
                return PlayerPrefs.GetInt("sun_event_" + eventId, 0);
            }
            set
            {
                if (!isUnlocked)
                {
                    PlayerPrefs.SetInt("sun_event_" + eventId, value);
                    PlayerPrefs.Save();
                }
                if (eventCount >= eventTotal)
                {
                    isUnlocked = true;
                }
            }
        }
        public string spawnPointName;
    }

    public class Sun : StandardEntity<SunState>
    {
        public static int sunIndex = 0;
        [SerializeField]
        public SunConfig sunConfig;
        public MusicArea musicArea;
        public ParticleSystem shineParticle;
        public AudioSource shineAudio;
        public override void ExecuteState()
        {
            switch (state)
            {
                case SunState.START:
                    SaveOriginalPosition();
                    mainRenderer.materials[1].SetTextureOffset("_baseTex", new Vector2(sunIndex * 0.137f, 0));
                    mainRenderer.materials[1].SetTextureOffset("_nrmTex", new Vector2(sunIndex * 0.137f, 0));
                    AssignSunTexture();
                    TrackJunk("ogScale", mainRenderer.transform.localScale);
                    TrackJunk("ogRotation", mainRenderer.transform.localRotation);
                    sunIndex++;
                    SetState(SunState.REVEAL);

                    if (sunConfig.eventId != null && SunManager.Instance != null)
                    {
                        SunManager.Instance.RegisterSunEvent(sunConfig.eventId, () =>
                        {
                            sunConfig.eventCount++;
                            if (sunConfig.isUnlocked && !sunConfig.unlockedThisSession)
                            {
                                sunConfig.unlockedThisSession = true;
                                SetState(SunState.UNLOCK);
                            }
                        });
                    }

                    if (sunConfig.isUnlocked && DreamBand.Instance != null && DreamBand.Instance is SA_DreamBand saDreamBand)
                    {
                        saDreamBand.CollectSun(false);
                    }

                    break;
                case SunState.IDLE:
                    AssignSunTexture();
                    break;
                case SunState.IDLING:
                    break;
                case SunState.UNLOCK:
                    musicArea.Enter();
                    "FX_SunReveal".SpawnAsset(transform.position, Quaternion.identity);
                    if (!String.IsNullOrEmpty(sunConfig.spawnPointName) && GameObject.Find(sunConfig.spawnPointName) != null)
                    {
                        transform.position = GameObject.Find(sunConfig.spawnPointName).transform.position;
                    }
                    else
                    {
                        transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1.4f + Vector3.up * 2f;
                    }
                    sunConfig.isUnlocked = true;

                    if (DreamBand.Instance != null && DreamBand.Instance is SA_DreamBand saDreamBand2)
                    {
                        saDreamBand2.CollectSun();
                    }

                    break;
                case SunState.UNLOCKING:

                    //what happens here?
                    //  star descends from the sky in front of the player

                    transform.position = Vector3.Lerp(transform.position, Camera.main.transform.position + Camera.main.transform.forward * 1.4f, Time.deltaTime * 5f);
                    transform.LookAt(Camera.main.transform.position);
                    if (timeSinceStateChange > 2f)
                    {
                        SetState(SunState.UNLOCK_2);
                    }
                    break;
                case SunState.UNLOCK_2:

                    //what happens here?
                    //  star 'unlocks' with lots of animation
                    AssignSunTexture();
                    shineAudio.PlayWithFadeIn(1f, this);
                    shineParticle.Play();
                    break;
                case SunState.UNLOCK_2ING:
                    //twirl
                    float t = Mathf.Clamp01(timeSinceStateChange / 1.4f);
                    t = 1f - Mathf.Pow(1f - t, 2f);
                    float angle = Mathf.Lerp(0f, 720f, t);
                    mainRenderer.transform.localRotation = GetJunk<Quaternion>("ogRotation") * Quaternion.Euler(0f, 0f, angle);

                    //jiggle
                    float frequency = 3f;
                    float amplitude = 0.25f;
                    float decay = 5f;
                    float t2 = timeSinceStateChange / 0.6f;
                    float damper = Mathf.Exp(-decay * t2);
                    float scaleFactor = 1f + amplitude * damper * Mathf.Sin(2f * Mathf.PI * frequency * t2);
                    mainRenderer.transform.localScale = GetJunk<Vector3>("ogScale") * scaleFactor;

                    if (timeSinceStateChange > 2f)
                    {
                        mainRenderer.transform.localRotation = GetJunk<Quaternion>("ogRotation") * Quaternion.Euler(0f, 360f, 0f);
                        mainRenderer.transform.localScale = GetJunk<Vector3>("ogScale");
                        SetState(SunState.UNLOCK_3);
                    }
                    break;
                case SunState.UNLOCK_3:

                    //what happens here?
                    //  star shrinks into nothingness

                    break;
                case SunState.UNLOCK_3ING:
                    //scale
                    transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 10f);
                    if (timeSinceStateChange > 1f)
                    {
                        musicArea.Exit();
                        SetState(SunState.REVEAL);
                    }

                    break;
                case SunState.HIDE:

                    shineAudio.PauseWithFadeOut(1f, this);
                    shineParticle.Stop();

                    if (timeSinceStateChange > 1f)
                    {
                        SetState(SunState.IDLE);
                    }

                    break;
                case SunState.HIDING:
                    transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, timeSinceStateChange);
                    if (timeSinceStateChange > 1f)
                    {
                        SetState(SunState.IDLE);
                    }
                    break;
                case SunState.REVEAL:
                    transform.position = ogPosition.position;
                    transform.rotation = ogPosition.rotation;

                    if (sunConfig.isUnlocked)
                    {
                        shineAudio.PlayWithFadeIn(1f, this);
                        shineParticle.Play();
                    }

                    break;
                case SunState.REVEALING:
                    transform.localScale = Vector3.Lerp(transform.localScale, ogPosition.localScale, timeSinceStateChange);

                    if (timeSinceStateChange > 1f)
                    {
                        SetState(SunState.IDLE);
                    }

                    break;
                case SunState.DELAY_REVEAL:
                    break;
                case SunState.DELAY_REVEALING:
                    if (timeSinceStateChange >= 0.4f)
                    {
                        SetState(SunState.REVEAL);
                    }
                    break;
            }
        }

        public void AssignSunTexture()
        {
            string textureName = sunConfig.isUnlocked ? "sun" : "sun_empty";
            textureName.GetAsset<Texture2D>(tex =>
            {
                mainRenderer.materials[0].SetTexture("_baseTex", tex);
                mainRenderer.materials[1].SetTexture("_baseTex", tex);
            }, error =>
            {
                Debug.LogError($"Failed to load sun texture: {error}");
            });
        }

        public void OnDestroy()
        {
            sunIndex--;
        }
    }
}
