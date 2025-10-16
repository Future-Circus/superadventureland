using DreamPark;
using UnityEngine;

[RequireComponent(typeof(OptimizedAFIgnore))]
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
    private LevelTemplate levelTemplate;
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
        audioSource.volume = 0;

        BodyTracker bodyTracker = musicEmitter.AddComponent<BodyTracker>();
        bodyTracker.yOffset = 2f;

        levelTemplate = GetComponent<LevelTemplate>();
        if (levelTemplate) {
            var bounds2D = GameLevelDimensions.GetDimensionsInMeters(levelTemplate.size);
            halfExtents = new Vector3(bounds2D.x/2f, 50f, bounds2D.y/2f);
        } else {
            halfExtents = transform.localScale * 0.5f;
        }
    }
    void Update () {
        if (!levelTemplate) {
            halfExtents = transform.localScale * 0.5f;
        }
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
        // Vector from center to point in world space
        Vector3 toPoint = point - obj.position;

        // Project onto the object’s local axes
        float x = Vector3.Dot(toPoint, obj.right);
        float y = Vector3.Dot(toPoint, obj.up);
        float z = Vector3.Dot(toPoint, obj.forward);

        // Check against half extents (which can come directly from localScale * 0.5f)
        return Mathf.Abs(x) <= halfExtents.x &&
            Mathf.Abs(y) <= halfExtents.y &&
            Mathf.Abs(z) <= halfExtents.z;
    }
    void OnDestroy() {
        if (currentMusicArea == this) {
            currentMusicArea = null;
        }
    }
#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        if (!TryGetComponent(out LevelTemplate levelTemplate)) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
#endif
}
