using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Path = System.Windows.Shapes.Path;

namespace NiuMa
{
    public partial class MainWindow
    {
        FrameworkElement BuildIllustration()
        {
            var _stage = new Canvas { Width = 330, Height = 168 };
            Brush body = Ui.B("#DCC7A5"), bodyDark = Ui.B("#D3BB95"), belly = Ui.B("#EBDCC2"),
                  hoof = Ui.B("#AE9169"), mane = Ui.B("#C9AF89"), tip = Ui.B("#BFA37C");

            var blob1 = El(48, 112, 28, 11, Ui.B("#FBF0DC")); blob1.Opacity = 0.9; _stage.Children.Add(blob1);
            var blob2 = El(150, 130, 60, 9, Ui.B("#FBF0DC")); blob2.Opacity = 0.85; _stage.Children.Add(blob2);

            _stage.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = 6, Y1 = 151, X2 = 324, Y2 = 151, Stroke = Ui.B("#DCC9A6"), StrokeThickness = 2.4,
                StrokeDashArray = new DoubleCollection { 1.6, 2.2 },
                StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
            });

            _motionLayer = new Canvas { Width = 330, Height = 168, Opacity = 0.2 };
            _motionLayer.Children.Add(RBox(10, 82, 24, 3.4, 2, Ui.B("#DFD0B2")));
            _motionLayer.Children.Add(RBox(2, 96, 16, 3.4, 2, Ui.B("#DFD0B2")));
            _stage.Children.Add(_motionLayer);

            var donkey = new Canvas { Width = 330, Height = 168, RenderTransform = _donkeyTf };

            donkey.Children.Add(P("M 98,74 C 78,70 64,88 70,102", mane, 4.5));
            donkey.Children.Add(El(70, 106, 5.5, 6.5, tip, -12));

            donkey.Children.Add(RBox(92, 96, 11, 41, 5, bodyDark));
            donkey.Children.Add(RBox(90, 137, 14, 13, 4, hoof));
            donkey.Children.Add(RBox(109, 96, 12, 39, 5, body));
            donkey.Children.Add(RBox(107, 135, 15, 15, 4, hoof));
            donkey.Children.Add(RBox(168, 96, 11, 41, 5, bodyDark));
            donkey.Children.Add(RBox(166, 137, 14, 13, 4, hoof));
            donkey.Children.Add(RBox(183, 96, 12, 39, 5, body));
            donkey.Children.Add(RBox(181, 135, 15, 15, 4, hoof));

            donkey.Children.Add(El(146, 84, 52, 24, body, 0));
            donkey.Children.Add(El(104, 82, 29, 26, bodyDark, 0));
            var bellyEl = El(150, 93, 40, 11, belly, 0); bellyEl.Opacity = 0.8; donkey.Children.Add(bellyEl);

            donkey.Children.Add(P("M 184,86 L 197,66", body, 26));
            donkey.Children.Add(P("M 197,48 C 192,60 187,72 183,82", mane, 4.5));

            donkey.Children.Add(El(211, 53, 25, 15, body, -20));
            donkey.Children.Add(El(231, 61, 14, 11, Ui.B("#E9D8BC"), -12));
            donkey.Children.Add(El(236, 59, 1.9, 2.5, Ui.B("#A38B69"), 0));
            donkey.Children.Add(P("M 228,68 C 234,71 240,70 242,66", Ui.B("#C4AA80"), 2));
            donkey.Children.Add(El(204, 47, 2.7, 2.7, Ui.B("#5C5040"), 0));

            donkey.Children.Add(El(199, 26, 5, 17, body, -24));
            donkey.Children.Add(El(199, 27, 2.6, 12, mane, -24));
            donkey.Children.Add(El(212, 21, 5, 17, body, -8));
            donkey.Children.Add(El(212, 22, 2.6, 12, mane, -8));

            var carrot = new Canvas { Width = 330, Height = 168, RenderTransformOrigin = new Point(0.5, 0.5) };
            var tg = new TransformGroup();
            tg.Children.Add(_carrotRot);
            tg.Children.Add(_carrotTfY);
            carrot.RenderTransform = tg;
            carrot.Children.Add(F("M 260,44 C 264,38 276,37 280,42 C 281,54 272,72 266,78 C 259,70 254,54 260,44 Z", Ui.B("#F0873F"), Ui.B("#DE6B2C"), 1.6));
            carrot.Children.Add(P("M 264,48 C 266,56 267,64 265,70", Ui.B("#F7AC70"), 3));
            carrot.Children.Add(P("M 276,40 C 273,30 277,22 283,17", Ui.B("#6F9B4E"), 4.5));
            carrot.Children.Add(P("M 278,42 C 285,33 293,29 299,28", Ui.B("#7FA85B"), 4.5));
            carrot.Children.Add(P("M 277,43 C 285,39 293,40 298,43", Ui.B("#6F9B4E"), 4));
            donkey.Children.Add(carrot);

            _stage.Children.Add(donkey);
            return _stage;
        }

        static System.Windows.Shapes.Ellipse El(double cx, double cy, double rx, double ry, Brush fill, double rotate = 0)
        {
            var e = new System.Windows.Shapes.Ellipse
            {
                Width = rx * 2, Height = ry * 2, Fill = fill,
                RenderTransformOrigin = new Point(0.5, 0.5),
            };
            if (rotate != 0) e.RenderTransform = new RotateTransform(rotate);
            Canvas.SetLeft(e, cx - rx);
            Canvas.SetTop(e, cy - ry);
            return e;
        }

        static Path P(string data, Brush stroke, double thickness)
        {
            return new Path
            {
                Data = Geometry.Parse(data), Stroke = stroke, StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
            };
        }

        static Path F(string data, Brush fill, Brush stroke, double thickness)
        {
            return new Path
            {
                Data = Geometry.Parse(data), Fill = fill, Stroke = stroke, StrokeThickness = thickness,
                StrokeLineJoin = PenLineJoin.Round,
            };
        }

        static Border RBox(double x, double y, double w, double h, double r, Brush fill)
        {
            var b = new Border { Width = w, Height = h, CornerRadius = new CornerRadius(r), Background = fill };
            Canvas.SetLeft(b, x);
            Canvas.SetTop(b, y);
            return b;
        }

    }
}
