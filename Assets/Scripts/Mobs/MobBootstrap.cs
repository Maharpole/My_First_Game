    using UnityEngine;
    public class MobBootstrap : MonoBehaviour
    {
        void Start()
        {
            var director = FindObjectOfType<MobDirector>();
            if (director != null) director.Generate();
        }
    }