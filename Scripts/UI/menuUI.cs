using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Events;
using Leap.Unity;
using Leap.Unity.Interaction;

/* This script deals with interacting with buttons - activation and appearance.
* Normally you would use Leap Motion's InteractionButton.cs for button pressing. 
* From my testing, it didnt work well. Buttons would only get pushed part of the time.
* This script adds onto InteractionButton.cs and relies on holding your hand close enough
*   to the button for it to activate.
* OnPress events are assigned from the Unity Inspector to this script instead of InteractionButton.cs 
* Both scripts must be attached to a button for it to work.
*/

[AddComponentMenu("")]
[RequireComponent(typeof(InteractionBehaviour))]
public class menuUI : MonoBehaviour
{
    InteractionBehaviour intBeha;

    // Coloring from LeapMotion's SimpleInteractionGlow.cs
    public Color defaultColor = Color.Lerp(Color.black, Color.white, 0.8F);
    Color currentColor;
    public Color activationColor = Color.green;
    private Material _material;

    // For length of time to hold button before it activates
    public float holdTimeThreshold = 3f;
    float holdTimeCurrent = 0;

    // Distance from the button for it to be considered a push/hold action
    public float contactDistanceThreshold = .85f;

    // OnPress setup comes from Leap Motion's InteractionButton.cs
    [SerializeField]
    [FormerlySerializedAs("OnPress")]
    private UnityEvent _OnPress = new UnityEvent();
    public Action OnPress = () => { };

    // Start is called before the first frame update
    void Start()
    {
        intBeha = GetComponent<InteractionBehaviour>();
        OnPress += _OnPress.Invoke;
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = GetComponentInChildren<Renderer>();
        }
        if (renderer != null)
        {
            _material = renderer.material;
        }
        defaultColor = _material.color;
    }

    // Called after Update() is
    // Distance map here comes from LeapMotion's SimpleInteractionGlow.cs
    // If detected as hovering over the button and the hand distance from the button is close enough for long enough, activate the button
    void LateUpdate()
    {

        if (intBeha.isPrimaryHovered && intBeha.closestHoveringControllerDistance.Map(0F, 0.2F, 1F, 0.0F) >= contactDistanceThreshold)
        {
            holdTimeCurrent += Time.deltaTime;

            // If we've held the button for long enough, assign the color and trigger the OnPress event assigned in the Unity GUI.
            if (holdTimeCurrent >= holdTimeThreshold)
            {
                currentColor = activationColor;
                OnPress();
                holdTimeCurrent = 0f;
            }
        }
        else
            currentColor = defaultColor;
    }

    // Update the button color once per frame
    void Update()
    {
        _material.color = currentColor;
    }
}
