using UnityEngine;
using UnityEngine.UI;

public class SliderTextUpdater : MonoBehaviour
{
    public Slider slider;
    public string format = "F2";
    public bool canunset = false;
    private bool isset = false;

    void Start() {
        
    }

    void Update() {
        if (!canunset || isset) {
            GetComponent<Text>().text = slider.value.ToString("F2");
        }
    }

    public void OnValueChange() {
        isset = true;
    }

    public float? GetValue() {
        return (canunset && !isset) ? null : slider.value;
    }
}
