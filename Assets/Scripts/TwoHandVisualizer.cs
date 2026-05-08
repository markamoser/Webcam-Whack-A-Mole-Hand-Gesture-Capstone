/// @file TwoHandVisualizer.cs
/// @brief Processes webcam input through a MediaPipe Holistic pipeline to detect, render,
/// and track both hands in 2D screen space. Calculates per-hand depth offsets based on
/// apparent hand size and exposes press/release gesture booleans for use by other scripts.

using UnityEngine;
using UnityEngine.UI;
using MediaPipe.Holistic;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// Manages webcam capture, MediaPipe hand tracking, hand mesh rendering, hand center
/// calculation, depth-based velocity tracking, and press/release gesture detection
/// for two hands simultaneously. Attach to a GameObject in the scene and assign the
/// required serialized fields in the Inspector.
/// </summary>
public class TwoHandVisualizer : MonoBehaviour
{
    /// <summary>Minimum confidence score for the hand detection model to accept a detection.</summary>
    public const float HAND_DETECTION_CONFIDENCE = 0.5f;
    /// <summary>Minimum confidence score for the hand tracking model to continue tracking.</summary>
    public const float HAND_TRACKING_CONFIDENCE = 0.5f;
    /// <summary>Maximum number of hands tracked simultaneously.</summary>
    public const int MAX_HANDS = 2;
    /// <summary>Number of landmark keypoints per hand as defined by MediaPipe.</summary>
    public const int NUMBER_OF_POINTS_ON_HANDS = 21;
    /// <summary>Number of vertices used to draw each keypoint circle in the hand shader.</summary>
    public const int VERTICES_PER_KEYPOINT_CIRCLE = 96;
    /// <summary>Number of vertices per bone line segment drawn between keypoints.</summary>
    public const int NUMBER_OF_VERTICIES_PER_LINE_SEGMENT = 2;
    /// <summary>Minimum confidence required before a recognised gesture is acted upon.</summary>
    public const float GESTURE_CONFIDENCE_THRESHOLD = 0.5f;
    /// <summary>Lerp factor applied each frame when smoothing gesture/depth values.</summary>
    public const float GESTURE_SMOOTHING_FACTOR = 0.5f;
    /// <summary>Default webcam capture width in pixels.</summary>
    public const int TEN_EIGHTY_P_WIDTH = 1920;
    /// <summary>Default webcam capture height in pixels.</summary>
    public const int TEN_EIGHTY_P_HEIGHT = 1080;
    /// <summary>Default webcam capture frame rate.</summary>
    public const int TEN_EIGHTY_P_FPS = 30;
    /// <summary>Number of past frames retained for gesture history smoothing.</summary>
    public const int MAX_FRAMES_IN_GESTURE_HISTORY = 5;
    /// <summary>Reference hand size (as a normalised landmark distance) used as the neutral/zero depth position.</summary>
    public const float NEUTRAL_HAND_SIZE = 0.2f;
    /// <summary>Scalar multiplier applied to the raw depth offset before clamping.</summary>
    public const float DEPTH_EFFECT_STRENGTH = 5.0f;
    /// <summary>Maximum depth offset allowed in the forward (towards camera) direction.</summary>
    public const float MAXIMUM_DEPTH_OFFSET_FORWARDS = 1.0f;
    /// <summary>Maximum depth offset allowed in the backward (away from camera) direction.</summary>
    public const float MAXIMUM_DEPTH_OFFSET_BACKWARDS = 1.0f;
    /// <summary>Lerp factor applied each frame when smoothing the calculated hand size.</summary>
    const float HAND_SIZE_SMOOTHING_FACTOR = 0.5f;
    /// <summary>Lerp factor applied each frame when smoothing the depth offset value.</summary>
    const float DEPTH_OFFSET_SMOOTHING_FACTOR = 0.5f;
    /// <summary>Default velocity sensitivity multiplier applied to the raw depth velocity.</summary>
    const float DEPTH_VELOCITY_SENSITIVITY = 1.0f;
    /// <summary>Depth velocity must fall below this value (negative = moving forward) to trigger a press.</summary>
    const float PRESS_VELOCITY_THRESHOLD_FOR_DEPTH_GESTURE = -2.5f;
    /// <summary>Depth velocity must rise above this value to release an active press.</summary>
    const float RELEASE_VELOCITY_THRESHOLD_FOR_DEPTH_GESTURE = 1.5f;

