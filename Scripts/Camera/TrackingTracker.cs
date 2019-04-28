using UnityEngine;
using UnityEngine.UI;

// Check on tracking state, reset tracking if it's in a SEARCHING state for too long.
// Tracking will try to reset itself to the last known 'good' position if it can.
// Make UI elements display change if the tracking is bad - red borders will display around the window.
// When tracking resets, a message will appear on screen "UPDATING TRACKING".
// The ZED camera uses what it sees to track its position.
// If the camera is moved while tracking is SEARCHING, the coordinates will be off by how much the camera was moved.
// There is no way to measure how much the camera was moved while the tracking is off.
public class TrackingTracker : MonoBehaviour
{
    // ZED Camera/Plane Objects
    ZEDManager manager;
    Transform LeftCameraTransform;
    sl.ZEDCamera zedCamera;

    // whether or not an offset should be used to try to recover the last tracked position
    public bool resetWithOffset = true;

    // Keeps track of how long the tracking is in a SEARCHING state.
    private float trackingInstabilityTime = 0f;
    public float trackingFailTime = 10f;

    void Start()
    {
        manager = FindObjectOfType(typeof(ZEDManager)) as ZEDManager;
        zedCamera = manager.zedCamera;
        LeftCameraTransform = manager.GetLeftCameraTransform();
    }

    // The normal Unity GameObject.Find() method excludes gameobjects that are disabled. This method includes the disabled ones.
    // https://docs.unity3d.com/ScriptReference/Resources.FindObjectsOfTypeAll.html
    GameObject GetGameObjectByName(string name)
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
        {
            if (name.Equals(go.name))
                return go;
        }
        return null;
    }

    // Provides a visual indicator for the status of the camera's tracking
    // Displays the red borders if the tracking goes into a SEARCHING state (lost tracking)
    // Will reset the tracking if the camera's tracking has been lost for too long
    private void checkTracking()
    {
        if (manager.ZEDTrackingState == sl.TRACKING_STATE.TRACKING_SEARCH)
        {
            // Enable the red borders, increase bad time counter
            GetGameObjectByName("UI - Borders").SetActive(true);
            
            trackingInstabilityTime += Time.deltaTime;

            // If it's been in a SEARCHING state for too long, reset tracking
            if (trackingInstabilityTime >= trackingFailTime)
            {
                // Update UI element visibility
                GameObject.Find("UI - Text").transform.Find("Crosshair").gameObject.GetComponent<Text>().enabled = false;
                GameObject.Find("UI - Text").transform.Find("Updating Tracking").gameObject.GetComponent<Text>().enabled = true;

                if (resetWithOffset)
                {
                    // Readjust the tracking to the last known position where the tracking was OK
                    zedCamera.ResetTrackingWithOffset(manager.OriginRotation, manager.OriginPosition, LeftCameraTransform.rotation, LeftCameraTransform.position);
                }
                else
                {
                    // Reset tracking to origin
                    zedCamera.ResetTracking(manager.OriginRotation, manager.OriginPosition);
                }
                trackingInstabilityTime = 0f;
            }
        }
        else if (manager.ZEDTrackingState == sl.TRACKING_STATE.TRACKING_OK)
        {
            GetGameObjectByName("UI - Borders").SetActive(false); 
            GameObject.Find("UI - Text").transform.Find("Updating Tracking").gameObject.GetComponent<Text>().enabled = false;
            GameObject.Find("UI - Text").transform.Find("Crosshair").gameObject.GetComponent<Text>().enabled = true;

            // Record last known good position to use if tracking goes bad again
            LeftCameraTransform = manager.GetLeftCameraTransform();
        }
    }

    // Update is called once per frame
    void Update()
    {
        checkTracking();
    }
}
