namespace SuperAdventureLand
{
    using System.Collections.Generic;
    using UnityEngine;

    public class KeepChildrenUpright : MonoBehaviour
    {
        private class UprightAnchor
        {
            public Transform child;
            public GameObject pivot;
            public Vector3 originalLocalPosition;
        }

        private List<UprightAnchor> anchors = new List<UprightAnchor>();

        void Start()
        {

            var children = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                children[i] = transform.GetChild(i);
            }

            for (int i = 0; i < children.Length; i++)
            {
                var child = children[i];

                Vector3 worldOrigin = child.position;
                Ray ray = new Ray(worldOrigin, Vector3.down);

                RaycastHit[] hits = Physics.RaycastAll(ray, 100f, LayerMask.GetMask("Level"));
                bool foundGround = false;
                RaycastHit hit = new RaycastHit();
                foreach (RaycastHit _ in hits)
                {
                    if (_.collider.CompareTag("Ground"))
                    {
                        foundGround = true;
                        hit = _;
                        break;
                    }
                }

                if (foundGround)
                {
                    GameObject pivot = new GameObject($"UprightPivot_{child.name}");
                    pivot.transform.position = hit.point;
                    pivot.transform.rotation = Quaternion.identity;

                    // Store original local position for safety (not used here, but could be)
                    Vector3 localPos = child.localPosition;

                    // Reparent
                    child.SetParent(pivot.transform, worldPositionStays: true);

                    // Make the pivot a child of this script's parent (for relative movement)
                    pivot.transform.SetParent(transform, worldPositionStays: true);

                    anchors.Add(new UprightAnchor
                    {
                        child = child,
                        pivot = pivot,
                        originalLocalPosition = localPos
                    });
                }
                else
                {
                    Debug.LogWarning($"No ground hit below child {child.name}");
                }
            }
        }

        void LateUpdate()
        {
            foreach (var anchor in anchors)
            {
                anchor.pivot.transform.rotation = Quaternion.identity;
            }
        }
    }
}
