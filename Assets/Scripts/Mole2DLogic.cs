/// @file Mole2DLogic.cs
/// @brief Controls the full lifecycle of a single mole instance in the Whack-a-Mole game,
/// managing its spawn position, rise animation, on-screen idle period, hit detection
/// against both hand rings, and retreat or whacked exit animations via a state machine.

using UnityEngine;

/// <summary>
/// Drives a mole GameObject through eight states: initialisation, rising, snapping to
/// full scale, idle (waiting for a hit or timeout), missed retreat, whacked flash,
/// whacked squash, whacked stretch, and whacked retreat. Hit detection is performed
/// manually each frame using distance comparisons against the left and right ring positions.
/// Attach to each mole prefab. Requires a parent GameObject carrying GameManagerLogic.
/// </summary>
public class Mole2DLogic : MonoBehaviour
{
    // --- Spawn ---
    /// <summary>X offset added to the base spawn position before scaling.</summary>
    private const float SPAWN_OFFSET_X = 0.43f;
    /// <summary>Y offset subtracted from the base spawn position before scaling.</summary>
    private const float SPAWN_OFFSET_Y = 0.34f;
    /// <summary>World-space X scale factor applied to the randomised spawn position.</summary>
    private const float SPAWN_SCALE_X = 2.51f;
    /// <summary>World-space Y scale factor applied to the randomised spawn position.</summary>
    private const float SPAWN_SCALE_Y = 2.52f;
    /// <summary>Upper bound of the random X spawn jitter range (exclusive), in hundredths of a unit.</summary>
    private const int SPAWN_RANDOM_RANGE_X = 90;
    /// <summary>Upper bound of the random Y spawn jitter range (exclusive), in hundredths of a unit.</summary>
    private const int SPAWN_RANDOM_RANGE_Y = 67;

    // --- Wait timer ---
    /// <summary>Minimum number of scaled frames a mole stays fully visible before retreating.</summary>
    private const int MIN_WAIT_TIMER = 120;
    /// <summary>Maximum number of scaled frames a mole stays fully visible before retreating.</summary>
    private const int MAX_WAIT_TIMER = 301;

    // --- Sprite scale ---
    /// <summary>X scale applied to the mole sprite. Negative value flips the sprite horizontally.</summary>
    private const float SPRITE_FLIP_X = -0.012f;
    /// <summary>The target full Y scale the mole reaches when completely risen.</summary>
    private const float FULL_SCALE_Y = 0.012f;

    // --- Rise animation (state 1) ---
    /// <summary>Upward positional movement applied each scaled frame during the rise phase.</summary>
    private const float RISE_MOVE_SPEED = 0.0006f;
    /// <summary>Initial scale growth rate applied each scaled frame at the start of the rise.</summary>
    private const float RISE_SCALE_SPEED = 0.001f;
    /// <summary>Deceleration factor multiplied by the counter to slow scale growth over time.</summary>
    private const float RISE_DECELERATION = 0.000038f;
    /// <summary>Counter value at which upward movement reverses to a slight downward bob.</summary>
    private const float RISE_PEAK_COUNTER = 20f;
    /// <summary>Counter value at which the rise animation ends and the mole snaps to full scale.</summary>
    private const float RISE_END_COUNTER = 30f;

    // --- Hit detection (state 3) ---
    /// <summary>Maximum distance in world units between the mole and a ring centre for a hit to register.</summary>
    private const float HIT_DETECTION_RADIUS = 0.265f;

    // --- Missed retreat animation (state 4) ---
    /// <summary>Y scale reduction applied each scaled frame during the missed retreat.</summary>
    private const float RETREAT_SCALE_SPEED = 0.0005f;
    /// <summary>Downward positional movement applied each scaled frame during the missed retreat.</summary>
    private const float RETREAT_MOVE_SPEED = 0.0006f;
    /// <summary>Number of scaled frames before the mole is destroyed after a missed retreat.</summary>
    private const float RETREAT_DURATION = 24f;

    // --- Whacked flash (state 5) ---
    /// <summary>Number of scaled frames the whacked flash sprite is held before transitioning.</summary>
    private const float WHACK_FLASH_DURATION = 5f;

