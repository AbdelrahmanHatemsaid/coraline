#version 450 core

in vec2 vTexCoord;

uniform sampler2D uScreenTexture;
uniform float uDetectionPercent; // 0.0 -> 100.0
uniform bool uIsOtherWorld;

out vec4 FragColor;

void main()
{
    vec4 sceneColor = texture(uScreenTexture, vTexCoord);

    // Optional Other World spectral tint
    vec3 tinted = uIsOtherWorld
        ? mix(sceneColor.rgb, sceneColor.rgb * vec3(0.78, 0.92, 1.08), 0.35)
        : sceneColor.rgb;

    // Color-Drain tension as Detection rises (0% -> 100%)
    float drainFactor = clamp(uDetectionPercent / 100.0, 0.0, 1.0);
    float luma = dot(tinted, vec3(0.299, 0.587, 0.114));
    vec3 coldGray = vec3(luma * 0.9, luma * 0.95, luma * 1.05);
    vec3 drained = mix(tinted, coldGray, drainFactor);

    // Tightening vignette proportional to DetectionPercent
    vec2 centered = vTexCoord * 2.0 - 1.0;
    float dist = length(centered);
    float vignetteRadius = mix(1.35, 0.65, drainFactor);
    float vignette = smoothstep(vignetteRadius, vignetteRadius - 0.45, dist);

    FragColor = vec4(drained * vignette, 1.0);
}
