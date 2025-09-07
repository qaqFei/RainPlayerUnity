using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    using NativeFileBrowser;
#endif

public class SelectChartButton : MonoBehaviour
{
    public string selectedPath;
    public Text pathText;

    void Start() {
        if (pathText != null) pathText.text = "Chart path: (none)";
    }

    void Update() {
        
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
            pathText.text = $"Chart path: {selectedPath}";
        }
    }

    public void ButtonOnClick() {
        Debug.Log("Select chart button clicked");

        #if UNITY_EDITOR || UNITY_STANDALONE_WIN
            var title = "Select milthm chart file";
            var extensions = new[] {
                new ExtensionFilter("Milthm Chart File", "zip"),
                new ExtensionFilter("All Files", "*.*")
            };

            var path = StandaloneFileBrowser.OpenFilePanel(title, extensions, false);

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
                        pathText.text = "Chart path: (permission denied)";
                        return;
                    }
                    SelectAndroid();
                });
            } else {
                SelectAndroid();
            }
        #endif
    }
}
