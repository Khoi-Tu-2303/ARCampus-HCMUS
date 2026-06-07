// Navigation/NavigationHUD.cs
using UnityEngine;
using TMPro;
using System.Collections;

public class NavigationHUD : MonoBehaviour
{
    [Header("AR Top Card")]
    public TextMeshProUGUI txtDestination;
    public TextMeshProUGUI txtDistanceETA;

    [Header("2D Map UI")]
    public GameObject mapNavCardPanel;
    public TextMeshProUGUI mapTxtDestination;
    public TextMeshProUGUI mapTxtDistanceETA;

    [Header("Map Morph Animation (MỚI)")]
    public RectTransform mapScrollView;    // Kéo cái Scroll View của bản đồ vào đây
    public float mapBottomPaddingActive = 350f; // Độ cao lúc có Card dẫn đường (tùy chỉnh)
    public float mapBottomPaddingInactive = 0f; // Độ cao lúc bình thường (0 là full màn)

    [Header("Compass")]
    public RectTransform arrowImage;

    private Coroutine _morphCoroutine;

    public void UpdateTopCard(string destinationName, float totalDistance)
    {
        if (txtDestination != null) txtDestination.text = destinationName;
        int walkMin = Mathf.Max(1, Mathf.CeilToInt(totalDistance / 70f));
        string etaText = $"Còn {totalDistance:F0}m - {walkMin} phút đi bộ";

        if (txtDistanceETA != null) txtDistanceETA.text = etaText;
        if (mapTxtDestination != null) mapTxtDestination.text = destinationName;
        if (mapTxtDistanceETA != null) mapTxtDistanceETA.text = etaText;

        // Hiện thẻ Bottom Card & Kéo Map co lại
        if (mapNavCardPanel != null && !mapNavCardPanel.activeSelf)
        {
            mapNavCardPanel.SetActive(true);
            if (mapScrollView != null)
            {
                if (_morphCoroutine != null) StopCoroutine(_morphCoroutine);
                _morphCoroutine = StartCoroutine(MorphMapBottom(mapBottomPaddingActive));
            }
        }
    }

    public void ClearUI()
    {
        if (txtDestination != null) txtDestination.text = "Đang tìm đường...";
        if (txtDistanceETA != null) txtDistanceETA.text = "-- m  •  -- min walk";

        // Tắt thẻ Bottom Card & Kéo Map giãn dài ra full màn hình
        if (mapNavCardPanel != null && mapNavCardPanel.activeSelf)
        {
            mapNavCardPanel.SetActive(false);
            if (mapScrollView != null)
            {
                if (_morphCoroutine != null) StopCoroutine(_morphCoroutine);
                _morphCoroutine = StartCoroutine(MorphMapBottom(mapBottomPaddingInactive));
            }
        }
    }

    public void SetArrowAngle(float angleDeg)
    {
        if (arrowImage != null) arrowImage.rotation = Quaternion.Euler(0, 0, -angleDeg);
    }

    // ✅ HIỆU ỨNG KÉO GIÃN BẢN ĐỒ MƯỢT MÀ
    private IEnumerator MorphMapBottom(float targetBottom)
    {
        Vector2 offsetMin = mapScrollView.offsetMin;
        float startBottom = offsetMin.y;
        float time = 0;
        float duration = 0.3f; // Thời gian chạy hiệu ứng: 0.3 giây

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            t = 1f - Mathf.Pow(1f - t, 3f); // Ease-out mượt mà
            offsetMin.y = Mathf.Lerp(startBottom, targetBottom, t);
            mapScrollView.offsetMin = offsetMin;
            yield return null;
        }

        offsetMin.y = targetBottom;
        mapScrollView.offsetMin = offsetMin;
    }
}