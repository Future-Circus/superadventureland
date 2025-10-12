using DreamPark;
using UnityEngine;

public class MusicArea : MonoBehaviour
{
    public static int? _priority;
    public static MusicArea currentMusicArea;
    public AudioClip musicTrack;
    public float volume = 0.6f;
    public int priority = 0;
    private bool isPlaying = false;
    private AudioSource audioSource;
    private Vector3 halfExtents = Vector3.zero;
    public virtual void Awake() {
        if (!musicTrack) {
            enabled = false;
            return;
        }
        GameObject musicEmitter = new GameObject("MusicEmitter");
        musicEmitter.transform.parent = transform;
        musicEmitter.transform.localPosition = Vector3.zero;
        musicEmitter.transform.localRotation = Quaternion.identity;
        musicEmitter.transform.localScale = Vector3.one;
        musicEmitter.AddComponent<RealisticRolloff>();
        audioSource = musicEmitter.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.priority = priority;
        audioSource.clip = musicTrack;
        audioSource.spatialBlend = 1;

        BodyTracker bodyTracker = musicEmitter.AddComponent<BodyTracker>();
        bodyTracker.yOffset = 2f;

        if (TryGetComponent(out LevelTemplate levelTemplate)) {
            var bounds2D = GameLevelDimensions.GetDimensionsInMeters(levelTemplate.size);
            halfExtents = new Vector3(bounds2D.x/2f, 50f, bounds2D.y/2f);
        }
    }
    void Update () {
        if (Camera.main) {
            if (IsPointWithinBounds(Camera.main.transform.position, transform, halfExtents)) {
                if (!isPlaying) {
                    Enter();
                }
            } else {
                if (isPlaying) {
                    Exit();
                }
            }
        }
    }
    public virtual void Enter()
    {
        if (currentMusicArea) {
            if (priority > currentMusicArea.priority) {
                currentMusicArea.Exit();
            } else {
                return;
            }
        }
        if (audioSource != null)
        {
            audioSource.PlayWithFadeIn(1f, this, volume);
            currentMusicArea = this;
            isPlaying = true;
        }
    }
    public virtual void Exit()
    {
        if (audioSource != null)
        {
            audioSource.PauseWithFadeOut(1f, this, volume);
            if (currentMusicArea == this) {
                currentMusicArea = null;
            }
            isPlaying = false;
        }
    }
    public void SwapAudioClip(AudioClip newClip, float time = 0.5f) {
        audioSource.PauseWithFadeOut(time, this, volume);
        this.Wait(time, () => {
            audioSource.clip = newClip;
            if (isPlaying) {
                audioSource.PlayWithFadeIn(time, this, volume);
            }
        });
    }
    bool IsPointWithinBounds(Vector3 point, Transform obj, Vector3 halfExtents)
    {
        // Convert the world point to local space of the object
        Vector3 localPoint = obj.InverseTransformPoint(point);

        // Now just check local bounds
        return Mathf.Abs(localPoint.x) <= halfExtents.x &&
            Mathf.Abs(localPoint.y) <= halfExtents.y &&
            Mathf.Abs(localPoint.z) <= halfExtents.z;
    }
    void OnDestroy() {
        if (currentMusicArea == this) {
            currentMusicArea = null;
        }
    }
// #if UNITY_EDITOR
//     public void OnDrawGizmos()
//     {
//         Gizmos.color = Color.yellow;
//         var visualHalfExtents = halfExtents;
//         visualHalfExtents.y = 5f;
//         Gizmos.DrawWireCube(transform.position + new Vector3(0, 5f, 0), visualHalfExtents * 2f);
//     }
// #endif
}
