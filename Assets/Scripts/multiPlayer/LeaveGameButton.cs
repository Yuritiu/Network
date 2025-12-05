using UnityEngine;

public class LeaveGameButtonHandler : MonoBehaviour
{
    public void OnLeaveButtonClicked()
    {
        if (NetworkSessionManager.Instance != null)
        {
            NetworkSessionManager.Instance.LeaveGame();
        }
        else
        {
            Debug.LogWarning("No NetworkSessionManager instance found when trying to leave game.");
        }
    }
}
