using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mole2DLogic : MonoBehaviour
{
    private int state;
    private int waittimer;
    private int counter;
    private GameObject leftring;
    private GameObject rightring;

    // Start is called before the first frame update
    void Start()
    {
        state = 0;
        counter = 0;
        waittimer = Random.Range(120, 301);
        leftring = GameObject.Find("LeftRing");
        rightring = GameObject.Find("RightRing");
        this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().moles[0];
        transform.localScale = new Vector3(-0.012f, 0, 1);
        float newx = (Random.Range(0, 90) * 0.01f);
        float newy = (Random.Range(0, 67) * 0.01f);
        transform.position = new Vector3(((transform.position.x + 0.43f) - newx) * 2.51f, ((transform.position.y - 0.34f) + newy) * 2.52f, transform.position.z);
        //transform.position = new Vector3(transform.position.x + 0.265f, transform.position.y, transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case 0:
                GetComponentInParent<GameManagerLogic>().UpdateCount(true);
                state = 1;
                break;
            case 1:
                float magnitude = 0.000038f;
                counter++;
                transform.position = new Vector3(transform.position.x, transform.position.y + 0.0006f, transform.position.z);
                transform.localScale = new Vector3(-0.012f, (transform.localScale.y + (0.001f - (magnitude * counter))), 1);
                if (counter > 20)
                {
                    transform.position = new Vector3(transform.position.x, transform.position.y - 0.0006f, transform.position.z);
                }
                if (counter > 30)
                {
                    state = 2;
                    counter = 0;
                }
                break;
            case 2:
                transform.localScale = new Vector3(-0.012f, 0.012f, 1);
                state = 3;
                break;
            case 3:
                counter++;

                if (leftring.GetComponent<RingLogic>().down && GetComponentInParent<GameManagerLogic>().stilltime)
                {
                    if (Mathf.Sqrt(((transform.position.x - leftring.transform.position.x) * (transform.position.x - leftring.transform.position.x)) + ((leftring.transform.position.y - transform.position.y) * (leftring.transform.position.y - transform.position.y))) < 0.265f)
                    {
                        counter = 0;
                        state = 5;
                        GetComponentInParent<GameManagerLogic>().UpdateHits();
                        GetComponentInParent<GameManagerLogic>().UpdateCount(false);
                        break;
                    }
                }

                if (rightring.GetComponent<Ring2Logic>().down && GetComponentInParent<GameManagerLogic>().stilltime)
                {
                    if (Mathf.Sqrt(((transform.position.x - rightring.transform.position.x) * (transform.position.x - rightring.transform.position.x)) + ((rightring.transform.position.y - transform.position.y) * (rightring.transform.position.y - transform.position.y))) < 0.265f)
                    {
                        counter = 0;
                        state = 5;
                        GetComponentInParent<GameManagerLogic>().UpdateHits();
                        GetComponentInParent<GameManagerLogic>().UpdateCount(false);
                        break;
                    }
                }

                if (counter > waittimer || !GetComponentInParent<GameManagerLogic>().stilltime)
                {
                    counter = 0;
                    state = 4;
                    GetComponentInParent<GameManagerLogic>().UpdateCount(false);
                }
                break;
            case 4:
                counter++;
                transform.localScale = new Vector3(-0.012f, transform.localScale.y - 0.0005f, 1);
                transform.position = new Vector3(transform.position.x, transform.position.y - 0.0006f, transform.position.z);
                if (counter > 24)
                {
                    Destroy(this.gameObject);
                }
                break;
            case 5:
                counter++;
                this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().moles[2];
                if (counter > 5)
                {
                    counter = 0;
                    state = 6;
                }
                break;
            case 6:
                counter++;
                this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().moles[1];
                transform.localScale = new Vector3(-0.012f, (transform.localScale.y - (0.0002f * counter)), 1);
                if (counter > 5)
                {
                    counter = 0;
                    state = 7;
                }
                break;
            case 7:
                counter++;
                transform.localScale = new Vector3(-0.012f, (transform.localScale.y + (0.0002f * counter)), 1);
                if (counter > 5)
                {
                    counter = 0;
                    state = 8;
                }
                break;
            case 8:
                counter++;
                transform.localScale = new Vector3(-0.012f, transform.localScale.y - 0.0010f, 1);
                transform.position = new Vector3(transform.position.x, transform.position.y - 0.0012f, transform.position.z);
                if (counter > 12)
                {
                    Destroy(this.gameObject);
                }
                break;
        }
    }
}
