
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
    public RectTransform mapScrollView;    
    public float mapBottomPaddingActive = 350f; 
    public float mapBottomPaddingInactive = 0f; 

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

    
    private IEnumerator MorphMapBottom(float targetBottom)
    {
        Vector2 offsetMin = mapScrollView.offsetMin;
        float startBottom = offsetMin.y;
        float time = 0;
        float duration = 0.3f; 

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            t = 1f - Mathf.Pow(1f - t, 3f); 
            offsetMin.y = Mathf.Lerp(startBottom, targetBottom, t);
            mapScrollView.offsetMin = offsetMin;
            yield return null;
        }

        offsetMin.y = targetBottom;
        mapScrollView.offsetMin = offsetMin;
    }
}
