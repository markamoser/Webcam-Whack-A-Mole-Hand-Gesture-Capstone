using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManagerLogic : MonoBehaviour
{
    private int amount;
    private int waitmin;
    private int waitmax;
    private int wait;
    private int total;
    private int cap;
    private float counter;
    private int hits;
    private int timeseconds;
    private float timeframes;
    public bool stilltime;
    public Text hitcount;
    public Text gametimera;
    public Text gametimerb;
    public Text gametimerc;
    public Text startmessage;
    public Text title;
    public GameObject mole;
    public GameObject outoftime;
    public GameObject ringstartarea;
    public Sprite[] moles;
    public Sprite[] ringareas;

    private GameObject leftringarea;
    private GameObject rightringarea;

    private enum State
    {
        GameWaitForUserToLineUpHands,
        GameWaitOnYellowCircles,
        GameWaitOnCyanCircles,
        GameShrinkCircles,
        GameSayGameStart,
        GameRun
    }

    private State stateenum;

    // Start is called before the first frame update
    void Start()
    {
        GameInit();
    }

    void GameInit()
    {
        Application.targetFrameRate = 60;
        amount = Random.Range(2, 5);
        total = 0;
        waitmin = 3;
        waitmax = 5;
        wait = 0;
        cap = 60;
        counter = 0;
        hits = 0;
        timeseconds = 45;
        timeframes = (timeseconds * 60);
        stilltime = true;
        stateenum = State.GameWaitForUserToLineUpHands;
        leftringarea = Instantiate(ringstartarea, this.transform);
        leftringarea.GetComponent<RingStartAreaLogic>().SetLeftArea();
        rightringarea = Instantiate(ringstartarea, this.transform);
        rightringarea.GetComponent<RingStartAreaLogic>().SetRightArea();
        hitcount.text = "";
        gametimera.text = "";
        gametimerb.text = "";
        gametimerc.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        switch (stateenum)
        {
            case State.GameWaitForUserToLineUpHands:
                startmessage.text = "Use Your Hands To Position The Rings Inside Both Circles To Start The Game";
                title.text = "Webcam Whack-A-Mole";
                if ((leftringarea.GetComponent<RingStartAreaLogic>().l || leftringarea.GetComponent<RingStartAreaLogic>().r) && (rightringarea.GetComponent<RingStartAreaLogic>().l || rightringarea.GetComponent<RingStartAreaLogic>().r))
                {
                    leftringarea.GetComponent<RingStartAreaLogic>().SetState(1);
                    rightringarea.GetComponent<RingStartAreaLogic>().SetState(1);
                    stateenum = State.GameWaitOnYellowCircles;
                }
                break;
            case State.GameWaitOnYellowCircles:
                counter = counter + (Time.deltaTime / (1f / 60f));
                if (counter > 60)
                {
                    counter = 0;
                    stateenum = State.GameWaitOnCyanCircles;
                }
                break;
            case State.GameWaitOnCyanCircles:
                startmessage.text = "OK!";
                title.text = "";
                counter = counter + (Time.deltaTime / (1f / 60f));
                leftringarea.GetComponent<RingStartAreaLogic>().SetState(2);
                rightringarea.GetComponent<RingStartAreaLogic>().SetState(2);
                if (counter > 60)
                {
                    counter = 0;
                    stateenum = State.GameShrinkCircles;
                }
                break;
            case State.GameShrinkCircles:
                counter = counter + (Time.deltaTime / (1f / 60f));
                leftringarea.GetComponent<RingStartAreaLogic>().SetState(3);
                rightringarea.GetComponent<RingStartAreaLogic>().SetState(3);
                if (counter > 60)
                {
                    counter = 0;
                    stateenum = State.GameSayGameStart;
                }
                break;
            case State.GameSayGameStart:
                startmessage.text = "GAME START!";
                counter = counter + (Time.deltaTime / (1f / 60f));
                if (counter > 60)
                {
                    startmessage.text = "";
                    counter = 0;
                    stateenum = State.GameRun;
                }
                break;
            case State.GameRun:
                RunGame();
                break;
        }
    }

    void RunGame()
    {
        if (total < cap && stilltime)
        {
            counter = counter + (Time.deltaTime / (1f / 60f));
            if (counter > wait)
            {
                int check = (amount + total) - cap;
                if (check < 0)
                {
                    check = 0;
                }
                int i = 0;
                while (i < (amount - check))
                {
                    Instantiate(mole, this.transform);
                    i++;
                }
                wait = Random.Range((waitmin * 60), ((waitmax * 60) + 1));
                amount = Random.Range(2, 5);
                counter = 0;
            }
        }
        hitcount.text = "Hits x " + hits;
        if (timeframes > 0)
        {
            float a = Mathf.Floor(Mathf.Ceil(timeframes) / 60f);
            if (a < 10)
            {
                gametimera.text = "0" + a + "      ";
            }
            else
            {
                gametimera.text = a + "      ";
            }
            if ((Mathf.Floor(timeframes / 30f) % 2) != 0)
            {
                gametimerb.text = ":";
            }
            else
            {
                gametimerb.text = " ";
            }
            float c = Mathf.Round((Mathf.Ceil(timeframes) % 60) * (100f / 60f));
            if (c < 10)
            {
                gametimerc.text = "      0" + c;
            }
            else
            {
                gametimerc.text = "      " + c;
            }
            timeframes = timeframes - (Time.deltaTime / (1f / 60f));
        }
        else
        {
            if (timeframes > -100)
            {
                title.text = "Press Space To Restart";
                Instantiate(outoftime, this.transform);
            }
            stilltime = false;
            gametimera.text = "00      ";
            gametimerb.text = ":";
            gametimerc.text = "      00";
            timeframes = -101;
            if (Input.GetKey(KeyCode.Space))
            {
                Destroy(GameObject.Find("TimeOut(Clone)"));
                GameInit();
            }
        }
    }

    public void UpdateCount(bool surfcing)
    {
        if (surfcing)
        {
            total++;
        }
        else
        {
            total--;
        }
    }

    public void UpdateHits()
    {
        hits++;
    }
}
