namespace SuperAdventureLand
{
    using System.Linq;
    using UnityEngine;

    public class InteractiveInflatableGenerator : MonoBehaviour
    {
        public Transform rootBone;
        public float boneRadius = 0.02f;
        public float colliderSpacing = 0.01f;
        public float spring = 5000000f;
        public float maxForce = 500f;
        public bool archway = false;
        public int clipEnd = 0;

        public void GenerateColliders()
        {
            if (!rootBone)
                return;

            Transform[] bones = rootBone.GetComponentsInChildren<Transform>();
            bones = bones.Where(b => !b.name.EndsWith("_end")).ToArray();
            bones = bones.Where(b => b != rootBone).ToArray();

            if (archway)
            {
                Transform[] chain1 = bones.Take(bones.Length / 2).ToArray();
                Transform[] chain2 = bones.Skip(bones.Length / 2).ToArray();
                JointChain(chain1);
                JointChain(chain2);

                // Connect the ends of the chains with a hinge
                Transform lastBone1 = chain1[chain1.Length - 1];
                Transform lastBone2 = chain2[chain2.Length - 1];

                Componentizer.DoComponent<HingeJoint>(lastBone1.gameObject, true).connectedBody = lastBone2.GetComponent<Rigidbody>();
                Componentizer.DoComponent<HingeJoint>(lastBone2.gameObject, true).connectedBody = lastBone1.GetComponent<Rigidbody>();

                maxForce = 100f;
            }
            else
            {
                JointChain(bones);
            }
        }

        public void JointChain(Transform[] bones)
        {
            for (var i = 0; i < bones.Length - clipEnd; i++)
            {
                Transform bone = bones[i];

                if (bone.childCount == 0)
                    continue;

                Transform child = bone.GetChild(0);
                float scaleFactor = rootBone.lossyScale.y;
                Vector3 direction = child.position - bone.position;
                float length = direction.magnitude / scaleFactor;

                if (length < 0.001f)
                    continue;

                BoxCollider collider = Componentizer.DoComponent<BoxCollider>(bone.gameObject, true);
                collider.size = new Vector3(boneRadius, Mathf.Max(0f, length - colliderSpacing), boneRadius);
                collider.center = new Vector3(0, length / 2, 0);

                Rigidbody rb = Componentizer.DoComponent<Rigidbody>(bone.gameObject, true);
                if (i == 0)
                {
                    rb.isKinematic = true;
                }
                else
                {
                    ConfigurableJoint joint = Componentizer.DoComponent<ConfigurableJoint>(bone.gameObject, true);
                    joint.connectedBody = bones[i - 1].GetComponent<Rigidbody>();

                    joint.xMotion = ConfigurableJointMotion.Locked;
                    joint.yMotion = ConfigurableJointMotion.Locked;
                    joint.zMotion = ConfigurableJointMotion.Locked;

                    joint.angularXMotion = ConfigurableJointMotion.Limited;
                    joint.angularYMotion = ConfigurableJointMotion.Limited;
                    joint.angularZMotion = ConfigurableJointMotion.Locked;

                    joint.angularXLimitSpring = new SoftJointLimitSpring
                    {
                        spring = 1f,
                        damper = 0f
                    };

                    joint.lowAngularXLimit = new SoftJointLimit
                    {
                        limit = 30f,
                        bounciness = 1f,
                        contactDistance = 0f
                    };
                    joint.highAngularXLimit = new SoftJointLimit
                    {
                        limit = 150f,
                        bounciness = 1f,
                        contactDistance = 0f
                    };
                    joint.angularYZLimitSpring = new SoftJointLimitSpring
                    {
                        spring = 1f,
                        damper = 0f
                    };
                    joint.angularYLimit = new SoftJointLimit
                    {
                        limit = 60f,
                        bounciness = 0f,
                        contactDistance = 0f
                    };
                    joint.angularZLimit = new SoftJointLimit
                    {
                        limit = 0f,
                        bounciness = 1f,
                        contactDistance = 0f
                    };

                    joint.angularXDrive = new JointDrive
                    {
                        positionSpring = spring,
                        positionDamper = 0f,
                        maximumForce = maxForce,
                        useAcceleration = true
                    };
                    joint.angularYZDrive = new JointDrive
                    {
                        positionSpring = spring,
                        positionDamper = 0f,
                        maximumForce = maxForce,
                        useAcceleration = true
                    };

                    joint.projectionMode = JointProjectionMode.PositionAndRotation;
                    joint.projectionDistance = 0.01f;
                    joint.projectionAngle = 1f;
                }
                rb.useGravity = false;
                rb.linearDamping = 0f;
                rb.angularDamping = 1f;
                rb.sleepThreshold = 0f;
                rb.mass = bones.Length - i;
            }
        }

        public void OnValidate()
        {
            if (!Application.isPlaying)
                GenerateColliders();
        }
    }
}
