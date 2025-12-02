using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Network";
    [SerializeField] private InputField clientCodeInput; // Input for client to type code
    [SerializeField] private InputField nameInput;
    public static string LocalPlayerName = "Player";

    public async void OnHostClicked()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm == null)
        {
            Debug.LogError("MainMenuUI: No NetworkManager found.");
            return;
        }

        if (nm.IsListening)
        {
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

            // Store it globally
            if (JoinCodeManager.Instance != null)
            {
                JoinCodeManager.Instance.SetCurrentCode(relayJoinCode);
            }

            Debug.Log($"[MainMenuUI] Host started with Relay. Join code: {relayJoinCode}");

            // Load game scene
            nm.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MainMenuUI] Exception while starting host with Relay: " + e);
        }
    }

    public async void OnClientClicked()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("MainMenuUI: No NetworkManager found.");
            return;
        }

        if (nm.IsListening)
        {
            nm.Shutdown();
        }

        SetLocalNameFromInput();

        string code = clientCodeInput != null ? clientCodeInput.text : "";
        code = code.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[MainMenuUI] No join code entered.");
            return;
        }
        
        try
        {
            bool success = await RelayManager.StartClientWithRelayAsync(code);

            if (success)
            {
                Debug.Log($"[MainMenuUI] Client started with Relay, join code {code}");
            }
            else
            {
                Debug.LogError("[MainMenuUI] Failed to start client with Relay.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[MainMenuUI] Exception while starting client with Relay: " + e);
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
        Debug.Log("[MainMenuUI] Quit pressed.");
   
        Application.Quit();
    }
}
