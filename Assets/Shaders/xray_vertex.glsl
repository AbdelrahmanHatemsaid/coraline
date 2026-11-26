#version 450 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoord;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 vTexCoord;
out float vWorldY;

void main()
{
    vec4 worldPos = uModel * vec4(aPosition, 1.0);
    vWorldY = worldPos.y;
    vTexCoord = aTexCoord;
    gl_Position = uProjection * uView * worldPos;
}
