using UnityEngine;
using UnityEngine.UI;
using MediaPipe.Holistic;
using System.Collections.Generic;
using System;
using System.Linq;

public class TwoHandVisualizer : MonoBehaviour
{
    public const float HAND_DETECTION_CONFIDENCE = 0.5f;
    public const float HAND_TRACKING_CONFIDENCE = 0.5f;
    public const int MAX_HANDS = 2;
    public const int NUMBER_OF_POINTS_ON_HANDS = 21;
    public const int VERTICES_PER_KEYPOINT_CIRCLE = 96;
    public const int NUMBER_OF_VERTICIES_PER_LINE_SEGMENT = 2;
    public const float GESTURE_CONFIDENCE_THRESHOLD = 0.5f;
    public const float GESTURE_SMOOTHING_FACTOR = 0.5f;
    public const int TEN_EIGHTY_P_WIDTH = 1920;
    public const int TEN_EIGHTY_P_HEIGHT = 1080;
    public const int TEN_EIGHTY_P_FPS = 30;
    public const int MAX_GESTURE_HISTORY = 5; // Number of frames to keep in gesture history for smoothing and stability analysis.
    public const float NEUTRAL_HAND_SIZE = 0.2f;
    public const float DEPTH_EFFECT_STRENGTH = 5.0f;   
    public const float MAXIMUM_DEPTH_OFFSET_FORWARDS = 1.0f;      
    public const float MAXIMUM_DEPTH_OFFSET_BACKWARDS = 1.0f;      
    // Webcam and rendering parameters
    [SerializeField] RawImage _screen;
    [SerializeField] Shader _handShader;
    [SerializeField, Range(0, 1)] float _handScoreThreshold = HAND_TRACKING_CONFIDENCE;
    [SerializeField] string _webcamName = "";
    [SerializeField] int _webcamWidth = TEN_EIGHTY_P_WIDTH;
    [SerializeField] int _webcamHeight = TEN_EIGHTY_P_HEIGHT;
    [SerializeField] int _webcamFPS = TEN_EIGHTY_P_FPS;

    // Hand size and depth effect parameters
    [SerializeField] float _neutralHandSize = NEUTRAL_HAND_SIZE;
    [SerializeField] float _depthStrength = DEPTH_EFFECT_STRENGTH;
    [SerializeField] float _clampDepthBackwards = MAXIMUM_DEPTH_OFFSET_BACKWARDS;      
    [SerializeField] float _clampDepthForwards = MAXIMUM_DEPTH_OFFSET_FORWARDS;      

    [SerializeField, Range(0, 4)] int _CenterCalcComplexity = 2; // Number of key points to consider when calculating hand center, set to 5 for better performance while maintaining reasonable accuracy.
    [SerializeField] bool _enableDepthEffect = true; 

    [SerializeField] private float _leftHandDepthOffset = 0.0f; // Depth offset for left hand, displayed in inspector for debugging and tuning purposes.
    [SerializeField] private float _rightHandDepthOffset = 0.0f; // Depth offset for right hand, displayed in inspector for debugging and tuning purposes.

    [SerializeField] private float _pressVelocityThreshold = -2.0f;
    [SerializeField] private float _releaseVelocityThreshold = 2.0f;
    [SerializeField] private float _sensitivity = 1.0f;
    [SerializeField] private float _leftVelocity;
    [SerializeField] private float _rightVelocity;
    [SerializeField] private bool _leftPressIndicator=false;
    [SerializeField] private bool _rightPressIndicator=false;

    private float _prevLeftDepth;
    private float _prevRightDepth;

     [SerializeField] public bool LeftPressed { get; private set; }
     [SerializeField] public bool RightPressed { get; private set; }
    public float LeftHandDepthOffset { get; private set; } // Depth offset for left hand, call from other scripts.
    public float RightHandDepthOffset { get; private set; } // Depth offset for right hand, call from other scripts.

    private Vector3 leftHandCenter;
    private Vector3 rightHandCenter;

    HolisticPipeline _pipeline;
    public HolisticPipeline Pipeline => _pipeline;
    WebCamTexture _webcam;
    RenderTexture _correctedTexture;
    Material _leftHandMaterial;
    Material _rightHandMaterial;

    private GameObject leftmark;
    private GameObject rightmark;

    static readonly (int, int)[] BonePairs =        // Reference only
    {
        (0, 1), (1, 2), (1, 2), (2, 3), (3, 4),     // Thumb
        (5, 6), (6, 7), (7, 8),                     // Index finger
        (9, 10), (10, 11), (11, 12),                // Middle finger
        (13, 14), (14, 15), (15, 16),               // Ring finger
        (17, 18), (18, 19), (19, 20),               // Pinky
        (0, 5), (5, 17), (0, 17),                   // Palm Triangle, 3 Key points for calculating hand size and depth adjustment
        (0, 9),                                     // Palm Segment, 2 Key points for calculating hand size and depth adjustment
        (2, 5), (5, 9), (9, 13), (13, 17)           // Rest of Palm
    };

