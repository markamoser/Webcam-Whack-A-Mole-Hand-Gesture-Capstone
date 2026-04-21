using UnityEngine;
using UnityEngine.UI;
using Klak.TestTools;
using MediaPipe.HandPose;

public sealed class HandAnimator : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] ImageSource _source = null;
    [Space]
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] bool _useAsyncReadback = true;
    [Space]
    [SerializeField] Mesh _jointMesh = null;
    [SerializeField] Mesh _boneMesh = null;
    [Space]
    [SerializeField] Material _jointMaterial = null;
    [SerializeField] Material _boneMaterial = null;
    [Space]
    [SerializeField] RawImage _monitorUI = null;
    [Space]
    [SerializeField] float _neutralHandSize = 0.2f; // Set neutral hand size relative to input image.
    [SerializeField] float _depthStrength = 5.0f;   // Strength of depth effect.
    [SerializeField] float _clampDepthBackwards = 1.0f;      // Maximum depth offset.
    [SerializeField] float _clampDepthForwards = 1.0f;      // Maximum depth offset.

    private GameObject testing;

    #endregion

    #region Private members

    HandPipeline _pipeline;

    static readonly (int, int)[] BonePairs =
    {
        (0, 1), (1, 2), (1, 2), (2, 3), (3, 4),     // Thumb
        (5, 6), (6, 7), (7, 8),                     // Index finger
        (9, 10), (10, 11), (11, 12),                // Middle finger
        (13, 14), (14, 15), (15, 16),               // Ring finger
        (17, 18), (18, 19), (19, 20),               // Pinky
        (0, 5), (5, 17), (0, 17),                   // Palm Triangle, Key points for calculating hand size and depth adjustment
        (2, 5), (5, 9), (9, 13), (13, 17)           // Rest of Palm
    };

    Matrix4x4 CalculateJointXform(Vector3 pos)
      => Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * 0.07f);

    Matrix4x4 CalculateBoneXform(Vector3 p1, Vector3 p2)
    {
        var length = Vector3.Distance(p1, p2) / 2;
        var radius = 0.03f;

        var center = (p1 + p2) / 2;
        var rotation = Quaternion.FromToRotation(Vector3.up, p2 - p1);
        var scale = new Vector3(radius, length, radius);

        return Matrix4x4.TRS(center, rotation, scale);
    }

    #endregion

    #region MonoBehaviour implementation

    void Start()
      => _pipeline = new HandPipeline(_resources);

    void OnDestroy()
      => _pipeline.Dispose();

    void LateUpdate()
    {
        //testing = GameObject.Find("Sphere");
        //testing.transform.position = _pipeline.GetKeyPoint(9);

        // Feed the input image to the Hand pose pipeline.
        _pipeline.UseAsyncReadback = _useAsyncReadback;
        _pipeline.ProcessImage(_source.Texture);

        // Calculate hand size and depth adjustment
        Vector3 p0 = _pipeline.GetKeyPoint(0);
        Vector3 p5 = _pipeline.GetKeyPoint(5);
        Vector3 p17 = _pipeline.GetKeyPoint(17);
        float d05 = Vector3.Distance(p0, p5);
        float d517 = Vector3.Distance(p5, p17);
        float d170 = Vector3.Distance(p17, p0);
        float maxDist = Mathf.Max(d05, d517, d170);     // Find the maximum distance between the three key points to determine hand size
        float scale = _neutralHandSize / maxDist;
        float depthOffset = Mathf.Clamp( (1/maxDist - 1/_neutralHandSize) * _depthStrength, -_clampDepthForwards, _clampDepthBackwards); // Calculate depth offset based on hand size, and clamp it to prevent excessive depth changes

        var layer = gameObject.layer;

        // Joint balls
        for (var i = 0; i < HandPipeline.KeyPointCount; i++)
        {
            Vector3 localPos = _pipeline.GetKeyPoint(i) - p0;
            Vector3 scaledPos = localPos * scale;
            Vector3 worldPos = p0 + scaledPos;          // Apply scaling to the joint position to maintain consistent hand size
            worldPos.z += depthOffset;                  // Apply depth offset to nodes
            var xform = CalculateJointXform(worldPos);
            Graphics.DrawMesh(_jointMesh, xform, _jointMaterial, layer);
        }

        // Bones
        foreach (var pair in BonePairs)
        {
            Vector3 p1_local = _pipeline.GetKeyPoint(pair.Item1) - p0;
            Vector3 p2_local = _pipeline.GetKeyPoint(pair.Item2) - p0;
            Vector3 p1_world = p0 + p1_local * scale;   // Apply scaling to the bone endpoints to maintain consistent hand size
            Vector3 p2_world = p0 + p2_local * scale;
            p1_world.z += depthOffset;                  // Apply depth offset to endpoints of bones
            p2_world.z += depthOffset;
            var xform = CalculateBoneXform(p1_world, p2_world);
            Graphics.DrawMesh(_boneMesh, xform, _boneMaterial, layer);
        }

        // UI update
        _monitorUI.texture = _source.Texture;
    }

    #endregion
}
