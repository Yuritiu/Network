using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{
    [SerializeField] private InputField chatInput;
    [SerializeField] private Text chatLog;
    [SerializeField] private ScrollRect chatScrollRect;

    // UI root object for the chat (scroll view + log)
    [SerializeField] private GameObject chatContainer;

    // How long before chat auto-hides
    [SerializeField] private float hideDelay = 4f;

    private float hideTimer = 0f;
    private bool chatVisible = false;   // start hidden

    private void Start()
    {
        if (chatInput != null)
        {
            // submit message when enter is pressed
            chatInput.onEndEdit.AddListener(OnChatInputEndEdit);

            // show chat when player starts typing in the input box
            chatInput.onValueChanged.AddListener(OnChatInputValueChanged);
        }

        // start hidden
        if (chatContainer != null)
        {
            chatContainer.SetActive(false);
        }
    }

    private void Update()
    {
        if (!chatVisible)
            return;

        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                HideChat();
            }
        }
    }

    private void OnDestroy()
    {
        if (chatInput != null)
        {
            chatInput.onEndEdit.RemoveListener(OnChatInputEndEdit);
            chatInput.onValueChanged.RemoveListener(OnChatInputValueChanged);
        }
    }

    private void OnChatInputEndEdit(string text)
    {
        // this runs when you press enter or remove focus
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
            return;

        if (string.IsNullOrWhiteSpace(text))
            return;

        // send to server
        SendChatMessageServerRpc(text.Trim());

        // clear local input
        chatInput.text = "";
        chatInput.ActivateInputField();
    }

    // called whenever the text in the input box changes (i.e. user starts typing)
    private void OnChatInputValueChanged(string _)
    {
        ShowChat();
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
        ShowChat();   // chat also appears / refreshes when a message arrives
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

    private void ShowChat()
    {
        if (chatContainer != null)
        {
            chatContainer.SetActive(true);
        }
        chatVisible = true;
        hideTimer = hideDelay;
    }

    private void HideChat()
    {
        if (chatContainer != null)
        {
            chatContainer.SetActive(false);
        }
        chatVisible = false;
    }
}