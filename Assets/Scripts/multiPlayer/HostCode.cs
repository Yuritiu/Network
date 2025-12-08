using UnityEngine;
using UnityEngine.UI;

public class HostCode : MonoBehaviour
{
    [SerializeField] private Text codeText;

    private void Start()
    {
        // exit if ui text or join code manager is missing
        if (codeText == null) return;
        if (JoinCodeManager.Instance == null) return;

        // get stored join code and display it if valid
        string code = JoinCodeManager.Instance.CurrentJoinCode;
        if (!string.IsNullOrEmpty(code))
        {
            codeText.text = $"Join Code: {code}";
        }
    }
}
