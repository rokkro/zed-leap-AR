using System;
using UnityEngine;

/* Additional methods and keybinds
 * Several methods here are called from the menu interface with a GameObject.sendMessage()
 * 
 * Adds keybinds to clear planes and toggle the plane detection abilities.
 * Change them here, or with the dropdown in the inspector.
 * Return = detection toggle
 * Space = Clear planes
 */
public class TogglesAndKeys : MonoBehaviour
{
    public KeyCode detectionToggleKey = KeyCode.Return;
    public KeyCode clearPlanesKey = KeyCode.Space;
    ZEDPlaneDetectionManager zpdm;

    // Start is called before the first frame update
    // Can be accessed from Update(), but not from methods called with GameObject.sendMessage()
    void Start()
    {
        zpdm = getZpdmInstance();
    }

    // Get the instance of the ZEDPlaneDetectionManager component.
    // Any other objects that call a method here will not have access to the instance if assigned in Start().
    ZEDPlaneDetectionManager getZpdmInstance()
    {
        ZEDPlaneDetectionManager zpdm = FindObjectOfType(typeof(ZEDPlaneDetectionManager)) as ZEDPlaneDetectionManager;
        return zpdm.GetComponent<ZEDPlaneDetectionManager>();
    }

    // Toggle from the menu UI. Needed for a GameObject.sendMessage() call.
    public void togglePlaneVisibilityInGame()
    {
        zpdm = getZpdmInstance();
        zpdm.planesVisibleInGame = !zpdm.planesVisibleInGame;
    }

    // Called with GameObject.SendMessage() for the menu.
    // Changes depth occlusion setting and reset the camera so that it takes effect
    public void toggleDepthOcclusion()
    {
        ZEDManager manager = FindObjectOfType(typeof(ZEDManager)) as ZEDManager;
        manager.depthOcclusion = !manager.depthOcclusion;

        // This updates the depth occlusion setting immediately, otherwise you'll have to wait for the camera to do it automatically
        manager.Reset();
    }

    // Called with GameObject.SendMessage() for the menu.
    public void toggleUnknownPlanes()
    {
        zpdm = getZpdmInstance();
        zpdm.blockUnknownPlanes = !zpdm.blockUnknownPlanes;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(detectionToggleKey))
        {
           zpdm.pauseDetection = !zpdm.pauseDetection;
        }
        else if (Input.GetKeyDown(clearPlanesKey)){
           zpdm.destroyAllPlanes();
        }
    }
}
