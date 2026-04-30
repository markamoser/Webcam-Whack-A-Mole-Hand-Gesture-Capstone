using UnityEngine;

public class RingLogic : MonoBehaviour
{
    private GameObject leftmark;
    private TwoHandVisualizer _visualizer;

    public bool down;

    void Start()
    {
        down = false;
        transform.position = new Vector3(transform.position.x + 0.6f, transform.position.y, transform.position.z);
        leftmark = GameObject.Find("Left Point");

        _visualizer = FindObjectOfType<TwoHandVisualizer>();
    }

    void Update()
{
    transform.position = new Vector3(leftmark.transform.position.x, leftmark.transform.position.y, transform.position.z);

    // Must be in pressed state AND velocity still below threshold right now
    down = _visualizer != null 
        && _visualizer.LeftPressed 
        && _visualizer.LeftVelocity < _visualizer.PressVelocityThreshold;
}
}