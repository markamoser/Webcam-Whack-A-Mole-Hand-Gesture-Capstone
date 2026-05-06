using UnityEngine;

public class Ring2Logic : MonoBehaviour
{
    private GameObject rightmark;
    private TwoHandVisualizer _visualizer;

    public bool down;

    void Start()
    {
        down = false;
        transform.position = new Vector3(transform.position.x - 0.6f, transform.position.y, transform.position.z);
        rightmark = GameObject.Find("Right Point");

        _visualizer = FindObjectOfType<TwoHandVisualizer>();
    }

    void Update()
{
    transform.position = new Vector3(rightmark.transform.position.x, rightmark.transform.position.y, transform.position.z);

    down = _visualizer != null 
        && _visualizer.RightPressed 
        && _visualizer.RightVelocity < _visualizer.PressVelocityThreshold;
}
}