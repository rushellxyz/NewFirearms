using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace NewFirearms
{
    public static class AudioLoader
    {
        private static readonly Dictionary<string, AudioType> audioTypes = new Dictionary<string, AudioType>(StringComparer.OrdinalIgnoreCase)
        {
            [".aif"] = AudioType.AIFF,
            [".aiff"] = AudioType.AIFF,
            [".it"] = AudioType.IT,
            [".mod"] = AudioType.MOD,
            [".mp3"] = AudioType.MPEG,
            [".ogg"] = AudioType.OGGVORBIS,
            [".s3m"] = AudioType.S3M,
            [".wav"] = AudioType.WAV,
            [".xm"] = AudioType.XM,
        };

        private static readonly string[] extensionlessAudioExtensions =
        {
            ".wav",
            ".ogg",
            ".mp3",
            ".aiff",
            ".aif",
            ".mod",
            ".it",
            ".s3m",
            ".xm",
        };

        public static void LoadAudio(string folder, string relativePath, Action<AudioClip> setClip, AudioClip fallback)
            => LoadAudio(folder, relativePath, setClip, fallback, "audio");

        public static void LoadAudio(string folder, string relativePath, Action<AudioClip> setClip, AudioClip fallback, string fieldName)
        {
            setClip(fallback);
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            string path = Path.Combine(folder, relativePath);
            string audioPath = ResolveAudioPath(path);
            if (null == audioPath)
            {
                UnityEngine.Debug.LogError($"[NewFirearms] {fieldName}: Audio file '{path}' doesn't exist or has an unsupported extension.");
                return;
            }

            LoadAudioAsync(audioPath, setClip, fieldName);
        }

        private static string ResolveAudioPath(string path)
        {
            string extension = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(extension))
            {
                if (!audioTypes.ContainsKey(extension) || !File.Exists(path))
                    return null;
                return path;
            }

            foreach (string candidateExtension in extensionlessAudioExtensions)
            {
                string candidatePath = path + candidateExtension;
                if (File.Exists(candidatePath))
                    return candidatePath;
            }

            return null;
        }

        private static async void LoadAudioAsync(string path, Action<AudioClip> setClip, string fieldName)
        {
            try
            {
                string extension = Path.GetExtension(path);
                AudioType audioType = audioTypes[extension];
                string uri = new Uri(Path.GetFullPath(path)).AbsoluteUri;

                using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
                {
                    UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                    var completion = new TaskCompletionSource<bool>();
                    operation.completed += _ => completion.TrySetResult(true);
                    await completion.Task;

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        UnityEngine.Debug.LogError($"[NewFirearms] {fieldName}: Failed to load audio '{path}': {request.error}");
                        return;
                    }

                    AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                    if (null == clip)
                    {
                        UnityEngine.Debug.LogError($"[NewFirearms] {fieldName}: Audio file '{path}' returned no clip.");
                        return;
                    }

                    setClip(clip);
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"[NewFirearms] {fieldName}: Exception loading audio '{path}': {exception}");
            }
        }
    }
}
