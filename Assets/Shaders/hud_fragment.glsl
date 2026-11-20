#version 450 core

in vec2 vTexCoord;

uniform sampler2D uHudTexture;
uniform vec4 uColor;
uniform float uUseTexture;
uniform float uChromaTolerance;
uniform float uDesaturate;

out vec4 FragColor;

void main()
{
    if (uUseTexture > 0.5)
    {
        vec4 texSample = texture(uHudTexture, vTexCoord);
        if (uChromaTolerance > 0.0)
        {
            vec3 bgColor = texture(uHudTexture, vec2(0.02, 0.98)).rgb;
            if (distance(texSample.rgb, bgColor) < uChromaTolerance)
            {
                discard;
            }
        }

        float luma = dot(texSample.rgb, vec3(0.299, 0.587, 0.114));
        vec3 rgb = mix(texSample.rgb, vec3(luma), clamp(uDesaturate, 0.0, 1.0));
        FragColor = vec4(rgb * uColor.rgb, texSample.a * uColor.a);
        return;
    }

    FragColor = uColor;
}
