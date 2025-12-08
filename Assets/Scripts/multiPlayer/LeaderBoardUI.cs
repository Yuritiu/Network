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
        var ordered = players.OrderByDescending(p => p.kills.Value).ThenBy(p => p.deaths.Value).ToList();

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Name             Kills   Deaths");
        stringBuilder.AppendLine("--------------------------------");

        foreach (var player in ordered)
        {
            // use fallback name if player name is empty
            string name;

            if (string.IsNullOrWhiteSpace(player.playerName))
                name = "Player " + player.OwnerClientId;
            else
                name = player.playerName;

            // format and add player row to leaderboard
            stringBuilder.AppendLine(string.Format("{0,-15}  {1,5}   {2,6}", name, player.kills.Value, player.deaths.Value));
        }

        // apply the final leaderboard text
        leaderboardText.text = stringBuilder.ToString();
    }
}
