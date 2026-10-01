Shader "RoomStudio/CTMarker" {
 Properties { _Color ("Color", Color) = (1,1,1,1) _VertexTint ("Vertex tint", Float) = 0 }
 SubShader {
  Tags { "Queue"="Overlay" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha
  ZWrite Off
  ZTest Always
  Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct Input { float4 vertex : POSITION; float4 color : COLOR; };
   struct Output { float4 vertex : SV_POSITION; float4 color : COLOR; };
   fixed4 _Color;
    float _VertexTint;
    Output vert(Input input) { Output output; output.vertex=UnityObjectToClipPos(input.vertex); output.color=lerp(_Color,input.color*_Color,_VertexTint); return output; }
   fixed4 frag(Output input) : SV_Target { return input.color; }
   ENDCG
  }
 }
}