using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image bgImage;
    public TextMeshProUGUI text;

    [Header("Màu sắc")]
    public Color normalBg = new Color32(245, 245, 245, 255); // Xám
    public Color pressedBg = new Color32(20, 60, 184, 255); // Xanh
    public Color normalText = Color.black;                      // Chữ đen
    public Color pressedText = Color.white;                     // Chữ trắng

    public void OnPointerDown(PointerEventData eventData)
    {
        // VỪA CHẠM TAY VÀO LÀ ĐỔI MÀU XANH + CHỮ TRẮNG NGAY LẬP TỨC
        if (bgImage) bgImage.color = pressedBg;
        if (text) text.color = pressedText;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // THẢ TAY RA LÀ VỀ XÁM + CHỮ ĐEN LẠI
        if (bgImage) bgImage.color = normalBg;
        if (text) text.color = normalText;
    }
}