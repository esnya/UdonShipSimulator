// Upgrade NOTE: upgraded instancing buffer 'UdonShipSimulatorBuoy' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "UdonShipSimulator/Buoy"
{
	Properties
	{
		_Color("Color", Color) = (0.8,0.8,0.8,1)
		[HDR]_EmissionColor("EmissionColor", Color) = (771.0118,771.0118,771.0118,1)
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_Smoothness("Smoothness", Range( 0 , 1)) = 0.2
		_Dt("Dt", Range( 0 , 1)) = 0.1
		_WaveStrength("Wave Strength", Range( 0 , 1)) = 0.1
		_Duration("Duration", Float) = 2
		_WaveScrollX("Wave Scroll X", Range( 0 , 1)) = 0.2
		_WaveScrollY("Wave Scroll Y", Range( 0 , 1)) = 0.5
		_WaveScrollMultiplier("Wave Scroll Multiplier", Range( 0 , 1)) = 0.05
		_Wave2ScrollX("Wave2 Scroll X", Range( 0 , 1)) = 0.1
		_Wave2ScrollY("Wave2 Scroll Y", Range( 0 , 1)) = 0.3
		_Wave2ScrollMultiplier("Wave2 Scroll Multiplier", Range( 0 , 1)) = 0.05
		_Wave2Strength("Wave2 Strength", Range( 0 , 1)) = 0.1
		[Normal]_Wave("Wave", 2D) = "bump" {}
		[Normal]_Wave2("Wave2", 2D) = "bump" {}
		_Shape("Shape", Range( 0 , 1)) = 0
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float4 vertexColor : COLOR;
		};

		uniform sampler2D _Wave;
		uniform float _WaveScrollX;
		uniform float _WaveScrollY;
		uniform float _WaveScrollMultiplier;
		uniform float _WaveStrength;
		uniform sampler2D _Wave2;
		uniform float _Wave2ScrollX;
		uniform float _Wave2ScrollY;
		uniform float _Wave2ScrollMultiplier;
		uniform float _Wave2Strength;
		uniform float4 _Color;
		uniform float4 _EmissionColor;
		uniform float _Duration;
		uniform float _Dt;
		uniform float _Metallic;
		uniform float _Smoothness;

		UNITY_INSTANCING_BUFFER_START(UdonShipSimulatorBuoy)
			UNITY_DEFINE_INSTANCED_PROP(float, _Shape)
#define _Shape_arr UdonShipSimulatorBuoy
		UNITY_INSTANCING_BUFFER_END(UdonShipSimulatorBuoy)


		float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
		{
			original -= center;
			float C = cos( angle );
			float S = sin( angle );
			float t = 1 - C;
			float m00 = t * u.x * u.x + C;
			float m01 = t * u.x * u.y - S * u.z;
			float m02 = t * u.x * u.z + S * u.y;
			float m10 = t * u.x * u.y + S * u.z;
			float m11 = t * u.y * u.y + C;
			float m12 = t * u.y * u.z - S * u.x;
			float m20 = t * u.x * u.z - S * u.y;
			float m21 = t * u.y * u.z + S * u.x;
			float m22 = t * u.z * u.z + C;
			float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
			return mul( finalMatrix, original ) + center;
		}


		inline float noise_randomValue (float2 uv) { return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453); }

		inline float noise_interpolate (float a, float b, float t) { return (1.0-t)*a + (t*b); }

		inline float valueNoise (float2 uv)
		{
			float2 i = floor(uv);
			float2 f = frac( uv );
			f = f* f * (3.0 - 2.0 * f);
			uv = abs( frac(uv) - 0.5);
			float2 c0 = i + float2( 0.0, 0.0 );
			float2 c1 = i + float2( 1.0, 0.0 );
			float2 c2 = i + float2( 0.0, 1.0 );
			float2 c3 = i + float2( 1.0, 1.0 );
			float r0 = noise_randomValue( c0 );
			float r1 = noise_randomValue( c1 );
			float r2 = noise_randomValue( c2 );
			float r3 = noise_randomValue( c3 );
			float bottomOfGrid = noise_interpolate( r0, r1, f.x );
			float topOfGrid = noise_interpolate( r2, r3, f.x );
			float t = noise_interpolate( bottomOfGrid, topOfGrid, f.y );
			return t;
		}


		float SimpleNoise(float2 UV)
		{
			float t = 0.0;
			float freq = pow( 2.0, float( 0 ) );
			float amp = pow( 0.5, float( 3 - 0 ) );
			t += valueNoise( UV/freq )*amp;
			freq = pow(2.0, float(1));
			amp = pow(0.5, float(3-1));
			t += valueNoise( UV/freq )*amp;
			freq = pow(2.0, float(2));
			amp = pow(0.5, float(3-2));
			t += valueNoise( UV/freq )*amp;
			return t;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float4 transform7 = mul(unity_ObjectToWorld,float4( 0,0,0,1 ));
			float2 appendResult8 = (float2(transform7.x , transform7.z));
			float2 appendResult22 = (float2(_WaveScrollX , _WaveScrollY));
			float2 appendResult28 = (float2(_Wave2ScrollX , _Wave2ScrollY));
			float3 break13 = BlendNormals( UnpackScaleNormal( tex2Dlod( _Wave, float4( ( appendResult8 + ( appendResult22 * _WaveScrollMultiplier * _Time.y ) ), 0, 0.0) ), _WaveStrength ) , UnpackScaleNormal( tex2Dlod( _Wave2, float4( ( float2( 0,0 ) + ( appendResult28 * _Wave2ScrollMultiplier * _Time.y ) ), 0, 0.0) ), _Wave2Strength ) );
			float _Shape_Instance = UNITY_ACCESS_INSTANCED_PROP(_Shape_arr, _Shape);
			float temp_output_56_0 = max( ( 1.0 - v.color.g ) , _Shape_Instance );
			float3 appendResult58 = (float3(temp_output_56_0 , 1.0 , temp_output_56_0));
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 rotatedValue36 = RotateAroundAxis( float3( 0,0,0 ), ( appendResult58 * ase_vertex3Pos ), float3( 0,0,1 ), atan2( break13.x , break13.z ) );
			float3 rotatedValue37 = RotateAroundAxis( float3( 0,0,0 ), rotatedValue36, float3( 1,0,0 ), atan2( break13.y , break13.z ) );
			v.vertex.xyz = rotatedValue37;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Albedo = ( _Color * max( ( 1.0 - i.vertexColor.r ) , 0.1 ) ).rgb;
			float mulTime51 = _Time.y * ( 1.0 / _Duration );
			float4 transform7 = mul(unity_ObjectToWorld,float4( 0,0,0,1 ));
			float2 appendResult8 = (float2(transform7.x , transform7.z));
			float simpleNoise60 = SimpleNoise( appendResult8 );
			o.Emission = ( _EmissionColor * i.vertexColor.r * step( frac( ( mulTime51 + simpleNoise60 ) ) , _Dt ) ).rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18900
0;1203;1750;877;3012.856;1090.01;3.214524;True;True
Node;AmplifyShaderEditor.RangedFloatNode;20;-1643.458,731.1014;Inherit;False;Property;_WaveScrollY;Wave Scroll Y;8;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;-1644.885,661.0008;Inherit;False;Property;_WaveScrollX;Wave Scroll X;7;0;Create;True;0;0;0;False;0;False;0.2;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;30;-1664,1152;Inherit;False;Property;_Wave2ScrollY;Wave2 Scroll Y;11;0;Create;True;0;0;0;False;0;False;0.3;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-1664,1072;Inherit;False;Property;_Wave2ScrollX;Wave2 Scroll X;10;0;Create;True;0;0;0;False;0;False;0.1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;21;-2266.918,708.8337;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;24;-1632,880;Inherit;False;Property;_WaveScrollMultiplier;Wave Scroll Multiplier;9;0;Create;True;0;0;0;False;0;False;0.05;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ObjectToWorldTransfNode;7;-2740.136,302.4019;Inherit;False;1;0;FLOAT4;0,0,0,1;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;31;-1648,1296;Inherit;False;Property;_Wave2ScrollMultiplier;Wave2 Scroll Multiplier;12;0;Create;True;0;0;0;False;0;False;0.05;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;22;-1339.458,667.1014;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;28;-1360,1088;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-1024,1120;Inherit;False;3;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;23;-1010.826,698.7802;Inherit;False;3;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;8;-2128.77,357.7147;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-1643.458,571.1014;Inherit;False;Property;_WaveStrength;Wave Strength;5;0;Create;True;0;0;0;False;0;False;0.1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;49;701.6012,-231.142;Inherit;False;Property;_Duration;Duration;6;0;Create;True;0;0;0;False;0;False;2;0.9;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;10;-1643.458,971.1014;Inherit;False;Property;_Wave2Strength;Wave2 Strength;13;0;Create;True;0;0;0;False;0;False;0.1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;26;-656,800;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;25;-640,384;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.VertexColorNode;40;226.6425,-1065.41;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;9;-384,640;Inherit;True;Property;_Wave2;Wave2;15;1;[Normal];Create;True;0;0;0;False;0;False;-1;b27033af63cbd7c4f9586ae88ec0e764;b27033af63cbd7c4f9586ae88ec0e764;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;4;-384,384;Inherit;True;Property;_Wave;Wave;14;1;[Normal];Create;True;0;0;0;False;0;False;-1;d73936c8c956d1f42a6d4ee47680d3f3;d73936c8c956d1f42a6d4ee47680d3f3;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;55;326.3122,-5.495061;Inherit;False;InstancedProperty;_Shape;Shape;16;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;57;454.3121,-85.49509;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;50;905.1194,-321.2185;Inherit;False;2;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;56;726.3119,-85.49509;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;11;16,544;Inherit;True;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;60;-1817.73,73.57912;Inherit;False;Simple;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;51;1063.987,-186.7701;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;15;560,120;Inherit;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;61;-1320.366,-12.84956;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;58;931.5374,-97.13171;Inherit;False;FLOAT3;4;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.BreakToComponentsNode;13;362.3503,516.6124;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.FractNode;52;1323.601,-103.142;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;59;1093.72,22.37169;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;43;624,-1024;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;45;80,-896;Inherit;False;Constant;_Float0;Float 0;13;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ATan2OpNode;34;695.1525,257.5883;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;53;512,-640;Inherit;False;Property;_Dt;Dt;4;0;Create;True;0;0;0;False;0;False;0.1;0.9;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RotateAboutAxisNode;36;1268.022,60.65619;Inherit;False;False;4;0;FLOAT3;0,0,1;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMaxOpNode;44;743.4003,-972.4428;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;48;-80,-208;Inherit;False;Property;_EmissionColor;EmissionColor;1;1;[HDR];Create;True;0;0;0;False;0;False;771.0118,771.0118,771.0118,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ATan2OpNode;35;768,512;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;54;912,-624;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;1;-96.7371,-729.9128;Inherit;False;Property;_Color;Color;0;0;Create;True;0;0;0;False;0;False;0.8,0.8,0.8,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;2;-85.74186,-523.5203;Inherit;False;Property;_Metallic;Metallic;2;0;Create;True;0;0;0;False;0;False;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;3;-85.74186,-363.5203;Inherit;False;Property;_Smoothness;Smoothness;3;0;Create;True;0;0;0;False;0;False;0.2;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;42;896,-896;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;47;1276.695,-630.6567;Inherit;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RotateAboutAxisNode;37;1723.852,46.18317;Inherit;False;False;4;0;FLOAT3;1,0,0;False;1;FLOAT;0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1734.193,-570.4037;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;UdonShipSimulator/Buoy;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Absolute;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;22;0;19;0
WireConnection;22;1;20;0
WireConnection;28;0;29;0
WireConnection;28;1;30;0
WireConnection;27;0;28;0
WireConnection;27;1;31;0
WireConnection;27;2;21;0
WireConnection;23;0;22;0
WireConnection;23;1;24;0
WireConnection;23;2;21;0
WireConnection;8;0;7;1
WireConnection;8;1;7;3
WireConnection;26;1;27;0
WireConnection;25;0;8;0
WireConnection;25;1;23;0
WireConnection;9;1;26;0
WireConnection;9;5;10;0
WireConnection;4;1;25;0
WireConnection;4;5;5;0
WireConnection;57;0;40;2
WireConnection;50;1;49;0
WireConnection;56;0;57;0
WireConnection;56;1;55;0
WireConnection;11;0;4;0
WireConnection;11;1;9;0
WireConnection;60;0;8;0
WireConnection;51;0;50;0
WireConnection;61;0;51;0
WireConnection;61;1;60;0
WireConnection;58;0;56;0
WireConnection;58;2;56;0
WireConnection;13;0;11;0
WireConnection;52;0;61;0
WireConnection;59;0;58;0
WireConnection;59;1;15;0
WireConnection;43;0;40;1
WireConnection;34;0;13;0
WireConnection;34;1;13;2
WireConnection;36;1;34;0
WireConnection;36;3;59;0
WireConnection;44;0;43;0
WireConnection;44;1;45;0
WireConnection;35;0;13;1
WireConnection;35;1;13;2
WireConnection;54;0;52;0
WireConnection;54;1;53;0
WireConnection;42;0;1;0
WireConnection;42;1;44;0
WireConnection;47;0;48;0
WireConnection;47;1;40;1
WireConnection;47;2;54;0
WireConnection;37;1;35;0
WireConnection;37;3;36;0
WireConnection;0;0;42;0
WireConnection;0;2;47;0
WireConnection;0;3;2;0
WireConnection;0;4;3;0
WireConnection;0;11;37;0
ASEEND*/
//CHKSM=32853E913F5D5CAAAC61938D34E1D787B43FDB19