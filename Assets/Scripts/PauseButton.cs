using UnityEngine;
using UnityEngine.UI;
using System;

public class PauseButton : MonoBehaviour {
    public GameObject PauseUI;
    public Action Callback;

    void Start() {
        
    }

    void Update() {
        
    }

    public void OnClick() {
        PauseUI.SetActive(true);
        Callback?.Invoke();
    }
}
