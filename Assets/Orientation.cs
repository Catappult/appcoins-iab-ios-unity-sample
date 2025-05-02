using UnityEngine;

public class UIOrientationSwitcher : MonoBehaviour
{
    public GameObject portraitUI;
    public GameObject landscapeUI;

    void Start()
    {
        ApplyOrientation();
    }

    void Update()
    {
        // Switch live when the user rotates the device:
        ApplyOrientation();
    }

    private void ApplyOrientation()
    {
        bool isLandscape = Screen.width > Screen.height;
        portraitUI.SetActive(!isLandscape);
        landscapeUI.SetActive(isLandscape);
    }
}