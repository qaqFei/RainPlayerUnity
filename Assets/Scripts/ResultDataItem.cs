using UnityEngine;
using UnityEngine.UI;
using System;

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
    public Material strokeRoundRect;

    void Start() {
        UpdateMaterial();
    }

    private void UpdateMaterial() {
        var board = transform.Find("Board").gameObject;
        var rawim = board.GetComponent<RawImage>();
        rawim.material = new Material(strokeRoundRect);
    }

    private void UpdateBoard() {
        var board = transform.Find("Board").gameObject;
        var rawim = board.GetComponent<RawImage>();

        board.GetComponent<RectTransform>().sizeDelta = new Vector2((float)width, (float)height);
        rawim.material.SetFloat("_Width", (float)width);
        rawim.material.SetFloat("_Height", (float)height);
        rawim.material.SetColor("_FillColor", fillColor);
        rawim.material.SetColor("_StrokeColor", outlineColor);
        rawim.material.SetFloat("_StrokeWidth", (float)outlineWidth);
        rawim.material.SetFloat("_RoundRadius", (float)height / 2);
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
        UpdateBoard();
        UpdateText();

        GetComponent<RectTransform>().sizeDelta = new Vector2((float)width, (float)height);
    }

    void OnValidate() {
        UpdateMaterial();
        Update();
    }
}
