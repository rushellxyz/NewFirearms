
namespace NewFirearms
{
    public static class AudioLoader
    {

        // i stole that code "Grom PE's custom character for Gunsaw"
        /*          public static AudioClip LoadSound(string path)
         {  *   *
         //Log.LogInfo($"Loading sound from {path}");
         if (!File.Exists(path))
         {
         Log.LogError($"Sound file not found: {path}");
         return null;
    }
    AudioType ty;
    if (path.EndsWith(".ogg")) ty = AudioType.OGGVORBIS;
    else if (path.EndsWith(".mp3")) ty = AudioType.MPEG;
    else if (path.EndsWith(".wav")) ty = AudioType.WAV;
    else if (path.EndsWith(".aiff")) ty = AudioType.AIFF;
    else if (path.EndsWith(".it")) ty = AudioType.IT;
    else if (path.EndsWith(".mod")) ty = AudioType.MOD;
    else if (path.EndsWith(".s3m")) ty = AudioType.S3M;
    else if (path.EndsWith(".xm")) ty = AudioType.XM;
    else
    {
    Log.LogError($"Failed to load sound from {path}: unknown file extension; expected .ogg|.mp3|.wav|.aiff|.it|.mod|.s3m|.xm");
    return null;
    }
    UnityWebRequest wr = UnityWebRequestMultimedia.GetAudioClip(path, ty);
    wr.SendWebRequest();
    while (!wr.isDone) {};
    AudioClip res = DownloadHandlerAudioClip.GetContent(wr);
    if (res == null)
    {
    Log.LogError($"Failed to load sound from {path}");
    } else {
        Log.LogInfo($"Loaded sound from {path}");
    }
    return res;
    }*/

    }
}
