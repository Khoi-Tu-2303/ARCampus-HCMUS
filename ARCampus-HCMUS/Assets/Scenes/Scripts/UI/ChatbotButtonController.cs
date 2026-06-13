using UnityEngine;
using UnityEngine.SceneManagement;

public class ChatbotButtonController : MonoBehaviour
{
    public void OpenChatbot()
    {
        SceneManager.LoadScene("ConversationListScene");
    }
}
