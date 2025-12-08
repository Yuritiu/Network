using System.IO;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Network";
    [SerializeField] private InputField clientCodeInput; // input for client to type code
    [SerializeField] private InputField nameInput; // input for client to type name
    
    public static string LocalPlayerName = "Player";
    private const string PlayerNameFileName = "playername.json"; // file to save name

    [System.Serializable]
    private class PlayerNameData
    {
        public string playerName;
    }

    private void Start()
    {
        //checks if name is saved and uses it 
        LoadPlayerName();
    }

    private void LoadPlayerName()
    {
        string path = Path.Combine(Application.persistentDataPath, PlayerNameFileName);

        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            PlayerNameData data = JsonUtility.FromJson<PlayerNameData>(json);

            if (data != null && !string.IsNullOrWhiteSpace(data.playerName))
            {
                LocalPlayerName = data.playerName;

                //types name into the input field
                if (nameInput != null)
                {
                    nameInput.text = LocalPlayerName;
                }
            }
        }
        catch (System.Exception e)
        {
            return;
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
            //generates random name if no name has been set
            LocalPlayerName = "Player_" + Random.Range(1000, 9999);

            SavePlayerName(LocalPlayerName);
        }
    }

    private void SavePlayerName(string name)
    {
        string path = Path.Combine(Application.persistentDataPath, PlayerNameFileName);

        PlayerNameData data = new PlayerNameData { playerName = name };

        try
        {
            //saves name to file
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }
        catch (System.Exception e)
        {
            return;
        }
    }

    public async void OnHostClicked()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            return;
        }

        if (networkManager.IsListening)
        {
            networkManager.Shutdown();
        }

        //save the name typed out
        SetLocalNameFromInput();

        try
        {
            // ask relay to start the host
            string relayJoinCode = await RelayManager.StartHostWithRelayAsync(maxConnections: 3);

            if (string.IsNullOrEmpty(relayJoinCode))
            {
                return;
            }

            // store join code globally
            if (JoinCodeManager.Instance != null)
            {
                JoinCodeManager.Instance.SetCurrentCode(relayJoinCode);
            }

            // Load game scene
            networkManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            return;
        }
    }

    public async void OnClientClicked()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            return;
        }

        if (networkManager.IsListening)
        {
            networkManager.Shutdown();
        }

        //save the name typed out
        SetLocalNameFromInput();

        string code;

        if (clientCodeInput != null)
        {
            code = clientCodeInput.text;
        }
        else
        {
            code = "";
        }

        //ignores captials and spaces
        code = code.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(code))
        {
            return;
        }
        
        try
        {
            //loads into the game with the code typed
            bool success = await RelayManager.StartClientWithRelayAsync(code);
        }
        catch (System.Exception e)
        {
            return;
        }
    }

    public void OnQuitClicked()
    {
        //leaves game
        Application.Quit();
    }
}
