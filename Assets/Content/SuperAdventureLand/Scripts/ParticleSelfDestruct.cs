namespace SuperAdventureLand.Scripts
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;

    public class ParticleSelfDestruct : MonoBehaviour
    {
        public bool destroyParent = false;
        // Start is called before the first frame update
        async void Start()
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

            if (destroyParent) {
                Destroy(transform.parent.gameObject);
            } else {
                Destroy(gameObject);
            }
        }
    }
}