    // --- Whacked squash (state 6) ---
    /// <summary>Number of scaled frames the squash phase of the whacked animation lasts.</summary>
    private const float WHACK_SHRINK_DURATION = 5f;
    /// <summary>Scale reduction rate per counter unit per scaled frame during the squash phase.</summary>
    private const float WHACK_SHRINK_SCALE_RATE = 0.0002f;

    // --- Whacked stretch (state 7) ---
    /// <summary>Number of scaled frames the stretch phase of the whacked animation lasts.</summary>
    private const float WHACK_GROW_DURATION = 5f;
    /// <summary>Scale growth rate per counter unit per scaled frame during the stretch phase.</summary>
    private const float WHACK_GROW_SCALE_RATE = 0.0002f;

    // --- Whacked retreat (state 8) ---
    /// <summary>Y scale reduction applied each scaled frame during the final whacked retreat.</summary>
    private const float WHACK_RETREAT_SCALE_SPEED = 0.0010f;
    /// <summary>Downward positional movement applied each scaled frame during the whacked retreat.</summary>
    private const float WHACK_RETREAT_MOVE_SPEED = 0.0012f;
    /// <summary>Number of scaled frames before the mole is destroyed after a whacked retreat.</summary>
    private const float WHACK_RETREAT_DURATION = 12f;

    private int state;
    private int waittimer;
    private float counter;
    private GameObject leftring;
    private GameObject rightring;

    /// <summary>
    /// Initialises the mole at state 0, randomises its on-screen wait duration,
    /// locates the ring GameObjects, sets the idle sprite, and calculates a
    /// randomised world-space spawn position.
    /// </summary>
    void Start()
    {
        stateenum = State.MoleUpdateGameManagerMoleCount;
        counter = 0;
        waittimer = Random.Range(MIN_WAIT_TIMER, MAX_WAIT_TIMER);
        leftring = GameObject.Find("LeftRing");
        rightring = GameObject.Find("RightRing");
        this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().moles[0];
        transform.localScale = new Vector3(SPRITE_FLIP_X, 0, 1);
        float newx = Random.Range(0, SPAWN_RANDOM_RANGE_X) * 0.01f;
        float newy = Random.Range(0, SPAWN_RANDOM_RANGE_Y) * 0.01f;
        transform.position = new Vector3(
            ((transform.position.x + SPAWN_OFFSET_X) - newx) * SPAWN_SCALE_X,
            ((transform.position.y - SPAWN_OFFSET_Y) + newy) * SPAWN_SCALE_Y,
            transform.position.z);
    }

