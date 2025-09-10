using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

using MilConst;

public interface I18nSupported {
    void OnI18nChanged();
}

public class I18nManager : MonoBehaviour
{
    public TextAsset i18nTextAsset;
    public Dropdown i18nDropdown;
    public string i18nLanguage = "en";
    private Dictionary<string, Dictionary<string, string>> i18nData;

    void Start() {
        i18nData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(i18nTextAsset.text);
        MilConst.MilConst.i18n = this;

        i18nDropdown.options.Clear();
        foreach (var key in i18nData.Keys) {
            var val = i18nData[key];
            i18nDropdown.options.Add(new Dropdown.OptionData(val["__LangName"]));
        }

        i18nDropdown.value = 0;
        i18nDropdown.RefreshShownValue();
    }

    void Update() {
        
    }

    public string GetText(string key) {
        if (!i18nData.ContainsKey(i18nLanguage)) return key;
        var lang = i18nData[i18nLanguage];
        if (lang.ContainsKey(key)) {
            return lang[key];
        }
        return key;
    }
    
    public void OnI18nChanged() {
        i18nLanguage = i18nData.Keys.ToArray()[i18nDropdown.value];
        var allMonobehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mb in allMonobehaviours) {
            if (mb is I18nSupported) {
                (mb as I18nSupported).OnI18nChanged();
            }
        }
    }

    void OnValidate() {
        Start();
    }
}
