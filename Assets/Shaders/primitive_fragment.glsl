#version 450 core

in vec3 vNormal;
in vec3 vFragPos;
in vec2 vTexCoord;

uniform vec4 uBaseColor;
uniform bool uUseTexture;
uniform sampler2D uMainTex;
uniform vec3 uLightDir;

out vec4 FragColor;

void main()
{
    vec3 norm = normalize(vNormal);
    vec3 lightDir = normalize(uLightDir);
    float ndotl = max(dot(norm, lightDir), 0.0);

    // Three-step toon shading quantization
    float toonBand = ndotl > 0.75 ? 1.0 : (ndotl > 0.35 ? 0.65 : 0.35);

    vec4 baseSample = uUseTexture ? texture(uMainTex, vTexCoord) * uBaseColor : uBaseColor;
    FragColor = vec4(baseSample.rgb * toonBand, baseSample.a);
}
