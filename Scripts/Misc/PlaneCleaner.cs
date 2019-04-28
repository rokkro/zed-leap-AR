using UnityEngine;

/*
 * Destroys old planes once the plane limit is hit.
 * Used to improve performance if planes are spawning frequently.
 * Not the best solution to plane clearing since the hit plane counter isnt reset, 
 *   but it should work until a full plane clear is done.
 * Should be attached to the ZEDPlaneDetectionManager GameObject.
 */
public class PlaneCleaner : MonoBehaviour
{
    public int planeCountLimit = 10;

    ZEDPlaneDetectionManager zpdm;

    // Start is called before the first frame update
    void Start()
    {
        zpdm = GetComponent<ZEDPlaneDetectionManager>();
    }

    // Destroy the oldest plane if the planeCountLimit is hit
    // Update is called once per frame
    void LateUpdate()
    {
        int planeCount = transform.GetChild(0).childCount;
        if(planeCount > planeCountLimit)
        {
            Destroy(transform.GetChild(0).GetChild(0).gameObject);
        }
    }
}
