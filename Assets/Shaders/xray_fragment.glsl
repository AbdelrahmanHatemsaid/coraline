#version 450 core

in vec2 vTexCoord;
in float vWorldY;

uniform sampler2D uMainTex;
uniform vec4 uXRayColor;
uniform float uTime;
uniform float uChromaTolerance;

out vec4 FragColor;

void main()
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

    // Spectral scanline shimmer + silhouette luminance boost for occluded X-Ray pass
    float scanline = 0.78 + 0.22 * sin(vWorldY * 16.0 - uTime * 5.5);
    float luma = dot(texSample.rgb, vec3(0.299, 0.587, 0.114));
    vec3 glow = mix(uXRayColor.rgb, uXRayColor.rgb * 1.35, smoothstep(0.15, 0.85, luma));

    FragColor = vec4(glow * scanline, uXRayColor.a * scanline);
}
