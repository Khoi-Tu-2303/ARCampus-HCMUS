using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChatApp.Managers
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private const string SCENE_LOGIN = "LoginScene";
        private const string SCENE_CONV  = "ConversationListScene";
        private const string SCENE_CHAT  = "ChatScene";
        private const string SCENE_UPDATE_INFO = "UpdateInforScene";
        private const string SCENE_MAIN = "MainScene";

        
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("UIManager initialized");
        }
        public void LogoutToLogin()
        {
            AuthManager.Instance.Logout();
            SceneManager.LoadScene("LoginScene");
        }
        public void GoToLogin()
        {
            AuthManager.Instance.Logout(null);
            SceneManager.LoadScene(SCENE_LOGIN);
        }
        public void GoToConversations()  => SceneManager.LoadScene(SCENE_CONV);
        public void GoToChat()           => SceneManager.LoadScene(SCENE_CHAT);
        public void GoToUpdateInformation() => SceneManager.LoadScene(SCENE_UPDATE_INFO);
        public void GoToMainScene() => SceneManager.LoadScene(SCENE_MAIN);
    }
}
