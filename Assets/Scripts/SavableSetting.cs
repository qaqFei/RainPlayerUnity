using UnityEngine;
using UnityEngine.UI;

public class SavableSetting : MonoBehaviour
{
    public enum SavableSettingType {
        Slider,
        Toggle,
        Dropdown,
        InputField
    }

    public SavableSettingType settingType;
    public string key;
    private bool inited;

    private bool HasKey() {
        return !string.IsNullOrEmpty(key) && PlayerPrefs.HasKey(_key);
    }

    private string _key {
        get { return $"SavableSetting_{key}"; }
    }

    private void LoadValue() {
        if (!HasKey()) return;

        switch (settingType) {
            case SavableSettingType.Slider:
                float sliderValue = PlayerPrefs.GetFloat(_key);
                GetComponent<Slider>().value = sliderValue;
                break;
            
            case SavableSettingType.Toggle:
                bool toggleValue = PlayerPrefs.GetInt(_key) == 1;
                GetComponent<Toggle>().isOn = toggleValue;
                break;

            case SavableSettingType.Dropdown:
                int dropdownValue = PlayerPrefs.GetInt(_key);
                GetComponent<Dropdown>().value = dropdownValue;
                GetComponent<Dropdown>().RefreshShownValue();
                break;

            case SavableSettingType.InputField:
                string inputFieldValue = PlayerPrefs.GetString(_key);
                GetComponent<InputField>().text = inputFieldValue;
                break;
        }
    }

    void Start() {
        inited = false;
    }

    void Update() {
        if (!inited) {
            LoadValue();
            inited = true;
        }
        
        switch (settingType) {
            case SavableSettingType.Slider:
                if (!HasKey() || PlayerPrefs.GetFloat(_key) != GetComponent<Slider>().value) {
                    PlayerPrefs.SetFloat(_key, GetComponent<Slider>().value);
                }
                break;

            case SavableSettingType.Toggle:
                if (!HasKey() || PlayerPrefs.GetInt(_key) != (GetComponent<Toggle>().isOn ? 1 : 0)) {
                    PlayerPrefs.SetInt(_key, GetComponent<Toggle>().isOn ? 1 : 0);
                }
                break;

            case SavableSettingType.Dropdown:
                if (!HasKey() || PlayerPrefs.GetInt(_key) != GetComponent<Dropdown>().value) {
                    PlayerPrefs.SetInt(_key, GetComponent<Dropdown>().value);
                }
                break;

            case SavableSettingType.InputField:
                if (!HasKey() || PlayerPrefs.GetString(_key) != GetComponent<InputField>().text) {
                    PlayerPrefs.SetString(_key, GetComponent<InputField>().text);
                }
                break;
        }
    }
}
