using UnityEngine;
using Text = TMPro.TMP_Text;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(DreamBand),true)]
public class DreamBandEditor : StandardEntityEditor<DreamBandState>
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        DreamBand targetController = (DreamBand)target;

        if (GUILayout.Button("TimesUp"))
        {
            if (Application.isPlaying)
            {
                targetController.timer = 3;
                targetController.SetState(DreamBandState.PLAY);
            }
        }
    }
}
#endif

public enum DreamBandState {
    START,
    STANDBY,
    STANDBYING,
    PLAY,
    PLAYING,
    PAUSE,
    PAUSING,
    END,
    ENDING,
    COLLECT,
    COLLECTING,
    INJURE,
    INJURING,
    RESTART,
    RESTARTING,
    ACHIEVEMENT,
    ACHIEVEMENTING,
    WIN,
    WINNING,
    DESTROY,
    DESTROYING
}

public class DreamBand : StandardEntity<DreamBandState>
{
    public static Dictionary<string, DreamBand> instances;
    [ReadOnly] public string gameId;
    public static DreamBand Instance;
    public Text timerText;
    [HideInInspector] public float timer = 1800; //default is 30 minutes
    private float lastTime = -1;
    private Vector3 lastFramePosition;
    private Vector3 lastStepPosition;
    private float stillThreshold = 0.001f;
    private float stillTimeRequired = 30f;
    private float stillTimer = float.MaxValue;
    public override void ExecuteState()
    {
        // Debug.Log($"DREAMBAND STATE {state}");
        if (!isPaused)
        {
            timer -= Time.deltaTime;
        }
        switch (state) {
            case DreamBandState.START:

                if (instances == null) {
                    instances = new Dictionary<string, DreamBand>();
                }
                instances.Add(gameId, this);
                SetState(DreamBandState.PLAY);
                break;
            case DreamBandState.STANDBY:
                break;
            case DreamBandState.STANDBYING:
                if (Mathf.Floor(Time.time) % 2 == 0) {
                    timerText.enabled = false;
                } else {
                    timerText.enabled = true;
                }
                break;
            case DreamBandState.PLAY:
                timerText.enabled = true;
                break;
            case DreamBandState.PLAYING:
                if (timer > 0) {
                    int minutes = (int)(timer / 60);
                    int seconds = (int)(timer % 60);
                    timerText.text = $"{minutes:00}:{seconds:00}";
                } else {
                    timerText.text = "00:00";
                    SetState(DreamBandState.END);
                }
                if (isHeadsetStill) {
                    SetState(DreamBandState.PAUSE);
                }
                break;
            case DreamBandState.PAUSE:
                break;
            case DreamBandState.PAUSING:
                if (Mathf.Floor(Time.time) % 2 == 0) {
                    timerText.enabled = false;
                } else {
                    timerText.enabled = true;
                }
                if (!isHeadsetStill) {
                    SetState(DreamBandState.PLAY);
                }
                break;
            case DreamBandState.COLLECTING:
                SetState(DreamBandState.PLAY);
                break;
            case DreamBandState.INJURING:
                SetState(DreamBandState.PLAY);
                break;
            case DreamBandState.ACHIEVEMENTING:
                SetState(DreamBandState.PLAY);
                break;
            case DreamBandState.DESTROY:
                Destroy(gameObject);
                break;
            case DreamBandState.END:
                break;
            case DreamBandState.ENDING:
                break;
        }
    }

    public void Show() {
        if (isEnded) {
            return;
        }
        if (Instance) {
            timer = Instance.timer;
            Debug.Log("DreamBand transferred remaining time: " + timer);
            Instance.Hide();
        }
        Instance = this;
        SetState(DreamBandState.PLAY);
    }

    public void Hide() {
        if (isEnded) {
            return;
        }
        SetState(DreamBandState.STANDBY);
    }
    
    public bool isPlaying {
        get {
            return state == DreamBandState.PLAY || state == DreamBandState.PLAYING;
        }
    }
    public bool isPaused {
        get {
            return state == DreamBandState.PAUSE || state == DreamBandState.PAUSING;
        }
    }
    public bool isEnded {
        get {
            return state == DreamBandState.END || state == DreamBandState.ENDING;
        }
    }

    public void OnDestroy() {
        if (DreamBand.instances != null && DreamBand.instances.ContainsKey(gameId)) {
            DreamBand.instances.Remove(gameId);
        }
        if (DreamBand.Instance == this) {
            DreamBand.Instance = null;
        }
    }

    public void OnEnable() {
        if (lastTime > 0) {
            timer -= Time.time - lastTime;
            lastTime = -1;
        }
    }

    public void OnDisable() {
        lastTime = Time.time;
    }

    public bool isHeadsetStill
    { 
        get {
            if (Camera.main == null) return true;

            Vector3 currentPosition = Camera.main.transform.position;
            float distanceMoved = Vector3.Distance(currentPosition, lastFramePosition);
            lastFramePosition = currentPosition;

            if (distanceMoved > stillThreshold)
            {
                stillTimer = 0f;
                return false;
            }

            stillTimer += Time.deltaTime;
            if (stillTimer < stillTimeRequired)
            {
                return false;
            }
            
            return true;
        }
    }
}
