/// @file RingStartAreaLogic.cs
/// @brief Controls the visual state and lifecycle of a ring start area indicator sprite,
/// highlighting when a hand ring is hovering over it and animating its destruction
/// when the game round ends.

using UnityEngine;

/// <summary>
/// Manages a single ring start area marker on the game board. Each frame it checks
/// whether the left or right ring is overlapping it and updates its sprite accordingly.
/// External scripts can push it into specific display states via SetState, and it
/// self-destructs with a shrink animation when told to do so.
/// Attach to each ring start area GameObject and ensure it is parented under a
/// GameObject carrying a GameManagerLogic component.
/// </summary>
public class RingStartAreaLogic : MonoBehaviour
{
    /// <summary>Radius in world units within which a ring is considered to be overlapping this area.</summary>
    private const float OVERLAP_RADIUS = 0.07f;

    /// <summary>The scale applied to this GameObject at full size (matches OVERLAP_RADIUS).</summary>
    private const float FULL_SCALE = 0.07f;

    /// <summary>Number of frames over which the shrink-and-destroy animation plays out.</summary>
    private const float DESTROY_ANIMATION_DURATION = 60f;

    /// <summary>X offset applied when positioning this area for the left hand side.</summary>
    private const float LEFT_AREA_OFFSET_X = 0.6f;

    /// <summary>X offset applied when positioning this area for the right hand side.</summary>
    private const float RIGHT_AREA_OFFSET_X = 0.6f;

    /// <summary>Y offset applied when positioning this area for either hand side.</summary>
    private const float AREA_OFFSET_Y = 0.4f;

    /// <summary>
    /// Current state of this area marker.
    /// 0 = idle, checking for ring overlap;
    /// 1 = forced highlighted;
    /// 2 = forced alternate sprite;
    /// 3 = shrinking toward destruction.
    /// </summary>
    private int state;

    private float counter;
    private GameObject leftring;
    private GameObject rightring;

    /// <summary>True this frame if the left ring is overlapping this start area.</summary>
    public bool l;
    /// <summary>True this frame if the right ring is overlapping this start area.</summary>
    public bool r;

    /// <summary>
    /// Initialises state and counter to zero and locates the LeftRing
    /// and RightRing GameObjects in the scene.
    /// </summary>
    void Start()
    {
        state = 0;
        counter = 0;
        leftring = GameObject.Find("LeftRing");
        rightring = GameObject.Find("RightRing");
    }

    /// <summary>
    /// Called once per frame. Behaviour depends on the current state:
    /// state 0 checks ring proximity and updates the sprite to idle or highlighted;
    /// state 1 forces the highlighted sprite;
    /// state 2 forces the alternate sprite;
    /// state 3 plays a shrink animation and destroys the GameObject when complete.
    /// </summary>
    void Update()
    {
        switch (state)
        {
            case 0:
                l = (Mathf.Sqrt(
                    ((transform.position.x - leftring.transform.position.x) * (transform.position.x - leftring.transform.position.x)) +
                    ((leftring.transform.position.y - transform.position.y) * (leftring.transform.position.y - transform.position.y)))
                    < OVERLAP_RADIUS);

                r = (Mathf.Sqrt(
                    ((transform.position.x - rightring.transform.position.x) * (transform.position.x - rightring.transform.position.x)) +
                    ((rightring.transform.position.y - transform.position.y) * (rightring.transform.position.y - transform.position.y)))
                    < OVERLAP_RADIUS);

                this.GetComponent<SpriteRenderer>().sprite = (l || r)
                    ? GetComponentInParent<GameManagerLogic>().ringareas[1]
                    : GetComponentInParent<GameManagerLogic>().ringareas[0];
                break;

            case 1:
                this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().ringareas[1];
                break;

            case 2:
                this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().ringareas[2];
                break;

            case 3:
                counter += Time.deltaTime / (1f / 60f);
                float shrinkFactor = FULL_SCALE * ((DESTROY_ANIMATION_DURATION - counter) / DESTROY_ANIMATION_DURATION);
                transform.localScale = new Vector3(shrinkFactor, shrinkFactor, 1);
                if (counter > DESTROY_ANIMATION_DURATION)
                    Destroy(this.gameObject);
                break;
        }
    }

    /// <summary>
    /// Repositions this area marker to the left-hand starting position by
    /// offsetting it rightward and downward from its current position.
    /// </summary>
    public void SetLeftArea()
    {
        transform.position = new Vector3(
            transform.position.x + LEFT_AREA_OFFSET_X,
            transform.position.y - AREA_OFFSET_Y,
            transform.position.z);
    }

    /// <summary>
    /// Repositions this area marker to the right-hand starting position by
    /// offsetting it leftward and downward from its current position.
    /// </summary>
    public void SetRightArea()
    {
        transform.position = new Vector3(
            transform.position.x - RIGHT_AREA_OFFSET_X,
            transform.position.y - AREA_OFFSET_Y,
            transform.position.z);
    }

    /// <summary>
    /// Sets the current display/behaviour state of this area marker.
    /// Called externally by GameManagerLogic to transition between idle,
    /// highlighted, alternate, and destroy states.
    /// </summary>
    /// <param name="value">The target state index (0-3).</param>
    public void SetState(int value)
    {
        state = value;
    }
}