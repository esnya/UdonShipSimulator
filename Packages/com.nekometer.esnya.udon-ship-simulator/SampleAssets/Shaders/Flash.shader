// Upgrade NOTE: upgraded instancing buffer 'UdonShipSimulatorFlash' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "UdonShipSimulator/Flash"
{
	Properties
	{
		_Color("Color", Color) = (0.2,0.2,0.2,1)
		[HDR]_EmissionColor("EmissionColor", Color) = (1,1,1,1)
		_Dt("Dt", Range( 0 , 1)) = 0.9
		_Duration("Duration", Float) = 2
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			half filler;
		};

		UNITY_INSTANCING_BUFFER_START(UdonShipSimulatorFlash)
			UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
#define _Color_arr UdonShipSimulatorFlash
			UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
#define _EmissionColor_arr UdonShipSimulatorFlash
			UNITY_DEFINE_INSTANCED_PROP(float, _Duration)
#define _Duration_arr UdonShipSimulatorFlash
			UNITY_DEFINE_INSTANCED_PROP(float, _Dt)
#define _Dt_arr UdonShipSimulatorFlash
		UNITY_INSTANCING_BUFFER_END(UdonShipSimulatorFlash)

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float4 _Color_Instance = UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color);
			o.Albedo = _Color_Instance.rgb;
			float4 _EmissionColor_Instance = UNITY_ACCESS_INSTANCED_PROP(_EmissionColor_arr, _EmissionColor);
			float _Duration_Instance = UNITY_ACCESS_INSTANCED_PROP(_Duration_arr, _Duration);
			float mulTime4 = _Time.y * ( 1.0 / _Duration_Instance );
			float _Dt_Instance = UNITY_ACCESS_INSTANCED_PROP(_Dt_arr, _Dt);
			o.Emission = ( _EmissionColor_Instance * step( frac( mulTime4 ) , _Dt_Instance ) ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18900
820;1173;1845;731;2086.092;337.3118;1.720069;True;True
Node;AmplifyShaderEditor.RangedFloatNode;9;-900.5,516;Inherit;False;InstancedProperty;_Duration;Duration;3;0;Create;True;0;0;0;False;0;False;2;0.9;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;10;-835.5,365;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;4;-499,387;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FractNode;8;-280.5,639;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-763.5,641;Inherit;False;InstancedProperty;_Dt;Dt;2;0;Create;True;0;0;0;False;0;False;0.9;0.9;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;5;-224.5,260;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;2;-624,192;Inherit;False;InstancedProperty;_EmissionColor;EmissionColor;1;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;-193.5,154;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;1;-624,0;Inherit;False;InstancedProperty;_Color;Color;0;0;Create;True;0;0;0;False;0;False;0.2,0.2,0.2,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;UdonShipSimulator/Flash;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;10;1;9;0
WireConnection;4;0;10;0
WireConnection;8;0;4;0
WireConnection;5;0;8;0
WireConnection;5;1;6;0
WireConnection;7;0;2;0
WireConnection;7;1;5;0
WireConnection;0;0;1;0
WireConnection;0;2;7;0
ASEEND*/
//CHKSM=219726051593B4317295ECE3061BB90650347D42