using Microsoft.JSInterop;

namespace BlazorApp.Game.UIObjectFactory
{
    public class UIObjectPolygon : UIObject
    {
        public List<(float x, float y)> Points { get; set; } = new();
        public string FillColor { get; set; } = "#FFFFFF";
        public float FillOpacity { get; set; } = 0.3f;

        public override void CollectDrawCommands(
            List<object> commands, IJSRuntime js,
            float fadeOpacity = 0f, float parentX = 0, float parentY = 0)
        {
            if (!Visible || Points.Count < 3) return;

            float fadeAlpha = 1.0f - fadeOpacity;
            commands.Add(new
            {
                type = "polygon",
                canvasId = "gameCanvas",
                points = Points.Select(p => new { x = p.x + parentX, y = p.y + parentY }).ToList(),
                color = FillColor,
                opacity = FillOpacity * fadeAlpha,
                zIndex = this.ZIndex   // ★これを追加
            });
        }
    }
}
