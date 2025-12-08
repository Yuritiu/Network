using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{
    [SerializeField] private InputField chatInput;
    [SerializeField] private Text chatLog;
    [SerializeField] private ScrollRect chatScrollRect;

    // UI object for the chat (scroll view & log)
    [SerializeField] private GameObject chatContainer;

    // How long before chat auto hides
    [SerializeField] private float hideDelay = 4f;

    private float hideTimer = 0f;
    
    // start hidden
    private bool chatVisible = false;

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
        {
            return;
        }

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
        // remove listeners when object is destroyed to avoid leaks
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

    // called whenever the text in the input box changes (when user starts typing)
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

            //force the content to rebuild so scroll view knows the new height
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatLog.rectTransform);

            //auto scroll to bottom
            if (chatScrollRect != null)
            {
                chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        ShowChat();   // chat also appears / refreshes when a message arrives
    }

    private string GetPlayerName(ulong clientId)
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            playerManager playerName = client.PlayerObject.GetComponent<playerManager>();

            if (playerName != null && !string.IsNullOrWhiteSpace(playerName.playerName))
            {
                return playerName.playerName;
            }
        }

        return "Player_" + clientId;
    }

    private void ShowChat()
    {
        // show chat ui container
        if (chatContainer != null)
        {
            chatContainer.SetActive(true);
        }

        // mark chat as visible and reset hide timer
        chatVisible = true;
        hideTimer = hideDelay;
    }

    private void HideChat()
    {
        // hide chat ui container
        if (chatContainer != null)
        {
            chatContainer.SetActive(false);
        }

        // mark chat as hidden
        chatVisible = false;
    }
}