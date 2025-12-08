using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private bool isOpen = false;

    private void Start()
    {
        if (pausePanel != null)
        {
            //disables pause menu
            pausePanel.SetActive(false);
        }
    }

    private void Update()
    {
        //shows leave button
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isOpen = !isOpen;

        //enables or disables pause menu (depends on current state)
        if (pausePanel != null)
        {
            pausePanel.SetActive(isOpen);
        }
    }
}
