using UnityEngine;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DreamPark.Easy {
    public class EasyParticle : EasyEvent
    {
        public GameObject particleEffect;
        public bool delayNextEvent = false;
        public override void OnEvent(object arg0 = null)
        {
            if (particleEffect != null) {
                GameObject particle = Instantiate(particleEffect, transform.position, transform.rotation);
                if (delayNextEvent) {
                    Delay(particle);
                } else {
                    onEvent?.Invoke(arg0);
                }
            }
        }

        async void Delay(GameObject gameObject)
        {
            if (this == null || gameObject == null)
                return;

            var ps = GetComponentInChildren<ParticleSystem>();
            while(ps != null && !ps.isPlaying)
            {
                await Task.Yield();
            }
            var duration = ps.main.duration + ps.main.startLifetime.constantMax;

            while (ps.main.loop == true && !ps.isStopped) {
                await Task.Yield();
            }

            await Task.Delay((int)(duration*1000));

            if (this == null || gameObject == null)
                return;

            onEvent?.Invoke(null);
        }
    }
}
