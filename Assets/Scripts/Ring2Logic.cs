/// @file Ring2Logic.cs
/// @brief Controls the position and press state of the right hand ring marker,
/// tracking the right hand's position via TwoHandVisualizer and exposing a
/// boolean that is true only while an active forward punch gesture is detected.

using UnityEngine;

/// <summary>
/// Follows the right hand marker point each frame and sets the public down boolean
/// to true only when the right hand is both in a latched press state and still
/// actively moving forward past the press velocity threshold. Used by Mole2DLogic
/// to determine whether a whack attempt is valid.
/// Attach to the RightRing GameObject in the scene.
/// </summary>
public class Ring2Logic : MonoBehaviour
{
    /// <summary>X offset applied to the ring's starting position on scene load.</summary>
    private const float INITIAL_X_OFFSET = 0.6f;

    private GameObject rightmark;
    private TwoHandVisualizer _visualizer;

    /// <summary>
    /// True when the right hand is in a latched press state and its current depth
    /// velocity is still below the press threshold, indicating an active punch.
    /// Read by Mole2DLogic each frame to validate whack attempts.
    /// </summary>
    public bool down;

    /// <summary>
    /// Initialises the ring to its starting position, locates the Right Point
    /// marker GameObject, and finds the TwoHandVisualizer in the scene.
    /// </summary>
    void Start()
    {
        down = false;
        transform.position = new Vector3(transform.position.x - INITIAL_X_OFFSET, transform.position.y, transform.position.z);
        rightmark = GameObject.Find("Right Point");
        _visualizer = FindObjectOfType<TwoHandVisualizer>();
    }

    /// <summary>
    /// Called once per frame. Snaps this ring's position to the current right hand
    /// marker position and updates down based on whether the right hand is actively
    /// punching forward past the press velocity threshold.
    /// </summary>
    void Update()
    {
        transform.position = new Vector3(rightmark.transform.position.x, rightmark.transform.position.y, transform.position.z);

        down = _visualizer != null
            && _visualizer.RightPressed
            && _visualizer.RightVelocity < _visualizer.PressVelocityThreshold;
    }
}