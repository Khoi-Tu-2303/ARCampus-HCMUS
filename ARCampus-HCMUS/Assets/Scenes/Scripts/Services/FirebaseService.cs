







using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class FirebaseService : MonoBehaviour
{
    public static FirebaseService Instance;
    private FirebaseFirestore db;
    public bool IsReady = false;

    
    
    public List<LocationData> AllLocations = new List<LocationData>();
    public HashSet<string> ValidIndoorIds = new HashSet<string>();

    public event Action OnLocationsLoaded;

    void Awake() => Instance = this;

    public List<LocationData> GetAllLocations() => AllLocations;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                IsReady = true;
                Debug.Log("✅ Firebase connected!");
                FetchAllLocations();
            }
            else
            {
                Debug.LogError("❌ Firebase failed: " + task.Result);
                if (SystemModalController.Instance != null)
                    SystemModalController.Instance.ShowWarning(WarningType.Server, BackActionTarget.GoToLogin);
            }
        });
    }

    
    
    
    public void FetchAllLocations()
    {
        db.Collection("locations").GetSnapshotAsync().ContinueWithOnMainThread((Task<QuerySnapshot> task) => {
            if (!task.IsCompleted || task.IsFaulted)
            {
                Debug.LogError("❌ Fetch failed: " + task.Exception);

                
                
                if (AllLocations.Count == 0)
                {
                    if (SystemModalController.Instance != null)
                        SystemModalController.Instance.ShowWarning(WarningType.Server, BackActionTarget.GoToMain);
                }
                else
                {
                    Debug.LogWarning("⚠️ [Firebase] Fetch failed but using cached data. Count: " + AllLocations.Count);
                }
                return;
            }

            
            var staging = new List<LocationData>(task.Result.Count);
            foreach (var doc in task.Result.Documents)
            {
                staging.Add(new LocationData
                {
                    location_id = doc.Id,
                    display_name = doc.ContainsField("display_name") ? doc.GetValue<string>("display_name") : "",
                    category = doc.ContainsField("category") ? doc.GetValue<string>("category") : "",
                    building = doc.ContainsField("building") ? doc.GetValue<string>("building") : "",
                    floor = doc.ContainsField("floor") ? doc.GetValue<string>("floor") : "",
                    lat = doc.ContainsField("lat") ? doc.GetValue<double>("lat") : 0,
                    lng = doc.ContainsField("lng") ? doc.GetValue<double>("lng") : 0,
                    description = doc.ContainsField("description") ? doc.GetValue<string>("description") : ""
                });
            }

            
            AllLocations = staging;
            Debug.Log($"✅ Total locations loaded: {AllLocations.Count}");

            FetchAllIndoorRooms();
        });
    }

    
    
    
    private void FetchAllIndoorRooms()
    {
        db.Collection("description").GetSnapshotAsync().ContinueWithOnMainThread((Task<QuerySnapshot> task) => {
            if (task.IsCompleted && !task.IsFaulted)
            {
                ValidIndoorIds.Clear();
                foreach (var doc in task.Result.Documents)
                    ValidIndoorIds.Add(doc.Id);
                Debug.Log($"✅ Loaded {ValidIndoorIds.Count} indoor room IDs.");
            }
            else
            {
                
                Debug.LogWarning("⚠️ [Firebase] Could not load indoor room IDs. Indoor search may be limited.");
            }

            
            OnLocationsLoaded?.Invoke();
        });
    }

    
    public void GetBuildingDescription(string buildingName, Action<string> onComplete)
    {
        FirebaseFirestore.DefaultInstance
            .Collection("locations")
            .WhereEqualTo("display_name", buildingName)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread((Task<QuerySnapshot> task) => {
                if (task.IsCompleted && !task.IsFaulted && task.Result.Count > 0)
                {
                    var snapshot = task.Result[0];
                    string desc = snapshot.ContainsField("description")
                        ? snapshot.GetValue<string>("description")
                        : "Địa điểm này chưa có bài mô tả.";
                    onComplete?.Invoke(desc);
                }
                else
                {
                    onComplete?.Invoke("Không tìm thấy thông tin tòa nhà này trên Database do lỗi kết nối.");
                }
            });
    }

    public void GetIndoorDescription(string docId, Action<string> onComplete)
    {
        FirebaseFirestore.DefaultInstance
            .Collection("description")
            .Document(docId)
            .GetSnapshotAsync()
            .ContinueWithOnMainThread((Task<DocumentSnapshot> task) => {
                if (task.IsCompleted && !task.IsFaulted && task.Result.Exists)
                {
                    var snapshot = task.Result;
                    string content = snapshot.ContainsField("content")
                        ? snapshot.GetValue<string>("content")
                        : "Phòng/Khu vực này chưa có bài mô tả chi tiết.";
                    onComplete?.Invoke(content);
                }
                else
                {
                    onComplete?.Invoke("Không tìm thấy thông tin chi tiết trên hệ thống.");
                }
            });
    }
}
