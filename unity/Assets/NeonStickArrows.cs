using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Decorative arrow glyphs for the mobile sticks, generated as UI geometry.
/// No textures and no font assets are involved: every arrow is a triangle or an
/// arc strip produced in OnPopulateMesh, so it stays crisp at any resolution.
/// </summary>
public sealed class NeonStickArrows : MaskableGraphic
{
    public bool rotationStyle;
    public float radius = 62f;
    public float head = 11f;
    public float thickness = 3.2f;
    public float arcSpan = 64f;
    public int segments = 10;

    public override Texture mainTexture
    {
        get { return s_WhiteTexture; }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var rect = GetPixelAdjustedRect();
        var center = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f);
        if (rotationStyle) BuildRotationArrows(vh, center);
        else BuildDirectionArrows(vh, center);
    }

    private void BuildDirectionArrows(VertexHelper vh, Vector2 center)
    {
        var directions = new[]
        {
            new Vector2(0f, 1f),
            new Vector2(1f, 0f),
            new Vector2(0f, -1f),
            new Vector2(-1f, 0f)
        };

        for (var i = 0; i < directions.Length; i++)
        {
            var direction = directions[i];
            var perpendicular = new Vector2(-direction.y, direction.x);
            var tip = center + direction * (radius + head);
            var basePoint = center + direction * (radius - head * 0.35f);
            var left = basePoint + perpendicular * head * 0.78f;
            var right = basePoint - perpendicular * head * 0.78f;
            AddTriangle(vh, tip, left, right);
        }
    }

    private void BuildRotationArrows(VertexHelper vh, Vector2 center)
    {
        var step = segments;
        for (var arrow = 0; arrow < 4; arrow++)
        {
            var startAngle = arrow * 90f + 8f;
            var endAngle = startAngle + arcSpan;
            var startIndex = vh.currentVertCount;

            for (var k = 0; k <= step; k++)
            {
                var angle = Mathf.Lerp(startAngle, endAngle, k / (float)step) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var outer = center + direction * (radius + thickness);
                var inner = center + direction * (radius - thickness);
                vh.AddVert(new Vector3(outer.x, outer.y, 0f), color, Vector2.zero);
                vh.AddVert(new Vector3(inner.x, inner.y, 0f), color, Vector2.zero);
            }

            for (var k = 0; k < step; k++)
            {
                var index = startIndex + k * 2;
                vh.AddTriangle(index, index + 2, index + 1);
                vh.AddTriangle(index + 1, index + 2, index + 3);
            }

            // Arrow head: tangential tip at the end of the arc, counter-clockwise travel.
            var headAngle = endAngle * Mathf.Deg2Rad;
            var radial = new Vector2(Mathf.Cos(headAngle), Mathf.Sin(headAngle));
            var tangent = new Vector2(-radial.y, radial.x);
            var tip = center + radial * radius + tangent * head * 1.15f;
            var shoulderOuter = center + radial * (radius + thickness * 1.9f);
            var shoulderInner = center + radial * (radius - thickness * 1.9f);
            AddTriangle(vh, tip, shoulderOuter, shoulderInner);
        }
    }

    private void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c)
    {
        var index = vh.currentVertCount;
        vh.AddVert(new Vector3(a.x, a.y, 0f), color, Vector2.zero);
        vh.AddVert(new Vector3(b.x, b.y, 0f), color, Vector2.zero);
        vh.AddVert(new Vector3(c.x, c.y, 0f), color, Vector2.zero);
        vh.AddTriangle(index, index + 1, index + 2);
    }
}
