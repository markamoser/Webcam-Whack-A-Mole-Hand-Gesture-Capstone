using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ring2Logic : MonoBehaviour
{
    public bool down;

    // Start is called before the first frame update
    void Start()
    {
        down = true;
        transform.position = new Vector3(transform.position.x - 0.6f, transform.position.y, transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        float changex = 0f;
        float changey = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            changex += 0.005f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            changex -= 0.005f;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            changey += 0.005f;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            changey -= 0.005f;
        }

        transform.position = new Vector3(transform.position.x + changex, transform.position.y + changey, transform.position.z);
    }
}
