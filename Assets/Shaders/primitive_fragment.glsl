#version 450 core

in vec3 vNormal;
in vec3 vFragPos;
in vec2 vTexCoord;

uniform vec4 uBaseColor;
uniform bool uUseTexture;
uniform sampler2D uMainTex;
uniform vec3 uLightDir;
uniform float uChromaTolerance;

out vec4 FragColor;

void main()
{
    if (uUseTexture)
    {
        vec4 texSample = texture(uMainTex, vTexCoord);
        if (uChromaTolerance > 0.0)
        {
            vec3 bgColor = texture(uMainTex, vec2(0.02, 0.98)).rgb;
            if (distance(texSample.rgb, bgColor) < uChromaTolerance)
            {
                discard;
            }
        }

        // Keep character & prop artwork vibrant rather than darkened by wall toon shadows
        FragColor = vec4(texSample.rgb * uBaseColor.rgb, texSample.a * uBaseColor.a);
        return;
    }

    vec3 norm = normalize(vNormal);
    vec3 lightDir = normalize(uLightDir);
    float ndotl = max(dot(norm, lightDir), 0.0);

    // Three-step toon shading quantization for 3D primitive architecture
    float toonBand = ndotl > 0.72 ? 1.0 : (ndotl > 0.30 ? 0.72 : 0.45);
    FragColor = vec4(uBaseColor.rgb * toonBand, uBaseColor.a);
}
