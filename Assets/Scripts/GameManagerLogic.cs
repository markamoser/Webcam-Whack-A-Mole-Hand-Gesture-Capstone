using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManagerLogic : MonoBehaviour
{
    private int amount;
    private int wait;
    private int total;
    private int cap;
    private float counter;
    private int hits;
    private float time;
    private int state;
    public bool stilltime;
    public Text hitcount;
    public Text gametimera;
    public Text gametimerb;
    public Text gametimerc;
    public Text startmessage;
    public GameObject mole;
    public GameObject outoftime;
    public GameObject ringstartarea;
    public Sprite[] moles;
    public Sprite[] ringareas;

    private GameObject leftringarea;
    private GameObject rightringarea;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        amount = Random.Range(1, 6);
        total = 0;
        wait = 0;
        cap = 90;
        counter = 0;
        hits = 0;
        time = (60 * 60);
        stilltime = true;
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

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case 0:
                startmessage.text = "Use Your Hands To Position The Rings Inside Both Circles To Start The Game";
                if ((leftringarea.GetComponent<RingStartAreaLogic>().l || leftringarea.GetComponent<RingStartAreaLogic>().r) && (rightringarea.GetComponent<RingStartAreaLogic>().l || rightringarea.GetComponent<RingStartAreaLogic>().r))
                {
                    leftringarea.GetComponent<RingStartAreaLogic>().SetState(1);
                    rightringarea.GetComponent<RingStartAreaLogic>().SetState(1);
                    state = 1;
                }
                break;
            case 1:
                counter = counter + (Time.deltaTime / (1f / 60f));
                if (counter > 60)
                {
                    counter = 0;
                    state = 2;
                }
                break;
            case 2:
                startmessage.text = "OK!";
                counter = counter + (Time.deltaTime / (1f / 60f));
                leftringarea.GetComponent<RingStartAreaLogic>().SetState(2);
                rightringarea.GetComponent<RingStartAreaLogic>().SetState(2);
                if (counter > 60)
                {
                    counter = 0;
                    state = 3;
                }
                break;
            case 3:
                counter = counter + (Time.deltaTime / (1f / 60f));
                leftringarea.GetComponent<RingStartAreaLogic>().SetState(3);
                rightringarea.GetComponent<RingStartAreaLogic>().SetState(3);
                if (counter > 60)
                {
                    counter = 0;
                    state = 4;
                }
                break;
            case 4:
                startmessage.text = "GAME START!";
                counter = counter + (Time.deltaTime / (1f / 60f));
                if (counter > 60)
                {
                    startmessage.text = "";
                    counter = 0;
                    state = 5;
                }
                break;
            case 5:
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
                wait = Random.Range(60, 241);
                amount = Random.Range(1, 6);
                counter = 0;
            }
        }
        hitcount.text = "Hits x " + hits;
        if (time > 0)
        {
            float a = Mathf.Floor(Mathf.Ceil(time) / 60f);
            if (a < 10)
            {
                gametimera.text = "0" + a + "      ";
            }
            else
            {
                gametimera.text = a + "      ";
            }
            if ((Mathf.Floor(time / 30f) % 2) != 0)
            {
                gametimerb.text = ":";
            }
            else
            {
                gametimerb.text = " ";
            }
            float c = Mathf.Round((Mathf.Ceil(time) % 60) * (100f / 60f));
            if (c < 10)
            {
                gametimerc.text = "      0" + c;
            }
            else
            {
                gametimerc.text = "      " + c;
            }
            time = time - (Time.deltaTime / (1f / 60f));
        }
        else
        {
            if (time > -100)
            {
                Instantiate(outoftime, this.transform);
            }
            stilltime = false;
            gametimera.text = "00      ";
            gametimerb.text = ":";
            gametimerc.text = "      00";
            time = -101;
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
