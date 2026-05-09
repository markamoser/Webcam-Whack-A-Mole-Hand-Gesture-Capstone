/// @file GameManagerLogic.cs
/// @brief Central game manager for the Whack-a-Mole game. Controls the pre-game
/// startup sequence, mole spawning, hit tracking, countdown timer display,
/// and end-of-round state. Also owns the ring start area indicators used to
/// position players before the game begins.

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the full game session from startup through to game over. Runs a
/// state machine that walks through the ring placement prompt, a countdown
/// sequence, and the main gameplay loop. During gameplay it spawns moles in
/// randomised batches, maintains the on-screen hit counter and countdown timer,
/// and signals to moles whether time remains via the public stilltime flag.
/// Attach to the root GameManager GameObject. All mole and UI prefab references
/// must be assigned in the Inspector.
/// </summary>
public class GameManagerLogic : MonoBehaviour
{
    // --- Spawn tuning ---
    /// <summary>Maximum total number of moles allowed on screen simultaneously.</summary>
    private const int MOLE_CAP = 60;
    /// <summary>Minimum scaled frames between mole spawn batches.</summary>
    private const int MIN_SPAWN_WAIT = 2 * 60;
    /// <summary>Maximum scaled frames between mole spawn batches.</summary>
    private const int MAX_SPAWN_WAIT = (5 * 60) + 1;
    /// <summary>Minimum number of moles spawned per batch.</summary>
    private const int MIN_SPAWN_AMOUNT = 2;
    /// <summary>Maximum number of moles spawned per batch (exclusive).</summary>
    private const int MAX_SPAWN_AMOUNT = 5;

    // --- Game timer ---
    /// <summary>Total game duration expressed as a scaled frame count (60 frames/sec * 60 seconds).</summary>
    private const float GAME_DURATION_FRAMES = 45 * 60;
    /// <summary>Sentinel value assigned to time after the out-of-time object has been spawned,
    /// preventing it from being instantiated more than once.</summary>
    private const float TIME_EXPIRED_SENTINEL = -101f;

    // --- Startup sequence frame counts ---
    /// <summary>Scaled frames spent holding the ring-highlighted state before showing OK.</summary>
    private const float STARTUP_HOLD_DURATION = 60f;
    /// <summary>Scaled frames the OK message is displayed.</summary>
    private const float STARTUP_OK_DURATION = 60f;
    /// <summary>Scaled frames the ring shrink-out animation plays.</summary>
    private const float STARTUP_SHRINK_DURATION = 60f;
    /// <summary>Scaled frames the GAME START message is displayed.</summary>
    private const float STARTUP_GAMESTART_DURATION = 60f;

    // --- Timer display ---
    /// <summary>Multiplier used to convert remaining seconds into a centisecond display value.</summary>
    private const float TIMER_CENTISECOND_SCALE = 100f / 60f;
    /// <summary>Divisor used to determine whether the colon separator should blink on or off.</summary>
    private const float TIMER_BLINK_DIVISOR = 30f;

    private int amount;
    private int wait;
    private int total;
    private int cap;
    private float counter;
    private int hits;
    private float time;
    private int state;

    /// <summary>
    /// True while the game timer has not yet reached zero. Moles read this flag
    /// each frame to decide whether to retreat early.
    /// </summary>
    public bool stilltime;

    /// <summary>UI Text element displaying the current hit count.</summary>
    public Text hitcount;
    /// <summary>UI Text element displaying the minutes portion of the countdown timer.</summary>
    public Text gametimera;
    /// <summary>UI Text element displaying the blinking colon separator of the countdown timer.</summary>
    public Text gametimerb;
    /// <summary>UI Text element displaying the seconds portion of the countdown timer.</summary>
    public Text gametimerc;
    /// <summary>UI Text element used to display startup and game state messages to the player.</summary>
    public Text startmessage;
    /// <summary>UI Text element used to display startup title messages to the player.</summary>
    public Text title;

