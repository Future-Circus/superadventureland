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

    public override void Start()
    {
        base.Start();
        
    }
    public override void OnEvent(object arg0 = null)
    {
        AudioSource audioSource = audioClip.PlaySFX(transform.position, volume, pitch+Random.Range(-pitchVariation, pitchVariation));

        if (!delayNextEvent) {
            onEvent?.Invoke(null);
        } else {
            StartCoroutine(DelayNextEvent(audioSource));
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
