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

    HolisticPipeline _pipeline;
    public HolisticPipeline Pipeline => _pipeline;
    WebCamTexture _webcam;
    RenderTexture _correctedTexture;
    Material _leftHandMaterial;
    Material _rightHandMaterial;

    private GameObject leftmark;
    private GameObject rightmark;

    void Start()
    {
        leftmark = GameObject.Find("Left Point");
        rightmark = GameObject.Find("Right Point");
        _webcam = new WebCamTexture(_webcamName, _webcamWidth, _webcamHeight, _webcamFPS);
        _webcam.Play();
        _screen.texture = _webcam;
        _correctedTexture = new RenderTexture(_webcamWidth, _webcamHeight, 0);
        _pipeline = new HolisticPipeline();
        _leftHandMaterial  = new Material(_handShader);
        _rightHandMaterial = new Material(_handShader);
        _leftHandMaterial.SetBuffer("_vertices",  _pipeline.rightHandVertexBuffer);
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
        var scale  = new Vector2(aspectGap, vMirrored ? -1 : 1);
        var offset = new Vector2((1 - aspectGap) / 2, vMirrored ? 1 : 0);
        Graphics.Blit(_webcam, _correctedTexture, scale, offset);
        _pipeline.ProcessImage(_correctedTexture, HolisticInferenceType.pose_and_hand);
    }

    void OnRenderObject()
    {
        RenderHand(_leftHandMaterial, Color.red);
        //leftmark.transform.position = new Vector3(-(_pipeline.GetLeftHandLandmark(0).x + (0.5f * (_pipeline.GetLeftHandLandmark(9).x - _pipeline.GetLeftHandLandmark(0).x)) - 0.5f) * (0.36f / 0.5f), (_pipeline.GetLeftHandLandmark(0).y + (0.5f * (_pipeline.GetLeftHandLandmark(9).y - _pipeline.GetLeftHandLandmark(0).y)) - 0.5f) * (0.2f / 0.5f), 0.35f);
        leftmark.transform.position = new Vector3((_pipeline.GetLeftHandLandmark(0).x + (0.5f * (_pipeline.GetLeftHandLandmark(9).x - _pipeline.GetLeftHandLandmark(0).x)) - 0.5f) * (1.8f / 0.5f), (_pipeline.GetLeftHandLandmark(0).y + (0.5f * (_pipeline.GetLeftHandLandmark(9).y - _pipeline.GetLeftHandLandmark(0).y)) - 0.5f) * (1.0f / 0.5f), 0.35f);
        RenderHand(_rightHandMaterial, Color.green);
        //rightmark.transform.position = new Vector3(-(_pipeline.GetRightHandLandmark(0).x + (0.5f * (_pipeline.GetRightHandLandmark(9).x - _pipeline.GetRightHandLandmark(0).x)) - 0.5f) * (0.36f / 0.5f), (_pipeline.GetRightHandLandmark(0).y + (0.5f * (_pipeline.GetRightHandLandmark(9).y - _pipeline.GetRightHandLandmark(0).y)) - 0.5f) * (0.2f / 0.5f), 0.35f);
        rightmark.transform.position = new Vector3((_pipeline.GetRightHandLandmark(0).x + (0.5f * (_pipeline.GetRightHandLandmark(9).x - _pipeline.GetRightHandLandmark(0).x)) - 0.5f) * (1.8f / 0.5f), (_pipeline.GetRightHandLandmark(0).y + (0.5f * (_pipeline.GetRightHandLandmark(9).y - _pipeline.GetRightHandLandmark(0).y)) - 0.5f) * (1.0f / 0.5f), 0.35f);
    }

    void RenderHand(Material mat, Color color)
    {
        var w = _screen.rectTransform.rect.width;
        var h = _screen.rectTransform.rect.height;
        mat.SetVector("_uiScale", new Vector2(-w, -h));
        mat.SetVector("_pointColor", color);
        mat.SetFloat("_handScoreThreshold", _handScoreThreshold);
        mat.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, MediaPipeVariables.VERTICES_PER_KEYPOINT_CIRCLE, _pipeline.handVertexCount);
        mat.SetPass(1);
        Graphics.DrawProceduralNow(MeshTopology.Lines, MediaPipeVariables.NUMBER_OF_VERTICIES_PER_LINE_SEGMENT, MediaPipeVariables.NUMBER_OF_POINTS_ON_HANDS);
    }
}