    static readonly int[][] CenterKeyPointIndices =        // Key points used to calculate hand center, set to 3 for better performance while maintaining reasonable accuracy.
    {
        new int[] { 0 }, // Single wrist key point only, not recommended but included for reference
        new int[] { 9 }, // Single middle finger MCP key point only, not recommended but included for reference
        new int[] { 0, 9 }, // Two key points (wrist and middle finger MCP) for a more stable center point, recommended for most use cases and performance benefits
        new int[] { 0, 5, 17 }, // 3 key points (wrist, index finger MCP, pinky MCP) for a more accurate center point, recommended if performance is not a big concern and the most accurate depth effect is desired.
        new int[] { 0, 5, 9, 17 } // 4 key points (wrist, index finger MCP, middle finger MCP, pinky MCP) for an even more accurate center point, recommended if performance is not a concern and the most accurate depth effect is desired.
    };

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

    void OnDestroy()
    {
        _webcam?.Stop();
        _correctedTexture?.Release();
        _pipeline?.Dispose();
        Destroy(_leftHandMaterial);
        Destroy(_rightHandMaterial);
    }

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

    Vector3 FindHandCenter(Vector3[] landmarks)
    {
        Vector3 center = Vector3.zero;
        foreach (var landmark in landmarks)
        {
            center += landmark;
        }
        return center / landmarks.Length;
    }

    float LongestKeyPointLength(Vector3[] landmarks)
    {
        float maxLength = 0f;
        for (int i = 0; i < landmarks.Length; i++)
        {
            int j = (i + 1) % landmarks.Length; // Wrap around to the first point
            float length = Vector3.Distance(landmarks[i], landmarks[j]);
            if (length > maxLength)
            {
                maxLength = length; 
            }
        }
        return maxLength;
    }

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

        leftHandCenter = FindHandCenter(leftHandLandmarks.ToArray());
        rightHandCenter = FindHandCenter(rightHandLandmarks.ToArray());
    }

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

        LeftHandDepthOffset = Mathf.Clamp((leftHandCenter.z*0 + 1/leftHandSize - 1/_neutralHandSize) * _depthStrength, -_clampDepthBackwards, _clampDepthForwards);
        RightHandDepthOffset = Mathf.Clamp((rightHandCenter.z*0 + 1/rightHandSize - 1/_neutralHandSize) * _depthStrength, -_clampDepthBackwards, _clampDepthForwards);
        _leftHandDepthOffset = LeftHandDepthOffset;
        _rightHandDepthOffset = RightHandDepthOffset;

        //Debug.Log($"Left Hand Depth Offset: {LeftHandDepthOffset}, Right Hand Depth Offset: {RightHandDepthOffset}, Using points: {string.Join(", ", CenterKeyPointIndices[_CenterCalcComplexity].Select(i => i.ToString()))}");
        // Debug.Log($"Left Hand Depth Offset: {_leftHandDepthOffset}, Right Hand Depth Offset: {_rightHandDepthOffset}");

        float depth_velocity_delta = Time.deltaTime;
        LeftHandDepthOffset = Mathf.Lerp(_prevLeftDepth, LeftHandDepthOffset, 0.5f);
        RightHandDepthOffset = Mathf.Lerp(_prevRightDepth, RightHandDepthOffset, 0.5f);
        _leftVelocity = ((LeftHandDepthOffset - _prevLeftDepth) / depth_velocity_delta) * _sensitivity;
        _rightVelocity = ((RightHandDepthOffset - _prevRightDepth) / depth_velocity_delta) * _sensitivity;
       //if (_rightVelocity < _pressVelocityThreshold){Debug.Log($"RIGHT BELOW THRESHOLD: {_rightVelocity}");}

        if (!LeftPressed && _leftVelocity < _pressVelocityThreshold)
        {
            LeftPressed = true;
            _leftPressIndicator = true;
        }
        if (LeftPressed && _leftVelocity > _releaseVelocityThreshold)
        {
            LeftPressed = false;
            _leftPressIndicator = false;
        }
        if (!RightPressed && _rightVelocity < _pressVelocityThreshold)
        {
            RightPressed = true;
            _rightPressIndicator = true;
        }
        if (RightPressed && _rightVelocity > _releaseVelocityThreshold)
        {
            RightPressed = false;
            _rightPressIndicator = false;
        }

        _prevLeftDepth = LeftHandDepthOffset;
        _prevRightDepth = RightHandDepthOffset;
        //Debug.Log($"L Vel: {_leftVelocity}, R Vel: {_rightVelocity}");

    }

}