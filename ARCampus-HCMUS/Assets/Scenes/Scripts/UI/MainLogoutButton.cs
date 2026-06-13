using UnityEngine;
using UnityEngine.SceneManagement;

public class MainLogoutButton : MonoBehaviour
{
    public void Logout()
    {
        PlayerPrefs.DeleteKey("user_id");
        PlayerPrefs.DeleteKey("username");
        PlayerPrefs.DeleteKey("is_guest");
        PlayerPrefs.Save();

        SceneManager.LoadScene("LoginScene");
    }
}