    /// <summary>
    /// Called once per frame. Advances the mole through its state machine:
    /// state 0 registers the mole with the game manager and enters the rise;
    /// state 1 animates the rise with a decelerated scale and positional bob;
    /// state 2 snaps the scale toward full in a single frame;
    /// state 3 idles and polls for ring hits or timeout;
    /// state 4 plays the missed retreat and destroys the mole;
    /// state 5 holds the whacked flash sprite;
    /// state 6 plays the squash phase of the whacked animation;
    /// state 7 plays the stretch phase of the whacked animation;
    /// state 8 plays the final whacked retreat and destroys the mole.
    /// </summary>
    void Update()
    {
        float delta = Time.deltaTime / (1f / 60f);

        switch (state)
        {
            case State.MoleUpdateGameManagerMoleCount:
                GetComponentInParent<GameManagerLogic>().UpdateCount(true);
                stateenum = State.MolePopUpFromGround;
                break;

            case 1:
                counter += delta;
                transform.position = new Vector3(transform.position.x, transform.position.y + (RISE_MOVE_SPEED * delta), transform.position.z);
                transform.localScale = new Vector3(SPRITE_FLIP_X, transform.localScale.y + ((RISE_SCALE_SPEED - (RISE_DECELERATION * counter)) * delta), 1);
                if (counter > RISE_PEAK_COUNTER)
                    transform.position = new Vector3(transform.position.x, transform.position.y - (RISE_MOVE_SPEED * delta), transform.position.z);
                if (counter > RISE_END_COUNTER)
                {
                    stateenum = State.MoleWaitToGetWhackedOrForWaitTimerToExpire;
                    counter = 0;
                }
                break;

            case 2:
                transform.localScale = new Vector3(SPRITE_FLIP_X, transform.localScale.y + ((FULL_SCALE_Y - transform.localScale.y) / 2f), 1);
                state = 3;
                break;

            case 3:
                counter += delta;
                transform.localScale = new Vector3(SPRITE_FLIP_X, transform.localScale.y + ((FULL_SCALE_Y - transform.localScale.y) / 2f), 1);

                if (leftring.GetComponent<RingLogic>().down && GetComponentInParent<GameManagerLogic>().stilltime)
                {
                    if (Mathf.Sqrt(
                        ((transform.position.x - leftring.transform.position.x) * (transform.position.x - leftring.transform.position.x)) +
                        ((leftring.transform.position.y - transform.position.y) * (leftring.transform.position.y - transform.position.y)))
                        < HIT_DETECTION_RADIUS)
                    {
                        counter = 0;
                        stateenum = State.MoleWhackedAndShowExplosion;
                        GetComponentInParent<GameManagerLogic>().UpdateHits();
                        GetComponentInParent<GameManagerLogic>().UpdateCount(false);
                        break;
                    }
                }

                if (rightring.GetComponent<Ring2Logic>().down && GetComponentInParent<GameManagerLogic>().stilltime)
                {
                    if (Mathf.Sqrt(
                        ((transform.position.x - rightring.transform.position.x) * (transform.position.x - rightring.transform.position.x)) +
                        ((rightring.transform.position.y - transform.position.y) * (rightring.transform.position.y - transform.position.y)))
                        < HIT_DETECTION_RADIUS)
                    {
                        counter = 0;
                        stateenum = State.MoleWhackedAndShowExplosion;
                        GetComponentInParent<GameManagerLogic>().UpdateHits();
                        GetComponentInParent<GameManagerLogic>().UpdateCount(false);
                        break;
                    }
                }

                if (counter > waittimer || !GetComponentInParent<GameManagerLogic>().stilltime)
                {
                    counter = 0;
                    stateenum = State.MoleDigIntoGroundAndLeave;
                    GetComponentInParent<GameManagerLogic>().UpdateCount(false);
                }
                break;

            case 4:
                counter += delta;
                transform.localScale = new Vector3(SPRITE_FLIP_X, transform.localScale.y - (RETREAT_SCALE_SPEED * delta), 1);
                transform.position = new Vector3(transform.position.x, transform.position.y - (RETREAT_MOVE_SPEED * delta), transform.position.z);
                if (counter > RETREAT_DURATION)
                    Destroy(this.gameObject);
                break;

            case 5:
                counter += delta;
                transform.localScale = new Vector3(SPRITE_FLIP_X, FULL_SCALE_Y, 1);
                this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().moles[2];
                if (counter > WHACK_FLASH_DURATION)
                {
                    counter = 0;
                    stateenum = State.MoleWhackedAndSquishDown;
                }
                break;

            case 6:
                counter += delta;
                this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().moles[1];
                transform.localScale = new Vector3(SPRITE_FLIP_X, transform.localScale.y - ((WHACK_SHRINK_SCALE_RATE * counter) * delta), 1);
                if (counter > WHACK_SHRINK_DURATION)
                {
                    counter = 0;
                    stateenum = State.MoleWhackedAndSquishUp;
                }
                break;

            case 7:
                counter += delta;
                transform.localScale = new Vector3(SPRITE_FLIP_X, transform.localScale.y + ((WHACK_GROW_SCALE_RATE * counter) * delta), 1);
                if (counter > WHACK_GROW_DURATION)
                {
                    counter = 0;
                    transform.localScale = new Vector3(-0.012f, 0.012f, 1);
                    stateenum = State.MoleWhackedAndFallIntoGround;
                }
                break;

            case 8:
                counter += delta;
                transform.localScale = new Vector3(SPRITE_FLIP_X, transform.localScale.y - (WHACK_RETREAT_SCALE_SPEED * delta), 1);
                transform.position = new Vector3(transform.position.x, transform.position.y - (WHACK_RETREAT_MOVE_SPEED * delta), transform.position.z);
                if (counter > WHACK_RETREAT_DURATION)
                    Destroy(this.gameObject);
                break;
        }
    }
}