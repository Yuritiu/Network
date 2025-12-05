using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Network";

    [Header("UI")]
    [SerializeField] private InputField clientCodeInput; // Input for client to type code
    [SerializeField] private InputField nameInput;

    public static string LocalPlayerName = "Player";

    public async void OnHostClicked()
    {
        var nm = NetworkManager.Singleton;

        if (nm == null)
        {
            Debug.LogError("[MainMenuUI] No NetworkManager found in scene.");
            return;
        }

        // If something is already running (old session), shut it down first
        if (nm.IsClient || nm.IsServer || nm.IsListening)
        {
            Debug.Log("[MainMenuUI] Shutting down previous NetworkManager session before hosting.");
            nm.Shutdown();
        }

        SetLocalNameFromInput();

        try
        {
            // Ask Relay to create an allocation and start the host
            string relayJoinCode = await RelayManager.StartHostWithRelayAsync(maxConnections: 3);

            if (string.IsNullOrEmpty(relayJoinCode))
            {
                Debug.LogError("[MainMenuUI] Failed to start host with Relay (join code is null/empty).");
                return;
            }

            // Store it globally so HUD can display it
            if (JoinCodeManager.Instance != null)
            {
                JoinCodeManager.Instance.SetCurrentCode(relayJoinCode);
            }

            Debug.Log($"[MainMenuUI] Host started with Relay. Join code: {relayJoinCode}");

            // 🔹 Let Netcode handle the scene load as a *networked* scene
            nm.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MainMenuUI] Exception while starting host with Relay: " + e);
        }
    }

    public async void OnClientClicked()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[MainMenuUI] No NetworkManager found in scene.");
            return;
        }

        if (nm.IsClient || nm.IsServer || nm.IsListening)
        {
            Debug.Log("[MainMenuUI] Shutting down previous NetworkManager session before joining.");
            nm.Shutdown();
        }

        string code = clientCodeInput != null
            ? clientCodeInput.text.Trim().ToUpperInvariant()
            : "";

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[MainMenuUI] No join code entered.");
            return;
        }

        SetLocalNameFromInput();

        try
        {
            Debug.Log($"[MainMenuUI] Attempting to join via Relay with code: {code}");
            bool success = await RelayManager.StartClientWithRelayAsync(code);
            if (success)
            {
                Debug.Log($"[MainMenuUI] Client started with Relay, join code {code}.");
                // 🔹 Do NOT manually load scenes here.
                // Host's SceneManager.LoadScene will move us once connection is established.
            }
            else
            {
                Debug.LogError("[MainMenuUI] Failed to start client (StartClient returned false).");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MainMenuUI] Exception while joining via Relay: " + e);
        }
    }

    private void SetLocalNameFromInput()
    {
        if (nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text))
        {
            LocalPlayerName = nameInput.text;
        }
        else
        {
            // fallback
            LocalPlayerName = $"Player_{Random.Range(1000, 9999)}";
        }
    }

    public void OnQuitClicked()
    {
        Application.Quit();
    }
}
