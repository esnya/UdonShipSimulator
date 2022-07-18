// Upgrade NOTE: upgraded instancing buffer 'USSAnchorChain' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "USS/AnchorChain"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_MainTex("Main Tex", 2D) = "white" {}
		[NoScaleOffset]_BumpMap("Normal Map", 2D) = "bump" {}
		[NoScaleOffset]_OcclusionMap("Occlusion Map", 2D) = "white" {}
		_Smoothness("Smoothness", Range( 0 , 1)) = 0.5
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_ShackleMarkWidth("Shackle Mark Width", Range( 0 , 1)) = 0.1
		_ShackleLength("Shackle Length", Float) = 25
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows exclude_path:deferred 
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
		};

		uniform sampler2D _BumpMap;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform float _ShackleMarkWidth;
		uniform float _Metallic;
		uniform float _Smoothness;
		uniform sampler2D _OcclusionMap;
		uniform float _Cutoff = 0.5;

		UNITY_INSTANCING_BUFFER_START(USSAnchorChain)
			UNITY_DEFINE_INSTANCED_PROP(float, _ShackleLength)
#define _ShackleLength_arr USSAnchorChain
		UNITY_INSTANCING_BUFFER_END(USSAnchorChain)

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			o.Normal = UnpackNormal( tex2D( _BumpMap, uv_MainTex ) );
			float4 tex2DNode2 = tex2D( _MainTex, uv_MainTex );
			float4 color22 = IsGammaSpace() ? float4(1,1,1,1) : float4(1,1,1,1);
			float _ShackleLength_Instance = UNITY_ACCESS_INSTANCED_PROP(_ShackleLength_arr, _ShackleLength);
			float3 lerpResult21 = lerp( ( (tex2DNode2).rgb * (i.vertexColor).rgb ) , (color22).rgb , step( frac( ( i.uv_texcoord.x / _ShackleLength_Instance ) ) , ( _ShackleMarkWidth / _ShackleLength_Instance ) ));
			o.Albedo = lerpResult21;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Occlusion = tex2D( _OcclusionMap, uv_MainTex ).r;
			o.Alpha = 1;
			clip( ( tex2DNode2.a * i.vertexColor.a ) - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
0;1179;1951;900;2174.141;1080.37;1.465464;True;False
Node;AmplifyShaderEditor.TexCoordVertexDataNode;11;-644,-759;Inherit;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;26;-1563.062,-17.88193;Inherit;False;0;2;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;18;-640,-640;Inherit;False;InstancedProperty;_ShackleLength;Shackle Length;7;0;Create;True;0;0;0;False;0;False;25;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-640,-544;Inherit;False;Property;_ShackleMarkWidth;Shackle Mark Width;6;0;Create;True;0;0;0;False;0;False;0.1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;19;-159.4332,-676.1344;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;3;-512,-64;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;2;-512,-256;Inherit;True;Property;_MainTex;Main Tex;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;25;-294.1162,14.28412;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ColorNode;22;-528,-448;Inherit;False;Constant;_ShackleMarkColor;Shackle Mark Color;8;0;Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;20;-176.4332,-530.1344;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;12;33,-668;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;10;-136,-242;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;24;-311.1162,-388.7159;Inherit;False;True;True;True;False;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StepOpNode;14;262.5668,-426.1344;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;4;144,-176;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;389.8838,-320.7159;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;9;-512,128;Inherit;True;Property;_BumpMap;Normal Map;2;1;[NoScaleOffset];Create;False;0;0;0;False;0;False;2;None;None;True;0;False;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;8;-528,656;Inherit;True;Property;_OcclusionMap;Occlusion Map;3;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;2;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;6;-528,560;Inherit;False;Property;_Smoothness;Smoothness;4;0;Create;True;0;0;0;False;0;False;0.5;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;21;-110.1162,-117.7159;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-528,464;Inherit;False;Property;_Metallic;Metallic;5;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;640,-288;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;USS/AnchorChain;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;ForwardOnly;18;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;19;0;11;1
WireConnection;19;1;18;0
WireConnection;2;1;26;0
WireConnection;25;0;3;0
WireConnection;20;0;17;0
WireConnection;20;1;18;0
WireConnection;12;0;19;0
WireConnection;10;0;2;0
WireConnection;24;0;22;0
WireConnection;14;0;12;0
WireConnection;14;1;20;0
WireConnection;4;0;10;0
WireConnection;4;1;25;0
WireConnection;23;0;2;4
WireConnection;23;1;3;4
WireConnection;9;1;26;0
WireConnection;8;1;26;0
WireConnection;21;0;4;0
WireConnection;21;1;24;0
WireConnection;21;2;14;0
WireConnection;0;0;21;0
WireConnection;0;1;9;0
WireConnection;0;3;7;0
WireConnection;0;4;6;0
WireConnection;0;5;8;1
WireConnection;0;10;23;0
ASEEND*/
//CHKSM=B2D9809179C182E51BD930F4432CD18071B38283