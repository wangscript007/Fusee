#version 300 es

#ifdef GL_ES
precision highp float;
#endif

in vec2 vUV;
in vec3 vMVNormal;

uniform sampler2D AlbedoTexture;
uniform vec4 AlbedoColor;
uniform float AlbedoMix;

out vec4 outColor;

//FontMaps are stored in PixelFormat Red8
void main()
{
	vec3 N = normalize(vMVNormal);
	vec3 L = vec3(0.0,0.0,-1.0);
	vec4 color = vec4(texture(AlbedoTexture,vUV).r * AlbedoMix);

	if(AlbedoMix == 0.0)
		color = vec4(1.0, 1.0, 1.0, texture(AlbedoTexture, vUV).r);	
	
	float red = texture(AlbedoTexture, vUV).r;
	outColor = color * AlbedoColor *  max(dot(N, L), 0.0);
}