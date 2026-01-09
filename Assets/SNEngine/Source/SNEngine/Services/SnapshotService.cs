using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using SNEngine.Debugging;
using SNEngine.SaveSystem.Models;
using SNEngine.SnapshotSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SNEngine.Services
{
    [CreateAssetMenu(menuName = "SNEngine/Services/Snapshot Service")]
    public class SnapshotService : ServiceBase, IService, IDisposable
    {
        private readonly Stack<SaveData> _historyStack = new();
        private ISnapshotProvider _provider;
        private bool _isWriting;

        public void InitializeSession(string saveName)
        {
            Dispose();
#if UNITY_WEBGL && !UNITY_EDITOR
            _provider = new PlayerPrefsSnapshotProvider(saveName);
#else
            _provider = new FileSnapshotProvider(saveName);
#endif
        }

        public void PushSnapshot(SaveData data)
        {
            if (data == null) return;

            if (_historyStack.Count > 0 && _historyStack.Peek().CurrentNode == data.CurrentNode)
            {
                NovelGameDebug.Log($"[SnapshotService] Skip push: Node {data.CurrentNode} is already at the top of stack.");
                return;
            }

            _historyStack.Push(data);
            NovelGameDebug.Log($"[SnapshotService] Snapshot pushed to RAM. Node: {data.CurrentNode}. Stack size: {_historyStack.Count}");

            SaveToProviderAsync(data).Forget();
        }

        public async UniTask<SaveData> PopSnapshotAsync()
        {
            if (_historyStack.Count > 0)
            {
                var data = _historyStack.Pop();
                _provider.PopLastAsync().Forget();
                NovelGameDebug.Log($"[SnapshotService] Pop from RAM. New stack size: {_historyStack.Count}");
                return data;
            }

            byte[] raw = await _provider.PopLastAsync();
            if (raw != null)
            {
                var data = Deserialize(raw);
                NovelGameDebug.Log($"[SnapshotService] Pop from Disk. Node: {data?.CurrentNode}");
                return data;
            }

            NovelGameDebug.LogWarning("[SnapshotService] Try pop snapshot, but history is empty.");
            return null;
        }

        private async UniTaskVoid SaveToProviderAsync(SaveData data)
        {
            while (_isWriting) await UniTask.Yield();
            _isWriting = true;
            try
            {
                await _provider.AppendAsync(Serialize(data));
            }
            finally
            {
                _isWriting = false;
            }
        }

        private byte[] Serialize(SaveData data)
        {
            Guid id = DeriveSmartGuid(data.CurrentNode);
            string json = JsonConvert.SerializeObject(data);
            byte[] payload = Encoding.UTF8.GetBytes(json);
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            writer.Write(id.ToByteArray());
            writer.Write(payload.Length);
            writer.Write(payload);
            return ms.ToArray();
        }

        private SaveData Deserialize(byte[] data)
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);
            ms.Position = 16;
            int len = reader.ReadInt32();
            return JsonConvert.DeserializeObject<SaveData>(Encoding.UTF8.GetString(reader.ReadBytes(len)));
        }

        private Guid DeriveSmartGuid(string nodeGuidStr)
        {
            if (!Guid.TryParse(nodeGuidStr, out Guid nodeGuid)) return Guid.NewGuid();
            byte[] b = nodeGuid.ToByteArray();
            using var md5 = MD5.Create();
            byte[] h = md5.ComputeHash(b);
            h[^1] = b[^1];
            return new Guid(h);
        }

        public void Dispose()
        {
            if (_provider is IDisposable d) d.Dispose();
            _provider = null;
            _historyStack.Clear();
        }
    }
}