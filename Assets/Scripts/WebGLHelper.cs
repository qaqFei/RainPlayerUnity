using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_WEBGL && !UNITY_EDITOR
public class WebGLHelper {
    private const string DLL_NAME = "__Internal";

    [DllImport(DLL_NAME)] public static extern void WebGLHelper_Initialize();
    [DllImport(DLL_NAME)] public static extern void WebGLHelper_SetCanvasFull();
    [DllImport(DLL_NAME)] public static extern int WebGLHelper_GetStringSize(int stringId);
    [DllImport(DLL_NAME)] public static extern void WebGLHelper_WriteStringIntoBuffer(int stringId, byte[] bufferPtr);
    [DllImport(DLL_NAME)] public static extern void WebGLHelper_ReleaseString(int stringId);
    [DllImport(DLL_NAME)] public static extern int WebGLHelper_GetUrlParam(byte[] keyPtr, int keyLen);
    [DllImport(DLL_NAME)] public static extern void WebGLHelper_FreeBlobURL(string url);
    [DllImport(DLL_NAME)] public static extern void WebGLHelper_ChartPlayerLoaded();
    [DllImport(DLL_NAME)] public static extern void WebGLHelper_ChartPlayerLoadFailed();
    [DllImport(DLL_NAME)] public static extern void WebGLHelper_ChartPlayerStartedLoad();
    [DllImport(DLL_NAME)] public static extern void WebGLHelper_BackToHub();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() {
        WebGLHelper_Initialize();
    }

    public static string WebGLHelper_GetUrlParamWarpper(string key) {
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
        var sid = WebGLHelper_GetUrlParam(keyBytes, keyBytes.Length);
        if (sid == 0) return null;

        var size = WebGLHelper_GetStringSize(sid);
        var buffer = new byte[size];
        WebGLHelper_WriteStringIntoBuffer(sid, buffer);
        WebGLHelper_ReleaseString(sid);
        return System.Text.Encoding.UTF8.GetString(buffer);
    }

    public static System.Collections.IEnumerator DownloadUrlAsTempFile(string url, Action<string> callback) {
        using (var uwr = UnityWebRequest.Get(url)) {
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"WebGL download failed: {uwr.error}");
                callback?.Invoke(null);
            }
            else {
                byte[] data = uwr.downloadHandler.data;
                string tmpPath = Path.Combine(Application.temporaryCachePath, $"{Guid.NewGuid().ToString("N")}.zip");
                File.WriteAllBytes(tmpPath, data);
                WebGLHelper.WebGLHelper_FreeBlobURL(url);
                callback?.Invoke(tmpPath);
            }
        }
    }
}
#endif
