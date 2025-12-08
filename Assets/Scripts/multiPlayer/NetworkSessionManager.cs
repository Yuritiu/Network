using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSessionManager : MonoBehaviour
{
    public static NetworkSessionManager Instance;

    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool quittingToMenu = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // NetworkBootstrap should already call DontDestroyOnLoad on NetworkManager.
        // This object lives alongside it.
        DontDestroyOnLoad(gameObject);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        // remove disconnect callback to avoid leftover event subscription
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    public void LeaveGame()
    {
        // if network does not exist just go to menu
        if (NetworkManager.Singleton == null)
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        // mark that we are intentionally leaving
        quittingToMenu = true;

        // clear stored relay join code
        if (JoinCodeManager.Instance != null)
        {
            JoinCodeManager.Instance.SetCurrentCode("");
        }

        // shut down networking before changing scenes
        NetworkManager.Singleton.Shutdown();

        // load main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (quittingToMenu)
        {
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        // if local player was disconnected return to main menu
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
