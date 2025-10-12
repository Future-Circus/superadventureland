namespace SuperAdventureLand.Scripts
{
    using System;
    using UnityEngine;

    public static class CoreExtensionsInterface
    {
        public static string gameId = "SuperAdventureLand";
        public static void GetAsset<T>(this string resourceName, Action<T> onSuccess = null, Action<string> onError = null) where T : UnityEngine.Object
        {
            CoreExtensions.GetAssetAsync<T>($"{gameId}/{resourceName}", onSuccess, onError).Forget();
        }

        public static void PlaySFX(this string clipName, Vector3 position, float volume = 1f, float pitch = 1f, Transform parent = null)
        {
            CoreExtensions.GetAssetAsync<AudioClip>($"{gameId}/{clipName}", (clip) => {
                if (clip)
                {
                clip.PlaySFX(position, volume, pitch, parent);
                }
            }, null).Forget();
        }

        public static void SpawnAsset(this string resourceName, Action<GameObject> onSuccess, Action<string> onError)
        {
            CoreExtensions.SpawnAsset($"{gameId}/{resourceName}", onSuccess, onError);
        }

        public static void SpawnAsset(this string resourceName, Vector3 position, Quaternion rotation, Transform parent = null, Action<GameObject> onSuccess = null, Action<string> onError = null)
        {
            CoreExtensions.SpawnAsset($"{gameId}/{resourceName}", position, rotation, parent, onSuccess, onError);
        }
    }
}
