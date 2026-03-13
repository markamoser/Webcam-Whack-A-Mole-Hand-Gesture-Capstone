using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeOutLogic : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        transform.localScale = new Vector3(0, 0, 1);
    }

    // Update is called once per frame
    void Update()
    {
        transform.localScale = new Vector3(transform.localScale.x + ((0.15f - transform.localScale.x) / 10), transform.localScale.y + ((0.15f - transform.localScale.y) / 10), 1);
    }
}
