using System.Collections;
using UnityEngine;

public class EasyAudio : EasyEvent
{
    public AudioClip audioClip;
    public float volume = 1f;
    public float pitch = 1f;
    public float pitchVariation = 0.1f;
    public bool loop = false;
    public bool delayNextEvent = false;
    private AudioSource audioSource;

    public override void Start()
    {
        base.Start();
        
    }
    public override void OnEvent(object arg0 = null)
    {
        isEnabled = true;
        Debug.Log("[EasyAudio] OnEvent: " + audioClip.name);
        audioSource = audioClip.PlaySFX(transform.position, volume, pitch+Random.Range(-pitchVariation, pitchVariation), loop ? transform : null);
        if (!delayNextEvent) {
            onEvent?.Invoke(null);
        } else {
            StartCoroutine(DelayNextEvent(audioSource));
        }
    }
    public void Update()
    {
        if (!isEnabled && audioSource != null) {
            audioSource.Stop();
            if (audioSource.gameObject != null) {
                Destroy(audioSource.gameObject);
            }
            audioSource = null;
        }
    } 
    private IEnumerator DelayNextEvent(AudioSource audioSource)
    {
        if (delayNextEvent) {
            yield return new WaitForSeconds(audioSource.clip.length);
        } else {
            yield return new WaitUntil(() => !audioSource.isPlaying);
        }
        onEvent?.Invoke(null);
    }
}
