using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;
    private FirebaseFirestore db;
    public bool IsReady = false;
    public List<LocationData> AllLocations = new List<LocationData>();

    void Awake() => Instance = this;
    public List<LocationData> GetAllLocations()
    {
        return AllLocations;
    }
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
            }
        });
    }

    public void FetchAllLocations()
    {
        db.Collection("locations").GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (!task.IsCompleted || task.IsFaulted)
            {
                Debug.LogError("❌ Fetch failed: " + task.Exception);
                return;
            }

            AllLocations.Clear();
            foreach (var doc in task.Result.Documents)
            {
                LocationData loc = new LocationData
                {
                    location_id = doc.Id,
                    display_name = doc.ContainsField("display_name") ? doc.GetValue<string>("display_name") : "",
                    category = doc.ContainsField("category") ? doc.GetValue<string>("category") : "",
                    building = doc.ContainsField("building") ? doc.GetValue<string>("building") : "",
                    floor = doc.ContainsField("floor") ? doc.GetValue<int>("floor") : 0,
                    lat = doc.ContainsField("lat") ? doc.GetValue<double>("lat") : 0,
                    lng = doc.ContainsField("lng") ? doc.GetValue<double>("lng") : 0,
                    description = doc.ContainsField("description") ? doc.GetValue<string>("description") : ""
                };
                AllLocations.Add(loc);
                Debug.Log($"📍 Loaded: {loc.display_name} ({loc.category})");
            }
            Debug.Log($"✅ Total locations loaded: {AllLocations.Count}");
        });
    }
}