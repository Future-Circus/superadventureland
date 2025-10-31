namespace SuperAdventureLand
{
    using UnityEngine;

    public class FortBreaker : EasyEvent
    {
        public int requiredHits = 1;
        private int hits = 0;
        public override void Start()
        {
            eventOnStart = false;
            base.Start();
        }
        public override void OnEvent(object arg0 = null)
        {
            hits++;
            Debug.Log("FortBreaker: hits = " + hits);
            if (hits < requiredHits) {
                return;
            }
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var collider in colliders) {
                collider.transform.SetParent(null,true);
                collider.enabled = true;
                var rb = Componentizer.DoComponent<Rigidbody>(collider.gameObject, true);
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.None;
                rb.mass = 1f;
                rb.linearDamping = 0.8f;
                rb.angularDamping = 0.8f;
            }
            onEvent?.Invoke(null);
        }

        public void SetupRigidbodies() {
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var collider in colliders) {
                var rb = Componentizer.DoComponent<Rigidbody>(collider.gameObject, true);
                rb.constraints = RigidbodyConstraints.FreezeAll;
                rb.useGravity = false;
            }
        }
    }

}
