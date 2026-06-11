using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using UnityEngine;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    private const string COLLECTION = "campusInfo";

    private FirebaseFirestore _db;
    private bool _initialized = false;

    public bool IsInitialized => _initialized;

    private readonly TaskCompletionSource<bool> _initTcs = new TaskCompletionSource<bool>();
    public Task WaitUntilInitialized => _initTcs.Task;

    void Awake()
    {
        Debug.Log("[FirebaseManager] Awake");

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Gọi init ngay trong Awake, không chờ Start
        _ = InitializeFirebase();
    }

    // Xóa hoặc để trống Start()
    void Start() { }

    // ───────────────── INIT ─────────────────
    private async Task InitializeFirebase()
    {
        Debug.Log("[FirebaseManager] >>> STEP 1: Begin");

        try
        {
            Debug.Log("[FirebaseManager] >>> STEP 2: Calling CheckAndFixDependenciesAsync...");

            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            Debug.Log($"[FirebaseManager] >>> STEP 3: Status = {dependencyStatus}");

            if (dependencyStatus == DependencyStatus.Available)
            {
                Debug.Log("[FirebaseManager] >>> STEP 4: Creating Firestore...");
                _db = FirebaseFirestore.DefaultInstance;
                _initialized = true;
                _initTcs.TrySetResult(true);
                Debug.Log("[FirebaseManager] >>> STEP 5: DONE");
            }
            else
            {
                Debug.LogError($"[FirebaseManager] >>> STEP 3 FAILED: {dependencyStatus}");
                _initTcs.TrySetResult(false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[FirebaseManager] >>> EXCEPTION at init: " + e);
            _initTcs.TrySetResult(false);
        }
    }

    // ───────────────── WAIT INIT ─────────────────
    private async Task<bool> EnsureInitializedAsync()
    {
        Debug.Log("[FirebaseManager] Waiting for Firebase init...");

        var ready =
            await Task.WhenAny(
                WaitUntilInitialized,
                Task.Delay(10_000));

        if (ready != WaitUntilInitialized || !_initialized)
        {
            Debug.LogError(
                "[FirebaseManager] Firebase NOT initialized");

            return false;
        }

        Debug.Log("[FirebaseManager] Firebase confirmed initialized");

        return true;
    }

    // ───────────────── WRITE ─────────────────
    public async Task<bool> UpdateDepartmentField(
        string documentId,
        string fieldKey,
        string value)
    {
        Debug.Log(
            $"[FirebaseManager] WRITE BEGIN: {documentId}.{fieldKey}");

        if (!await EnsureInitializedAsync())
            return false;

        try
        {
            var writeTask =
                _db.Collection(COLLECTION)
                   .Document(documentId)
                   .SetAsync(
                        new Dictionary<string, object>
                        {
                            { fieldKey, value }
                        },
                        SetOptions.MergeAll
                    );

            var timeoutTask = Task.Delay(5_000);

            var completed =
                await Task.WhenAny(writeTask, timeoutTask);

            if (completed == timeoutTask)
            {
                Debug.LogError(
                    $"[FirebaseManager] WRITE TIMEOUT: {documentId}.{fieldKey}");

                return false;
            }

            await writeTask;

            Debug.Log(
                $"[FirebaseManager] WRITE SUCCESS: {documentId}.{fieldKey}");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[FirebaseManager] WRITE ERROR: {documentId}.{fieldKey}");

            Debug.LogError(e.ToString());

            return false;
        }
    }

    // ───────────────── READ ─────────────────
    public async Task<string> GetDepartmentField(
        string documentId,
        string fieldKey)
    {
        Debug.Log(
            $"[FirebaseManager] READ BEGIN: {documentId}.{fieldKey}");

        if (!await EnsureInitializedAsync())
            return null;

        try
        {
            var firestoreTask =
                _db.Collection(COLLECTION)
                   .Document(documentId)
                   .GetSnapshotAsync();

            var timeoutTask = Task.Delay(5_000);

            var completed =
                await Task.WhenAny(
                    firestoreTask,
                    timeoutTask);

            if (completed == timeoutTask)
            {
                Debug.LogError(
                    $"[FirebaseManager] READ TIMEOUT: {documentId}.{fieldKey}");

                return null;
            }

            DocumentSnapshot snapshot =
                await firestoreTask;

            if (!snapshot.Exists)
            {
                Debug.LogWarning(
                    $"[FirebaseManager] DOCUMENT NOT FOUND: {documentId}");

                return "";
            }

            var dict = snapshot.ToDictionary();

            string result =
                dict.TryGetValue(fieldKey, out var v)
                ? v?.ToString() ?? ""
                : "";

            Debug.Log(
                $"[FirebaseManager] READ SUCCESS: {documentId}.{fieldKey} = {result}");

            return result;
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[FirebaseManager] READ ERROR: {documentId}.{fieldKey}");

            Debug.LogError(e.ToString());

            return null;
        }
    }
}