#version 450 core

in vec2 vTexCoord;

uniform sampler2D uScreenTexture;
uniform float uDetectionPercent; // 0.0 -> 100.0
uniform float uOtherWorldBlend;  // 0.0 (RealWorld) -> 1.0 (OtherWorld)
uniform float uVignetteRadius;   // Tightens as detection rises

out vec4 FragColor;

void main()
{
    vec4 sceneColor = texture(uScreenTexture, vTexCoord);

    // Subtle spectral indigo-cyan atmospheric tint when in the Other World
    vec3 otherTinted = sceneColor.rgb * vec3(0.84, 0.92, 1.12);
    vec3 baseRgb = mix(sceneColor.rgb, otherTinted, clamp(uOtherWorldBlend, 0.0, 1.0) * 0.45);

    // Color-Drain tension as Detection rises (0% -> 100%)
    float drainFactor = clamp(uDetectionPercent / 100.0, 0.0, 1.0);
    float luma = dot(baseRgb, vec3(0.299, 0.587, 0.114));
    vec3 coldGray = vec3(luma * 0.90, luma * 0.95, luma * 1.06);
    vec3 drained = mix(baseRgb, coldGray, drainFactor);

    // Tightening vignette proportional to DetectionPercent
    vec2 centered = vTexCoord * 2.0 - 1.0;
    float dist = length(centered);
    float vignette = smoothstep(uVignetteRadius, uVignetteRadius - 0.50, dist);

    // Subtle crimson dread edge when detection approaches 100%
    float dangerEdge = smoothstep(0.65, 1.0, drainFactor) * (1.0 - vignette);
    vec3 finalRgb = mix(drained * vignette, vec3(0.28, 0.03, 0.05), dangerEdge * 0.45);

    FragColor = vec4(finalRgb, 1.0);
}
