using System.IO;
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
    private const string PlayerNameFileName = "playername.json";

    [System.Serializable]
    private class PlayerNameData
    {
        public string playerName;
    }

    private void Start()
    {
        LoadPlayerName();
    }

    private void LoadPlayerName()
    {
        string path = Path.Combine(Application.persistentDataPath, PlayerNameFileName);

        if (!File.Exists(path))
            return;

        try
        {
            string json = File.ReadAllText(path);
            PlayerNameData data = JsonUtility.FromJson<PlayerNameData>(json);

            if (data != null && !string.IsNullOrWhiteSpace(data.playerName))
            {
                LocalPlayerName = data.playerName;

                // Auto-type it into the input field
                if (nameInput != null)
                {
                    nameInput.text = LocalPlayerName;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[MainMenuUI] Failed to load player name: " + e);
        }
    }

    private void SetLocalNameFromInput()
    {
        if (nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text))
        {
            LocalPlayerName = nameInput.text.Trim();
            SavePlayerName(LocalPlayerName);
        }
        else
        {
            LocalPlayerName = $"Player_{Random.Range(1000, 9999)}";
            SavePlayerName(LocalPlayerName);
        }
    }

    private void SavePlayerName(string name)
    {
        string path = Path.Combine(Application.persistentDataPath, PlayerNameFileName);

        PlayerNameData data = new PlayerNameData { playerName = name };

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[MainMenuUI] Failed to save player name: " + e);
        }
    }

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

    public void OnQuitClicked()
    {
        Debug.Log("[MainMenuUI] Quit pressed.");
   
        Application.Quit();
    }
}
