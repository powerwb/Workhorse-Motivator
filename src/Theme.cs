using System;
using System.Windows.Media;

namespace NiuMa
{
    public sealed class Theme
    {
        public readonly string Name;
        readonly string[] _colors;

        Theme(string name, params string[] colors)
        {
            Name = name;
            _colors = colors;
        }

        public static readonly Theme[] All =
        {
            new Theme("暖米白", "#FCFAF5", "#E8E2D7", "#2A57C4", "#1B2432", "#D2541E", "#FDF5E7", "#F6E2C4", "#89A6F2", "#ECE7DC"),
            new Theme("晴空蓝", "#EFF7FF", "#CADDED", "#2269B2", "#234A70", "#2269B2", "#E5F3FF", "#CBE3F8", "#8CC4ED", "#D8E8F5"),
            new Theme("薄荷绿", "#F0F8F2", "#CEDFD2", "#28734F", "#254C3B", "#28734F", "#E5F4E9", "#CFE7D6", "#91C7A2", "#DBEADD"),
            new Theme("浅薰衣草", "#F7F2FC", "#DFD3EB", "#8055B0", "#49335F", "#8055B0", "#F0E7FA", "#DFD0EF", "#B99AD9", "#E7DDEE")
        };

        public static int Normalize(int index)
        {
            return index >= 0 && index < All.Length ? index : 0;
        }

        static Color ColorOf(string value)
        {
            return (Color)ColorConverter.ConvertFromString(value);
        }

        public void Apply()
        {
            Ui.Paper.Color = ColorOf(_colors[0]);
            Ui.Line.Color = ColorOf(_colors[1]);
            Ui.Accent.Color = Ui.Blue.Color = ColorOf(_colors[2]);
            Ui.Navy.Color = ColorOf(_colors[3]);
            Ui.Carrot.Color = ColorOf(_colors[4]);
            Ui.Track.Color = ColorOf(_colors[8]);
        }

        public LinearGradientBrush Progress()
        {
            return new LinearGradientBrush(ColorOf(_colors[2]), ColorOf(_colors[7]), 0);
        }

        public LinearGradientBrush Card(double opacity)
        {
            byte alpha = (byte)Math.Round((0.45 + 0.55 * Math.Max(0, Math.Min(1, opacity))) * 255);
            var start = ColorOf(_colors[5]);
            var end = ColorOf(_colors[6]);
            start.A = end.A = alpha;
            return new LinearGradientBrush(start, end, 90);
        }
    }
}
