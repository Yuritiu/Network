using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSessionManager : MonoBehaviour
{
    public static NetworkSessionManager Instance { get; private set; }

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

        // We assume NetworkBootstrap already calls DontDestroyOnLoad on NetworkManager.
        // This object lives alongside it.
        DontDestroyOnLoad(gameObject);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    public void LeaveGame()
    {
        if (NetworkManager.Singleton == null)
        {
            // Just go back to menu if no network session
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        quittingToMenu = true;

        // Clear join code, optional
        if (JoinCodeManager.Instance != null)
        {
            JoinCodeManager.Instance.SetCurrentCode("");
        }

        // Shut down NGO (host or client, doesn't matter)
        NetworkManager.Singleton.Shutdown();

        // Return to main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Ignore callbacks caused by our own explicit LeaveGame
        if (quittingToMenu)
            return;

        if (NetworkManager.Singleton == null)
            return;

        // Only react for *this* client
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("[NetworkSessionManager] Disconnected from host, returning to main menu.");
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
