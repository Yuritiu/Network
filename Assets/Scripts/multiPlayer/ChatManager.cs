using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{
    [SerializeField] private InputField chatInput;
    [SerializeField] private Text chatLog;
    [SerializeField] private ScrollRect chatScrollRect;

    private void Start()
    {
        if (chatInput != null)
        {
            // submit when pressing Enter
            chatInput.onEndEdit.AddListener(OnChatInputEndEdit);
        }
    }

    private void OnDestroy()
    {
        if (chatInput != null)
        {
            chatInput.onEndEdit.RemoveListener(OnChatInputEndEdit);
        }
    }

    private void OnChatInputEndEdit(string text)
    {
        // This fires when you press Enter or remove focus
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
            return;

        if (string.IsNullOrWhiteSpace(text))
            return;

        // send to server
        SendChatMessageServerRpc(text.Trim());

        // clear local input and refocus
        chatInput.text = "";
        chatInput.ActivateInputField();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendChatMessageServerRpc(string message, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        string senderName = GetPlayerName(senderId);

        // send to all clients
        ReceiveChatMessageClientRpc(senderId, senderName, message);
    }

    [ClientRpc]
    private void ReceiveChatMessageClientRpc(ulong senderId, string senderName, string message)
    {
        string line = $"{senderName}: {message}\n";

        if (chatLog != null)
        {
            chatLog.text += line;

            // Force the content to rebuild so scroll view knows the new height
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatLog.rectTransform);

            // Auto-scroll to bottom
            if (chatScrollRect != null)
            {
                chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        Debug.Log($"[Chat] {line}");
    }

    private string GetPlayerName(ulong clientId)
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            playerMovement pm = client.PlayerObject.GetComponent<playerMovement>();
            if (pm != null && !string.IsNullOrWhiteSpace(pm.playerName))
            {
                return pm.playerName;
            }
        }

        return $"Player_{clientId}";
    }
}
