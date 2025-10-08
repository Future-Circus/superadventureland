using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Defective.JSON;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public static class Extensions
{
    public static Quaternion StringToQuaternion(this string rotationString)
    {
        string[] values = rotationString.TrimStart('(').TrimEnd(')').Split(',');

        if (values.Length != 4)
        {
            Debug.LogError("Invalid rotation string format");
            return Quaternion.identity;
        }

        float x, y, z, w;
        if (float.TryParse(values[0], out x) &&
            float.TryParse(values[1], out y) &&
            float.TryParse(values[2], out z) &&
            float.TryParse(values[3], out w))
        {
            return new Quaternion(x, y, z, w);
        }
        else
        {
            Debug.LogError("Failed to parse rotation values");
            return Quaternion.identity;
        }
    }

    public static Vector3 UniformScale(this Vector3 vector3, float multiplyBy)
    {
        return vector3.UniformScale(multiplyBy);
    }

    public static int MakeInt(this string s)
    {
        if (int.TryParse(s, out int i))
        {
            return i;
        }
        else
        {
            return 0;
        }
    }

    public static float MakeFloat(this string s)
    {
        if (float.TryParse(s, out float f))
        {
            return f;
        }
        else
        {
            return 0;
        }
    }

    public static bool MakeBool(this string s)
    {
        if (bool.TryParse(s, out bool b))
        {
            return b;
        }
        else
        {
            return false;
        }
    }

    public static long MakeLong(this string s)
    {
        if (long.TryParse(s, out long l))
        {
            return l;
        }
        else
        {
            return 0;
        }
    }

    public static Vector3 Midpoint(this Vector3 a, Vector3 b)
    {
        return (a + b) / 2f;
    }

    public static Vector3 PerpendicularDirection(this Vector3 fromPoint, Vector3 toPoint)
    {
        Vector3 direction = -(toPoint - fromPoint).normalized;
        Vector3 perpendicular = Vector3.Cross(Vector3.up, direction).normalized;

        // If the resulting perpendicular vector is close to zero, use a different up direction
        if (perpendicular.sqrMagnitude < 0.001f)
            perpendicular = Vector3.Cross(Vector3.forward, direction).normalized;

        return perpendicular;
    }

    public static float Difference(this float x, float y)
    {
        return Math.Abs(x - y);
    }

    public static float CalculateDegreeDifference(this float currentAngle, float targetAngle)
    {
        float difference = currentAngle - targetAngle;
        // Adjust the difference when it is greater than 180 (to go the "short way" around the circle)
        difference = (difference + 180) % 360 - 180;
        // If the difference is less than -180, adjust to take the shortest path
        return difference < -180 ? difference + 360 : difference;
    }

    public static float Distance(this Vector3 pos1, Vector3 pos2)
    {
        return Vector3.Distance(pos1, pos2);
    }

    public static AudioSource PlaySFX(this AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, Transform parent = null)
    {
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;
        AudioSource audioSource = tempGO.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 1;
        audioSource.maxDistance = 10;
        audioSource.pitch = pitch;
        tempGO.AddComponent<RealisticRolloff>();
        audioSource.Play();
        if (parent != null)
        {
            tempGO.transform.SetParent(parent);
            tempGO.transform.localPosition = Vector3.zero;
            tempGO.transform.localRotation = Quaternion.identity;
            audioSource.loop = true;
        }
        else
        {
            UnityEngine.Object.Destroy(tempGO, clip.length);
        }
        return audioSource;
    }

    public static AudioSource PlaySFX(this string clipName, Vector3 position, float volume = 1f, float pitch = 1f, Transform parent = null)
    {
        AudioClip clip = Resources.Load<AudioClip>(clipName);
        if (clip == null)
        {
            Debug.LogWarning($"Audio clip '{clipName}' not found in Resources");
            return null;
        }
        return clip.PlaySFX(position, volume, pitch);
    }

    public static ParticleSystem PlayVFX(this string vfxName, Vector3 position, Quaternion rotation = default, Transform parent = null, float durationOverride = -1f)
    {
        ParticleSystem vfxPrefab = Resources.Load<ParticleSystem>(vfxName);
        if (vfxPrefab == null)
        {
            Debug.LogWarning($"VFX '{vfxName}' not found in Resources");
            return null;
        }

        // Instantiate the VFX at the given position and rotation, and optionally set parent
        ParticleSystem instance = UnityEngine.Object.Instantiate(vfxPrefab, position, rotation == default ? vfxPrefab.transform.rotation : rotation, parent);

        instance.Play();

        // Destroy after the system's duration unless overridden
        float lifetime = durationOverride > 0f ? durationOverride : instance.main.duration + instance.main.startLifetime.constantMax;
        UnityEngine.Object.Destroy(instance.gameObject, lifetime);

        return instance;
    }

    public static void SetParentAndCenter(this Transform child, Transform parent)
    {
        child.position = parent.position;
        child.rotation = parent.rotation;
        child.SetParent(parent, true);
    }

    public static void PlayWithRandomPitch(this AudioSource audioSource, float minPitch = 0.8f, float maxPitch = 1.2f)
    {
        audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
        audioSource.Play();
    }

    public static void PlayWithRandomPitch(this AudioSource audioSource, AudioClip clip, float minPitch = 0.8f, float maxPitch = 1.2f)
    {
        audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
        audioSource.clip = clip;
        audioSource.Play();
    }

    public static bool TryGetComponentInParent<T>(this GameObject gameObject, out T result) where T : Component
    {
        if (gameObject.transform.parent == null)
        {
            result = null;
            return false;
        }

        // Start with the parent of the current component's transform
        Transform currentTransform = gameObject.transform.parent;

        // Traverse up the hierarchy until we either find the component or run out of parents
        while (currentTransform != null)
        {
            // Try to get the component from the current transform
            if (currentTransform.TryGetComponent(out result))
            {
                return true; // Component found
            }

            // Move to the next parent in the hierarchy
            currentTransform = currentTransform.parent;
        }

        // Component not found in any parent
        result = null;
        return false;
    }

    public static bool TryGetComponentInChildren<T>(this GameObject gameObject, out T component) where T : Component
    {
        component = gameObject.GetComponentInChildren<T>();
        return component != null;
    }

    public static Transform FindRecursive(this Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = child.FindRecursive(name);
            if (result != null)
                return result;
        }

        return null;
    }
    public static List<T> GetComponentsInChildrenUntil<T, TStop>(this GameObject parent, bool includeSelf = false) where T : Component where TStop : Component
    {
        List<T> results = new List<T>();
        if (includeSelf)
        {
            T component = parent.GetComponent<T>();
            if (component != null)
            {
                results.Add(component);
            }
        }
        GetComponentsInChildrenUntilRecursive<T, TStop>(parent.transform, results);
        return results;
    }

    private static void GetComponentsInChildrenUntilRecursive<T, TStop>(Transform parent, List<T> results) where T : Component where TStop : Component
    {
        foreach (Transform child in parent)
        {

            // If the child has the target component, add it to the results list
            T component = child.GetComponent<T>();
            if (component != null)
            {
                results.Add(component);
            }

            // If the child has the stop component, stop searching this branch
            if (child.GetComponent<TStop>() != null)
            {
                continue;
            }

            // Recursively search this child's children
            GetComponentsInChildrenUntilRecursive<T, TStop>(child, results);
        }
    }

    public static void BroadcastMessageUntil<TStop>(this GameObject parent, string methodName, System.Object parameter = null, SendMessageOptions options = SendMessageOptions.DontRequireReceiver)
    {
        parent.SendMessage(methodName, parameter, options);
        BroadcastMessageUntilRecursive<TStop>(parent.transform, methodName, parameter, options);
    }

    private static void BroadcastMessageUntilRecursive<TStop>(Transform parent, string methodName, System.Object parameter, SendMessageOptions options)
    {
        foreach (Transform child in parent)
        {
            // If the child has the stop component, stop broadcasting down this branch
            if (child.GetComponent<TStop>() != null)
            {
                continue;
            }

            // Broadcast the message to this child
            child.gameObject.SendMessage(methodName, parameter, options);

            // Recursively broadcast to the child's children
            BroadcastMessageUntilRecursive<TStop>(child, methodName, parameter, options);
        }
    }


    public static void CancelOnAnimationComplete(this Animator animator, Coroutine c)
    {
        animator.GetComponent<MonoBehaviour>().StopCoroutine(c);
    }

    public static void OnAnimationComplete(this Animator animator, Action onComplete, out Coroutine c, string animationName = null)
    {
        if (!animator.gameObject.activeInHierarchy || !animator.enabled)
        {
            c = null;
            return;
        }
        c = animator.GetComponent<MonoBehaviour>().StartCoroutine(WaitForAnimation(animator, onComplete, animationName));
    }

    private static IEnumerator WaitForAnimation(Animator animator, Action onComplete, string animationName)
    {
        // Wait until any animation state is no longer transitioning
        while (animator.IsInTransition(0))
        {
            yield return null;
        }

        // Determine the animation name if not provided
        if (string.IsNullOrEmpty(animationName))
        {
            if (animator.GetCurrentAnimatorClipInfo(0).Length > 0)
            {
                animationName = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
            }
        }

        if (string.IsNullOrEmpty(animationName))
        {
            if (animator.GetNextAnimatorClipInfo(0).Length > 0)
            {
                animationName = animator.GetNextAnimatorClipInfo(0)[0].clip.name;
            }
        }

        if (string.IsNullOrEmpty(animationName))
        {
            yield break;
        }

        if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1f)
        {
            animator.playbackTime = 0;
            yield return null;
        }

        // Get the hash of the current animation state to detect changes
        int currentAnimationHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            if (animator.IsInTransition(0))
            {
                yield break;
            }

            // Break if a new animation starts
            if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash != currentAnimationHash)
            {
                yield break;
            }

            yield return null;
        }

        // Trigger the callback action
        onComplete?.Invoke();

        yield return null;
    }

    public static bool TryFindObjectOfType<T>(this GameObject gameObject, out T obj) where T : Component
    {
        obj = GameObject.FindFirstObjectByType<T>();
        return obj != null;
    }

    public static Vector3 LocalToWorldPosition(this Transform childTransform, Vector3 localPosition)
    {
        return childTransform.TransformPoint(localPosition);
    }

    public static string CleanName(this string gameObjectName)
    {
        // Regular expression to find "(Clone)" or any " (number)" at the end of the name
        System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"(\(Clone\)\s\(\d+\))|(\(Clone\))$");
        return regex.Replace(gameObjectName, "").Trim();
    }

    public static void ToggleVisibility(this GameObject gameObject, bool visible = true)
    {
        foreach (Renderer r in gameObject.GetComponentsInChildren<Renderer>())
        {
            r.enabled = visible;
        }
        foreach (Collider c in gameObject.GetComponentsInChildren<Collider>())
        {
            c.enabled = visible;
        }
    }

    public static string ToCommaString(this int number)
    {
        return number.ToString("N0", CultureInfo.InvariantCulture);
    }

    public static Dictionary<AudioSource, Coroutine> fadeCoroutines = new Dictionary<AudioSource, Coroutine>();

    public static void PlayWithFadeIn(this AudioSource audioSource, float duration, MonoBehaviour monoBehaviour, float startVolume = -1f)
    {
        if (fadeCoroutines.ContainsKey(audioSource))
        {
            monoBehaviour.StopCoroutine(fadeCoroutines[audioSource]);
            fadeCoroutines.Remove(audioSource);
        }
        fadeCoroutines.Add(audioSource, monoBehaviour.StartCoroutine(FadeIn(audioSource, duration, startVolume)));
    }

    public static void PauseWithFadeOut(this AudioSource audioSource, float duration, MonoBehaviour monoBehaviour, float startVolume = -1f)
    {
        if (fadeCoroutines.ContainsKey(audioSource))
        {
            monoBehaviour.StopCoroutine(fadeCoroutines[audioSource]);
            fadeCoroutines.Remove(audioSource);
        }
        fadeCoroutines.Add(audioSource, monoBehaviour.StartCoroutine(FadeOutAndPause(audioSource, duration, startVolume)));
    }

    private static IEnumerator FadeIn(AudioSource audioSource, float duration, float startVolume = -1f)
    {
        float currentTime = 0;
        if (startVolume < 0)
            startVolume = audioSource.volume;
        audioSource.volume = 0;
        audioSource.Play();

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Clamp01(startVolume * (currentTime / duration));
            yield return null;
        }
    }

    private static IEnumerator FadeOutAndPause(AudioSource audioSource, float duration, float startVolume = -1f)
    {
        float currentTime = 0;
        if (startVolume < 0)
            startVolume = audioSource.volume;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Clamp01(startVolume * (1 - currentTime / duration));
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    public static void IterateAll<T>(this T[] monoBehaviour, Action<T> action)
    {
        foreach (T mb in monoBehaviour)
        {
            action(mb);
        }
    }

    public static LayerMask AddLayer(this LayerMask layerMask, int layer)
    {
        layerMask |= (1 << layer); // Use bitwise OR to add the layer to the mask
        return layerMask;
    }

    // Remove a layer from exclusion
    public static LayerMask RemoveLayer(this LayerMask layerMask, int layer)
    {
        layerMask &= ~(1 << layer); // Use bitwise AND with NOT to remove the layer
        return layerMask;
    }

    public static void InvokeMethodWithReflection(this MonoBehaviour target, string methodName, params object[] parameters)
    {
        //Debug.Log("InvokeMethodWithReflection: " + methodName + " " + (parameters[0] as GameObject).name);

        if (target == null)
        {
            goto skipInvoke;
        }

        var method = target.GetType().GetMethod(methodName);

        if (method != null)
        {
            if (target.GetType().GetMethod(methodName).GetParameters().Length > 0)
            {
                method.Invoke(target, parameters);
            }
            else
            {
                method.Invoke(target, null);
            }
        }
        else
        {
            Debug.LogError($"Method {methodName} not found on {target.GetType()}");
        }

    skipInvoke:;
    }

    public static Transform FindObjectInScene(this Scene scene, string name)
    {
        // Get all root GameObjects in the scene
        GameObject[] rootObjects = scene.GetRootGameObjects();

        // Iterate through all root objects to find the desired GameObject
        foreach (GameObject rootObject in rootObjects)
        {
            // Recursively search for the object with the given name
            Transform found = FindInChildren(rootObject.transform, name);
            if (found != null)
                return found;
        }

        return null;
    }

    public static Transform FindInChildren(Transform parent, string name)
    {
        // Check the parent itself
        if (parent.name == name)
            return parent;

        // Recursively search in children
        foreach (Transform child in parent)
        {
            Transform found = FindInChildren(child, name);
            if (found != null)
                return found;
        }

        return null;
    }

    public static void OnAudioComplete(this AudioSource audioSource, Action onComplete)
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is null.");
            return;
        }

        if (onComplete == null)
        {
            Debug.LogError("onComplete action is null.");
            return;
        }

        if (!audioSource.gameObject.activeInHierarchy)
        {
            Debug.LogError("AudioSource's GameObject is not active in the hierarchy.");
            return;
        }

        Componentizer.DoComponent<MonoBehaviour>(audioSource.gameObject, true)
        .StartCoroutine(TrackAudioCompletion(audioSource, onComplete));
    }

    public static void OnAudioFrame(this AudioSource audioSource, float time, Action onComplete)
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is null.");
            return;
        }

        if (onComplete == null)
        {
            Debug.LogError("onComplete action is null.");
            return;
        }

        if (!audioSource.gameObject.activeInHierarchy)
        {
            Debug.LogError("AudioSource's GameObject is not active in the hierarchy.");
            return;
        }

        Componentizer.DoComponent<MonoBehaviour>(audioSource.gameObject, true)
        .StartCoroutine(TrackAudioFrame(audioSource, time, onComplete));
    }

    private static IEnumerator TrackAudioFrame(AudioSource audioSource, float time, Action onComplete)
    {
        while (audioSource.time < time)
        {
            yield return null;
        }

        onComplete?.Invoke();
    }

    private static IEnumerator TrackAudioCompletion(AudioSource audioSource, Action onComplete)
    {
        while (audioSource.isPlaying)
        {
            yield return null;
        }

        onComplete?.Invoke();
    }

    private static GameObject EvaluateColliders(GameObject gameobject, Collider[] colliders, ref float highestScore, bool isEntityLayer, GameObject currentBestTarget = null)
    {
        GameObject bestTarget = currentBestTarget;
        Transform player = Camera.main.transform;

        foreach (Collider collider in colliders)
        {
            string[] filteredTags = { gameobject.tag, "ActiveHit", "Ground", "Stone", "", "Item" };

            // Dont target objects if it has one of these tags
            if (filteredTags.Contains(collider.gameObject.tag))
            {
                continue;
            }

            // Check if the object is in front of the player
            Vector3 toTarget = collider.transform.position - player.position;

            // Project both vectors onto the ground plane (ignore the height)
            Vector3 forwardOnGround = new Vector3(player.forward.x, 0, player.forward.z).normalized;
            Vector3 toTargetOnGround = new Vector3(toTarget.x, 0, toTarget.z).normalized;
            float dotProduct = Vector3.Dot(forwardOnGround, toTargetOnGround);

            if (dotProduct < 0.7f)
            {
                continue; // Ignore targets outside the field of view on the ground plane
            }

            // Calculate distance score (closer objects are better)
            float distance = Vector3.Distance(gameobject.transform.position, collider.transform.position);
            float distanceScore = 1 / distance;

            // Add layer bonus if it's on the "Entity" layer
            float layerBonus = (isEntityLayer && collider.gameObject.tag != "Fence") ? 2f : 0f;

            // Combine distance score and layer bonus
            float totalScore = distanceScore + layerBonus;

            // Prioritize objects with the highest total score
            if (totalScore > highestScore)
            {
                highestScore = totalScore;
                bestTarget = collider.gameObject;
            }
        }

        return bestTarget;
    }

    public static Vector3 CalculateLobVelocity(Vector3 start, Vector3 target, float height)
    {
        var gravity = 9.81f;
        // Displacement in XZ plane
        Vector3 displacementXZ = new Vector3(target.x - start.x, 0, target.z - start.z);

        // Height difference between the start and target
        float verticalDisplacement = target.y - start.y;

        // Ensure height is positive and higher than the target position
        float peakHeight = Mathf.Max(height, verticalDisplacement + 0.1f); // Add a small buffer to avoid negative sqrt

        // Time to reach the apex (vertical motion)
        float timeToApex = Mathf.Sqrt(2 * peakHeight / gravity);

        // Total time for the projectile to reach the target
        float totalFlightTime = timeToApex + Mathf.Sqrt(2 * (peakHeight - verticalDisplacement) / gravity);

        if (float.IsNaN(totalFlightTime) || totalFlightTime <= 0)
        {
            Debug.LogError("Invalid flight time calculation. Check gravity and height settings.");
            return Vector3.zero;
        }

        // Velocity in XZ plane
        Vector3 velocityXZ = displacementXZ / totalFlightTime;

        // Velocity in Y (vertical motion)
        float velocityY = Mathf.Sqrt(2 * gravity * peakHeight);

        // Combine horizontal and vertical components
        return velocityXZ + Vector3.up * velocityY;
    }

    public static bool LaunchAtTarget(this Rigidbody gameObject, Vector3 targetPosition, float launchHeight = 1f, bool applyRandomTorque = true, string tag = "ActiveHit")
    {

        Transform transform = gameObject.transform;

        if (gameObject.linearDamping > 0.2f)
        {
            gameObject.linearDamping = 0.2f;
        }
        gameObject.angularDamping = 0.2f;
        gameObject.tag = tag;
        gameObject.constraints = RigidbodyConstraints.None;

        Vector3 velocity = CalculateLobVelocity(transform.position, targetPosition, launchHeight);
        gameObject.linearVelocity = velocity;

        if (applyRandomTorque)
        {
            Vector3 randomTorque = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f)
            );
            gameObject.AddTorque(randomTorque, ForceMode.Impulse);
        }
        return true;
    }

    public static bool LaunchAtTarget(this Rigidbody gameObject, GameObject target, float launchHeight = 1f, bool applyRandomTorque = true, string tag = "ActiveHit")
    {

        if (target != null)
        {
            Vector3 targetPosition = target.transform.position;
            if (target.TryGetComponent(out Collider collider))
            {
                targetPosition = collider.bounds.center;
            }
            return LaunchAtTarget(gameObject, targetPosition, launchHeight, applyRandomTorque, tag);
        }
        return false;
    }

    public static bool LaunchAtLayer(this Rigidbody gameObject, LayerMask priorityLayer, float priorityDist = 20f, LayerMask fallbackLayer = default, float fallbackDist = 10f)
    {

        float highestScore = Mathf.NegativeInfinity;
        GameObject bestTarget;

        Collider[] levelColliders = Physics.OverlapSphere(gameObject.transform.position, fallbackDist, 1 << (fallbackLayer == default ? LayerMask.NameToLayer("Level") : fallbackLayer));
        Collider[] entityColliders = Physics.OverlapSphere(gameObject.transform.position, priorityDist, 1 << priorityLayer);

        // Debug.Log("found " + entityColliders.Length + " " + LayerMask.LayerToName(priorityLayer));
        // Debug.Log("found " + levelColliders.Length + " " + LayerMask.LayerToName(fallbackLayer) + " (as backup)");

        bestTarget = EvaluateColliders(gameObject.gameObject, levelColliders, ref highestScore, false);
        bestTarget = EvaluateColliders(gameObject.gameObject, entityColliders, ref highestScore, true, bestTarget);

        if (bestTarget)
        {
            Debug.Log("LaunchAtLayer - Launching at " + bestTarget.name);
            return LaunchAtTarget(gameObject, bestTarget);
        }
        else
        {
            Debug.Log("LaunchAtLayer - No target found");
        }
        return false;
    }

    public static bool LaunchAtLayer(this Rigidbody gameObject, LayerMask priorityLayer, float priorityDist = 20f)
    {
        return LaunchAtLayer(gameObject, priorityLayer, priorityDist, LayerMask.NameToLayer("Level"), 10f);
    }

    public static bool LaunchAtLayerWithTag(this Rigidbody gameObject, LayerMask priorityLayer, string tagFilter, float priorityDist = 20f)
    {
        float highestScore = Mathf.NegativeInfinity;
        GameObject bestTarget = null;

        Collider[] entityColliders = Physics.OverlapSphere(gameObject.transform.position, priorityDist, 1 << priorityLayer);
        entityColliders = entityColliders.Where(c => tagFilter.Contains(c.gameObject.tag)).ToArray();
        bestTarget = EvaluateColliders(gameObject.gameObject, entityColliders, ref highestScore, true, bestTarget);

        if (bestTarget)
        {
            return LaunchAtTarget(gameObject, bestTarget);
        }
        return false;
    }

    public static bool LaunchInDirection(this Rigidbody gameObject, Vector3 direction, float force = 15f, float upwardForce = 8f, bool applyRandomTorque = true)
    {
        if (gameObject.linearDamping > 0.2f)
        {
            gameObject.linearDamping = 0.2f;
        }
        gameObject.angularDamping = 0.2f;
        gameObject.constraints = RigidbodyConstraints.None;

        Vector3 launchVelocity = direction * force + Vector3.up * upwardForce;
        gameObject.linearVelocity = launchVelocity;

        if (applyRandomTorque)
        {
            Vector3 randomTorque = new Vector3(
                UnityEngine.Random.Range(-2f, 2f),
                UnityEngine.Random.Range(-2f, 2f),
                UnityEngine.Random.Range(-2f, 2f)
            );
            gameObject.AddTorque(randomTorque, ForceMode.Impulse);
        }

        return true;
    }

    public static void ShakeEngine(this Transform shakeThis, float shakeIntensity = 0.05f, Vector3 shakeStartPos = default)
    {
        var shakeSpeed = 100f;
        var noiseSeed = 32f;
        var shakeDirection = new Vector3(0.2f, -0.2f, 1);
        // Generate perlin noise-based offsets
        float noiseX = Mathf.PerlinNoise(Time.time * shakeSpeed, noiseSeed) - 0.5f;
        float noiseY = Mathf.PerlinNoise(Time.time * shakeSpeed, noiseSeed + 1) - 0.5f;
        float noiseZ = Mathf.PerlinNoise(Time.time * shakeSpeed, noiseSeed + 2) - 0.5f;

        // Combine shake direction with noise
        Vector3 shakeOffset = new Vector3(
            noiseX * shakeDirection.x,
            noiseY * shakeDirection.y,
            noiseZ * shakeDirection.z
        ) * shakeIntensity;

        // Apply the shake
        shakeThis.localPosition = shakeStartPos + shakeOffset;
    }

    private static Dictionary<MonoBehaviour, Coroutine> _coroutines = new Dictionary<MonoBehaviour, Coroutine>();

    private static IEnumerator _Wait(this MonoBehaviour monoBehaviour, float duration, Action action)
    {
        yield return new WaitForSeconds(duration);
        action.Invoke();
    }

    public static Coroutine Wait(this MonoBehaviour monoBehaviour, float duration, Action action, bool replace = true)
    {
        if (replace && _coroutines.ContainsKey(monoBehaviour))
        {
            monoBehaviour.StopCoroutine(_coroutines[monoBehaviour]);
            _coroutines[monoBehaviour] = null;
            _coroutines.Remove(monoBehaviour);
        }
        var coroutine = monoBehaviour.StartCoroutine(_Wait(monoBehaviour, duration, action));
        if (replace)
        {
            _coroutines.Add(monoBehaviour, coroutine);
        }
        return coroutine;
    }
    private static Dictionary<MonoBehaviour, Coroutine> _animation_coroutines = new Dictionary<MonoBehaviour, Coroutine>();

    public static void StartJiggle(this Transform transform, MonoBehaviour monoBehaviour, float duration = 0.6f, float frequency = 3f, float amplitude = 0.25f, float decay = 5f)
    {
        if (_animation_coroutines.ContainsKey(monoBehaviour))
        {
            monoBehaviour.StopCoroutine(_animation_coroutines[monoBehaviour]);
            _animation_coroutines[monoBehaviour] = null;
            _animation_coroutines.Remove(monoBehaviour);
        }
        _animation_coroutines.Add(monoBehaviour, monoBehaviour.StartCoroutine(JiggleCoroutine(transform, duration, frequency, amplitude, decay)));
    }

    private static IEnumerator JiggleCoroutine(Transform transform, float duration, float frequency, float amplitude, float decay)
    {
        // Record the original scale
        Vector3 originalScale = transform.localScale;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Exponential decay to reduce amplitude over time
            float damper = Mathf.Exp(-decay * t);

            // Apply a sine wave for the jiggle, multiplied by the decaying factor
            float scaleFactor = 1f + amplitude * damper * Mathf.Sin(2f * Mathf.PI * frequency * t);

            // Update the object's scale
            transform.localScale = originalScale * scaleFactor;

            yield return null;
        }

        // Ensure final scale is the original
        transform.localScale = originalScale;
    }

    public static GameObject TryFind(string name, out GameObject foundObj)
    {
        foundObj = GameObject.Find(name);
        return foundObj != null ? foundObj : null;
    }

    public static void DisableAllButRendering(this Transform root, Type[] ignoreScripts = null, String[] ignoreNames = null)
    {
        // Disable all components except Renderers
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            foreach (var comp in t.GetComponents<Component>())
            {
                if (comp is Transform || comp is Renderer)
                    continue;

                if (ignoreNames != null && ignoreNames.Where(name => comp.gameObject.name.Contains(name)).Any())
                {
                    continue;
                }

                if (comp is Behaviour behaviour)
                {
                    if (ignoreScripts != null && ignoreScripts.Contains(behaviour.GetType()))
                    {
                        continue;
                    }
                    behaviour.enabled = false;
                }

                if (comp is Collider collider)
                {
                    collider.enabled = false;
                }

                if (comp is Rigidbody rb && rb != null)
                {
                    try
                    {
                        rb.isKinematic = true;
                        rb.detectCollisions = false;
                        rb.useGravity = false;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.constraints = RigidbodyConstraints.FreezeAll;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Failed to modify Rigidbody on {comp.transform.name}: {ex.Message}");
                    }
                }

                // For other non-Behaviour components (like Joint, AudioSource, etc)
                // you can destroy or disable them if needed:
                // Destroy(comp);

                if (comp is Joint joint)
                {
                    if (joint.connectedBody != null)
                    {
                        joint.connectedBody.isKinematic = true;
                        joint.connectedBody.detectCollisions = false;
                        joint.connectedBody.useGravity = false;
                        joint.connectedBody.linearVelocity = Vector3.zero;
                        joint.connectedBody.angularVelocity = Vector3.zero;
                        joint.connectedBody.constraints = RigidbodyConstraints.FreezeAll;
                    }
                }
            }
        }
    }

    public static void DisableAllButRendering(this GameObject root)
    {
        DisableAllButRendering(root.transform);
    }

    public static void BecomeProjectile(this GameObject gameObj)
    {
        gameObj.tag = "ActiveHit";
    }

    public static bool IsOnNavMeshWhileDisabled(this NavMeshAgent agent)
    {
        NavMeshHit hit;
        // Cast slightly above the agent's position to ensure we don't miss due to floating point precision
        Vector3 sourcePoint = agent.transform.position + Vector3.up * 0.1f;
        // Check if a point on NavMesh is found below the agent within reasonable distance
        return NavMesh.SamplePosition(sourcePoint, out hit, 1.0f, NavMesh.AllAreas);
    }

    public static bool TryFindMesh(this GameObject gameObject, out Mesh mesh, out GameObject obj)
    {
        MeshFilter meshFilter = gameObject.GetComponentInChildren<MeshFilter>();
        if (meshFilter && meshFilter.mesh)
        {
            mesh = meshFilter.mesh;
            obj = meshFilter.gameObject;
            return true;
        }
        SkinnedMeshRenderer skinnedMeshRenderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer && skinnedMeshRenderer.sharedMesh)
        {
            mesh = skinnedMeshRenderer.sharedMesh;
            obj = skinnedMeshRenderer.gameObject;
            return true;
        }
        mesh = null;
        obj = null;
        return false;
    }

    public static void ShakeEngine(this Transform shakeThis, Vector3 shakeStartPos, float shakeIntensity = 0.05f, float shakeSpeed = 100f)
    {
        var noiseSeed = 32f;
        var shakeDirection = new Vector3(0.2f, -0.2f, 1);
        // Generate perlin noise-based offsets
        float noiseX = Mathf.PerlinNoise(Time.time * shakeSpeed, noiseSeed) - 0.5f;
        float noiseY = Mathf.PerlinNoise(Time.time * shakeSpeed, noiseSeed + 1) - 0.5f;
        float noiseZ = Mathf.PerlinNoise(Time.time * shakeSpeed, noiseSeed + 2) - 0.5f;

        // Combine shake direction with noise
        Vector3 shakeOffset = new Vector3(
            noiseX * shakeDirection.x,
            noiseY * shakeDirection.y,
            noiseZ * shakeDirection.z
        ) * shakeIntensity;

        // Apply the shake
        shakeThis.localPosition = shakeStartPos + shakeOffset;
    }

    public static Bounds GetBounds(this Transform t, bool includeChildren = false)
    {
        Bounds bounds;
        if (t.TryGetComponent<Collider>(out var collider))
        {
            bounds = collider.bounds;
        }
        else if (t.TryGetComponent<Renderer>(out var renderer))
        {
            bounds = renderer.bounds;
        }
        else
        {
            bounds = new Bounds(t.transform.position, Vector3.one);
        }

        if (includeChildren)
        {
            foreach (Transform child in t.transform)
            {
                bounds.Encapsulate(child.GetBounds());
            }
        }
        return bounds;
    }

    public static Bounds GetBounds(this GameObject gameObject, bool includeChildren = false)
    {
        return gameObject.transform.GetBounds(includeChildren);
    }

    public static float EaseOutBounce(this float t)
    {
        if (t < (1 / 2.75f))
        {
            return 7.5625f * t * t;
        }
        else if (t < (2 / 2.75f))
        {
            t -= (1.5f / 2.75f);
            return 7.5625f * t * t + 0.75f;
        }
        else if (t < (2.5f / 2.75f))
        {
            t -= (2.25f / 2.75f);
            return 7.5625f * t * t + 0.9375f;
        }
        else
        {
            t -= (2.625f / 2.75f);
            return 7.5625f * t * t + 0.984375f;
        }
    }

    public static float DistanceAlongPlane(this Vector3 pos1, Vector3 pos2)
    {
        var _pos1 = new Vector3(pos1.x, 0, pos1.z);
        var _pos2 = new Vector3(pos2.x, 0, pos2.z);
        return _pos1.Distance(_pos2);
    }

    public static GameObject CreateVertexAnchor(this GameObject gameObject)
    {
        if (gameObject.TryFindMesh(out Mesh mesh2, out GameObject obj2))
        {
            var isSkinnedMesh = obj2.TryGetComponentInChildren(out SkinnedMeshRenderer skinnedMesh);
            Vector3[] vertices = mesh2.vertices;
            int randomIndex = UnityEngine.Random.Range(0, vertices.Length);

            var endPosAnchor = new GameObject("anchor");
            endPosAnchor.transform.position = obj2.transform.TransformPoint(vertices[randomIndex]); ;

            if (isSkinnedMesh)
            {
                BoneWeight[] boneWeights = mesh2.boneWeights;
                Transform[] bones = skinnedMesh.bones;

                BoneWeight bw = boneWeights[randomIndex];

                int bestBoneIndex = bw.weight0 > bw.weight1 && bw.weight0 > bw.weight2 && bw.weight0 > bw.weight3 ? bw.boneIndex0 :
                                    bw.weight1 > bw.weight2 && bw.weight1 > bw.weight3 ? bw.boneIndex1 :
                                    bw.weight2 > bw.weight3 ? bw.boneIndex2 :
                                    bw.boneIndex3;

                endPosAnchor.transform.SetParent(bones[bestBoneIndex], true);
            }
            else
            {
                endPosAnchor.transform.SetParent(obj2.transform, true);
            }
            return endPosAnchor;
        }
        return null;
    }

    public static void HideVisual(this GameObject gameObject)
    {
        gameObject.GetComponentsInChildren<Renderer>().ToList().ForEach(r => r.enabled = false);
        gameObject.GetComponentsInChildren<ParticleSystem>().ToList().ForEach(r => r.Stop());
    }

    public static void UnHideVisual(this GameObject gameObject)
    {
        gameObject.GetComponentsInChildren<Renderer>().ToList().ForEach(r => r.enabled = true);
        gameObject.GetComponentsInChildren<ParticleSystem>().ToList().ForEach(r =>
        {
            if (r.main.playOnAwake && r.isPlaying == false)
            {
                r.Play();
            }
        });
    }

    public static void DisableChildren(this GameObject gameObject)
    {
        var children = gameObject.GetComponentsInChildren<Transform>(true);
        foreach (var child in children)
        {
            if (child != gameObject.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    public static void EnableChildren(this GameObject gameObject)
    {
        var children = gameObject.GetComponentsInChildren<Transform>(true);
        foreach (var child in children)
        {
            child.gameObject.SetActive(true);
        }
    }

    public static Color WithAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    public static void AssignLayerToAllChildren(this GameObject gameObject, LayerMask layerMask)
    {
        gameObject.layer = layerMask;
        foreach (Transform child in gameObject.transform)
        {
            child.gameObject.layer = layerMask;
            AssignLayerToAllChildren(child.gameObject, layerMask);
        }
    }

    public static void AssignTagToAllChildren(this GameObject gameObject, string tag)
    {
        gameObject.tag = tag;
        foreach (Transform child in gameObject.transform)
        {
            child.gameObject.tag = tag;
            AssignTagToAllChildren(child.gameObject, tag);
        }
    }

    public static JSONObject MakeLevelObject(this string resourceName, Vector3 position, Quaternion rotation, Vector3 scale) {
        JSONObject levelObject = new JSONObject();
                    
        JSONObject positionObj = new JSONObject();
        positionObj.AddField("x", position.x);
        positionObj.AddField("y", position.y);
        positionObj.AddField("z", position.z);

        JSONObject rotationObj = new JSONObject();
        rotationObj.AddField("x", rotation.x);
        rotationObj.AddField("y", rotation.y);
        rotationObj.AddField("z", rotation.z);

        JSONObject scaleObj = new JSONObject();
        scaleObj.AddField("x", scale.x);
        scaleObj.AddField("y", scale.y);
        scaleObj.AddField("z", scale.z);

        levelObject.AddField("name", resourceName);
        levelObject.AddField("resourceName", resourceName.Split('(')[0].Trim());
        levelObject.AddField("position", positionObj);
        levelObject.AddField("rotation", rotationObj);
        levelObject.AddField("scale", scaleObj);
        return levelObject;
    }

    //find first parent with Root in its name
	public static Transform FindRoot(this Transform transform) {
		Transform parent = transform.parent;
		while (parent != null) {
			if (parent.name.EndsWith("Root")) {
				return parent;
			}
			parent = parent.parent;
		}
		return transform.parent;
	}
}