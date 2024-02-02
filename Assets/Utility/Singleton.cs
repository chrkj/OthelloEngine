using UnityEngine;

namespace Othello.Utility
{
    public abstract class StaticInstanceMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }
        protected virtual void Awake() => Instance = this as T;
    }
    
    public abstract class SingletonMono<T> : StaticInstanceMonoBehaviour<T> where T : MonoBehaviour
    {
        protected override void Awake()
        {
            if (Instance != null) 
                Destroy(gameObject);
            else
                base.Awake();
        }
    }

}