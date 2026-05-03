using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingStartAreaLogic : MonoBehaviour
{
    private float counter;
    private GameObject leftring;
    private GameObject rightring;

    public bool l;
    public bool r;

    private enum State
    {
        RingAreaCheckForHand,
        RingAreaWaitOnYellowCircle,
        RingAreaWaitOnCyanCircle,
        RingAreaShrinkCircle
    }

    private State stateenum;

    // Start is called before the first frame update
    void Start()
    {
        stateenum = State.RingAreaCheckForHand;
        counter = 0;
        leftring = GameObject.Find("LeftRing");
        rightring = GameObject.Find("RightRing");
    }

    // Update is called once per frame
    void Update()
    {
        switch (stateenum)
        {
            case State.RingAreaCheckForHand:
                l = (Mathf.Sqrt(((transform.position.x - leftring.transform.position.x) * (transform.position.x - leftring.transform.position.x)) + ((leftring.transform.position.y - transform.position.y) * (leftring.transform.position.y - transform.position.y))) < 0.08f);
                r = (Mathf.Sqrt(((transform.position.x - rightring.transform.position.x) * (transform.position.x - rightring.transform.position.x)) + ((rightring.transform.position.y - transform.position.y) * (rightring.transform.position.y - transform.position.y))) < 0.08f);
                if (l || r)
                {
                    this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().ringareas[1];
                }
                else
                {
                    this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().ringareas[0];
                }
                break;
            case State.RingAreaWaitOnYellowCircle:
                this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().ringareas[1];
                break;
            case State.RingAreaWaitOnCyanCircle:
                this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().ringareas[2];
                break;
            case State.RingAreaShrinkCircle:
                counter = counter + (Time.deltaTime / (1f / 60f));
                transform.localScale = new Vector3(0.07f * ((60f - counter) / 60), 0.07f * ((60f - counter) / 60), 1);
                if (counter > 60)
                {
                    Destroy(this.gameObject);
                }
                break;
        }
    }

    public void SetLeftArea()
    {
        transform.position = new Vector3(transform.position.x + 0.6f, transform.position.y - 0.4f, transform.position.z);
    }

    public void SetRightArea()
    {
        transform.position = new Vector3(transform.position.x - 0.6f, transform.position.y - 0.4f, transform.position.z);
    }

    public void SetState(int value)
    {
        switch (value)
        {
            case 0:
                stateenum = State.RingAreaCheckForHand;
                break;
            case 1:
                stateenum = State.RingAreaWaitOnYellowCircle;
                break;
            case 2:
                stateenum = State.RingAreaWaitOnCyanCircle;
                break;
            case 3:
                stateenum = State.RingAreaShrinkCircle;
                break;
        }
    }
}
