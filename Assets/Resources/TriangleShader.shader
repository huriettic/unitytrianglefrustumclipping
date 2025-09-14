Shader "Custom/TriangleShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct Triangle
            {
                float3 v0, v1, v2;
                float2 uv0, uv1, uv2;
            };

            StructuredBuffer<Triangle> triangleBuffer;

            struct v2f 
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(uint id : SV_VertexID)
            {
                uint triangleIndex = id / 3;
                uint triangleVertex = id % 3;

                Triangle tri = triangleBuffer[triangleIndex];

                float3 vertexTriangle;
                float2 uvTriangle;

                if (triangleVertex == 0)
                {
                    vertexTriangle = tri.v0;
                    uvTriangle = tri.uv0;
                }
                else if (triangleVertex == 1)
                {
                    vertexTriangle = tri.v1;
                    uvTriangle = tri.uv1;
                }
                else
                {
                    vertexTriangle = tri.v2;
                    uvTriangle = tri.uv2; 
                }

                v2f o;
                o.pos = UnityObjectToClipPos(float4(vertexTriangle, 1.0));
                o.uv = uvTriangle;
                return o; 
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}