    /// <summary>Prefab instantiated for each mole spawned during gameplay.</summary>
    public GameObject mole;
    /// <summary>Prefab instantiated when the game timer expires to display the Game Over screen.</summary>
    public GameObject outoftime;
    /// <summary>Prefab instantiated at startup for each hand's ring placement target area.</summary>
    public GameObject ringstartarea;

    /// <summary>Sprite array for mole states: index 0 = idle, 1 = whacked, 2 = flash.</summary>
    public Sprite[] moles;
    /// <summary>Sprite array for ring start area states: index 0 = idle, 1 = highlighted, 2 = confirmed.</summary>
    public Sprite[] ringareas;

    private GameObject leftringarea;
    private GameObject rightringarea;

    public enum State
    {
        GameWaitForUserToLineUpHands,
        GameWaitOnYellowCircles,
        GameWaitOnCyanCircles,
        GameShrinkCircles,
        GameSayGameStart,
        GameRun
    }

    private State stateenum;

    /// <summary>
    /// Initializes all game state variables through calling the associated Game Initialization method.
    /// </summary>
    void Start()
    {
        GameInit();
    }

    /// <summary>
    /// Initialises all game state variables, locks the frame rate to 60 fps,
    /// instantiates and positions the two ring start area indicators, and
    /// clears all UI text elements.
    /// </summary>
    void GameInit()
    {
        Application.targetFrameRate = 60;
        amount = Random.Range(MIN_SPAWN_AMOUNT, MAX_SPAWN_AMOUNT);
        total = 0;
        wait = 0;
        cap = MOLE_CAP;
        counter = 0;
        hits = 0;
        time = GAME_DURATION_FRAMES;
        stilltime = true;
        stateenum = State.GameWaitForUserToLineUpHands;
        state = 0;

        leftringarea = Instantiate(ringstartarea, this.transform);
        leftringarea.GetComponent<RingStartAreaLogic>().SetLeftArea();
        rightringarea = Instantiate(ringstartarea, this.transform);
        rightringarea.GetComponent<RingStartAreaLogic>().SetRightArea();

        hitcount.text = "";
        gametimera.text = "";
        gametimerb.text = "";
        gametimerc.text = "";
    }

    /// <summary>
    /// Called once per frame. Advances the startup state machine through ring placement
    /// detection, countdown, and game start before handing off to RunGame each frame
    /// during active gameplay.
    /// State GameWaitForUserToLineUpHands: waits for both rings to be placed inside their target areas;
    /// State GameWaitOnYellowCircles: holds the highlighted ring state briefly;
    /// State GameWaitOnCyanCircles: displays the OK confirmation message;
    /// State GameShrinkCircles: plays the ring area shrink-out animation;
    /// State GameSayGameStart: displays the GAME START message;
    /// State GameRun: runs the active gameplay loop each frame via RunGame.
    /// </summary>
    void Update()
    {
        if (state == 5)
        {
            stateenum = State.GameRun;
        }
        switch (stateenum)
        {
            case State.GameWaitForUserToLineUpHands:
                startmessage.text = "Use Your Hands To Position The Rings Inside Both Circles To Start The Game";
                title.text = "Webcam Whack-A-Mole";
                if ((leftringarea.GetComponent<RingStartAreaLogic>().l || leftringarea.GetComponent<RingStartAreaLogic>().r) &&
                    (rightringarea.GetComponent<RingStartAreaLogic>().l || rightringarea.GetComponent<RingStartAreaLogic>().r))
                {
                    leftringarea.GetComponent<RingStartAreaLogic>().SetState(1);
                    rightringarea.GetComponent<RingStartAreaLogic>().SetState(1);
                    stateenum = State.GameWaitOnYellowCircles;
                }
                break;

            case State.GameWaitOnYellowCircles:
                counter += Time.deltaTime / (1f / 60f);
                if (counter > STARTUP_HOLD_DURATION)
                {
                    counter = 0;
                    stateenum = State.GameWaitOnCyanCircles;
                    state = 2;
                }
                break;

            case State.GameWaitOnCyanCircles:
                startmessage.text = "OK!";
                title.text = "";
                counter += Time.deltaTime / (1f / 60f);
                leftringarea.GetComponent<RingStartAreaLogic>().SetState(2);
                rightringarea.GetComponent<RingStartAreaLogic>().SetState(2);
                if (counter > STARTUP_OK_DURATION)
                {
                    counter = 0;
                    stateenum = State.GameShrinkCircles;
                    state = 3;
                }
                break;

            case State.GameShrinkCircles:
                counter += Time.deltaTime / (1f / 60f);
                leftringarea.GetComponent<RingStartAreaLogic>().SetState(3);
                rightringarea.GetComponent<RingStartAreaLogic>().SetState(3);
                if (counter > STARTUP_SHRINK_DURATION)
                {
                    counter = 0;
                    stateenum = State.GameSayGameStart;
                    state = 4;
                }
                break;

            case State.GameSayGameStart:
                startmessage.text = "GAME START!";
                counter += Time.deltaTime / (1f / 60f);
                if (counter > STARTUP_GAMESTART_DURATION)
                {
                    startmessage.text = "";
                    counter = 0;
                    stateenum = State.GameRun;
                    state = 5;
                }
                break;

            case State.GameRun:
                RunGame();
                break;
        }
    }

