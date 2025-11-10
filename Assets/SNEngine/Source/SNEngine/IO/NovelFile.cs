using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using SNEngine.Debugging;
using UnityEngine;

namespace SNEngine.IO
{
    public static class NovelFile
    {
        public static string PersistentDataPath => Application.persistentDataPath;
        public static string StreamingAssetsPath => Application.streamingAssetsPath;
        public static string DataPath => Application.dataPath;
        public static string TemporaryCachePath => Application.temporaryCachePath;

        static async UniTask<T> WithStream<T>(string path, FileMode mode, FileAccess access, FileShare share, Func<Stream, UniTask<T>> action)
        {
            using var stream = new FileStream(path, mode, access, share, 4096, true);
            return await action(stream);
        }

        static UniTask WithStream(string path, FileMode mode, FileAccess access, FileShare share, Func<Stream, UniTask> action) =>
            WithStream<object>(path, mode, access, share, async s => { await action(s); return null; });

        public static UniTask<string> ReadAllTextAsync(string path, Encoding encoding = null) =>
            WithStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, async s =>
            {
                encoding ??= Encoding.UTF8;
                using var reader = new StreamReader(s, encoding);
                return await reader.ReadToEndAsync();
            });

        public static string ReadAllText(string path, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return File.ReadAllText(path, encoding);
        }

        public static UniTask WriteAllTextAsync(string path, string contents, Encoding encoding = null) =>
            WithStream(path, FileMode.Create, FileAccess.Write, FileShare.None, async s =>
            {
                encoding ??= Encoding.UTF8;
                using var writer = new StreamWriter(s, encoding);
                await writer.WriteAsync(contents);
            });

        public static void WriteAllText(string path, string contents, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            File.WriteAllText(path, contents, encoding);
        }

        public static UniTask<byte[]> ReadAllBytesAsync(string path) =>
            WithStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, async s =>
            {
                var buffer = new byte[s.Length];
                await s.ReadAsync(buffer, 0, buffer.Length);
                return buffer;
            });

        public static byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

        public static UniTask WriteAllBytesAsync(string path, byte[] bytes) =>
            WithStream(path, FileMode.Create, FileAccess.Write, FileShare.None, s => s.WriteAsync(bytes, 0, bytes.Length).AsUniTask());

        public static void WriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);

        public static UniTask AppendAllTextAsync(string path, string contents, Encoding encoding = null) =>
            WithStream(path, FileMode.Append, FileAccess.Write, FileShare.None, async s =>
            {
                encoding ??= Encoding.UTF8;
                using var writer = new StreamWriter(s, encoding);
                await writer.WriteAsync(contents);
            });

        public static void AppendAllText(string path, string contents, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            File.AppendAllText(path, contents, encoding);
        }

        public static UniTask AppendAllBytesAsync(string path, byte[] bytes) =>
            WithStream(path, FileMode.Append, FileAccess.Write, FileShare.None, s => s.WriteAsync(bytes, 0, bytes.Length).AsUniTask());

        public static bool Exists(string path) => File.Exists(path);

        public static void Delete(string path) => File.Delete(path);

        public static void Move(string sourceFileName, string destFileName, bool overwrite = false)
        {
            if (overwrite && File.Exists(destFileName)) File.Delete(destFileName);
            File.Move(sourceFileName, destFileName);
        }

        public static UniTask CopyAsync(string sourceFileName, string destFileName, bool overwrite = false) =>
            UniTask.RunOnThreadPool(() =>
            {
                if (overwrite && File.Exists(destFileName)) File.Delete(destFileName);
                File.Copy(sourceFileName, destFileName, overwrite);
            });

        public static void Copy(string sourceFileName, string destFileName, bool overwrite = false) =>
            File.Copy(sourceFileName, destFileName, overwrite);

        public static DateTime GetCreationTime(string path) => File.GetCreationTime(path);
        public static DateTime GetCreationTimeUtc(string path) => File.GetCreationTimeUtc(path);
        public static void SetCreationTime(string path, DateTime creationTime) => File.SetCreationTime(path, creationTime);
        public static void SetCreationTimeUtc(string path, DateTime creationTimeUtc) => File.SetCreationTimeUtc(path, creationTimeUtc);

        public static DateTime GetLastAccessTime(string path) => File.GetLastAccessTime(path);
        public static DateTime GetLastAccessTimeUtc(string path) => File.GetLastAccessTimeUtc(path);
        public static void SetLastAccessTime(string path, DateTime lastAccessTime) => File.SetLastAccessTime(path, lastAccessTime);
        public static void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => File.SetLastAccessTimeUtc(path, lastAccessTimeUtc);

        public static DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);
        public static DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);
        public static void SetLastWriteTime(string path, DateTime lastWriteTime) => File.SetLastWriteTime(path, lastWriteTime);
        public static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) => File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);

        public static FileStream Open(string path, FileMode mode) => File.Open(path, mode);
        public static FileStream Open(string path, FileMode mode, FileAccess access) => File.Open(path, mode, access);
        public static FileStream Open(string path, FileMode mode, FileAccess access, FileShare share) => File.Open(path, mode, access, share);

        public static FileStream OpenRead(string path) => File.OpenRead(path);
        public static FileStream OpenWrite(string path) => File.OpenWrite(path);

        public static StreamReader OpenText(string path) => File.OpenText(path);

        public static void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName) =>
            File.Replace(sourceFileName, destinationFileName, destinationBackupFileName);

        public static void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors) =>
            File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);

        public static string GetAbsolutePath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception ex)
            {
                NovelGameDebug.LogError($"Failed to get full path for {path}: {ex.Message}");
                return path;
            }
        }
    }
}
