using SNEngine.Repositories;
using SNEngine.Services;
using System.Linq;
using UnityEngine;

namespace SNEngine.Repositories
{
    public class RepositoryContainer
    {
        [SerializeField] private RepositoryBase[] _repositories;

        public void Initialize()
        {
            foreach (var repository in _repositories)
            {
                repository.Initialize();
            }
        }

        internal T Get<T>() where T : RepositoryBase
        {
            return _repositories.FirstOrDefault(x => x is T) as T;
        }
    }
}
