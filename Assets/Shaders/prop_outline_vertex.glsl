#version 450 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform float uOutlineThickness;

void main()
{
    vec3 extrudeDir = length(aPosition) > 0.0001 ? normalize(aPosition) : normalize(aNormal);
    vec4 worldPos = uModel * vec4(aPosition, 1.0);
    vec3 worldDir = normalize(mat3(uModel) * extrudeDir);
    worldPos.xyz += worldDir * uOutlineThickness;

    gl_Position = uProjection * uView * worldPos;
}
