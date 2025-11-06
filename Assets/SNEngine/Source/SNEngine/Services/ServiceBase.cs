using UnityEngine;

namespace SNEngine.Services
{
    public abstract class ServiceBase : ScriptableObject, IService
    {
        public virtual void Initialize () { }
    }
}
