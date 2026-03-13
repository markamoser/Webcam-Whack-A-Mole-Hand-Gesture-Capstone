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
    private int counter;
    private int hits;
    private int time;

    public bool stilltime;
    public Text hitcount;
    public Text gametimera;
    public Text gametimerb;
    public Text gametimerc;
    public GameObject mole;
    public GameObject outoftime;
    public Sprite[] moles;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        amount = Random.Range(1, 6);
        total = 0;
        wait = 0;
        cap = 60;
        counter = 0;
        hits = 0;
        time = (60 * 60);
        stilltime = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (total < cap && stilltime)
        {
            counter++;
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
            float a = Mathf.Floor(time / 60f);
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
            float c = Mathf.Round((time % 60) * (100f / 60f));
            if (c < 10)
            {
                gametimerc.text = "      0" + c;
            }
            else
            {
                gametimerc.text = "      " + c;
            }
            time--;
        }
        else
        {
            if (time == 0)
            {
                Instantiate(outoftime, this.transform);
            }
            stilltime = false;
            gametimera.text = "00      ";
            gametimerb.text = ":";
            gametimerc.text = "      00";
            time = -1;
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
