using System.Linq;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Text leaderboardText;

    private void Start()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Only care about clients (including host)
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
            return;

        bool show = Input.GetKey(KeyCode.Tab);

        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(show);
        }

        if (show && leaderboardText != null)
        {
            RefreshLeaderboard();
        }
    }

    private void RefreshLeaderboard()
    {
        // Get all playerMovement components currently in the scene
        var players = FindObjectsOfType<playerManager>();

        // Order by kills descending, then deaths ascending
        var ordered = players
            .OrderByDescending(p => p.kills.Value)
            .ThenBy(p => p.deaths.Value)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("Name             Kills   Deaths");
        sb.AppendLine("--------------------------------");

        foreach (var p in ordered)
        {
            string name = !string.IsNullOrWhiteSpace(p.playerName)
                ? p.playerName
                : $"Player {p.OwnerClientId}";

            sb.AppendLine(
                $"{name,-15}  {p.kills.Value,5}   {p.deaths.Value,6}"
            );
        }

        leaderboardText.text = sb.ToString();
    }
}
