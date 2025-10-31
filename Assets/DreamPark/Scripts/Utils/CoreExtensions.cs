using System;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if DREAMPARKCORE
using DreamPark.ParkBuilder;
#endif

public static class CoreExtensions
{
    private static Task _addressablesInitTask;

    private static async Task EnsureAddressablesInitialized()
    {
        // already started or done? just await the same one
        if (_addressablesInitTask != null)
        {
            await _addressablesInitTask;
            return;
        }

        // create and store the shared initialization task
        _addressablesInitTask = Addressables.InitializeAsync().Task;
        await _addressablesInitTask;
    }
    public static void GetAsset<T>(this string resourceName, Action<T> onSuccess = null, Action<string> onError = null) where T : UnityEngine.Object
    {
        GetAssetAsync(resourceName, onSuccess, onError).Forget();
    }

    public static async UniTaskVoid GetAssetAsync<T>(string resourceName, Action<T> onSuccess = null, Action<string> onError = null) where T : UnityEngine.Object
    {
        T obj = await resourceName.GetAsset<T>();
        if (obj != null)
        {
            onSuccess?.Invoke(obj);
        }
        else
        {
            onError?.Invoke(resourceName);
        }
    }

    public static async Task<T> GetAsset<T>(this string resourceName) where T : UnityEngine.Object
    {
        await EnsureAddressablesInitialized();

        // Try common Resources paths
        T asset = Resources.Load<T>(resourceName)
                ?? Resources.Load<T>($"Prefabs/{resourceName}")
                ?? Resources.Load<T>($"Levels/{resourceName}")
                ?? Resources.Load<T>($"Audio/{resourceName}")
                ?? Resources.Load<T>($"Music/{resourceName}");

        if (asset != null)
            return asset;

        // Try Addressables (robust locator check)
        if (Addressables.ResourceLocators.Any(l => l.Locate(resourceName, typeof(T), out _)))
        {
            var handle = Addressables.LoadAssetAsync<T>(resourceName);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result;

            Addressables.Release(handle);
        }

        Debug.LogWarning($"❌ Asset '{resourceName}' of type {typeof(T).Name} not found in Resources or Addressables.");
        return null;
    }

    public static async UniTask<GameObject> InstantiateAssetAsync(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null, Action<GameObject> onSuccess = null, Action<string> onError = null)
    {
       GameObject instance = GameObject.Instantiate(prefab, position, rotation, parent);
       #if DREAMPARKCORE
        if (LevelObjectManager.Instance != null) {
            LevelObjectManager.Instance.RegisterLevelObject(instance);
        }
        #endif
        if (onSuccess != null) {
            onSuccess(instance);
        }
        return instance;
    }

    public static void PrioritizeAsset(this GameObject gameObject) {    
        #if DREAMPARKCORE
        if (LevelObjectManager.Instance != null) {
            LevelObjectManager.Instance.PrioritizeLevelObject(gameObject);
        }
        #else
        Debug.Log("PrioritizeLevelObject makes our Automated Optimization ignore this asset keeping it alive forever");
        #endif
    }

    public static AudioSource PlaySFX(this AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, Transform parent = null)
    {
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;
        AudioSource audioSource = tempGO.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = Mathf.Clamp01(volume);
        audioSource.spatialBlend = 1;
        audioSource.maxDistance = 10 + ((volume-1f) * 10f);
        audioSource.pitch = pitch;
        tempGO.AddComponent<RealisticRolloff>();
        audioSource.Play();

        if (parent != null)
        {
            tempGO.transform.SetParent(parent,true);
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

    public static void PlaySFX(this string clipName, Vector3 position, float volume = 1f, float pitch = 1f, Transform parent = null)
    {
         GetAssetAsync<AudioClip>(clipName, (clip) => {
            if (clip)
            {
            clip.PlaySFX(position, volume, pitch, parent);
            }
         }, null).Forget();
    }

    public static async UniTask<GameObject> InstantiateAssetAsync(this string resourceName, Vector3 position = default, Quaternion rotation = default, Transform parent = null, Action<GameObject> onSuccess = null, Action<string> onError = null)
    {
        GameObject prefab = await resourceName.GetAsset<GameObject>();
        if (prefab != null) {
            return await InstantiateAssetAsync(prefab, position, rotation, parent, onSuccess, onError);
        } else if (onError != null) {
            onError(resourceName);
        }
        return null;
    }
    public static void SpawnAsset(this string resourceName, Action<GameObject> onSuccess, Action<string> onError)
    {
        InstantiateAssetAsync(resourceName, onSuccess: onSuccess, onError: onError).Forget();
    }

    public static void SpawnAsset(this string resourceName, Vector3 position, Quaternion rotation, Transform parent = null, Action<GameObject> onSuccess = null, Action<string> onError = null)
    {
        InstantiateAssetAsync(resourceName, position, rotation, parent, onSuccess, onError).Forget();
    }

    public static void SpawnAsset(this GameObject prefab, Action<GameObject> onSuccess, Action<string> onError)
    {
        InstantiateAssetAsync(prefab, onSuccess: onSuccess, onError: onError).Forget();
    }

    public static void SpawnAsset(this GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, Action<GameObject> onSuccess = null, Action<string> onError = null)
    {
        InstantiateAssetAsync(prefab, position, rotation, parent, onSuccess, onError).Forget();
    }
}