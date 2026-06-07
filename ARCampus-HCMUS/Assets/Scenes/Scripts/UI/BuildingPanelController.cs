// UI/BuildingPanelController.cs
// THAY: hardcoded list → load từ FirebaseService
// THAY: GPSManager → GPSService (nếu có dùng)

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class BuildingPanelController : MonoBehaviour
{
    [Header("Setup")]
    public Transform contentParent;
    public GameObject buildingBtnPrefab;

    void OnEnable() => StartCoroutine(LoadBuildingsFromFirebase());

    IEnumerator LoadBuildingsFromFirebase()
    {
        // Chờ Firebase sẵn sàng
        float timeout = 5f;
        while ((FirebaseService.Instance == null || !FirebaseService.Instance.IsReady
                || FirebaseService.Instance.AllLocations.Count == 0) && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Xóa buttons cũ
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // Lấy danh sách tên tòa nhà duy nhất từ Firebase (dùng building field)
        var seen = new HashSet<string>();
        foreach (var loc in FirebaseService.Instance.AllLocations)
        {
            string b = string.IsNullOrEmpty(loc.building) ? loc.display_name : loc.building;
            if (seen.Add(b))
            {
                var btn = Instantiate(buildingBtnPrefab, contentParent);
                btn.GetComponentInChildren<TextMeshProUGUI>().text = b;
                string captured = b;
                btn.GetComponent<Button>().onClick.AddListener(() => OnBuildingSelected(captured));
            }
        }
    }

    void OnBuildingSelected(string buildingName)
    {
        Debug.Log($"🏢 Selected: {buildingName}");
        CampusUIManager.Instance.CloseAllPanels();
        // TODO: Mở FloorMap panel với tòa được chọn
    }
}