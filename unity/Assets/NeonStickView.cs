using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual layer for one mobile stick: translucent dial, neon ring with a soft
/// halo, decorative arrow glyphs and a gradient knob that follows the finger.
/// The whole visual layer is decorative (raycastTarget = false); touches are
/// handled by MobileStick on the parent panel.
/// </summary>
public sealed class NeonStickView : MonoBehaviour
{
    private const string ShaderResourcePath = "NeonStickGlow";

    private static Shader stickShader;

    private Image dialImage;
    private Image glowImage;
    private Image knobImage;
    private Material dialMaterial;
    private Material glowMaterial;
    private Material knobMaterial;
    private RectTransform knob;
    private Vector2 knobTarget;
    private float knobTravel;
    private float pulseTarget;
    private float pulseCurrent;

    public static NeonStickView Build(Transform root, bool rotationStyle)
    {
        var view = root.gameObject.AddComponent<NeonStickView>();
        view.BuildInternal(rotationStyle);
        return view;
    }

    public void SetValue(Vector2 value)
    {
        knobTarget = Vector2.ClampMagnitude(value, 1f) * knobTravel;
    }

    public void SetHeld(bool held)
    {
        pulseTarget = held ? 1f : 0f;
    }

    public void ResetKnob()
    {
        knobTarget = Vector2.zero;
    }

    private void Update()
    {
        if (knob == null) return;
        var dt = Time.unscaledDeltaTime;
        var follow = 1f - Mathf.Exp(-22f * dt);
        knob.anchoredPosition = Vector2.Lerp(knob.anchoredPosition, knobTarget, follow);
        pulseCurrent = Mathf.MoveTowards(pulseCurrent, pulseTarget, dt * 7f);
        if (dialMaterial != null) dialMaterial.SetFloat("_Pulse", pulseCurrent);
        if (glowMaterial != null) glowMaterial.SetFloat("_Pulse", pulseCurrent * 0.9f);
        if (knobMaterial != null) knobMaterial.SetFloat("_Pulse", pulseCurrent);
    }

    private void BuildInternal(bool rotationStyle)
    {
        var rootRect = (RectTransform)transform;
        var size = rootRect.rect.width > 1f ? rootRect.rect.width : 190f;
        var accent = rotationStyle ? new Color(1f, 0.2f, 0.72f) : new Color(0.22f, 0.83f, 1f);

        glowMaterial = CreateMaterial(accent, true);
        dialMaterial = CreateMaterial(accent, false);
        knobMaterial = CreateMaterial(accent, false);
        knobMaterial.SetFloat("_Knob", 1f);

        glowImage = CreateLayer(rootRect, "stick-glow", size * 1.34f, glowMaterial);
        dialImage = CreateLayer(rootRect, "stick-dial", size, dialMaterial);
        knobImage = CreateLayer(rootRect, "stick-knob", size * 0.4f, knobMaterial);

        var arrowsRect = CreateRect(rootRect, "stick-arrows", size);
        var arrowsGo = arrowsRect.gameObject;
        if (arrowsGo.GetComponent<CanvasRenderer>() == null) arrowsGo.AddComponent<CanvasRenderer>();
        var arrows = arrowsGo.AddComponent<NeonStickArrows>();
        arrows.rotationStyle = rotationStyle;
        arrows.radius = size * 0.325f;
        arrows.head = size * 0.062f;
        arrows.thickness = Mathf.Max(2f, size * 0.017f);
        arrows.arcSpan = 60f;
        arrows.color = new Color(accent.r, accent.g, accent.b, 0.92f);
        arrows.raycastTarget = false;

        knob = knobImage.rectTransform;
        knobTravel = size * 0.5f - size * 0.4f * 0.5f;
        knob.anchoredPosition = Vector2.zero;
        knob.SetAsLastSibling();
    }

    private static Material CreateMaterial(Color accent, bool additive)
    {
        if (stickShader == null) stickShader = Resources.Load<Shader>(ShaderResourcePath);
        if (stickShader == null) stickShader = Shader.Find("NeonRoom/UI Stick Glow");
        if (stickShader == null) return null;

        var material = new Material(stickShader) { name = additive ? "stick-glow" : "stick-dial" };
        material.SetColor("_Color", new Color(accent.r, accent.g, accent.b, 1f));
        material.SetColor("_GlowColor", new Color(accent.r, accent.g, accent.b, 1f));
        material.SetFloat("_RingRadius", 0.78f);
        material.SetFloat("_RingWidth", additive ? 0.03f : 0.028f);
        material.SetFloat("_GlowPower", additive ? 7.5f : 11f);
        material.SetFloat("_RingIntensity", additive ? 0.55f : 1.35f);
        material.SetFloat("_GlowIntensity", additive ? 1.1f : 0.55f);
        material.SetFloat("_InnerGlow", additive ? 0.15f : 0.55f);
        material.SetFloat("_Fill", additive ? 0f : 0.16f);

        if (additive)
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        }
        else
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        return material;
    }

    private static Image CreateLayer(RectTransform parent, string name, float size, Material material)
    {
        var rect = CreateRect(parent, name, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white;
        image.material = material;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform CreateRect(RectTransform parent, string name, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }
}
