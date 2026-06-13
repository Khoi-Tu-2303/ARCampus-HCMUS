using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image bgImage;
    public TextMeshProUGUI text;

    [Header("Màu sắc")]
    public Color normalBg = new Color32(245, 245, 245, 255); 
    public Color pressedBg = new Color32(20, 60, 184, 255); 
    public Color normalText = Color.black;                      
    public Color pressedText = Color.white;                     

    public void OnPointerDown(PointerEventData eventData)
    {
        
        if (bgImage) bgImage.color = pressedBg;
        if (text) text.color = pressedText;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        
        if (bgImage) bgImage.color = normalBg;
        if (text) text.color = normalText;
    }
}
