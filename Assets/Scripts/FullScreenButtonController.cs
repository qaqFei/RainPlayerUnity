using UnityEngine;

public class FullScreenButtonController : MonoBehaviour
{
    private bool canFullScreen;

    void Start() {
        canFullScreen = Screen.fullScreenMode != FullScreenMode.Windowed;
        if (!canFullScreen) {
            gameObject.SetActive(false);
        }
    }

    void Update() {
        
    }

    public void ButtonOnClick() {
        if (!canFullScreen) return;

        Screen.fullScreen = !Screen.fullScreen;
        GetComponent<I18nController>().key = Screen.fullScreen ? "Fullscreen" : "ExitFullscreen";
    }
}
