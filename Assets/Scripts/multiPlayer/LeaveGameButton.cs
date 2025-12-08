using UnityEngine;

public class LeaveGameButtonHandler : MonoBehaviour
{
    public void OnLeaveButtonClicked()
    {
        if (NetworkSessionManager.Instance != null)
        {
            NetworkSessionManager.Instance.LeaveGame();
        }
    }
}
