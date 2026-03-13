using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingLogic : MonoBehaviour
{
    public bool down;

    // Start is called before the first frame update
    void Start()
    {
        down = true;
        transform.position = new Vector3(transform.position.x + 0.6f, transform.position.y, transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        float changex = 0f;
        float changey = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            changex += 0.005f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            changex -= 0.005f;
        }

        if (Input.GetKey(KeyCode.W))
        {
            changey += 0.005f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            changey -= 0.005f;
        }

        transform.position = new Vector3(transform.position.x + changex, transform.position.y + changey, transform.position.z);
    }
}
