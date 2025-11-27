using UnityEngine;
using Unity.Netcode;
using Unity.Collections;      // for FixedString-types
using UnityEngine.UI;         // legacy UI Text

public class JoinCode : NetworkBehaviour
{
    [SerializeField] private Text joinCodeText;  // assign in prefab

    // Networked join code (server writes, everyone reads)
    private NetworkVariable<FixedString32Bytes> joinCode =
        new NetworkVariable<FixedString32Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Server sets the code once, based on JoinCodeManager
        if (IsServer)
        {
            if (JoinCodeManager.Instance != null &&
                !string.IsNullOrEmpty(JoinCodeManager.Instance.CurrentJoinCode))
            {
                joinCode.Value = JoinCodeManager.Instance.CurrentJoinCode;
            }
        }

        // Subscribe for when the value arrives / changes
        joinCode.OnValueChanged += OnJoinCodeChanged;

        // Also update immediately with whatever value we currently have
        OnJoinCodeChanged(joinCode.Value, joinCode.Value);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        joinCode.OnValueChanged -= OnJoinCodeChanged;
    }

    private void OnJoinCodeChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        if (joinCodeText == null) return;

        if (newValue.Length == 0)
        {
            joinCodeText.text = "";
        }
        else
        {
            joinCodeText.text = "Join: " + newValue.ToString();
        }
    }
}
