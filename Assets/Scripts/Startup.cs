using UnityEngine;

public class Startup {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void OnBeforeSceneLoadRuntimeMethod() {
        #if UNITY_WEBGL && !UNITY_EDITOR
            WebGLHelper.WebGLHelper_SetCanvasFull();
        #endif
    }
}