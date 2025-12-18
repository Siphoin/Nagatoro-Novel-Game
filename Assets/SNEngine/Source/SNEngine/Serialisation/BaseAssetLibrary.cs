using System;
using System.Collections.Generic;
using UnityEngine;
using SNEngine.Debugging;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SNEngine.Serialization
{
    public abstract class BaseAssetLibrary : ScriptableObjectIdentity
    {

    }
    public abstract partial class BaseAssetLibrary<T> : BaseAssetLibrary where T : UnityEngine.Object
    {

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => _entries;

        [field: SerializeReference]  protected Dictionary<string, T> GuidToAsset { get; set; } = new Dictionary<string, T>();
        [field: SerializeReference]  protected Dictionary<T, string> AssetToGuid { get; set; } = new Dictionary<T, string>();

        public virtual void Initialize()
        {
            GuidToAsset.Clear();
            AssetToGuid.Clear();

            foreach (var entry in _entries)
            {
                if (entry.Asset != null && !string.IsNullOrEmpty(entry.Guid))
                {
                    GuidToAsset[entry.Guid] = entry.Asset;
                    AssetToGuid[entry.Asset] = entry.Guid;
                }
            }
        }

        public void Add(object asset)
        {
            var targetType = GetTypeAsset();

            if (asset.GetType() != targetType)
            {
                NovelGameDebug.LogError($"invalid type asset for library {GetType().Name} Type: {asset.GetType().Name}");
                return;
            }
            if (asset is null)
            {
                return;
            }

            string guid = string.Empty;
            T convertedAsset = asset as T;

#if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(convertedAsset);
            if (!string.IsNullOrEmpty(path))
            {
                guid = AssetDatabase.AssetPathToGUID(path);
            }
#endif

            if (string.IsNullOrEmpty(guid))
            {
                guid = Guid.NewGuid().ToString();
            }

            if (!_entries.Exists(e => e.Guid == guid))
            {
                _entries.Add(new Entry { Guid = guid, Asset = convertedAsset });
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }

            GuidToAsset[guid] = convertedAsset;
            AssetToGuid[convertedAsset] = guid;
        }

        public T GetAsset(string guid) =>
            GuidToAsset.TryGetValue(guid, out var asset) ? asset : null;

        public string GetGuid(T asset) =>
            AssetToGuid.TryGetValue(asset, out var guid) ? guid : null;

        public Type GetTypeAsset ()
        {
            return typeof(T); 
        }
    }

}