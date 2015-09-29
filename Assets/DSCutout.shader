﻿Shader "Transparent/Cutout/DoubleSided" {
	Properties {
		_Color ("Main Color", Color) = (.5, .5, .5, .5)
		_MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
		_Cutoff ("Base Alpha cutoff", Range (0,.9)) = .5
	}
	SubShader {
		UsePass "Transparent/Cutout/DiffuseBack/FORWARD"
		UsePass "Transparent/Cutout/Diffuse/FORWARD"
	}

	Fallback "Transparent/Cutout/VertexLit"
}