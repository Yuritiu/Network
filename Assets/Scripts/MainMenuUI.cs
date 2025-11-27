using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Network";

    [Header("UI References")]
    [SerializeField] private Text hostCodeText;        // Text to show the host's code
    [SerializeField] private InputField clientCodeInput; // Input for client to type code

    public void OnHostClicked()
    {
        NetworkManager nm = NetworkManager.Singleton;
        
        if (nm == null)
        {
            Debug.LogError("MainMenuUI: No NetworkManager found.");
            return;
        }

        // generate and set host join code
        JoinCodeManager.Instance.GenerateAndSetHostCode();
        string code = JoinCodeManager.Instance.CurrentJoinCode;

        if (hostCodeText != null)
        {
            hostCodeText.text = $"Join Code: {code}";
        }

        // host usually doesn't need to send connectionData for itself
        nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(code);

        if (nm.StartHost())
        {
            Debug.Log("[MainMenuUI] Host started, loading game scene...");
            nm.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("[MainMenuUI] Failed to start host.");
        }
    }

    public void OnClientClicked()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("MainMenuUI: No NetworkManager found.");
            return;
        }

        string code = clientCodeInput != null ? clientCodeInput.text : "";
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
        {
            Debug.LogWarning("[MainMenuUI] Invalid join code entered.");
            return;
        }

        // set the code to send with the connection request
        nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(code);

        if (nm.StartClient())
        {
            Debug.Log($"[MainMenuUI] Client started with join code {code}, waiting for approval...");
        }
        else
        {
            Debug.LogError("[MainMenuUI] Failed to start client.");
        }
    }

    public void OnQuitClicked()
    {
        Debug.Log("[MainMenuUI] Quit pressed.");
   
        Application.Quit();
    }
}
