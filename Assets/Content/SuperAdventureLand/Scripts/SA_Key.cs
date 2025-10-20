namespace SuperAdventureLand
{
    using UnityEngine;

    public class SA_Key : SA_PowerUp
    {
        public override void ExecuteState()
        {
            switch (state)
            {
                case SA_PowerUpState.USE:
                    TrackJunk("ogParent", transform.gameObject);
                    TrackJunk("ogPosition", transform.localPosition);
                    TrackJunk("ogRotation", transform.localRotation);
                    TrackJunk("KeyHole", lastCollision.collider.gameObject);
                    transform.SetParent(null,true);
                    "secret".PlaySFX(transform.position);
                    break;
                case SA_PowerUpState.USING:
                    rb.isKinematic = true;
                    transform.SetParent(null,true);
                    transform.position = Vector3.MoveTowards(transform.position, GetJunk<GameObject>("KeyHole").transform.position, Time.deltaTime*3f);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, GetJunk<GameObject>("KeyHole").transform.rotation, Time.deltaTime*200f);
                    transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one*2f, Time.deltaTime*10f);

                    if (transform.position.Distance(GetJunk<GameObject>("KeyHole").transform.position) <= 0.1f && Quaternion.Angle(transform.rotation, GetJunk<GameObject>("KeyHole").transform.rotation) <= 0.1f) {
                        SetState(SA_PowerUpState.KEY_ANIM);
                    }
                    break;
                case SA_PowerUpState.KEY_ANIM:
                    transform.rotation = GetJunk<GameObject>("KeyHole").transform.rotation;
                    break;
                case SA_PowerUpState.KEY_ANIMING:

                    if (stateWillChange) {
                        if (HasJunk("ogParent")) {
                            transform.SetParent(GetJunk<GameObject>("ogParent").transform,true);
                            transform.localPosition = GetJunk<Vector3>("ogPosition");
                            transform.localRotation = GetJunk<Quaternion>("ogRotation");
                            UntrackJunk("ogParent");
                        }
                        UntrackJunk("ogPosition");
                        UntrackJunk("ogRotation");
                        UntrackJunk("KeyHole");
                        UntrackJunk("steamVfx");
                        break;
                    }

                    if (!HasJunk("KeyHole")) {
                        SetState(SA_PowerUpState.IDLE);
                        break;
                    }

                    if (timeSinceStateChange < 0.5f) {
                        break;
                    } else if (timeSinceStateChange < 0.7f) {
                        transform.rotation = Quaternion.Lerp(transform.rotation, GetJunk<GameObject>("KeyHole").transform.rotation * Quaternion.Euler(90, 0, 0), timeSinceStateChange-0.5f);
                        break;
                    } else if (timeSinceStateChange < 1.2f) {
                        if (!HasJunk("steamVfx")) {
                            "FX_SteamPuff".GetAsset<GameObject>(steamPrefab => {
                                GameObject steam = Instantiate(steamPrefab);
                                steam.transform.position = transform.position + GetJunk<GameObject>("KeyHole").transform.right * 0.3f;
                                steam.transform.rotation = Quaternion.LookRotation(steam.transform.position - Camera.main.transform.position);
                            }, error => {
                                Debug.LogError($"Failed to load steam puff: {error}");
                            });
                            gameObject.HideVisual();
                            TrackJunk("steamVfx", true);
                        }
                    } else {
                        GetJunk<GameObject>("KeyHole").GetComponentInParent<KeyHoleBehaviour>().Unlock();
                        SetState(SA_PowerUpState.DEACTIVATE);
                    }
                    break;
                default:
                    base.ExecuteState();
                    break;
            }
        }

        public void KeyHoleHit (CollisionWrapper collision) {
            if (isActive && !isInUse) {
                lastCollision = collision;
                SetState(SA_PowerUpState.USE);
            }
        }

        public bool isInUse {
            get {
                return state == SA_PowerUpState.USE || state == SA_PowerUpState.USING || state == SA_PowerUpState.KEY_ANIM || state == SA_PowerUpState.KEY_ANIMING;
            }
        }

        public override bool PLAYER_INPUT {
            get {
                return false;
            }
        }
    }
}
