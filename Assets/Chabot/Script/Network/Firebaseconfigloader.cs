using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ChatApp.Network
{
    public class FirebaseConfigLoader : MonoBehaviour
    {
        [Header("Firestore Config")]
        [Tooltip("https://firestore.googleapis.com/v1/projects/aicampus-858b3/databases/(default)/documents/server_config/backend_url")]
        [SerializeField] private string firestoreBaseUrl = "https://firestore.googleapis.com/v1/projects/aicampus-858b3/databases/(default)/documents";

        [Tooltip("Collection/DocumentId — ví dụ: server_config/backend_url")]
        [SerializeField] private string firestoreDocPath = "server_config/backend_url";

        [Header("Timeout (giây)")]
        [SerializeField] private float timeoutSeconds = 10f;

        public event Action<string> OnConfigLoaded;

        public event Action<string> OnLoadFailed;

        public IEnumerator LoadConfig()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Fail("Không có kết nối Internet.\nVui lòng kiểm tra lại mạng.");
                yield break;
            }

            yield return FetchFromFirestore();
        }

        private IEnumerator FetchFromFirestore()
        {
            string url = $"{firestoreBaseUrl.TrimEnd('/')}/{firestoreDocPath}";
            Debug.Log($"[FirebaseConfig] Fetching: {url}");

            using var req = UnityWebRequest.Get(url);
            req.timeout = Mathf.RoundToInt(timeoutSeconds);
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"RESULT = {req.result}");
                Debug.Log($"HTTP CODE = {req.responseCode}");
                Debug.Log($"ERROR = {req.error}");
                Debug.Log($"BODY = {req.downloadHandler.text}");
                Fail("Không thể kết nối đến máy chủ.\nVui lòng thử lại.");
                yield break;
            }

            try
            {
                var doc = JsonUtility.FromJson<FirestoreDocument>(req.downloadHandler.text);
                string publicUrl = doc?.fields?.base_url?.stringValue;

                if (!string.IsNullOrEmpty(publicUrl))
                {
                    OnConfigLoaded?.Invoke(publicUrl.TrimEnd('/'));
                }
                else
                {
                    Fail("Không lấy được địa chỉ máy chủ.\nVui lòng thử lại.");
                }
            }
            catch (Exception e)
            {
                Fail("Lỗi xử lý dữ liệu từ máy chủ.\nVui lòng thử lại.");
            }
        }

        private void Fail(string msg)
        {
            OnLoadFailed?.Invoke(msg);
        }

        [Serializable] private class FirestoreDocument { public FirestoreFields fields; }
        [Serializable] private class FirestoreFields { public FirestoreStringValue base_url; }
        [Serializable] private class FirestoreStringValue { public string stringValue; }
    }
}