namespace DreamPark.Easy
{
    using UnityEngine;
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(EasyDetect), true)]
    public class EasyDetectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        public void OnSceneGUI() {
            OnSceneGUI_EasyDetect();
        }
        public void OnSceneGUI_EasyDetect() {
            EasyDetect easyDetect = (EasyDetect)target;
            Handles.color = Color.red;

            easyDetect.detectionRange = Handles.ScaleValueHandle(
                easyDetect.detectionRange,
                easyDetect.transform.position + Vector3.forward * easyDetect.detectionRange,
                Quaternion.identity,
                1f,
                Handles.CubeHandleCap,
                0.1f
            );

            Handles.DrawWireDisc(easyDetect.transform.position, Vector3.up, easyDetect.detectionRange);

            if (GUI.changed)
            {
                Undo.RecordObject(easyDetect, "Change Range");
            }
        }
    }
    #endif
    public class EasyDetect : EasyEvent
    {
        public bool detectPlayer = true;
        [HideIf("detectPlayer")] public bool detectTarget = false;
        [HideIf("detectPlayer")][ShowIf("detectTarget")] public Transform target;
        [HideIf("detectPlayer")] public bool detectTag = false;
        [HideIf("detectPlayer")][ShowIf("detectTag")] public string targetTag = "Player";
        public float detectionRange = 1.8f;
        public bool alwaysPursueAfterDetection = false;
        private bool hasTarget = false;
        private GameObject _target;
        private float cooldown = 0f;

        public override void OnEvent(object arg0 = null) {
            Debug.Log("[EasyDetect] OnEvent called");
            if (detectPlayer) {
                _target = Camera.main.gameObject;
            } else if (detectTarget) {
                _target = target.gameObject;
            }
            isEnabled = true;
        }
        public void Update()
        {
            if (!isEnabled) {
                return;
            }
            if (cooldown > 0f) {
                cooldown -= Time.deltaTime;
                return;
            }
            if (detectTag && _target == null) {
                //do a spherecast to find the target with tag
                Collider[] targets = Physics.OverlapSphere(gameObject.transform.position, detectionRange);
                foreach (Collider collider in targets) {
                    if (collider.gameObject.CompareTag(targetTag)) {
                        _target = target.gameObject;
                        break;
                    }
                }
                cooldown = 0.5f;
            }
            if (_target != null) {
                var p1 = new Vector3(Camera.main.transform.position.x,0,Camera.main.transform.position.z);
                var p2 = new Vector3(transform.position.x,0,transform.position.z);
                if (p1.Distance(p2) < detectionRange || hasTarget && alwaysPursueAfterDetection) {
                    isEnabled = false;
                    hasTarget = true;
                    onEvent?.Invoke(_target);
                }
            }
        }
    }

}
