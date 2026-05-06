using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mole2DLogic : MonoBehaviour
{
    private int waitmin;
    private int waitmax;
    private int waittimer;
    private float counter;
    private GameObject leftring;
    private GameObject rightring;

    private enum State
    {
        MoleUpdateGameManagerMoleCount,
        MolePopUpFromGround,
        MoleWaitToGetWhackedOrForWaitTimerToExpire,
        MoleDigIntoGroundAndLeave,
        MoleWhackedAndShowExplosion,
        MoleWhackedAndSquishDown,
        MoleWhackedAndSquishUp,
        MoleWhackedAndFallIntoGround
    }

    private State stateenum;

    // Start is called before the first frame update
    void Start()
    {
        stateenum = State.MoleUpdateGameManagerMoleCount;
        counter = 0;
        waitmin = 4;
        waitmax = 6;
        waittimer = Random.Range((waitmin * 60), ((waitmax * 60) + 1));
        leftring = GameObject.Find("LeftRing");
        rightring = GameObject.Find("RightRing");
        this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().moles[0];
        transform.localScale = new Vector3(-0.012f, 0, 1);
        float newx = (Random.Range(0, 90) * 0.01f);
        float newy = (Random.Range(0, 67) * 0.01f);
        transform.position = new Vector3(((transform.position.x + 0.43f) - newx) * 2.51f, ((transform.position.y - 0.34f) + newy) * 2.52f, transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        switch (stateenum)
        {
            case State.MoleUpdateGameManagerMoleCount:
                GetComponentInParent<GameManagerLogic>().UpdateCount(true);
                stateenum = State.MolePopUpFromGround;
                break;
            case State.MolePopUpFromGround:
                float magnitude = 0.000038f;
                float positionchange = 0.0006f;
                float scalechange = 0.001f;
                counter = counter + (Time.deltaTime / (1f / 60f));
                transform.position = new Vector3(transform.position.x, transform.position.y + (positionchange * (Time.deltaTime / (1f / 60f))), transform.position.z);
                transform.localScale = new Vector3(-0.012f, (transform.localScale.y + ((scalechange - (magnitude * counter)) * (Time.deltaTime / (1f / 60f)))), 1);
                if (counter > 20)
                {
                    transform.position = new Vector3(transform.position.x, transform.position.y - (positionchange * (Time.deltaTime / (1f / 60f))), transform.position.z);
                }
                if (counter > 30)
                {
                    stateenum = State.MoleWaitToGetWhackedOrForWaitTimerToExpire;
                    counter = 0;
                }
                break;
            case State.MoleWaitToGetWhackedOrForWaitTimerToExpire:
                counter = counter + (Time.deltaTime / (1f / 60f));

                transform.localScale = new Vector3(-0.012f, transform.localScale.y + ((0.012f - transform.localScale.y) / 2f), 1);

                float circlecheckrange = 0.265f;

                if (leftring.GetComponent<RingLogic>().down && GetComponentInParent<GameManagerLogic>().stilltime)
                {
                    if (Mathf.Sqrt(((transform.position.x - leftring.transform.position.x) * (transform.position.x - leftring.transform.position.x)) + ((leftring.transform.position.y - transform.position.y) * (leftring.transform.position.y - transform.position.y))) < circlecheckrange)
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
                    if (Mathf.Sqrt(((transform.position.x - rightring.transform.position.x) * (transform.position.x - rightring.transform.position.x)) + ((rightring.transform.position.y - transform.position.y) * (rightring.transform.position.y - transform.position.y))) < circlecheckrange)
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
            case State.MoleDigIntoGroundAndLeave:
                float positionchange2 = 0.0006f;
                float scalechange2 = 0.0005f;
                counter = counter + (Time.deltaTime / (1f / 60f));
                transform.localScale = new Vector3(-0.012f, transform.localScale.y - (scalechange2 * (Time.deltaTime / (1f / 60f))), 1);
                transform.position = new Vector3(transform.position.x, transform.position.y - (positionchange2 * (Time.deltaTime / (1f / 60f))), transform.position.z);
                if (counter > 24)
                {
                    Destroy(this.gameObject);
                }
                break;
            case State.MoleWhackedAndShowExplosion:
                counter = counter + (Time.deltaTime / (1f / 60f));
                transform.localScale = new Vector3(-0.012f, 0.012f, 1);
                this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().moles[2];
                if (counter > 5)
                {
                    counter = 0;
                    stateenum = State.MoleWhackedAndSquishDown;
                }
                break;
            case State.MoleWhackedAndSquishDown:
                float scalechange3 = 0.0002f;
                counter = counter + (Time.deltaTime / (1f / 60f));
                this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().moles[1];
                transform.localScale = new Vector3(-0.012f, (transform.localScale.y - ((scalechange3 * counter) * (Time.deltaTime / (1f / 60f)))), 1);
                if (counter > 5)
                {
                    counter = 0;
                    stateenum = State.MoleWhackedAndSquishUp;
                }
                break;
            case State.MoleWhackedAndSquishUp:
                float scalechange4 = 0.0002f;
                counter = counter + (Time.deltaTime / (1f / 60f));
                transform.localScale = new Vector3(-0.012f, (transform.localScale.y + ((scalechange4 * counter) * (Time.deltaTime / (1f / 60f)))), 1);
                if (counter > 5)
                {
                    counter = 0;
                    transform.localScale = new Vector3(-0.012f, 0.012f, 1);
                    stateenum = State.MoleWhackedAndFallIntoGround;
                }
                break;
            case State.MoleWhackedAndFallIntoGround:
                float positionchange3 = 0.001f;
                float scalechange5 = 0.0012f;
                counter = counter + (Time.deltaTime / (1f / 60f));
                transform.localScale = new Vector3(-0.012f, transform.localScale.y - (positionchange3 * (Time.deltaTime / (1f / 60f))), 1);
                transform.position = new Vector3(transform.position.x, transform.position.y - (scalechange5 * (Time.deltaTime / (1f / 60f))), transform.position.z);
                if (transform.localScale.y < 0)
                {
                    transform.localScale = new Vector3(-0.012f, 0, 1);
                }
                if (counter > 12)
                {
                    Destroy(this.gameObject);
                }
                break;
        }
    }
}
