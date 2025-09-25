using UnityEngine;
using UnityEngine.UI;
using System;

using MilConst;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
using NativeFileBrowser;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Runtime.InteropServices;

public class WebGLFilePicker : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void PickZipFile(string go, string cb);
    private Action<string> onRealPath;

    public static void PickFileAndCache(Action<string> onRealPath) {
        var go = new GameObject("WebGLFilePickerTemp"); // 如果这里的两个名字改了, jslib 的也要改
        var picker = go.AddComponent<WebGLFilePicker>();
        picker.onRealPath = onRealPath;
        PickZipFile(go.name, "OnBlobPicked");
    }

    private void OnBlobPicked(string blobUrl) {
        DownloadAndCache(blobUrl);
    }

    private void DownloadAndCache(string blobUrl) {
        StartCoroutine(WebGLHelper.DownloadUrlAsTempFile(blobUrl, tmpPath => {
            onRealPath?.Invoke(tmpPath);
            Destroy(gameObject);
        }));
    }
}
#endif

public class SelectChartButton : MonoBehaviour, I18nSupported
{
    public string selectedPath;
    public Text pathText;
    private bool resetedPathText;
    private string ChartPathI18nKey = "ChartPath";
    private Action pathTextSetter;

    void Start() {
        resetedPathText = false;

        #if UNITY_WEBGL && !UNITY_EDITOR
        var chartUrl = WebGLHelper.WebGLHelper_GetUrlParamWarpper("chartUrl");
        if (chartUrl != null) {
            StartCoroutine(WebGLHelper.DownloadUrlAsTempFile(chartUrl, tmpPath => {
                resetedPathText = true;
                selectedPath = tmpPath;
                SelectEnd();
            }));
        }
        #endif
    }

    void Update() {
        if (!resetedPathText) {
            if (pathText != null) {
                SetTextSetter(() => {
                    pathText.text = $"{MilConst.MilConst.i18n.GetText(ChartPathI18nKey)}: ({MilConst.MilConst.i18n.GetText("ChartPathNotSelected")})";
                });
            }
            resetedPathText = true;
        }
    }

    #if UNITY_ANDROID
        private void SelectAndroid() {
            NativeFilePicker.PickFile((path) => {
                selectedPath = path;
                SelectEnd();
            });
        }
    #else
        private void SelectAndroid() { }
    #endif

    private void SelectEnd() {
        if (selectedPath == null) return;

        Debug.Log($"Selected path: {selectedPath}");

        if (pathText != null) {
            Debug.Log("Updating path text");
            SetTextSetter(() => {
                pathText.text = $"{MilConst.MilConst.i18n.GetText(ChartPathI18nKey)}: {selectedPath}";
            });
        }
    }

    public void ButtonOnClick() {
        Debug.Log("Select chart button clicked");

        #if UNITY_EDITOR || UNITY_STANDALONE_WIN
            var title = "Select milthm chart file";
            var extensions = new[] {
                new ExtensionFilter("Milthm Chart File", "zip"),
                new ExtensionFilter("All Files", "*")
            };

            var path = StandaloneFileBrowser.OpenFilePanel(title, extensions, false);

            if (path == null || path.Length == 0) return;

            selectedPath = path[0];
            SelectEnd();
        #elif UNITY_WEBGL && !UNITY_EDITOR
            WebGLFilePicker.PickFileAndCache((res) => {
                selectedPath = res;
                SelectEnd();
            });
        #else
            if (NativeFilePicker.IsFilePickerBusy()) {
                Debug.LogWarning("File picker is busy");
                return;
            }
            
            if (!NativeFilePicker.CheckPermission()) {
                Debug.Log("Requesting file access permission");
                NativeFilePicker.RequestPermissionAsync((permission) => {
                    if (permission != NativeFilePicker.Permission.Granted) {
                        Debug.LogWarning("File access permission denied");
                        SetTextSetter(() => {
                            pathText.text = $"{MilConst.MilConst.i18n.GetText(ChartPathI18nKey)}: ({MilConst.MilConst.i18n.GetText("PermissionDenied")})";
                        });
                        return;
                    }
                    SelectAndroid();
                });
            } else {
                SelectAndroid();
            }
        #endif
    }

    private void SetTextSetter(Action setter) {
        pathTextSetter = setter;
        pathTextSetter.Invoke();
    }

    public void OnI18nChanged() {
        pathTextSetter?.Invoke();
    }
}