    /// <summary>UI element used to display the webcam texture on screen.</summary>
    [SerializeField] RawImage _screen;
    /// <summary>Shader used to render hand landmarks and bones over the webcam feed.</summary>
    [SerializeField] Shader _handShader;
    /// <summary>Minimum confidence threshold for tracking to render hand annotations.</summary>
    [SerializeField, Range(0, 1)] float _handScoreThreshold = HAND_TRACKING_CONFIDENCE;
    /// <summary>Name of the webcam device to use for capture. Empty selects the default camera.</summary>
    [SerializeField] string _webcamName = "";
    /// <summary>Requested webcam texture width in pixels.</summary>
    [SerializeField] int _webcamWidth = TEN_EIGHTY_P_WIDTH;
    /// <summary>Requested webcam texture height in pixels.</summary>
    [SerializeField] int _webcamHeight = TEN_EIGHTY_P_HEIGHT;
    /// <summary>Requested webcam capture frame rate.</summary>
    [SerializeField] int _webcamFPS = TEN_EIGHTY_P_FPS;
    /// <summary>Neutral hand size used as the baseline for depth offset calculations.</summary>
    [SerializeField] float _neutralHandSize = NEUTRAL_HAND_SIZE;
    /// <summary>Strength multiplier applied to calculated depth offsets.</summary>
    [SerializeField] float _depthStrength = DEPTH_EFFECT_STRENGTH;
    /// <summary>Maximum allowed backward depth offset for hands.</summary>
    [SerializeField] float _clampDepthBackwards = MAXIMUM_DEPTH_OFFSET_BACKWARDS;
    /// <summary>Maximum allowed forward depth offset for hands.</summary>
    [SerializeField] float _clampDepthForwards = MAXIMUM_DEPTH_OFFSET_FORWARDS;
    /// <summary>Number of keypoints averaged to find the hand center. Higher values are more accurate but cost more performance. Range 0-4.</summary>
    [SerializeField, Range(0, 4)] int _CenterCalcComplexity = 2;
    /// <summary>Whether the depth effect is enabled for hand tracking.</summary>
    [SerializeField] bool _enableDepthEffect = true;
    /// <summary>Current depth offset for the left hand. Visible in Inspector for debugging and tuning.</summary>
    [SerializeField] private float _leftHandDepthOffset = 0.0f;
    /// <summary>Current depth offset for the right hand. Visible in Inspector for debugging and tuning.</summary>
    [SerializeField] private float _rightHandDepthOffset = 0.0f;
    /// <summary>Velocity threshold used to register a press gesture from depth motion.</summary>
    [SerializeField] private float _pressVelocityThreshold = PRESS_VELOCITY_THRESHOLD_FOR_DEPTH_GESTURE;
    /// <summary>Velocity threshold used to release a press gesture from depth motion.</summary>
    [SerializeField] private float _releaseVelocityThreshold = RELEASE_VELOCITY_THRESHOLD_FOR_DEPTH_GESTURE;
    /// <summary>Multiplier that scales raw depth velocity before gesture threshold checks.</summary>
    [SerializeField] private float _sensitivity = DEPTH_VELOCITY_SENSITIVITY;
    /// <summary>Calculated depth velocity for the left hand this frame. Visible in Inspector for debugging.</summary>
    [SerializeField] private float _leftVelocity;
    /// <summary>Calculated depth velocity for the right hand this frame. Visible in Inspector for debugging.</summary>
    [SerializeField] private float _rightVelocity;

    /// <summary>Current depth velocity of the left hand. Negative values indicate forward motion.</summary>
    public float LeftVelocity => _leftVelocity;
    /// <summary>Current depth velocity of the right hand. Negative values indicate forward motion.</summary>
    public float RightVelocity => _rightVelocity;
    /// <summary>The velocity threshold a hand must cross to register a press gesture.</summary>
    public float PressVelocityThreshold => _pressVelocityThreshold;
    /// <summary>The velocity threshold a hand must cross to release an active press gesture.</summary>
    public float ReleaseVelocityThreshold => _releaseVelocityThreshold;

    /// <summary>Whether the left hand is currently in a pressed state. Latches true on press, releases on pull-back.</summary>
    [SerializeField] private bool _LeftPressed;
    /// <summary>Whether the right hand is currently in a pressed state. Latches true on press, releases on pull-back.</summary>
    [SerializeField] private bool _RightPressed;

    /// <summary>True while the left hand is in a latched press state and has not yet been released or consumed.</summary>
    public bool LeftPressed => _LeftPressed;
    /// <summary>True while the right hand is in a latched press state and has not yet been released or consumed.</summary>
    public bool RightPressed => _RightPressed;

    /// <summary>Smoothed depth offset for the left hand, readable by other scripts.</summary>
    public float LeftHandDepthOffset { get; private set; }
    /// <summary>Smoothed depth offset for the right hand, readable by other scripts.</summary>
    public float RightHandDepthOffset { get; private set; }

