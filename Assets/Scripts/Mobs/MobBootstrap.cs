    using UnityEngine;
    public class MobBootstrap : MonoBehaviour
    {
        void Start()
        {
            var director = Object.FindFirstObjectByType<MobDirector>();
            if (director != null) director.Generate();
        }
    }