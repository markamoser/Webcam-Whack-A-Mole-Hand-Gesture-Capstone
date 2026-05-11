/// @file HandVisualizer.shader
/// @brief Procedural hand overlay shader for the MediaPipe Holistic hand tracking visualiser.
/// Renders 21 hand keypoints as filled circles and connects them with bone line segments
/// using two passes driven entirely by structured GPU buffers — no mesh data is required.

Shader "Hidden/HolisticBarracuda/HandVisuallizer"
{
    CGINCLUDE

    #include "UnityCG.cginc"

    /// @brief Total number of hand landmark keypoints as defined by MediaPipe.
    #define VERTEX_COUNT 21

    /// @brief UI canvas scale vector passed from TwoHandVisualizer to map normalised
    ///        landmark coordinates into screen-space clip coordinates.
    float2 _uiScale;

    /// @brief Tint color applied to all keypoint circles for this hand draw call.
    float4 _pointColor;

    /// @brief Minimum hand detection confidence score below which the hand is not rendered.
    float _handScoreThreshold;

    /// @brief Structured buffer containing 22 float4 entries: indices 0-20 are the
    ///        normalised XYZ landmark positions, index 21 carries the detection score in X.
    StructuredBuffer<float4> _vertices;

    /// @brief Vertex shader for Pass 0. Renders each of the 21 hand landmarks as a
    ///        filled circle using a triangle-fan approach driven by vertex and instance IDs.
    ///        Each circle is built from (VERTICES_PER_KEYPOINT_CIRCLE / 3) fan triangles.
    ///        The hand is hidden entirely if the detection score is below _handScoreThreshold.
    /// @param vid   SV_VertexID   Index of the current vertex within the procedural draw.
    /// @param iid   SV_InstanceID Index of the landmark keypoint this instance represents (0-20).
    /// @param position SV_Position Output clip-space position of the vertex.
    /// @param color    COLOR       Output color; equals _pointColor if score passes threshold, else transparent.
    void VertexKeys(uint vid : SV_VertexID,
                    uint iid : SV_InstanceID,
                    out float4 position : SV_Position,
                    out float4 color : COLOR)
    {
        float3 p = _vertices[iid].xyz;

        uint fan = vid / 3;
        uint segment = vid % 3;

        float theta = (fan + segment - 1) * UNITY_PI / 16;
        float radius = (segment > 0) * 0.008;

        p.xy += float2(cos(theta), sin(theta)) * radius;
        p.xy = (2 * p.xy - 1) * _uiScale / _ScreenParams.xy;

        float score = _vertices[VERTEX_COUNT].x;

        position = float4(p.x, -p.y, 0, 1);
        color = (score >= _handScoreThreshold) ? _pointColor : float4(0, 0, 0, 0);
    }

    /// @brief Vertex shader for Pass 1. Renders the hand skeleton as line segments
    ///        connecting adjacent landmark keypoints. Each bone segment is one instance;
    ///        vertex IDs 0 and 1 select the start and end landmark of that segment.
    ///        Palm root connections are handled by remapping the index for finger bases.
    /// @param vid   SV_VertexID   0 or 1, selecting the start or end point of this bone.
    /// @param iid   SV_InstanceID Index of the bone segment being drawn.
    /// @param position SV_Position Output clip-space position of the vertex.
    /// @param color    COLOR       Output color; always opaque white for all bone segments.
    void VertexBones(uint vid : SV_VertexID,
                     uint iid : SV_InstanceID,
                     out float4 position : SV_Position,
                     out float4 color : COLOR)
    {
        uint finger = iid / 4;
        uint segment = iid % 4;

        uint i = min(4, finger) * 4 + segment + vid;
        uint root = finger > 1 && finger < 5 ? i - 3 : 0;

        i = max(segment, vid) == 0 ? root : i;

        float3 p = _vertices[i].xyz;
        p.xy = (2 * p.xy - 1) * _uiScale / _ScreenParams.xy;

        position = float4(p.x, -p.y, 0, 1);
        color = float4(1, 1, 1, 1);
    }

    /// @brief Shared fragment shader for both passes. Passes the interpolated
    ///        vertex color directly to the render target with no modification.
    /// @param position SV_Position Fragment screen position (unused).
    /// @param color    COLOR       Interpolated color from the vertex shader.
    /// @return SV_Target The final RGBA color written to the render target.
    float4 Fragment(float4 position : SV_Position,
                    float4 color : COLOR) : SV_Target
    {
        return color;
    }

    ENDCG

    SubShader
    {
        // Disable depth write and test so the overlay always draws on top.
        // Alpha blending enabled for transparent keypoint hiding below score threshold.
        ZWrite Off ZTest Always Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        /// @brief Pass 0 — Keypoint circles. Draws filled circles at each of the
        ///        21 landmark positions using the VertexKeys triangle-fan shader.
        Pass
        {
            CGPROGRAM
            #pragma vertex VertexKeys
            #pragma fragment Fragment
            ENDCG
        }

        /// @brief Pass 1 — Skeleton bones. Draws white line segments connecting
        ///        adjacent landmarks to form the hand skeleton using VertexBones.
        Pass
        {
            CGPROGRAM
            #pragma vertex VertexBones
            #pragma fragment Fragment
            ENDCG
        }
    }
}