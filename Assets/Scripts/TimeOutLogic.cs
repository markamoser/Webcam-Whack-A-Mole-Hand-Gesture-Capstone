/// @file TimeOutLogic.cs
/// @brief Controls the animated scale-in of a Game Over or Time Out UI element,
/// smoothly growing it from invisible to its target size when the scene loads.

using UnityEngine;

/// <summary>
/// Animates a GameObject from zero scale up to a target scale of 0.15 on both
/// X and Y axes using a framerate-independent exponential ease-in approach.
/// Attach to the Time Out / Game Over UI GameObject that should pop into view
/// at the end of a round.
/// </summary>
public class TimeOutLogic : MonoBehaviour
{
    /// <summary>The target scale the GameObject eases toward on both X and Y axes.</summary>
    private const float TARGET_SCALE = 0.15f;

    /// <summary>
    /// Divisor controlling how quickly the scale approaches the target.
    /// Higher values produce a slower, smoother ease-in.
    /// </summary>
    private const float EASE_DIVISOR = 10f;

    /// <summary>
    /// Initialises the GameObject at zero scale so it is invisible when the scene starts.
    /// </summary>
    /// <remarks>Called automatically by Unity at scene start. No parameters or return value.</remarks>
    void Start()
    {
        transform.localScale = new Vector3(0, 0, 1);
    }

    /// <summary>
    /// Called once per frame. Moves the current scale toward TARGET_SCALE each frame
    /// using a framerate-independent exponential ease, so the growth slows as it
    /// approaches the target size.
    /// </summary>
    /// <remarks>Called automatically by Unity each frame. No parameters or return value.</remarks>
    void Update()
    {
        float delta = Time.deltaTime / (1f / 60f);
        float newX = transform.localScale.x + (((TARGET_SCALE - transform.localScale.x) / EASE_DIVISOR) * delta);
        float newY = transform.localScale.y + (((TARGET_SCALE - transform.localScale.y) / EASE_DIVISOR) * delta);
        transform.localScale = new Vector3(newX, newY, 1);
    }
}