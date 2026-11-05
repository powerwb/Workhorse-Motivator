using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NiuMa
{
    public static class Ui
    {
        public static readonly FontFamily Cn = new FontFamily("Microsoft YaHei UI, Microsoft YaHei, Segoe UI");
        public static readonly FontFamily Num = new FontFamily("Bahnschrift, Consolas, Segoe UI");
        public static readonly FontFamily Icon = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets");

        public static SolidColorBrush B(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }

        public static readonly SolidColorBrush Ink = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A2925"));
        public static readonly SolidColorBrush Sub = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B665A"));
        public static readonly SolidColorBrush Carrot = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D2541E"));
        public static readonly SolidColorBrush Blue = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D5592"));
        public static readonly SolidColorBrush Gray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8A8579"));

        public static readonly SolidColorBrush Paper = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCFAF5"));
        public static readonly SolidColorBrush Muted = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A8A29A"));
        public static readonly SolidColorBrush MutedStrong = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B9B3A6"));
        public static readonly SolidColorBrush Line = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E2D7"));
        public static readonly SolidColorBrush Navy = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B2432"));
        public static readonly SolidColorBrush Accent = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A57C4"));

        public static readonly SolidColorBrush Track = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ECE7DC"));

        public static TextBlock T(string text, double size, Brush fg, FontWeight? weight = null, FontFamily font = null)
        {
            return new TextBlock
            {
                Text = text, FontSize = size, Foreground = fg, FontFamily = font ?? Cn,
                FontWeight = weight ?? FontWeights.Normal, TextWrapping = TextWrapping.Wrap,
            };
        }

        public static Stream Res(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("NiuMa." + name);
        }

        public static BitmapSource LoadBackground(string path)
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.DecodePixelWidth = 900;
                bi.UriSource = new Uri(path, UriKind.Absolute);
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            catch { return null; }
        }

        public static System.Drawing.Icon AppIcon()
        {
            try { using (var s = Res("app.ico")) return new System.Drawing.Icon(s); }
            catch { return System.Drawing.SystemIcons.Application; }
        }

        public static ImageSource AppIconImage()
        {
            try
            {
                using (var stream = Res("app.ico"))
                {
                    var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                    BitmapFrame best = decoder.Frames[0];
                    foreach (var frame in decoder.Frames)
                        if (frame.PixelWidth > best.PixelWidth) best = frame;
                    best.Freeze();
                    return best;
                }
            }
            catch { return null; }
        }

        public static ControlTemplate FlatTemplate()
        {
            var t = new ControlTemplate(typeof(Button));
            var bd = new FrameworkElementFactory(typeof(Border));
            bd.Name = "bd";
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            bd.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            t.VisualTree = bd;
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.82));
            hover.Setters.Add(new Setter(Border.BackgroundProperty, B("#33000000"), "bd"));
            t.Triggers.Add(hover);
            var press = new Trigger { Property = System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty, Value = true };
            press.Setters.Add(new Setter(UIElement.OpacityProperty, 0.65));
            t.Triggers.Add(press);
            return t;
        }

        public static ControlTemplate RoundTemplate(double radius)
        {
            var t = new ControlTemplate(typeof(Button));
            var bd = new FrameworkElementFactory(typeof(Border));
            bd.Name = "bd";
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            bd.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            bd.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            bd.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            t.VisualTree = bd;
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.88, "bd"));
            t.Triggers.Add(hover);
            var press = new Trigger { Property = System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty, Value = true };
            press.Setters.Add(new Setter(UIElement.OpacityProperty, 0.7, "bd"));
            t.Triggers.Add(press);
            return t;
        }

        public static Button Ghost(UIElement content, string tip, Action onClick)
        {
            var b = new Button
            {
                Content = content, ToolTip = tip, Cursor = Cursors.Hand, Background = Brushes.Transparent,
                Padding = new Thickness(7, 4, 7, 4), BorderThickness = new Thickness(0),
                Template = FlatTemplate(), VerticalAlignment = VerticalAlignment.Center,
            };
            b.Click += (s, e) => onClick();
            return b;
        }

    }
}
