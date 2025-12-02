using UnityEngine;

public class JoinCodeManager : MonoBehaviour
{
    public static JoinCodeManager Instance { get; private set; }

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

    public void SetCurrentCode(string code)
    {
        CurrentJoinCode = code;
        Debug.Log($"[JoinCodeManager] Relay join code set to {CurrentJoinCode}");
    }
}
