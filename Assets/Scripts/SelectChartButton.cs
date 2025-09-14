using UnityEngine;
using UnityEngine.UI;
using System;

using MilConst;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_WEBGL
    using SFB;
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

        #if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_WEBGL
            var title = "Select milthm chart file";
            var extensions = new[] {
                new ExtensionFilter("Milthm Chart File", "zip"),
                new ExtensionFilter("All Files", "*")
            };

            var path = StandaloneFileBrowser.OpenFilePanel(title, "", extensions, false);

            if (path == null || path.Length == 0) return;

            selectedPath = path[0];
            SelectEnd();
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
