namespace SuperAdventureLand.Scripts
{
    using System;
    using System.Collections;
    using UnityEngine;
    using Random = UnityEngine.Random;

    [ExecuteInEditMode]
    public class EyeController : MonoBehaviour
    {
        public Renderer eyes;
        [SerializeField] private string pupilPositionProperty = "_PupilPosition";
        [SerializeField] private string pupilSizeProperty = "_PupilSize";
        [SerializeField] private string eyeLidProperty = "_EyeLid";

        [SerializeField] private string eyeLidTiltProperty = "_EyeLidTilt";
        [SerializeField] private string eyeLidMagnitudeProperty = "_EyeLidMagnitude";
        [SerializeField] private string ellipseProperty = "_Ellipse";
        public Vector2 PupilPosition = Vector3.zero;
        [Range(0.01f, 1f)] public float PupilSize = 0.08f;
        [Range(0f, 2f)] public float Ellipse = 1f;
        [Range(0f, 0.5f)] public float EyeLid = 0.25f;
        [Range(-1f, 1f)] public float EyeLidTilt = 0f;
        [Range(0f, 1f)] public float LookAroundIntensity = 0.35f;
        public Vector2 EyeLidMagnitude = Vector2.one;
        public float BlinkSpeed = 15f;
        public float EyeSpeed = 5f;

        public enum EyeState { None, Blink, LookAround, LookAtPlayer }
        public enum EmotionState { None, Bored, Surprise, Happy, Angry, Sad }
        public EyeState eyeState = EyeState.None;
        public EmotionState emotionState = EmotionState.None;
        private bool EyeLidOverride = false;
        private Material eyeMaterial;
        private Coroutine eyeStateCoroutine;
        private Coroutine emotionStateCoroutine;
        private EyeState _eyeState;
        private EmotionState _emotionState;
        private float lastBlinkTime;
        private float blinkInterval = 2f;
        private EyeState defaultEyeState;
        private float ogPupilSize;
        private Vector2 ogEyeLidMagnitude;

        private void Awake()
        {
            _eyeState = eyeState;
            _emotionState = emotionState;
            defaultEyeState = eyeState;
            ogPupilSize = PupilSize;
            ogEyeLidMagnitude = EyeLidMagnitude;
        }

        private void Start()
        {
            if (!Application.isPlaying) {
                return;
            }
            if (eyes != null)
            {
                eyeMaterial = eyes.material;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) {
                return;
            }
            if (eyeState != _eyeState) {
                if (eyeStateCoroutine != null) {
                    StopCoroutine(eyeStateCoroutine);
                    eyeStateCoroutine = null;
                }
                _eyeState = eyeState;
                if (eyeState != EyeState.Blink) {
                    defaultEyeState = eyeState;
                }
            }

            if (emotionState != _emotionState) {
                if (emotionStateCoroutine != null) {
                    StopCoroutine(emotionStateCoroutine);
                    emotionStateCoroutine = null;
                }
                _emotionState = emotionState;

            }

            switch (eyeState)
            {
                case EyeState.Blink:
                    Blink();
                    break;
                case EyeState.LookAround:
                    LookAround();
                    break;
                case EyeState.LookAtPlayer:
                    LookAtPlayer();
                    break;
            }

            switch (emotionState)
            {
                case EmotionState.None:
                    Normal();
                    break;
                case EmotionState.Bored:
                    Bored();
                    break;
                case EmotionState.Surprise:
                    Surprise();
                    break;
                case EmotionState.Happy:
                    Happy();
                    break;
                case EmotionState.Angry:
                    Angry();
                    break;
                case EmotionState.Sad:
                    Sad();
                    break;
            }

            if (eyeMaterial != null)
            {
                eyeMaterial.SetVector(pupilPositionProperty, PupilPosition);
                eyeMaterial.SetFloat(pupilSizeProperty, PupilSize);
                eyeMaterial.SetFloat(eyeLidProperty, EyeLid);
                eyeMaterial.SetFloat(eyeLidTiltProperty, EyeLidTilt);
                eyeMaterial.SetVector(eyeLidMagnitudeProperty, EyeLidMagnitude);
            }

            if (Time.time - lastBlinkTime > blinkInterval) {
                lastBlinkTime = Time.time;
                blinkInterval = Random.Range(0.5f, 3f);
                eyeState = EyeState.Blink;
            }
        }

        private void Blink()
        {
            if (eyeStateCoroutine != null) {
                return;
            }
            eyeStateCoroutine = StartCoroutine(BlinkCoroutine());
        }