    /// <summary>
    /// Executes the active gameplay loop each frame. Spawns mole batches at randomised
    /// intervals while the mole cap has not been reached and time remains. Updates the
    /// hit counter display and the countdown timer. When time expires, instantiates the
    /// Game Over object, freezes the timer display at zero, and clears stilltime.
    /// </summary>
    void RunGame()
    {
        if (total < cap && stilltime)
        {
            counter += Time.deltaTime / (1f / 60f);
            if (counter > wait)
            {
                int check = Mathf.Max(0, (amount + total) - cap);
                int i = 0;
                while (i < (amount - check))
                {
                    Instantiate(mole, this.transform);
                    i++;
                }
                wait = Random.Range(MIN_SPAWN_WAIT, MAX_SPAWN_WAIT);
                amount = Random.Range(MIN_SPAWN_AMOUNT, MAX_SPAWN_AMOUNT);
                counter = 0;
            }
        }

        hitcount.text = "Hits x " + hits;

        if (time > 0)
        {
            float a = Mathf.Floor(Mathf.Ceil(time) / 60f);
            gametimera.text = (a < 10 ? "0" + a : "" + a) + "      ";
            gametimerb.text = (Mathf.Floor(time / TIMER_BLINK_DIVISOR) % 2) != 0 ? ":" : " ";
            float c = Mathf.Round((Mathf.Ceil(time) % 60) * TIMER_CENTISECOND_SCALE);
            gametimerc.text = "      " + (c < 10 ? "0" + c : "" + c);
            time -= Time.deltaTime / (1f / 60f);
        }
        else
        {
            if (time > TIME_EXPIRED_SENTINEL)
            {
                title.text = "Press Space To Restart";
                Instantiate(outoftime, this.transform);
            }
            stilltime = false;
            gametimera.text = "00      ";
            gametimerb.text = ":";
            gametimerc.text = "      00";
            time = TIME_EXPIRED_SENTINEL;
            if (Input.GetKey(KeyCode.Space))
            {
                Destroy(GameObject.Find("TimeOut(Clone)"));
                GameInit();
            }
        }
    }

    /// <summary>
    /// Increments or decrements the count of moles currently on screen.
    /// Called by each mole on spawn and on removal to keep the total accurate
    /// for spawn cap enforcement.
    /// </summary>
    /// <param name="surfcing">True when a mole is surfacing (increment); false when it is leaving (decrement).</param>
    public void UpdateCount(bool surfcing)
    {
        if (surfcing)
            total++;
        else
            total--;
    }

    /// <summary>
    /// Increments the player's hit count by one. Called by Mole2DLogic
    /// when a mole successfully transitions to the whacked state.
    /// </summary>
    public void UpdateHits()
    {
        hits++;
    }
}