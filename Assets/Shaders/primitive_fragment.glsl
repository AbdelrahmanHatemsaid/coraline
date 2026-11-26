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

    // Four-step cel-shaded toon ramp with cool shadow shift
    vec3 litRgb;
    if (ndotl > 0.82)
    {
        litRgb = uBaseColor.rgb * 1.08;
    }
    else if (ndotl > 0.45)
    {
        litRgb = uBaseColor.rgb * 0.86;
    }
    else if (ndotl > 0.18)
    {
        litRgb = uBaseColor.rgb * 0.64;
    }
    else
    {
        litRgb = uBaseColor.rgb * vec3(0.42, 0.40, 0.50);
    }

    FragColor = vec4(litRgb, uBaseColor.a);
}
