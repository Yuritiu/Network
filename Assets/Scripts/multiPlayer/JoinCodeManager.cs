using UnityEngine;

public class JoinCodeManager : MonoBehaviour
{
    public static JoinCodeManager Instance { get; private set; }

    // Relay join code currently in use (host only really cares about this)
    public string CurrentJoinCode { get; private set; } = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called from MainMenuUI after Relay gives us the join code
    public void SetCurrentCode(string code)
    {
        CurrentJoinCode = code;
        Debug.Log($"[JoinCodeManager] Relay join code set to {CurrentJoinCode}");
    }
}
