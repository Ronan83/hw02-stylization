// Procedural, artist-style colouring for textured toon surfaces.
// Instead of just darkening the texture in shadow, every toon band shifts hue, saturation and value
// the way painters do: lit areas drift warm (yellow) and slightly desaturate, shadows drift cool
// (violet) and get MORE saturated, so the colours stay rich instead of going muddy grey.

float3 PC_RgbToHsv(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 PC_HsvToRgb(float3 c)
{
    float3 p = abs(frac(c.xxx + float3(1.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
    return c.z * lerp(float3(1, 1, 1), saturate(p - 1.0), c.y);
}

// move hue h toward target by amount t along the shortest way round the colour wheel
float PC_HueToward(float h, float target, float t)
{
    float d = target - h;
    d -= round(d);
    return frac(h + d * t);
}

void ProceduralTint_float(float3 Albedo, float3 Tone, float Diffuse, float2 Thresholds, out float3 Out)
{
    // which toon band are we in? 0 = shadow, 0.5 = midtone, 1 = highlight
    float band = Diffuse < Thresholds.x ? 0.0 : (Diffuse < Thresholds.y ? 0.5 : 1.0);

    float3 hsv = PC_RgbToHsv(Albedo);
    float lit = band;
    float shade = 1.0 - band;

    // hue: warm (yellow, 0.12) in light, cool (violet, 0.74) in shadow
    hsv.x = PC_HueToward(hsv.x, 0.12, 0.08 * lit);
    hsv.x = PC_HueToward(hsv.x, 0.74, 0.12 * shade);
    // saturation: richer in shadow, a little paler in highlight
    hsv.y = saturate(hsv.y * lerp(1.25, 0.92, band));
    // value: three steps instead of one flat multiply
    hsv.z = saturate(hsv.z * lerp(0.64, 1.05, band));

    float3 col = PC_HsvToRgb(hsv);

    // keep the material's palette as a gentle tint (lets night mode recolour the hero)
    float m = max(max(Tone.r, Tone.g), max(Tone.b, 1e-3));
    Out = col * lerp(float3(1, 1, 1), Tone / m, 0.45);
}