    /// <summary>Previous frame left hand depth measurement used for velocity calculation.</summary>
    private float _prevLeftDepth;
    /// <summary>Previous frame right hand depth measurement used for velocity calculation.</summary>
    private float _prevRightDepth;
    /// <summary>Cached normalized screen-space center position of the left hand.</summary>
    private Vector3 leftHandCenter;
    /// <summary>Cached normalized screen-space center position of the right hand.</summary>
    private Vector3 rightHandCenter;

    /// <summary>MediaPipe Holistic pipeline instance used for hand and pose inference.</summary>
    HolisticPipeline _pipeline;

    /// <summary>The underlying MediaPipe HolisticPipeline instance. Exposed for external inspection if needed.</summary>
    public HolisticPipeline Pipeline => _pipeline;

    /// <summary>WebCamTexture used to capture live video from the webcam.</summary>
    WebCamTexture _webcam;
    /// <summary>Render texture used to correct aspect ratio and vertical mirroring of the webcam feed.</summary>
    RenderTexture _correctedTexture;
    /// <summary>Material used to render the left hand overlay.</summary>
    Material _leftHandMaterial;
    /// <summary>Material used to render the right hand overlay.</summary>
    Material _rightHandMaterial;

    /// <summary>GameObject used as a marker for the left hand position in the scene.</summary>
    private GameObject leftmark;
    /// <summary>GameObject used as a marker for the right hand position in the scene.</summary>
    private GameObject rightmark;

    /// <summary>
    /// Bone connectivity pairs used by the hand shader to draw skeleton lines between keypoints.
    /// Indices correspond to MediaPipe hand landmark numbering. Kept for shader reference only.
    /// </summary>
    static readonly (int, int)[] BonePairs =
    {
        (0, 1), (1, 2), (1, 2), (2, 3), (3, 4),    // Thumb
        (5, 6), (6, 7), (7, 8),                     // Index finger
        (9, 10), (10, 11), (11, 12),                // Middle finger
        (13, 14), (14, 15), (15, 16),               // Ring finger
        (17, 18), (18, 19), (19, 20),               // Pinky
        (0, 5), (5, 17), (0, 17),                   // Palm triangle
        (0, 9),                                     // Palm centre segment
        (2, 5), (5, 9), (9, 13), (13, 17)           // Remaining palm connections
    };

    /// <summary>
    /// Jagged array of landmark index sets used to calculate the hand center point.
    /// The outer index corresponds to _CenterCalcComplexity (0-4). Higher complexity
    /// uses more keypoints for a more stable but costlier center estimate.
    /// </summary>
    static readonly int[][] CenterKeyPointIndices =
    {
        new int[] { 0 },           // Complexity 0: wrist only
        new int[] { 9 },           // Complexity 1: middle finger MCP only
        new int[] { 0, 9 },        // Complexity 2: wrist + middle finger MCP
        new int[] { 0, 5, 17 },    // Complexity 3: wrist + index MCP + pinky MCP
        new int[] { 0, 5, 9, 17 }  // Complexity 4: wrist + index MCP + middle MCP + pinky MCP
    };

    /// <summary>
    /// Initialises the webcam, MediaPipe pipeline, render textures, hand materials,
    /// and locates the left and right marker GameObjects in the scene.
    /// </summary>
    /// <remarks>Called automatically by Unity at scene start. No parameters or return value.</remarks>
    void Start()
    {
        leftmark = GameObject.Find("Left Point");
        rightmark = GameObject.Find("Right Point");
        _webcam = new WebCamTexture(_webcamName, _webcamWidth, _webcamHeight, _webcamFPS);
        _webcam.Play();
        _screen.texture = _webcam;
        _correctedTexture = new RenderTexture(_webcamWidth, _webcamHeight, 0);
        _pipeline = new HolisticPipeline();
        _leftHandMaterial = new Material(_handShader);
        _rightHandMaterial = new Material(_handShader);
        _leftHandMaterial.SetBuffer("_vertices", _pipeline.rightHandVertexBuffer);
        _rightHandMaterial.SetBuffer("_vertices", _pipeline.leftHandVertexBuffer);
        LeftHandDepthOffset = _leftHandDepthOffset;
        RightHandDepthOffset = _rightHandDepthOffset;
    }

