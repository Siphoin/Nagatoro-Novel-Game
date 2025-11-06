using System;
using System.Linq;
using UnityEngine;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/New Service Container")]
    public class ServiceContainer : ScriptableObject
    {
        [SerializeField] private ServiceBase[] _services;

        public void Initialize ()
        {
            foreach (var service in _services)
            {
                service.Initialize();
            }
        }

        internal T Get<T>() where T : ServiceBase
        {
            return _services.FirstOrDefault(x => x is T) as T;
        }

        internal void ResetState()
        {
            
        }
    }
}