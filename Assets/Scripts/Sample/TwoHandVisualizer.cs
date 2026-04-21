using UnityEngine;
using UnityEngine.UI;
using MediaPipe.Holistic;

public class TwoHandVisualizer : MonoBehaviour
{
    [SerializeField] RawImage _screen;
    [SerializeField] Shader _handShader;
    [SerializeField, Range(0, 1)] float _handScoreThreshold = MediaPipeVariables.HAND_TRACKING_CONFIDENCE;
    [SerializeField] string _webcamName = "";
    [SerializeField] int _webcamWidth = 1920;
    [SerializeField] int _webcamHeight = 1080;
    [SerializeField] int _webcamFPS = 30;
    [SerializeField] float _leftNeutralHandSize = 0.2f;
    [SerializeField] float _rightNeutralHandSize = 0.2f;
    [SerializeField] float _depthStrength = 5.0f;
    [SerializeField] float _clampDepthBackwards = 1.0f;
    [SerializeField] float _clampDepthForwards = 1.0f;
    HolisticPipeline _pipeline;
    public HolisticPipeline Pipeline => _pipeline;
    WebCamTexture _webcam;
    RenderTexture _correctedTexture;
    Material _leftHandMaterial;
    Material _rightHandMaterial;

    private GameObject leftmark;
    private GameObject rightmark;

    public float LeftHandDepth { get; private set; }
    public float RightHandDepth { get; private set; }
    public float LeftHandScale { get; private set; }
    public float RightHandScale { get; private set; }
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

        // Calculate left hand depth and scale
        {
            Vector3 p0 = _pipeline.GetLeftHandLandmark(0);
            Vector3 p5 = _pipeline.GetLeftHandLandmark(5);
            Vector3 p17 = _pipeline.GetLeftHandLandmark(17);
            float d05 = Vector3.Distance(p0, p5);
            float d517 = Vector3.Distance(p5, p17);
            float d170 = Vector3.Distance(p17, p0);
            float maxDist = Mathf.Max(d05, d517, d170);
            if (maxDist > 0)
            {
                LeftHandScale = _leftNeutralHandSize / maxDist;
                LeftHandDepth = Mathf.Clamp((1 / maxDist - 1 / _leftNeutralHandSize) * _depthStrength, -_clampDepthForwards, _clampDepthBackwards);
            }
            else
            {
                LeftHandScale = 1f;
                LeftHandDepth = 0f;
            }
        }

        // Calculate right hand depth and scale
        {
            Vector3 p0 = _pipeline.GetRightHandLandmark(0);
            Vector3 p5 = _pipeline.GetRightHandLandmark(5);
            Vector3 p17 = _pipeline.GetRightHandLandmark(17);
            float d05 = Vector3.Distance(p0, p5);
            float d517 = Vector3.Distance(p5, p17);
            float d170 = Vector3.Distance(p17, p0);
            float maxDist = Mathf.Max(d05, d517, d170);
            if (maxDist > 0)
            {
                RightHandScale = _rightNeutralHandSize / maxDist;
                RightHandDepth = Mathf.Clamp((1 / maxDist - 1 / _rightNeutralHandSize) * _depthStrength, -_clampDepthForwards, _clampDepthBackwards);
            }
            else
            {
                RightHandScale = 1f;
                RightHandDepth = 0f;
            }
        }
    }
    void OnRenderObject()
    {
        RenderHand(_leftHandMaterial, Color.red);
        //leftmark.transform.position = new Vector3(-(_pipeline.GetLeftHandLandmark(0).x + (0.5f * (_pipeline.GetLeftHandLandmark(9).x - _pipeline.GetLeftHandLandmark(0).x)) - 0.5f) * (0.36f / 0.5f), (_pipeline.GetLeftHandLandmark(0).y + (0.5f * (_pipeline.GetLeftHandLandmark(9).y - _pipeline.GetLeftHandLandmark(0).y)) - 0.5f) * (0.2f / 0.5f), 0.35f);
        float leftCenterX = (_pipeline.GetLeftHandLandmark(0).x + (0.5f * (_pipeline.GetLeftHandLandmark(9).x - _pipeline.GetLeftHandLandmark(0).x)) - 0.5f) * (1.8f / 0.5f) * LeftHandScale;
        float leftCenterY = (_pipeline.GetLeftHandLandmark(0).y + (0.5f * (_pipeline.GetLeftHandLandmark(9).y - _pipeline.GetLeftHandLandmark(0).y)) - 0.5f) * (1.0f / 0.5f) * LeftHandScale;
        leftmark.transform.position = new Vector3(leftCenterX, leftCenterY, 0.35f);
        RenderHand(_rightHandMaterial, Color.green);
        //rightmark.transform.position = new Vector3(-(_pipeline.GetRightHandLandmark(0).x + (0.5f * (_pipeline.GetRightHandLandmark(9).x - _pipeline.GetRightHandLandmark(0).x)) - 0.5f) * (0.36f / 0.5f), (_pipeline.GetRightHandLandmark(0).y + (0.5f * (_pipeline.GetRightHandLandmark(9).y - _pipeline.GetRightHandLandmark(0).y)) - 0.5f) * (0.2f / 0.5f), 0.35f);
        float rightCenterX = (_pipeline.GetRightHandLandmark(0).x + (0.5f * (_pipeline.GetRightHandLandmark(9).x - _pipeline.GetRightHandLandmark(0).x)) - 0.5f) * (1.8f / 0.5f) * RightHandScale;
        float rightCenterY = (_pipeline.GetRightHandLandmark(0).y + (0.5f * (_pipeline.GetRightHandLandmark(9).y - _pipeline.GetRightHandLandmark(0).y)) - 0.5f) * (1.0f / 0.5f) * RightHandScale;
        rightmark.transform.position = new Vector3(rightCenterX, rightCenterY, 0.35f);
    }

    void RenderHand(Material mat, Color color)
    {
        var w = _screen.rectTransform.rect.width;
        var h = _screen.rectTransform.rect.height;
        // Apply hand-specific scaling to the UI scale
        float scaleFactor = (mat == _leftHandMaterial) ? LeftHandScale : RightHandScale;
        mat.SetVector("_uiScale", new Vector2(-w * scaleFactor, -h * scaleFactor));
        mat.SetVector("_pointColor", color);
        mat.SetFloat("_handScoreThreshold", _handScoreThreshold);
        mat.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, MediaPipeVariables.VERTICES_PER_KEYPOINT_CIRCLE, _pipeline.handVertexCount);
        mat.SetPass(1);
        Graphics.DrawProceduralNow(MeshTopology.Lines, MediaPipeVariables.NUMBER_OF_VERTICIES_PER_LINE_SEGMENT, MediaPipeVariables.NUMBER_OF_POINTS_ON_HANDS);
    }
}