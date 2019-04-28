using UnityEngine;
using UnityEngine.UI;

/* 
 * Deals with crosshair position and color
 * If the crosshair is directly in front of a ZED plane, it turns green.
 * It tests this using Raycasts
 *
 * Green = raycast hitting a ZED plane
 * Red = raycast not hitting a ZED plane
 * It doesnt raycast every frame since it's not really necessary.
*/
public class CenterIndicator : MonoBehaviour
{
    Text crosshair;
    SurfacePlacementManager spm;
    float horizontal;
    float vertical;
    float RequiredInterval = 1f;
    float CurrentTimeInterval = 0f;

    // Start is called before the first frame update
    void Start()
    {
        crosshair = GetComponent<Text>();
        spm = FindObjectOfType(typeof(SurfacePlacementManager)) as SurfacePlacementManager;
        spm = spm.GetComponent<SurfacePlacementManager>();
    }

    // Update center in case screen size changes
    // Move crosshair to the new screen center
    void updateScreenPositioning()
    {
        horizontal = (Screen.width / 2);
        vertical = (Screen.height / 2);

        transform.position = new Vector3(horizontal, vertical, 0f);
    }

    // Raycast to try to hit a ZED Plane. If our ray hits a plane, then the crosshair will be green, otherwise red.
    // Raycast will be wrong if ZED tracking is off.
    // Raycast also seems to be completely off when using ZED_Rig_Mono instead of ZED_Rig_Stereo. Might be an SDK issue.
    void updateColor()
    {
        RaycastHit hit = spm.raycastFromScreenSpace(new Vector2(horizontal,vertical));
        if (hit.collider != null && hit.collider.gameObject.GetComponent<ZEDPlaneGameObject>())
            crosshair.color = Color.green;
        else
            crosshair.color = Color.red;
    }

    // Update is called once per frame
    void Update()
    {
        updateScreenPositioning();

        // Time since last frame added to interval
        CurrentTimeInterval += Time.deltaTime;
        if (CurrentTimeInterval >= RequiredInterval)
        {
            updateColor();
            CurrentTimeInterval = 0f;
        }
    }
}
