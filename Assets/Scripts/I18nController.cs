using UnityEngine;
using UnityEngine.UI;

using MilConst;

public class I18nController : MonoBehaviour
{
    public string key = "i18n_key";
    public string formatTemplate = "{0}";

    void Start() {
        
    }

    private void SetText(string text) {
        if (GetComponent<Text>() != null) GetComponent<Text>().text = text;
        if (GetComponent<TextMesh>() != null) GetComponent<TextMesh>().text = text;
    }

    void Update() {
        if (MilConst.MilConst.i18n == null) return;
        var text = MilConst.MilConst.i18n.GetText(key);
        text = string.Format(formatTemplate, text);

        SetText(text);
    }

    void OnValidate() {
        Update();
    }
}
