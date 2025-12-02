using UnityEngine;
using UnityEngine.UI;

public class HostCode : MonoBehaviour
{
    [SerializeField] private Text codeText;

    private void Start()
    {
        if (codeText == null) return;
        if (JoinCodeManager.Instance == null) return;

        string code = JoinCodeManager.Instance.CurrentJoinCode;
        if (!string.IsNullOrEmpty(code))
        {
            codeText.text = $"Join Code: {code}";
        }
    }
}
