using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace NiuMa
{
    public class SettingsWindow : Window
    {
        readonly AppSettings _orig;
        readonly AppSettings _s;
        readonly MainWindow _main;
        TextBox _amount, _monthDays, _start, _end, _lunchS, _lunchE;
        RadioButton _daily, _monthly;
        CheckBox _lunch;
        CheckBox[] _days = new CheckBox[7];
        TextBlock _error;
        bool _saved, _ready;

        public SettingsWindow(AppSettings s, MainWindow main)
        {
            _s = s; _main = main; _orig = main.Current.Clone();
            Title = "设置 - " + s.AppName;
            Icon = Ui.AppIconImage();
            var mainRoot = main.Content as FrameworkElement;
            var mainScale = mainRoot == null ? null : mainRoot.LayoutTransform as ScaleTransform;
            double scale = mainScale == null ? 1.0 : mainScale.ScaleX;
            scale = Math.Min(scale, Math.Min(SystemParameters.WorkArea.Width * 0.94 / 380, SystemParameters.WorkArea.Height * 0.94 / 640));
            Width = 380 * scale;
            Height = 640 * scale;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = main.Topmost;
            FontFamily = Ui.Cn; FontSize = 13;
            Foreground = Ui.Ink;
            UseLayoutRounding = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            Build();
            ((FrameworkElement)Content).LayoutTransform = new ScaleTransform(scale, scale);
            _ready = true;
            Closed += (o, e) =>
            {
                if (_saved) return;
                _orig.CopyWindowStateFrom(_main.Current);
                _main.ApplySettings(_orig, false);
            };
        }

        static Binding TemplateBinding(string name)
        {
            return new Binding(name) { RelativeSource = RelativeSource.TemplatedParent };
        }

        TextBox Box(string text)
        {
            var template = new ControlTemplate(typeof(TextBox));
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "frame";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            border.SetBinding(Border.BackgroundProperty, TemplateBinding("Background"));
            border.SetBinding(Border.BorderBrushProperty, TemplateBinding("BorderBrush"));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            var host = new FrameworkElementFactory(typeof(ScrollViewer));
            host.Name = "PART_ContentHost";
            host.SetValue(FrameworkElement.MarginProperty, new Thickness(10, 0, 10, 0));
            border.AppendChild(host);
            template.VisualTree = border;
            var focus = new Trigger { Property = UIElement.IsKeyboardFocusWithinProperty, Value = true };
            focus.Setters.Add(new Setter(Border.BorderBrushProperty, Ui.Accent.Clone(), "frame"));
            focus.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2), "frame"));
            template.Triggers.Add(focus);
            var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
            disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.4));
            template.Triggers.Add(disabled);
            var t = new TextBox
            {
                Text = text, Height = 34, FontSize = 14, Foreground = Ui.Ink,
                Background = Ui.Paper, BorderBrush = Ui.Line, Template = template,
                VerticalContentAlignment = VerticalAlignment.Center,
                SelectionBrush = Ui.Accent, Padding = new Thickness(0, 6, 0, 6)
            };
            t.TextChanged += (o, e) => Changed();
            return t;
        }

        ControlTemplate PillTemplate(Type type)
        {
            var template = new ControlTemplate(type);
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "pill";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
            border.SetBinding(Border.BackgroundProperty, TemplateBinding("Background"));
            border.SetBinding(Border.BorderBrushProperty, TemplateBinding("BorderBrush"));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);
            template.VisualTree = border;
            var selected = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
            selected.Setters.Add(new Setter(Border.BackgroundProperty, Ui.Navy.Clone(), "pill"));
            selected.Setters.Add(new Setter(Border.BorderBrushProperty, Ui.Navy.Clone(), "pill"));
            selected.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            template.Triggers.Add(selected);
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.82, "pill"));
            template.Triggers.Add(hover);
            var focus = new Trigger { Property = UIElement.IsKeyboardFocusedProperty, Value = true };
            focus.Setters.Add(new Setter(Border.BorderBrushProperty, Ui.Accent.Clone(), "pill"));
            focus.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(2), "pill"));
            template.Triggers.Add(focus);
            return template;
        }

        RadioButton Mode(string text, bool value)
        {
            var r = new RadioButton
            {
                Content = text, IsChecked = value, GroupName = "salary", Height = 30,
                Background = Brushes.Transparent, BorderBrush = Brushes.Transparent,
                Style = ToggleStyle(), Cursor = Cursors.Hand, FontWeight = FontWeights.SemiBold,
                Template = PillTemplate(typeof(RadioButton))
            };
            r.Checked += (o, e) => Changed();
            return r;
        }

        static Style ToggleStyle()
        {
            var style = new Style();
            style.Setters.Add(new Setter(Control.ForegroundProperty, Ui.Sub.Clone()));
            return style;
        }

        static StackPanel Stack(params UIElement[] items)
        {
            var p = new StackPanel();
            foreach (var item in items) p.Children.Add(item);
            return p;
        }

        static Grid Pair(UIElement left, UIElement right, double gap)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(gap) });
            g.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(right, 2);
            g.Children.Add(left); g.Children.Add(right);
            return g;
        }

        static UIElement Field(string label, TextBox input)
        {
            System.Windows.Automation.AutomationProperties.SetName(input, label);
            var t = Ui.T(label, 12, Ui.Sub);
            t.Margin = new Thickness(0, 0, 0, 5);
            return Stack(t, input);
        }

        Border Card(string title, UIElement content)
        {
            var t = Ui.T(title, 14, Ui.Ink, FontWeights.SemiBold);
            t.Margin = new Thickness(0, 0, 0, 10);
            return new Border
            {
                Background = Ui.B("#A8FFFFFF"), CornerRadius = new CornerRadius(16),
                BorderBrush = Ui.B("#70FFFFFF"), BorderThickness = new Thickness(1),
                Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8),
                Child = Stack(t, content)
            };
        }

        static string D(double v) { return v.ToString("0.##", CultureInfo.InvariantCulture); }

        Button ActionButton(string text, bool primary, Action action)
        {
            var b = new Button
            {
                Content = text, Height = 36, FontSize = 13, FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand, Background = primary ? Ui.Navy : Brushes.Transparent,
                Foreground = primary ? Brushes.White : Ui.Sub, BorderBrush = primary ? Ui.Navy : Ui.Line,
                BorderThickness = new Thickness(1), Template = Ui.RoundTemplate(12)
            };
            b.Click += (o, e) => action();
            return b;
        }

        void Build()
        {
            var scrollTemplate = (ControlTemplate)System.Windows.Markup.XamlReader.Parse(
                "<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='ScrollBar'>" +
                "<Track x:Name='PART_Track' IsDirectionReversed='True' Orientation='Vertical'><Track.Thumb><Thumb><Thumb.Template><ControlTemplate TargetType='Thumb'>" +
                "<Border CornerRadius='3' Background='{Binding Background, RelativeSource={RelativeSource AncestorType=ScrollBar}}'/>" +
                "</ControlTemplate></Thumb.Template></Thumb></Track.Thumb></Track></ControlTemplate>");
            var scrollbarStyle = new Style(typeof(ScrollBar));
            scrollbarStyle.Setters.Add(new Setter(Control.TemplateProperty, scrollTemplate));
            scrollbarStyle.Setters.Add(new Setter(WidthProperty, 6.0));
            scrollbarStyle.Setters.Add(new Setter(Control.BackgroundProperty, Ui.Line.Clone()));
            Resources.Add(typeof(ScrollBar), scrollbarStyle);
            var root = new DockPanel { Margin = new Thickness(16, 14, 16, 16), LastChildFill = true };
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            header.MouseLeftButtonDown += (o, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };
            var close = Ui.Ghost(Ui.T("\uE8BB", 12, Ui.Sub, null, Ui.Icon), "关闭设置", Close);
            close.Width = close.Height = 28;
            System.Windows.Automation.AutomationProperties.SetName(close, "关闭设置");
            close.VerticalAlignment = VerticalAlignment.Top;
            DockPanel.SetDock(close, Dock.Right); header.Children.Add(close);
            var icon = new Image { Source = Ui.AppIconImage(), Width = 30, Height = 30, Margin = new Thickness(0, 2, 9, 0), VerticalAlignment = VerticalAlignment.Top };
            DockPanel.SetDock(icon, Dock.Left); header.Children.Add(icon);
            var title = Ui.T("设置", 20, Ui.Ink, FontWeights.Bold);
            var subtitle = Ui.T("工作与薪资偏好", 11, Ui.Gray);
            subtitle.Margin = new Thickness(0, 2, 0, 0);
            header.Children.Add(Stack(title, subtitle));
            DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);

            var footer = new StackPanel { Margin = new Thickness(0, 2, 0, 0) };
            _error = Ui.T("", 12, Ui.Carrot);
            _error.MinHeight = 18;
            footer.Children.Add(_error);
            var actions = new Grid();
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(new ColumnDefinition());
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(106) });
            var restore = Ui.Ghost(Ui.T("恢复默认", 12, Ui.Sub), "恢复默认设置", RestoreDefaults);
            restore.Width = 64; restore.Height = 36;
            actions.Children.Add(restore);
            var cancel = ActionButton("取消", false, Close); cancel.IsCancel = true;
            Grid.SetColumn(cancel, 2); actions.Children.Add(cancel);
            var save = ActionButton("保存设置", true, Save); save.IsDefault = true;
            Grid.SetColumn(save, 4); actions.Children.Add(save);
            footer.Children.Add(actions);
            DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);

            var body = new StackPanel();
            _daily = Mode("按日薪", _s.SalaryMode != 1);
            _monthly = Mode("按月薪", _s.SalaryMode == 1);
            var modes = new Border { Background = Ui.Paper, Padding = new Thickness(4), CornerRadius = new CornerRadius(12), Child = Pair(_daily, _monthly, 4), Margin = new Thickness(0, 0, 0, 10) };
            _amount = Box(D(_s.Amount)); _monthDays = Box(D(_s.MonthDays));
            var salaryFields = Pair(Field("薪资金额", _amount), Field("月计薪天数 · 天", _monthDays), 12);
            _monthDays.ToolTip = "月薪模式下，日薪 = 月薪 ÷ 计薪天数";
            body.Children.Add(Card("薪资设置", Stack(modes, salaryFields)));

            _start = Box(_s.StartTime); _end = Box(_s.EndTime);
            var times = Pair(Field("上班时间", _start), Field("下班时间", _end), 12);
            _lunch = new CheckBox { Content = "扣除午休", IsChecked = _s.LunchEnabled, Height = 28, Width = 92, Cursor = Cursors.Hand, Style = ToggleStyle(), Background = Ui.Paper, BorderBrush = Ui.Line, Template = PillTemplate(typeof(CheckBox)) };
            _lunch.Click += (o, e) => Changed();
            var lunchTop = new DockPanel { Margin = new Thickness(0, 10, 0, 8) };
            DockPanel.SetDock(_lunch, Dock.Right); lunchTop.Children.Add(_lunch);
            var lunchLabel = Ui.T("午休不计薪", 12, Ui.Sub); lunchLabel.VerticalAlignment = VerticalAlignment.Center;
            lunchTop.Children.Add(lunchLabel);
            _lunchS = Box(_s.LunchStart); _lunchE = Box(_s.LunchEnd);
            _lunchS.SetBinding(IsEnabledProperty, new Binding("IsChecked") { Source = _lunch });
            _lunchE.SetBinding(IsEnabledProperty, new Binding("IsChecked") { Source = _lunch });
            System.Windows.Automation.AutomationProperties.SetName(_lunchS, "午休开始时间");
            System.Windows.Automation.AutomationProperties.SetName(_lunchE, "午休结束时间");
            body.Children.Add(Card("工作与午休", Stack(times, lunchTop, Pair(_lunchS, _lunchE, 12))));

            var days = new UniformGrid { Columns = 7 };
            var workDays = _s.WorkDaySet();
            string[] names = { "日", "一", "二", "三", "四", "五", "六" };
            int[] order = { 1, 2, 3, 4, 5, 6, 0 };
            foreach (int d in order)
            {
                var c = new CheckBox
                {
                    Content = "周" + names[d], IsChecked = workDays[d], Height = 32,
                    Margin = new Thickness(0, 0, d == 0 ? 0 : 3, 0), Cursor = Cursors.Hand,
                    Style = ToggleStyle(), Background = Ui.Paper, BorderBrush = Ui.Line,
                    Template = PillTemplate(typeof(CheckBox))
                };
                c.Click += (o, e) => Changed(); _days[d] = c; days.Children.Add(c);
            }
            var daysHint = Ui.T("临时调休可在主窗口右键设置。", 11, Ui.Gray);
            daysHint.Margin = new Thickness(0, 8, 0, 0);
            var workCard = Card("每周工作日", Stack(days, daysHint));
            workCard.Margin = new Thickness(0);
            body.Children.Add(workCard);
            root.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, PanningMode = PanningMode.VerticalOnly, FocusVisualStyle = null });
            Content = new Border
            {
                CornerRadius = new CornerRadius(22), Background = Ui.Paper,
                BorderBrush = Ui.Line, BorderThickness = new Thickness(1), Margin = new Thickness(10),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18, ShadowDepth = 3, Opacity = 0.14 },
                Child = root
            };
        }
        static bool Num(TextBox t, out double v)
        {
            return double.TryParse(t.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v) && !double.IsNaN(v) && !double.IsInfinity(v);
        }

        string Collect(AppSettings t)
        {
            double v;
            TimeSpan ts, te;
            t.SalaryMode = _monthly.IsChecked == true ? 1 : 0;
            if (!Num(_amount, out v) || v < 0 || v > 1e9) return "薪资金额不正确";
            t.Amount = v;
            if (!Num(_monthDays, out v) || v <= 0 || v > 31) return "月计薪天数应在 1~31 之间";
            t.MonthDays = v;
            if (!AppSettings.TryParseTime(_start.Text, out ts) || !AppSettings.TryParseTime(_end.Text, out te)) return "上下班时间格式应为 HH:mm";
            if (te <= ts) return "下班时间要晚于上班时间";
            t.StartTime = ts.ToString(@"hh\:mm"); t.EndTime = te.ToString(@"hh\:mm");
            t.LunchEnabled = _lunch.IsChecked == true;
            TimeSpan ls, le;
            if (!AppSettings.TryParseTime(_lunchS.Text, out ls) || !AppSettings.TryParseTime(_lunchE.Text, out le))
            {
                if (t.LunchEnabled) return "午休时间格式应为 HH:mm";
            }
            else
            {
                if (t.LunchEnabled && (le <= ls || ls < ts || le > te)) return "午休时间需在上班时段内，且结束晚于开始";
                t.LunchStart = ls.ToString(@"hh\:mm"); t.LunchEnd = le.ToString(@"hh\:mm");
            }
            var days = new System.Collections.Generic.List<string>();
            for (int i = 0; i < 7; i++) if (_days[i].IsChecked == true) days.Add(i.ToString());
            t.WorkDays = string.Join(",", days);
            return null;
        }

        void Changed()
        {
            if (!_ready) return;
            var err = Collect(_s);
            _error.Text = err ?? "";
            if (err == null)
            {
                _s.CopyWindowStateFrom(_main.Current);
                _main.ApplySettings(_s.Clone(), false);
                Title = "设置 - " + _s.AppName;
            }
        }

        void Save()
        {
            var err = Collect(_s);
            if (err != null) { _error.Text = err; return; }
            _s.CopyWindowStateFrom(_main.Current);
            _saved = true;
            _main.ApplySettings(_s.Clone(), true);
            Close();
        }

        void RestoreDefaults()
        {
            if (MessageBox.Show(this, "把薪资和上下班时间恢复为默认值？", "恢复默认", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            var d = new AppSettings();
            _ready = false;
            _daily.IsChecked = d.SalaryMode == 0; _monthly.IsChecked = d.SalaryMode == 1; _amount.Text = D(d.Amount); _s.Currency = d.Currency;
            _monthDays.Text = D(d.MonthDays); _start.Text = d.StartTime; _end.Text = d.EndTime;
            _lunch.IsChecked = d.LunchEnabled; _lunchS.Text = d.LunchStart; _lunchE.Text = d.LunchEnd;
            var set = d.WorkDaySet();
            for (int i = 0; i < 7; i++) _days[i].IsChecked = set[i];
            _ready = true;
            Changed();
        }

    }
}
