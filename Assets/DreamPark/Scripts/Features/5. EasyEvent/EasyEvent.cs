#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Events;
public class EasyEvent : MonoBehaviour
{
    [HideInInspector] public UnityEvent<object> onEvent = new UnityEvent<object>();
    [HideInInspector] public EasyEvent aboveEvent;
    [HideInInspector] public EasyEvent belowEvent;
    [ReadOnly] public bool eventOnStart = false;
    [HideInInspector] public bool isEnabled = false;

    public virtual void Awake() {
        // Debug.Log("[EasyEvent] " + gameObject.name + " status: " + (isEnabled ? "Enabled" : "Disabled"));
        // Debug.Log("[EasyEvent] " + gameObject.name + " aboveEvent: " + (aboveEvent != null ? aboveEvent.gameObject.name : "None"));
        // Debug.Log("[EasyEvent] " + gameObject.name + " belowEvent: " + (belowEvent != null ? belowEvent.gameObject.name : "None"));
        // Debug.Log("[EasyEvent] " + gameObject.name + " onEvent: " + (onEvent != null ? onEvent.GetPersistentEventCount() : 0));
        // Debug.Log("--------------------------------");
    }

    public virtual void Start()
    {
        if (Application.isPlaying && eventOnStart) {
            OnEvent();
        }
    }

    public virtual void OnEvent(object arg0 = null)
    {
        isEnabled = true;
        onEvent?.Invoke(arg0);
    }

    public virtual void OnValidate()
    {
        #if UNITY_EDITOR
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
            
        // Skip validation when this object is part of a prefab asset (during Apply/Save/etc)
        if (PrefabUtility.IsPartOfPrefabAsset(this))
            return;

        // If in prefab edit mode (Prefab Stage), only rebuild when user is actively editing the prefab contents
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.scene == gameObject.scene)
        {
            BuildSelfLink();
            return;
        }

        // If this is a prefab instance in a normal scene, also allow rebuild
        if (PrefabUtility.IsPartOfPrefabInstance(this))
        {
            BuildSelfLink();
            return;
        }

        if (!PrefabUtility.IsPartOfAnyPrefab(gameObject))
        {
            BuildSelfLink();
            return;
        }
        #endif
    }

#if UNITY_EDITOR
    private void BuildSelfLink(bool relink = true)
    {
        try {
            Debug.Log("🚧 [EasyEvent] Rebuilding" + gameObject.name + " events");
            EasyEvent[] allComponents = GetComponents<EasyEvent>();
            if (allComponents.Length > 1)
            {
                for (int i = 0; i < allComponents.Length; i++)
                {
                    if (allComponents[i] == this)
                    {
                        // Below
                        if (i + 1 < allComponents.Length)
                            belowEvent = allComponents[i + 1];

                        // Is Top
                        if (i == 0)
                        {
                            eventOnStart = true;
                            break;
                        }

                        // Above
                        aboveEvent = allComponents[i - 1];

                        // Remove + re-add persistent listener for above
                        for (int j = aboveEvent.onEvent.GetPersistentEventCount() - 1; j >= 0; j--)
                            UnityEventTools.RemovePersistentListener(aboveEvent.onEvent, j);

                        if (relink) {
                            UnityEventTools.AddPersistentListener(aboveEvent.onEvent, OnEvent); 
                        }

                        break; // ✅ preserves original control flow
                    }
                }
            }
            else
            {
                eventOnStart = true;
            }

            EditorUtility.SetDirty(this);
            Debug.Log("✅ [EasyEvent] Rebuilt " + gameObject.name + " events");
        } catch (Exception e) {
            Debug.LogError("❌ [EasyEvent] Error rebuilding" + gameObject.name + " events: " + e.Message);
        }
    }

    public void RemoveSelfLink() {
        BuildSelfLink(false);
        belowEvent = null;
        aboveEvent = null;
    }
#endif
}