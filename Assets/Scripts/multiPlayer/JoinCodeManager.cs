using UnityEngine;

public class JoinCodeManager : MonoBehaviour
{
    public static JoinCodeManager Instance;

    public string CurrentJoinCode { get; private set; } = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // assign instance and persist across scenes
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCurrentCode(string code)
    {
        // update stored relay join code
        CurrentJoinCode = code;
        print(code);
    }
}
