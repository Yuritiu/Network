using System.Text;
using Unity.Netcode;
using UnityEngine;

public class JoinCodeManager : MonoBehaviour
{
    public static JoinCodeManager Instance { get; private set; }

    [Tooltip("The current 6-digit join code for this host.")]
    [SerializeField] private string currentJoinCode = "";

    public string CurrentJoinCode => currentJoinCode;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        NetworkManager nm = NetworkManager.Singleton;
        
        if (nm != null)
        {
            nm.ConnectionApprovalCallback += OnConnectionApproval;
        }
        else
        {
            Debug.LogError("JoinCodeManager: No NetworkManager found in scene.");
        }
    }

    //call this on the host before StartHost().
    public void GenerateAndSetHostCode()
    {
        //random 6-digit code 
        int code = Random.Range(0, 1000000);
        currentJoinCode = code.ToString("D6");
        Debug.Log($"[JoinCodeManager] Host code set to {currentJoinCode}");
    }

    //sets the expected code explicitly
    public void SetHostCode(string code)
    {
        currentJoinCode = code;
        Debug.Log($"[JoinCodeManager] Host code set to {currentJoinCode}");
    }

    private void OnConnectionApproval(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        var nm = NetworkManager.Singleton;

        //host connects first with possibly empty data
        bool isLocalHost = request.ClientNetworkId == nm.LocalClientId;

        string clientCode = "";

        if (request.Payload != null && request.Payload.Length > 0)
        {
            clientCode = Encoding.UTF8.GetString(request.Payload);
        }

        bool approved = isLocalHost || (!string.IsNullOrEmpty(currentJoinCode) && clientCode == currentJoinCode);

        Debug.Log($"[JoinCodeManager] Client {request.ClientNetworkId} join code '{clientCode}' " + $"vs host code '{currentJoinCode}' => approved = {approved}");

        response.Approved = approved;
        response.CreatePlayerObject = approved;
        response.PlayerPrefabHash = null;
        response.Position = null;
        response.Rotation = null;
    }
}
