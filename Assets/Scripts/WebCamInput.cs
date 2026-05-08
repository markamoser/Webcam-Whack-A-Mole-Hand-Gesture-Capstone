/// @file WebCamInput.cs
/// @brief Manages webcam capture and provides a corrected RenderTexture for use by the
/// MediaPipe pipeline. Supports both live webcam input and a static fallback texture
/// for testing without a physical camera.

using UnityEngine;

/// <summary>
/// Captures frames from a named webcam device and blits them into a RenderTexture,
/// correcting for aspect ratio and vertical mirroring. If a static input texture is
/// assigned in the Inspector, that texture is used instead and no webcam is opened.
/// Attach to any GameObject and reference inputImageTexture from other scripts.
/// </summary>
public class WebCamInput : MonoBehaviour
{
    /// <summary>The device name of the webcam to open. Leave empty to use the system default.</summary>
    [SerializeField] string webCamName;
    /// <summary>The resolution the webcam will be requested to capture at. Actual resolution may differ by device.</summary>
    [SerializeField] Vector2 webCamResolution = new Vector2(1920, 1080);
    /// <summary>Optional static texture used instead of a live webcam feed. Assign in the Inspector for offline testing.</summary>
    [SerializeField] Texture staticInput;

    /// <summary>
    /// The current input image to be consumed by the MediaPipe pipeline.
    /// Returns the static texture if one is assigned, otherwise returns the
    /// corrected webcam RenderTexture updated each frame.
    /// </summary>
    public Texture inputImageTexture
    {
        get
        {
            if (staticInput != null) return staticInput;
            return inputRT;
        }
    }

    WebCamTexture webCamTexture;
    RenderTexture inputRT;

    /// <summary>
    /// Initialises the webcam device and allocates the output RenderTexture.
    /// If a static input texture is assigned, the webcam is not opened.
    /// </summary>
    /// <remarks>Called automatically by Unity at scene start. No parameters or return value.</remarks>
    void Start()
    {
        if (staticInput == null)
        {
            webCamTexture = new WebCamTexture(webCamName, (int)webCamResolution.x, (int)webCamResolution.y);
            webCamTexture.Play();
        }

        inputRT = new RenderTexture((int)webCamResolution.x, (int)webCamResolution.y, 0);
    }

    /// <summary>
    /// Called once per frame. Skips processing if a static texture is in use or if the
    /// webcam has not produced a new frame. Otherwise corrects the aspect ratio and
    /// vertical mirror of the webcam feed and blits the result into the output RenderTexture.
    /// </summary>
    /// <remarks>Called automatically by Unity each frame. No parameters or return value.</remarks>
    void Update()
    {
        if (staticInput != null) return;
        if (!webCamTexture.didUpdateThisFrame) return;

        var aspect1 = (float)webCamTexture.width / webCamTexture.height;
        var aspect2 = (float)inputRT.width / inputRT.height;
        var aspectGap = aspect2 / aspect1;

        var vMirrored = webCamTexture.videoVerticallyMirrored;
        var scale = new Vector2(aspectGap, vMirrored ? -1 : 1);
        var offset = new Vector2((1 - aspectGap) / 2, vMirrored ? 1 : 0);

        Graphics.Blit(webCamTexture, inputRT, scale, offset);
    }

    /// <summary>
    /// Releases the webcam device and RenderTexture when this GameObject is destroyed.
    /// </summary>
    /// <remarks>Called automatically by Unity when the GameObject is destroyed. No parameters or return value.</remarks>
    void OnDestroy()
    {
        if (webCamTexture != null) Destroy(webCamTexture);
        if (inputRT != null) Destroy(inputRT);
    }
}