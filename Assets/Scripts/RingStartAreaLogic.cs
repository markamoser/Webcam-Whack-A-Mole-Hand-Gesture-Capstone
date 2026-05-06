using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingStartAreaLogic : MonoBehaviour
{
    private int state;
    private float counter;
    private GameObject leftring;
    private GameObject rightring;

    public bool l;
    public bool r;

    // Start is called before the first frame update
    void Start()
    {
        state = 0;
        counter = 0;
        leftring = GameObject.Find("LeftRing");
        rightring = GameObject.Find("RightRing");
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case 0:
                l = (Mathf.Sqrt(((transform.position.x - leftring.transform.position.x) * (transform.position.x - leftring.transform.position.x)) + ((leftring.transform.position.y - transform.position.y) * (leftring.transform.position.y - transform.position.y))) < 0.07f);
                r = (Mathf.Sqrt(((transform.position.x - rightring.transform.position.x) * (transform.position.x - rightring.transform.position.x)) + ((rightring.transform.position.y - transform.position.y) * (rightring.transform.position.y - transform.position.y))) < 0.07f);
                if (l || r)
                {
                    this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().ringareas[1];
                }
                else
                {
                    this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().ringareas[0];
                }
                break;
            case 1:
                this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().ringareas[1];
                break;
            case 2:
                this.GetComponent<SpriteRenderer>().sprite = GetComponentInParent<GameManagerLogic>().ringareas[2];
                break;
            case 3:
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
        state = value;
    }
}