        private IEnumerator BlinkCoroutine()
        {

            float _EyeLid = EyeLid;
            Vector2 _EyeLidMagnitude = EyeLidMagnitude;
            EyeLidOverride = true;

            while (EyeLid < 0.5f || EyeLidMagnitude.y < 0.99f) {
                EyeLidMagnitude = Vector2.Lerp(EyeLidMagnitude, Vector2.one, Time.deltaTime * BlinkSpeed);
                EyeLid = Mathf.Lerp(EyeLid, 0.51f, Time.deltaTime * BlinkSpeed);
                yield return null;
            }

            while (EyeLid > _EyeLid || EyeLidMagnitude.y > _EyeLidMagnitude.y+0.01f) {
                EyeLidMagnitude = Vector2.Lerp(EyeLidMagnitude, _EyeLidMagnitude, Time.deltaTime * BlinkSpeed);
                EyeLid = Mathf.Lerp(EyeLid, _EyeLid-0.01f, Time.deltaTime * BlinkSpeed);
                yield return null;
            }
            EyeLid = _EyeLid;
            EyeLidMagnitude = _EyeLidMagnitude*ogEyeLidMagnitude;

            EyeLidOverride = false;
            eyeStateCoroutine = null;

            //set eye to whatever the default state is
            eyeState = defaultEyeState;
        }

         private void LookAround()
        {
            if (eyeStateCoroutine != null) {
                return;
            }
            eyeStateCoroutine = StartCoroutine(LookAroundCoroutine());
        }

        private IEnumerator LookAroundCoroutine()
        {
            while (true) {
                Vector2 targetPosition = new Vector2(
                    Random.Range(-LookAroundIntensity, LookAroundIntensity),
                    Random.Range(-LookAroundIntensity/2, LookAroundIntensity/2)*(1-EyeLid)
                );

                float elapsedTime = 0f;
                float duration = 0.2f;
                Vector2 startPosition = PupilPosition;

                while (elapsedTime < duration) {
                    PupilPosition = Vector2.Lerp(startPosition, targetPosition, elapsedTime / duration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                PupilPosition = targetPosition;
                yield return new WaitForSeconds(Random.Range(0.4f,2f));
            }
        }

        private void LookAtPlayer()
        {
            Vector3 playerPos = Camera.main.transform.position;
            Vector3 directionToPlayer = (playerPos - transform.position).normalized;
            Vector3 eyeForward = transform.forward;

            // Get dot product between forward and direction to player
            float dotProduct = Vector3.Dot(directionToPlayer, eyeForward);

            // Map the -1 to 1 range to our pupil position range (-0.5 to 0.5)
            PupilPosition.x = dotProduct * 0.5f;

            // Do the same for up/down using the dot product with up vector
            float upDot = Vector3.Dot(directionToPlayer, transform.up);
            PupilPosition.y = 0.1f + upDot * -0.25f;
        }

        private void Normal () {
            SetEyeParameters(0.25f, 0, CalculatePupilRatio(1f), Vector2.one, EyeSpeed);
        }

        private void Bored() {
            SetEyeParameters(0.4f, 0f, CalculatePupilRatio(0.875f), new Vector2(1f,0f), EyeSpeed);
        }

        private void Angry()
        {
            SetEyeParameters(0.45f, 0.42f, CalculatePupilRatio(0.875f), Vector2.one, EyeSpeed);
        }

        private void Happy()
        {
            SetEyeParameters(0, 0, CalculatePupilRatio(1.25f), Vector2.one, EyeSpeed);
        }

        private void Surprise()
        {
            SetEyeParameters(0, 0, CalculatePupilRatio(0.875f), Vector2.one, EyeSpeed);
        }

        private void Sad()
        {
            SetEyeParameters(0.45f, -0.42f, CalculatePupilRatio(0.875f), Vector2.one, EyeSpeed);
        }

        public float CalculatePupilRatio (float ratio) {
            return ogPupilSize * ratio;
        }

        private void SetEyeParameters(float eyeLid, float eyeLidTilt, float pupilSize, Vector2? eyeLidMagnitude = null, float? speed = null) {
            if (speed == null) speed = EyeSpeed;
            if (eyeLidMagnitude == null) eyeLidMagnitude = Vector2.one;

             EyeLidTilt = Mathf.Lerp(EyeLidTilt, eyeLidTilt, Time.deltaTime * speed.Value);
             if (!EyeLidOverride) {
                EyeLid = Mathf.Lerp(EyeLid, eyeLid, Time.deltaTime * speed.Value);
                EyeLidMagnitude = Vector2.Lerp(EyeLidMagnitude, eyeLidMagnitude.Value*ogEyeLidMagnitude, Time.deltaTime * speed.Value);
             }
             PupilSize = Mathf.Lerp(PupilSize, pupilSize, Time.deltaTime * speed.Value);
        }

        public void OnValidate()
        {
            if (Application.isPlaying) {
                return;
            }
            if (eyes != null) {
                eyeMaterial = eyes.sharedMaterial;
                if (eyeMaterial != null) {
                    eyeMaterial.SetVector(pupilPositionProperty, PupilPosition);
                    eyeMaterial.SetFloat(pupilSizeProperty, PupilSize);
                    eyeMaterial.SetFloat(eyeLidProperty, EyeLid);
                    eyeMaterial.SetFloat(eyeLidTiltProperty, EyeLidTilt);
                    eyeMaterial.SetVector(eyeLidMagnitudeProperty, EyeLidMagnitude);
                    eyeMaterial.SetFloat(ellipseProperty, Ellipse);
                }
            }
        }
    }
}