    /// <summary>
    /// Releases all unmanaged resources: stops the webcam, releases the render texture,
    /// disposes the MediaPipe pipeline, and destroys hand materials.
    /// </summary>
    /// <remarks>Called automatically by Unity when the GameObject is destroyed. No parameters or return value.</remarks>
    void OnDestroy()
    {
        _webcam?.Stop();
        _correctedTexture?.Release();
        _pipeline?.Dispose();
        Destroy(_leftHandMaterial);
        Destroy(_rightHandMaterial);
    }

    /// <summary>
    /// Called once per frame after all Updates. Corrects webcam aspect ratio and vertical
    /// mirror, blits the result into the corrected render texture, runs the MediaPipe
    /// inference pass, then updates hand centers and depth offsets.
    /// </summary>
    /// <remarks>Called automatically by Unity after Update(). No parameters or return value.</remarks>
    void LateUpdate()
    {
        var aspect1 = (float)_webcam.width / _webcam.height;
        var aspect2 = (float)_correctedTexture.width / _correctedTexture.height;
        var aspectGap = aspect2 / aspect1;
        var vMirrored = _webcam.videoVerticallyMirrored;
        var scale = new Vector2(aspectGap, vMirrored ? -1 : 1);
        var offset = new Vector2((1 - aspectGap) / 2, vMirrored ? 1 : 0);
        Graphics.Blit(_webcam, _correctedTexture, scale, offset);
        _pipeline.ProcessImage(_correctedTexture, HolisticInferenceType.pose_and_hand);
        UpdateHandCenters();
        UpdateHandDepths();
    }

    /// <summary>
    /// Called by Unity's rendering system. Draws both hand meshes via the hand shader
    /// and repositions the left and right marker GameObjects to the current hand centers.
    /// </summary>
    /// <remarks>Called automatically by Unity for rendering. No parameters or return value.</remarks>
    void OnRenderObject()
    {
        Vector3 MarkOffset = new Vector3(0, 0, 0.35f);
        Vector3 MarkScale = new Vector3((1.8f / 0.5f), (1.0f / 0.5f), 1.0f);

        RenderHand(_leftHandMaterial, Color.red);
        leftmark.transform.position = new Vector3(
            (leftHandCenter.x - 0.5f) * MarkScale.x,
            (leftHandCenter.y - 0.5f) * MarkScale.y,
            MarkOffset.z
        );

        RenderHand(_rightHandMaterial, Color.green);
        rightmark.transform.position = new Vector3(
            (rightHandCenter.x - 0.5f) * MarkScale.x,
            (rightHandCenter.y - 0.5f) * MarkScale.y,
            MarkOffset.z
        );
    }

