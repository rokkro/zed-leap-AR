using UnityEngine;

/* Attached to portal game objects. These portal game objects must use trigger based collision and should have it enabled for their collider.
 * 
 * Triggers vs default collision:
 * Using collision triggers instead of normal collider behavior. When using triggers, the portals are not solid, but their box colliders detect incoming collision.
 * Using onColliderEnter instead of triggers makes the object solid and vulnerable to being moved by other nearby solid objects.
 * Making the object kinematic prevents it from being moved, but according to the documentation: "Collision events are only sent if one of the colliders also has a non-kinematic rigidbody attached."
 * So using triggers are the only way to avoid the portals from moving around while having collision enabled.
 * 
 * Con of using triggers:
 * You cant easily get the point at which an object collided with trigger object. I get around this by doing a raycast to get the position relative to the trigger object (in PortalManager.cs).
 *
 * Triggers will be fired when something touches the entry portal, it stops touching the entry portal, it touches the exit portal, and it stops touching the exit portal.
 * Triggers may be fired multiple times at once for the same object. For example, sometimes multiple OnTriggerExit() events get fired just as an object leaves a portal.
 * 
 * Triggers here pass off the information to the PortalManager to handle what happens.
 */
public class PortalTriggerController : MonoBehaviour
{
    PortalManager portalManager;

    // Start is called before the first frame update
    void Start()
    {
        portalManager = FindObjectOfType(typeof(PortalManager)) as PortalManager;
        portalManager = portalManager.GetComponent<PortalManager>();
    }

    // Triggered when the collision object no longer makes contact with a portal
    void OnTriggerExit(Collider collider)
    {
        if (collider == null)
            return;
        GameObject colliderGO = collider.gameObject;

        if (colliderGO == null || colliderGO.tag == null)
            return;

        portalManager.handleTriggerExit(gameObject, colliderGO);
    }

    // Triggered when the collision object makes contact with a portal.
    void OnTriggerEnter(Collider collider)
    {
        if (collider == null)
            return;
        GameObject colliderGO = collider.gameObject;

        if (colliderGO == null || colliderGO.tag == null)
            return;

        portalManager.handleTriggerEnter(gameObject, colliderGO);
    }
}
