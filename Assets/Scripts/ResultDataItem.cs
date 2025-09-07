using UnityEngine;
using UnityEngine.UI;

public class ResultDataItem : MonoBehaviour
{
    [Header("Data")]
    public double width;
    public double height;
    public Color fillColor;
    public double outlineWidth;
    public Color outlineColor;
    public double padding;
    public int fontSize;
    public Color textColor;
    public string leftText;
    public string rightText;
    public int textFontScale;

    [Header("References")]
    public Material halfCircMaterial;

    void Start() {
        UpdateMaterial();
    }
    
    private void SafeDestroyMaterial(Material mat) {
        if (mat == null) return;

        #if UNITY_EDITOR
            if (Application.isPlaying) Destroy(mat);
            else DestroyImmediate(mat, false);
        #else
            Destroy(mat);
        #endif
    }

    private void UpdateMaterial() {
        var outline = transform.Find("Outline").gameObject;
        var leftCircRawImage = outline.transform.Find("LeftCirc").gameObject.GetComponent<RawImage>();
        var rightCircRawImage = outline.transform.Find("RightCirc").gameObject.GetComponent<RawImage>();
        if (leftCircRawImage.material) SafeDestroyMaterial(leftCircRawImage.material);
        if (rightCircRawImage.material) SafeDestroyMaterial(rightCircRawImage.material);
        leftCircRawImage.material = new Material(halfCircMaterial);
        rightCircRawImage.material = new Material(halfCircMaterial);
    }

    private void UpdateFill() {
        var fill = transform.Find("Fill").gameObject;
        var rect = fill.transform.Find("Rect").gameObject;
        var leftCirc = fill.transform.Find("LeftCirc").gameObject;
        var rightCirc = fill.transform.Find("RightCirc").gameObject;

        rect.GetComponent<RawImage>().color = fillColor;
        leftCirc.GetComponent<RawImage>().color = fillColor;
        rightCirc.GetComponent<RawImage>().color = fillColor;
        rect.GetComponent<RectTransform>().sizeDelta = new Vector2((float)(width - height), (float)height);
        leftCirc.GetComponent<RectTransform>().sizeDelta = new Vector2((float)height, (float)(height / 2));
        rightCirc.GetComponent<RectTransform>().sizeDelta = new Vector2((float)height, (float)(height / 2));
        leftCirc.transform.localPosition = new Vector3((float)(-(width - height) / 2), 0, 0);
        rightCirc.transform.localPosition = new Vector3((float)((width - height) / 2), 0, 0);
    }

    private void UpdateOutline() {
        var outline = transform.Find("Outline").gameObject;
        var rectBartop = outline.transform.Find("RectBartop").gameObject;
        var rectBarbottom = outline.transform.Find("RectBarbottom").gameObject;
        var leftCirc = outline.transform.Find("LeftCirc").gameObject;
        var rightCirc = outline.transform.Find("RightCirc").gameObject;

        rectBartop.GetComponent<RawImage>().color = outlineColor;
        rectBarbottom.GetComponent<RawImage>().color = outlineColor;
        rectBartop.GetComponent<RectTransform>().sizeDelta = new Vector2((float)(width - height), (float)outlineWidth);
        rectBarbottom.GetComponent<RectTransform>().sizeDelta = new Vector2((float)(width - height), (float)outlineWidth);
        rectBartop.transform.localPosition = new Vector3(0, (float)(height / 2), 0);
        rectBarbottom.transform.localPosition = new Vector3(0, (float)(-height / 2), 0);
        leftCirc.GetComponent<RectTransform>().sizeDelta = new Vector2((float)height, (float)(height / 2));
        rightCirc.GetComponent<RectTransform>().sizeDelta = new Vector2((float)height, (float)(height / 2));
        leftCirc.transform.localPosition = new Vector3((float)(-(width - height) / 2), 0, 0);
        rightCirc.transform.localPosition = new Vector3((float)((width - height) / 2), 0, 0);

        var leftCircRawImage = leftCirc.GetComponent<RawImage>();
        var rightCircRawImage = rightCirc.GetComponent<RawImage>();
        leftCircRawImage.color = outlineColor;
        rightCircRawImage.color = outlineColor;
        leftCircRawImage.material.SetFloat("_R", (float)(1.0 - outlineWidth / (height / 2)));
        leftCircRawImage.material.SetColor("_MultColor", outlineColor);
        rightCircRawImage.material.SetFloat("_R", (float)(1.0 - outlineWidth / (height / 2)));
        rightCircRawImage.material.SetColor("_MultColor", outlineColor);
    }

    private void UpdateText() {
        var leftTextGameObject = transform.Find("LeftText").gameObject;
        var rightTextGameObject = transform.Find("RightText").gameObject;
        var leftTextComp = leftTextGameObject.GetComponent<Text>();
        var rightTextComp = rightTextGameObject.GetComponent<Text>();
        
        leftTextComp.text = leftText;
        rightTextComp.text = rightText;
        leftTextComp.color = textColor;
        rightTextComp.color = textColor;
        leftTextComp.fontSize = fontSize * textFontScale;
        rightTextComp.fontSize = fontSize * textFontScale;

        leftTextGameObject.transform.localPosition = new Vector3((float)(-width / 2 + padding), 0, 0);
        rightTextGameObject.transform.localPosition = new Vector3((float)(width / 2 - padding), 0, 0);
        leftTextGameObject.transform.localScale = new Vector3(1.0f / (float)textFontScale, 1.0f / (float)textFontScale, 1.0f);
        rightTextGameObject.transform.localScale = new Vector3(1.0f / (float)textFontScale, 1.0f / (float)textFontScale, 1.0f);
    }

    void Update() {
        UpdateFill();
        UpdateOutline();
        UpdateText();
    }

    void OnValidate() {
        UpdateMaterial();
        Update();
    }
}
