using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DreamPark {
    public class GameArea : MonoBehaviour
    {
        public static GameArea currentGameArea;
        [ReadOnly] public string gameId;
        public int priority = 0;
        private bool isPlaying = false;
        private Vector3 halfExtents = Vector3.zero;
        void Awake()
        {
            if (TryGetComponent(out LevelTemplate levelTemplate)) {
                var bounds2D = GameLevelDimensions.GetDimensionsInMeters(levelTemplate.size);
                halfExtents = new Vector3(bounds2D.x/2f, 50f, bounds2D.y/2f);
            }
        } 
        void Update () {
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
        public void Enter()
        {
            if (currentGameArea) {
                if (priority > currentGameArea.priority) {
                    currentGameArea.Exit();
                } else {
                    return;
                }
            }

            if (PlayerRig.instances != null && PlayerRig.instances.ContainsKey(gameId))
            {
                PlayerRig.instances[gameId].Show();
            }

            if (DreamBand.instances != null && DreamBand.instances.ContainsKey(gameId))
            {
                DreamBand.instances[gameId].Show();
            }

            isPlaying = true;
        }
        public void Exit()
        {
            isPlaying = false;
        }
        bool IsPointWithinBounds(Vector3 point, Transform obj, Vector3 halfExtents)
        {
            Vector3 localPoint = obj.InverseTransformPoint(point);
            return Mathf.Abs(localPoint.x) <= halfExtents.x &&
                Mathf.Abs(localPoint.y) <= halfExtents.y &&
                Mathf.Abs(localPoint.z) <= halfExtents.z;
        }
        void OnDestroy() {
            if (currentGameArea == this) {
                currentGameArea = null;
            }
        }
    }
}