    /// <summary>
    /// Issues procedural draw calls for a single hand using the provided material and tint color.
    /// Draws keypoint circles on pass 0 and bone lines on pass 1.
    /// </summary>
    /// <param name="mat">The hand shader material configured with the correct vertex buffer.</param>
    /// <param name="color">The tint color applied to the rendered hand overlay.</param>
    /// <returns>No return value (void).</returns>
    void RenderHand(Material mat, Color color)
    {
        var w = _screen.rectTransform.rect.width;
        var h = _screen.rectTransform.rect.height;
        mat.SetVector("_uiScale", new Vector2(-w, -h));
        mat.SetVector("_pointColor", color);
        mat.SetFloat("_handScoreThreshold", _handScoreThreshold);
        mat.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, VERTICES_PER_KEYPOINT_CIRCLE, _pipeline.handVertexCount);
        mat.SetPass(1);
        Graphics.DrawProceduralNow(MeshTopology.Lines, NUMBER_OF_VERTICIES_PER_LINE_SEGMENT, NUMBER_OF_POINTS_ON_HANDS);
    }

    /// <summary>
    /// Calculates the arithmetic mean position of a set of landmark points.
    /// </summary>
    /// <param name="landmarks">Array of 3D landmark positions to average.</param>
    /// <returns>The mean Vector3 center of all provided landmarks.</returns>
    Vector3 FindHandCenter(Vector3[] landmarks)
    {
        Vector3 center = Vector3.zero;
        foreach (var landmark in landmarks)
            center += landmark;
        return center / landmarks.Length;
    }

    /// <summary>
    /// Finds the longest edge length in a closed polygon formed by the given landmarks,
    /// visited in order with wrap-around. Used as a proxy for apparent hand size.
    /// </summary>
    /// <param name="landmarks">Array of 3D landmark positions forming the polygon.</param>
    /// <returns>The length of the longest consecutive edge in the landmark sequence.</returns>
    float LongestKeyPointLength(Vector3[] landmarks)
    {
        float maxLength = 0f;
        for (int i = 0; i < landmarks.Length; i++)
        {
            int j = (i + 1) % landmarks.Length;
            float length = Vector3.Distance(landmarks[i], landmarks[j]);
            if (length > maxLength)
                maxLength = length;
        }
        return maxLength;
    }

    /// <summary>
    /// Samples the keypoints defined by CenterKeyPointIndices at the current complexity
    /// setting and updates leftHandCenter and rightHandCenter for both hands.
    /// </summary>
    /// <remarks>Called by LateUpdate() each frame. No parameters or return value.</remarks>
    void UpdateHandCenters()
    {
        int keyPointCount = CenterKeyPointIndices[_CenterCalcComplexity].Length;
        var leftHandLandmarks = new Vector3[keyPointCount];
        var rightHandLandmarks = new Vector3[keyPointCount];

        for (int i = 0; i < keyPointCount; i++)
        {
            leftHandLandmarks[i] = _pipeline.GetLeftHandLandmark(CenterKeyPointIndices[_CenterCalcComplexity][i]);
            rightHandLandmarks[i] = _pipeline.GetRightHandLandmark(CenterKeyPointIndices[_CenterCalcComplexity][i]);
        }

        leftHandCenter = FindHandCenter(leftHandLandmarks);
        rightHandCenter = FindHandCenter(rightHandLandmarks);
    }

    /// <summary>
    /// Calculates the depth offset for each hand from its apparent size relative to
    /// the neutral reference size, smooths the result with Lerp, computes per-hand
    /// depth velocity, and updates the LeftPressed/RightPressed latch booleans.
    /// Does nothing if _enableDepthEffect is false.
    /// </summary>
    /// <remarks>Called by LateUpdate() each frame. No parameters or return value.</remarks>
    void UpdateHandDepths()
    {
        if (!_enableDepthEffect) return;

        int keyPointCount = CenterKeyPointIndices[_CenterCalcComplexity].Length;
        var leftHandLandmarks = new Vector3[keyPointCount];
        var rightHandLandmarks = new Vector3[keyPointCount];

        for (int i = 0; i < keyPointCount; i++)
        {
            leftHandLandmarks[i] = _pipeline.GetLeftHandLandmark(CenterKeyPointIndices[_CenterCalcComplexity][i]);
            rightHandLandmarks[i] = _pipeline.GetRightHandLandmark(CenterKeyPointIndices[_CenterCalcComplexity][i]);
        }

        var leftHandSize = LongestKeyPointLength(leftHandLandmarks) / _neutralHandSize;
        var rightHandSize = LongestKeyPointLength(rightHandLandmarks) / _neutralHandSize;

        LeftHandDepthOffset = Mathf.Clamp((1 / leftHandSize - 1 / _neutralHandSize) * _depthStrength, -_clampDepthBackwards, _clampDepthForwards);
        RightHandDepthOffset = Mathf.Clamp((1 / rightHandSize - 1 / _neutralHandSize) * _depthStrength, -_clampDepthBackwards, _clampDepthForwards);
        _leftHandDepthOffset = LeftHandDepthOffset;
        _rightHandDepthOffset = RightHandDepthOffset;

        float depth_velocity_delta = Time.deltaTime;
        LeftHandDepthOffset = Mathf.Lerp(_prevLeftDepth, LeftHandDepthOffset, GESTURE_SMOOTHING_FACTOR);
        RightHandDepthOffset = Mathf.Lerp(_prevRightDepth, RightHandDepthOffset, GESTURE_SMOOTHING_FACTOR);
        _leftVelocity = ((LeftHandDepthOffset - _prevLeftDepth) / depth_velocity_delta) * _sensitivity;
        _rightVelocity = ((RightHandDepthOffset - _prevRightDepth) / depth_velocity_delta) * _sensitivity;

        if (!_LeftPressed && _leftVelocity < _pressVelocityThreshold)
            _LeftPressed = true;
        if (_LeftPressed && _leftVelocity > _releaseVelocityThreshold)
            _LeftPressed = false;

        if (!_RightPressed && _rightVelocity < _pressVelocityThreshold)
            _RightPressed = true;
        if (_RightPressed && _rightVelocity > _releaseVelocityThreshold)
            _RightPressed = false;

        _prevLeftDepth = LeftHandDepthOffset;
        _prevRightDepth = RightHandDepthOffset;
    }

    /// <summary>
    /// Forcibly clears the left hand press latch. Call this after a successful whack
    /// to require the player to pull their left hand back before pressing again.
    /// </summary>
    public void ConsumeLeftPress()  { _LeftPressed = false; }

    /// <summary>
    /// Forcibly clears the right hand press latch. Call this after a successful whack
    /// to require the player to pull their right hand back before pressing again.
    /// </summary>
    public void ConsumeRightPress() { _RightPressed = false; }
}