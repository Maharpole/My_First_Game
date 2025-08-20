using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Ensures a single EventSystem persists across scenes. Optional; add if scenes lack an EventSystem.
/// </summary>
public class PersistentEventSystem : MonoBehaviour
{
    void Awake()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(go);
            Debug.Log("[PersistentEventSystem] Created persistent EventSystem");
        }
        // This helper object can be destroyed immediately; it only ensures the ES exists once
        Destroy(gameObject);
    }
}


