using UnityEngine;
using UnityEngine.SceneManagement;

public class SavableSettingsManager : MonoBehaviour
{
    void Start() {
        
    }

    void Update() {
        
    }

    public void ResetSettings() {
        foreach (var monob in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            if (monob is SavableSetting) {
                var s = (monob as SavableSetting);
                if (s.HasKey()) {
                    PlayerPrefs.DeleteKey(s.GetRealKey());
                }
                s.disabled = true;
            }
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
