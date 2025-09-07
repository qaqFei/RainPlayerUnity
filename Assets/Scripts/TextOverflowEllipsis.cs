using UnityEngine;
using UnityEngine.UI;
using System.Text;

[RequireComponent(typeof(Text))]
public class TextOverflowEllipsis : MonoBehaviour
{
    void Start() {
        
    }
    
    public void SetOverflowEllipsisText(string originalText) {
        var textComponent = GetComponent<Text>();
        var rectTransform = GetComponent<RectTransform>();
        textComponent.text = originalText;
        Canvas.ForceUpdateCanvases();

        if (textComponent.preferredWidth <= rectTransform.rect.width) return;
            
        int min = 0;
        int max = originalText.Length;
        int best = 0;
        
        while (min <= max)
        {
            int mid = (min + max) / 2;
            textComponent.text = $"{originalText.Substring(0, mid)}...";
            Canvas.ForceUpdateCanvases();
            
            if (textComponent.preferredWidth <= rectTransform.rect.width)
            {
                best = mid;
                min = mid + 1;
            }
            else
            {
                max = mid - 1;
            }
        }
        
        textComponent.text = $"{originalText.Substring(0, best)}...";
    }
}
