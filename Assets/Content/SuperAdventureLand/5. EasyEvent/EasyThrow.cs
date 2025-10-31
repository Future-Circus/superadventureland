namespace SuperAdventureLand {
using System.Linq;
using UnityEngine;
    public class EasyThrow : DreamPark.Easy.EasyThrow
    {
        public override void OnValidate() {
            if (targetLayerOrder == null || targetLayerOrder.Length == 0) {
                targetLayerOrder = new string[] { "Entity", "Level" };
            }
            base.OnValidate();
        }
        public override void OnEvent(object arg0 = null)
        {
            CollisionWrapper lastCollision = GetLastCollision(arg0);
            if (lastCollision != null && lastCollision.gameObject != null && !lastCollision.gameObject.IsDestroyed() && lastCollision.gameObject.TryGetComponent(out PlayerInteractor interactor)) {
                Throw(interactor.GetDirection(), interactor.GetVelocity());
                onEvent?.Invoke(null);
            } else {
                base.OnEvent(arg0);
            }
        }
    }

}