#version 450 core

uniform vec4 uOutlineColor;

out vec4 FragColor;

void main()
{
    FragColor = uOutlineColor;
}
