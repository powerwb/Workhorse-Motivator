using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace NiuMa
{
    public partial class MainWindow : Window
    {
        AppSettings _s;
        Border _root, _card, _barFill, _mini, _miniDot;
        Grid _barTrack;
        Border _bg;
        FrameworkElement _full;
        TextBlock _title, _clock, _status, _sub, _caption, _wish, _cur, _mask,
                  _progressText, _progressRight, _hoursText, _month,
                  _miniMask, _miniStatus, _pauseLabel, _pauseGlyph, _countdownLabel, _pinLabel;
        Button _themeButton;
        Canvas _motionLayer;
        AnimView _anim;
        Odometer _odo, _miniOdo;
        TranslateTransform _donkeyTf = new TranslateTransform();
        TranslateTransform _carrotTfY = new TranslateTransform();
        RotateTransform _carrotRot = new RotateTransform(8);
        ScaleTransform _scale = new ScaleTransform(1, 1);
        Forms.NotifyIcon _tray;
        Stopwatch _sw = Stopwatch.StartNew();
        DateTime _base = DateTime.Now;
        WorkState? _lastState;
        long _lastSecond = -1;
        DispatcherTimer _timer;
        SettingsWindow _settingsWin;
        bool _exiting, _paused;
        Snapshot _snap;

        public MainWindow(AppSettings s)
        {
            _s = s;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            UseLayoutRounding = true;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Ideal);
            Icon = Ui.AppIconImage();

            BuildUi();
            BuildTray();
            ApplySettings(_s, false);

            SourceInitialized += OnSourceInitialized;
            Loaded += (o, e) => { PlaceWindow(); SyncAnimation(); };
            DpiChanged += (o, e) => Dispatcher.BeginInvoke(new Action(ApplyScale), DispatcherPriority.Background);
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += (o, e) => Dispatcher.BeginInvoke(new Action(ApplyScale), DispatcherPriority.Background);
            CompositionTarget.Rendering += OnRender;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (o, e) => Tick(true);
            _timer.Start();
            Closing += (o, e) => { if (!_exiting) { e.Cancel = true; HideToTray(); } };
        }

        public DateTime Now()
        {
            var t = _base + _sw.Elapsed;
            var real = DateTime.Now;
            if (Math.Abs((real - t).TotalSeconds) > 0.5) { _base = real; _sw.Restart(); t = real; }
            return t;
        }

        void OnSourceInitialized(object sender, EventArgs e)
        {
            var src = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            src.AddHook(WndProc);
            var h = src.Handle;
            uint mods = Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT;
            Native.RegisterHotKey(h, 1, mods, (uint)'M');
            Native.RegisterHotKey(h, 2, mods, (uint)'T');
            Native.RegisterHotKey(h, 3, mods, (uint)'H');
            Native.SetExStyle(this, Native.WS_EX_TOOLWINDOW, true);
            Native.SetExStyle(this, Native.WS_EX_TRANSPARENT, _s.ClickThrough);
        }

        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr w, IntPtr l, ref bool handled)
        {
            if (msg == Native.WM_HOTKEY)
            {
                int id = w.ToInt32();
                if (id == 1) ToggleVisible();
                else if (id == 2) ToggleClickThrough();
                else if (id == 3) ToggleMask();
                handled = true;
            }
            else if (msg == Native.ShowMeMessage)
            {
                ShowFromTray();
                handled = true;
            }
            return IntPtr.Zero;
        }

        void PlaceWindow()
        {
            ApplyScale();
            UpdateLayout();
            var wa = CurrentWorkArea();
            double vl = SystemParameters.VirtualScreenLeft, vt = SystemParameters.VirtualScreenTop;
            double vr = vl + SystemParameters.VirtualScreenWidth, vb = vt + SystemParameters.VirtualScreenHeight;
            bool valid = !double.IsNaN(_s.Left) && !double.IsNaN(_s.Top)
                && _s.Left + 60 < vr && _s.Left + ActualWidth - 60 > vl
                && _s.Top >= vt - 10 && _s.Top + 40 < vb;
            if (valid) { Left = _s.Left; Top = _s.Top; }
            else
            {
                Left = wa.Right - ActualWidth - 24;
                Top = wa.Top + 24;
            }
        }

        void SavePosition()
        {
            _s.Left = Left; _s.Top = Top;
            _s.Save();
        }

        Rect CurrentWorkArea()
        {
            var h = new WindowInteropHelper(this).Handle;
            var src = PresentationSource.FromVisual(this);
            if (h == IntPtr.Zero || src == null || src.CompositionTarget == null) return SystemParameters.WorkArea;
            var wa = Forms.Screen.FromHandle(h).WorkingArea;
            var m = src.CompositionTarget.TransformFromDevice;
            return new Rect(m.Transform(new Point(wa.Left, wa.Top)), m.Transform(new Point(wa.Right, wa.Bottom)));
        }

        const double RefHeight = 1050;

        double EffectiveScale()
        {
            var wa = CurrentWorkArea();
            double auto = Math.Max(0.6, Math.Min(3.0, wa.Height / RefHeight));
            double sc = auto * Math.Max(0.5, Math.Min(2.0, _s.UiZoom));
            var c = (UIElement)Content;
            if (c != null && _scale.ScaleY > 0)
            {
                c.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double natW = c.DesiredSize.Width / _scale.ScaleX, natH = c.DesiredSize.Height / _scale.ScaleY;
                if (natH > 0) sc = Math.Min(sc, wa.Height * 0.94 / natH);
                if (natW > 0) sc = Math.Min(sc, wa.Width * 0.94 / natW);
            }
            return Math.Max(0.3, sc);
        }

        void ApplyScale()
        {
            double sc = Math.Round(EffectiveScale(), 3);
            if (Math.Abs(_scale.ScaleX - sc) < 0.001) return;
            _scale.ScaleX = sc; _scale.ScaleY = sc;
            if (!IsLoaded) return;
            UpdateLayout();
            KeepOnScreen();
        }

        void KeepOnScreen()
        {
            var wa = CurrentWorkArea();
            if (Left + ActualWidth > wa.Right) Left = Math.Max(wa.Left, wa.Right - ActualWidth);
            if (Top + ActualHeight > wa.Bottom) Top = Math.Max(wa.Top, wa.Bottom - ActualHeight);
            if (Left < wa.Left) Left = wa.Left;
            if (Top < wa.Top) Top = wa.Top;
        }

        void BuildUi()
        {
            _root = new Border
            {
                CornerRadius = new CornerRadius(16), Background = Ui.Paper,
                BorderBrush = Ui.Line, BorderThickness = new Thickness(1), Margin = new Thickness(12),
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 18, ShadowDepth = 3, Opacity = 0.16, Color = Colors.Black },
            };
            var layers = new Grid { ClipToBounds = true };
            _bg = new Border();
            RenderOptions.SetBitmapScalingMode(_bg, BitmapScalingMode.HighQuality);
            layers.Children.Add(_bg);
            _root.SizeChanged += (o, e) =>
            {
                layers.Clip = new RectangleGeometry(new Rect(0, 0, _root.ActualWidth - 2, _root.ActualHeight - 2), 15, 15);
            };

            var content = new Grid();
            content.Children.Add(BuildFull());
            content.Children.Add(BuildMini());
            layers.Children.Add(content);
            _root.Child = layers;
            _root.LayoutTransform = _scale;
            Content = _root;

            _root.MouseLeftButtonDown += (o, e) =>
            {
                if (e.ClickCount == 2) { ToggleMini(); return; }
                try { DragMove(); } catch { }
                SnapToEdges();
                SavePosition();
            };
            _root.ContextMenu = BuildMenu();
        }

        FrameworkElement BuildFull()
        {
            var sp = new StackPanel { Width = 352, Margin = new Thickness(18, 12, 18, 16) };

            var header = new DockPanel { LastChildFill = true };
            var tools = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            _themeButton = Ui.Ghost(Ui.T(Glyph(0xE790), 15, Ui.Sub, null, Ui.Icon), "切换皮肤", CycleTheme);
            _themeButton.Width = 28;
            _themeButton.Height = 28;
            System.Windows.Automation.AutomationProperties.SetName(_themeButton, "切换皮肤");
            tools.Children.Add(_themeButton);

            _pinLabel = Ui.T("置顶", 12, Ui.Gray);
            var pinBtn = Ui.Ghost(_pinLabel, "窗口置顶 / 取消置顶", ToggleTopmost);
            pinBtn.Margin = new Thickness(0, 0, 2, 0);
            tools.Children.Add(pinBtn);
            var setGhost = Ui.Ghost(Ui.T("设置", 12, Ui.Sub), "打开设置", OpenSettings);
            setGhost.Margin = new Thickness(2, 0, 4, 0);
            tools.Children.Add(setGhost);
            var minBtn = Ui.Ghost(Ui.T(Glyph(0xE921), 11, Ui.Sub, null, Ui.Icon), "隐藏到托盘 (Ctrl+Alt+M)", HideToTray);
            minBtn.Width = minBtn.Height = 26;
            minBtn.Margin = new Thickness(6, 0, 0, 0);
            tools.Children.Add(minBtn);
            var closeBtn = Ui.Ghost(Ui.T(Glyph(0xE8BB), 11, Ui.Sub, null, Ui.Icon), "隐藏到托盘（右键菜单可退出）", HideToTray);
            closeBtn.Width = closeBtn.Height = 26;
            closeBtn.Margin = new Thickness(2, 0, 0, 0);
            tools.Children.Add(closeBtn);
            DockPanel.SetDock(tools, Dock.Right);
            header.Children.Add(tools);

            var logo = BuildLogo();
            DockPanel.SetDock(logo, Dock.Left);
            header.Children.Add(logo);

            _title = Ui.T("", 15.5, Ui.Ink, FontWeights.Bold);
            _title.TextWrapping = TextWrapping.NoWrap;
            _title.TextTrimming = TextTrimming.CharacterEllipsis;
            _title.VerticalAlignment = VerticalAlignment.Center;
            _title.Margin = new Thickness(10, 1, 8, 0);
            header.Children.Add(_title);
            sp.Children.Add(header);

            _clock = Ui.T("", 12, Ui.Gray, null, Ui.Num);
            _clock.Margin = new Thickness(0, 14, 0, 0);
            sp.Children.Add(_clock);

            _status = Ui.T("", 34, Ui.MutedStrong, FontWeights.SemiBold);
            _status.Margin = new Thickness(0, 4, 0, 0);
            _status.TextWrapping = TextWrapping.NoWrap;
            sp.Children.Add(_status);

            _sub = Ui.T("", 13, Ui.Sub);
            _sub.Margin = new Thickness(0, 2, 0, 0);
            sp.Children.Add(_sub);

            _card = new Border
            {
                CornerRadius = new CornerRadius(14), Padding = new Thickness(14, 12, 14, 8),
                Margin = new Thickness(0, 14, 0, 0),
                Background = Ui.Paper,
            };
            var cardBody = new StackPanel();
            var capRow = new DockPanel { LastChildFill = true };

            var pillContent = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _pauseLabel = Ui.T("暂停", 11.5, Ui.Sub, FontWeights.SemiBold);
            _pauseGlyph = Ui.T(Glyph(0xE769), 9, Ui.Sub, null, Ui.Icon);
            _pauseGlyph.Margin = new Thickness(5, 1, 0, 0);
            _pauseGlyph.VerticalAlignment = VerticalAlignment.Center;
            pillContent.Children.Add(_pauseLabel);
            pillContent.Children.Add(_pauseGlyph);
            var pill = new Button
            {
                Content = pillContent, Cursor = Cursors.Hand, Background = Ui.Paper,
                BorderBrush = Ui.Line, BorderThickness = new Thickness(1),
                Padding = new Thickness(11, 4, 11, 4), Template = Ui.RoundTemplate(13),
                VerticalAlignment = VerticalAlignment.Center, ToolTip = "暂停 / 继续小驴动画",
            };
            pill.Click += (o, e) => TogglePause();
            DockPanel.SetDock(pill, Dock.Right);
            capRow.Children.Add(pill);

            _caption = Ui.T("", 12.5, Ui.Sub);
            _caption.VerticalAlignment = VerticalAlignment.Center;
            capRow.Children.Add(_caption);
            cardBody.Children.Add(capRow);

            var artHost = new Grid { Height = 162, Margin = new Thickness(0, 8, 0, 0) };
            _anim = new AnimView("anim.bin");
            if (_anim.Ok) artHost.Children.Add(_anim);
            else
            {
                var fallback = new Viewbox { Stretch = Stretch.Uniform, VerticalAlignment = VerticalAlignment.Center };
                fallback.Child = BuildIllustration();
                artHost.Children.Add(fallback);
            }
            cardBody.Children.Add(artHost);
            _card.Child = cardBody;
            sp.Children.Add(_card);

            var earnedLabel = Ui.T("今日已赚 · 估算", 12, Ui.Gray);
            earnedLabel.Margin = new Thickness(0, 16, 0, 0);
            sp.Children.Add(earnedLabel);

            var amtRow = new Grid { Margin = new Thickness(0, 2, 0, 0), Height = 60 };
            var amtStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _cur = Ui.T("", 23, Ui.Carrot, FontWeights.Bold, Ui.Num);
            _cur.VerticalAlignment = VerticalAlignment.Bottom;
            _cur.Margin = new Thickness(0, 0, 6, 10);
            amtStack.Children.Add(_cur);
            _odo = new Odometer(Ui.Num, 40, Ui.Carrot) { VerticalAlignment = VerticalAlignment.Center };
            amtStack.Children.Add(_odo);
            _mask = Ui.T("", 40, Ui.Carrot, FontWeights.Bold, Ui.Num);
            _mask.VerticalAlignment = VerticalAlignment.Center;
            amtRow.Children.Add(amtStack);
            amtRow.Children.Add(_mask);
            sp.Children.Add(amtRow);

            _wish = Ui.T("", 13, Ui.Ink);
            _wish.Margin = new Thickness(0, 2, 0, 0);
            sp.Children.Add(_wish);

            var focus = new Button
            {
                Height = 44, Margin = new Thickness(0, 16, 0, 0), Cursor = Cursors.Hand,
                Background = Ui.Navy, BorderThickness = new Thickness(0), Padding = new Thickness(0),
                Template = Ui.RoundTemplate(12), HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            _countdownLabel = Ui.T("隐藏倒计时 · 专注一会儿", 13.5, Ui.B("#FBFAF5"), FontWeights.SemiBold);
            focus.Content = _countdownLabel;
            focus.Click += (o, e) => ToggleCountdown();
            sp.Children.Add(focus);

            var prow = new Grid { Margin = new Thickness(0, 18, 0, 0) };
            _progressText = Ui.T("", 13, Ui.Ink, FontWeights.SemiBold);
            _progressText.VerticalAlignment = VerticalAlignment.Center;
            _progressRight = Ui.T("", 12, Ui.Gray);
            _progressRight.HorizontalAlignment = HorizontalAlignment.Right;
            _progressRight.VerticalAlignment = VerticalAlignment.Center;
            prow.Children.Add(_progressText);
            prow.Children.Add(_progressRight);
            sp.Children.Add(prow);

            _barTrack = new Grid { Height = 6, Margin = new Thickness(0, 8, 0, 8) };
            _barTrack.Children.Add(new Border { CornerRadius = new CornerRadius(3), Background = Ui.Track });
            _barFill = new Border
            {
                CornerRadius = new CornerRadius(3), HorizontalAlignment = HorizontalAlignment.Left, Width = 0,
                Background = Ui.Accent,
            };
            _barTrack.Children.Add(_barFill);
            sp.Children.Add(_barTrack);

            _hoursText = Ui.T("", 12, Ui.Gray, null, Ui.Num);
            sp.Children.Add(_hoursText);

            var monthLabel = Ui.T("本月累计", 12.5, Ui.Gray);
            monthLabel.Margin = new Thickness(0, 16, 0, 0);
            sp.Children.Add(monthLabel);

            _month = Ui.T("", 26, Ui.Accent, FontWeights.SemiBold, Ui.Num);
            _month.Margin = new Thickness(0, 2, 0, 0);
            sp.Children.Add(_month);

            _full = sp;
            return sp;
        }

        FrameworkElement BuildLogo()
        {
            var image = new Image { Source = Ui.AppIconImage(), Width = 34, Height = 34, VerticalAlignment = VerticalAlignment.Center };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            return image;
        }

        FrameworkElement BuildMini()
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _miniDot = new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(_miniDot);
            var host = new Grid();
            _miniOdo = new Odometer(Ui.Num, 24, Ui.Carrot) { VerticalAlignment = VerticalAlignment.Center };
            _miniMask = Ui.T("", 24, Ui.Carrot, FontWeights.Bold, Ui.Num);
            host.Children.Add(_miniOdo);
            host.Children.Add(_miniMask);
            row.Children.Add(host);
            _miniStatus = Ui.T("", 12, Ui.Sub);
            _miniStatus.VerticalAlignment = VerticalAlignment.Center;
            _miniStatus.Margin = new Thickness(10, 0, 0, 0);
            row.Children.Add(_miniStatus);
            _mini = new Border
            {
                Child = row, Padding = new Thickness(14, 8, 14, 8), CornerRadius = new CornerRadius(15),
                Background = Ui.Paper, ToolTip = "双击展开 · 右键菜单",
            };
            return _mini;
        }

        static string Glyph(int code) { return ((char)code).ToString(); }

        MenuItem Item(string header, Action a)
        {
            var m = new MenuItem { Header = header };
            m.Click += (o, e) => a();
            return m;
        }

        ContextMenu BuildMenu()
        {
            var cm = new ContextMenu();
            cm.Opened += (o, e) =>
            {
                cm.Items.Clear();
                cm.Items.Add(Item("设置…", OpenSettings));
                cm.Items.Add(Item("切换皮肤（" + Theme.All[Theme.Normalize(_s.Skin)].Name + "）", CycleTheme));
                cm.Items.Add(Item(_s.Mini ? "展开完整面板" : "切换迷你模式", ToggleMini));
                cm.Items.Add(Item(_s.Masked ? "显示金额" : "老板来了（隐藏金额）", ToggleMask));
                cm.Items.Add(Item(_s.HideCountdown ? "显示倒计时" : "隐藏倒计时", ToggleCountdown));
                cm.Items.Add(Item(_paused ? "继续小驴动画" : "暂停小驴动画", TogglePause));
                var pin = new MenuItem { Header = "窗口置顶", IsCheckable = true, IsChecked = _s.Topmost };
                cm.Items.Add(pin);
                pin.Click += (a, b) => ToggleTopmost();
                cm.Items.Add(Item("鼠标穿透（Ctrl+Alt+T 取消）", ToggleClickThrough));
                cm.Items.Add(new Separator());
                bool work = Engine.IsWorkDay(_s, Now());
                cm.Items.Add(Item(work ? "今天请假/休息（不计薪）" : "今天要上班（计薪）", () =>
                {
                    bool normal = _s.WorkDaySet()[(int)Now().DayOfWeek];
                    bool target = !work;
                    _s.OverrideDate = Engine.DateKey(Now());
                    _s.OverrideMode = target == normal ? 0 : (target ? 2 : 1);
                    _s.Save(); _lastState = null; Tick(true);
                }));
                cm.Items.Add(Item("复制今日战报", CopyReport));
                cm.Items.Add(new Separator());
                cm.Items.Add(Item("隐藏到托盘", HideToTray));
                cm.Items.Add(Item("退出", ExitApp));
            };
            cm.Items.Add(new MenuItem());
            return cm;
        }

        void BuildTray()
        {
            _tray = new Forms.NotifyIcon { Icon = Ui.AppIcon(), Visible = true };
            var m = new Forms.ContextMenuStrip();
            m.Items.Add("显示 / 隐藏", null, (o, e) => ToggleVisible());
            m.Items.Add("设置…", null, (o, e) => OpenSettings());
            m.Items.Add("鼠标穿透 开/关", null, (o, e) => ToggleClickThrough());
            m.Items.Add("复制今日战报", null, (o, e) => CopyReport());
            m.Items.Add("-");
            m.Items.Add("退出", null, (o, e) => ExitApp());
            _tray.ContextMenuStrip = m;
            _tray.MouseClick += (o, e) => { if (e.Button == Forms.MouseButtons.Left) ToggleVisible(); };
        }

        public void ApplySettings(AppSettings s, bool save)
        {
            _s = s;

            ImageSource img = null;
            if (!string.IsNullOrEmpty(_s.BgPath) && System.IO.File.Exists(_s.BgPath)) img = Ui.LoadBackground(_s.BgPath);
            _bg.Background = img == null ? null : new ImageBrush(img) { Stretch = Stretch.UniformToFill, AlignmentY = AlignmentY.Bottom };
            _odo.Configure("", _s.Odometer);
            _miniOdo.Configure(_s.Currency, _s.Odometer);
            ApplyVisual(_s);
            if (IsLoaded) Native.SetExStyle(this, Native.WS_EX_TRANSPARENT, _s.ClickThrough);
            _lastState = null;
            _lastSecond = -1;
            Tick(true);
            if (save) _s.Save();
        }

        public void ApplyVisual(AppSettings s)
        {
            string name = string.IsNullOrWhiteSpace(s.AppName) ? "牛马鼓励器" : s.AppName.Trim();
            Title = name;
            _title.Text = name;
            if (_tray != null) _tray.Text = name.Length > 60 ? name.Substring(0, 60) : name;
            _bg.Opacity = Math.Max(0, Math.Min(1, s.BgOpacity));
            _cur.Text = s.Currency;

            s.Skin = Theme.Normalize(s.Skin);
            var theme = Theme.All[s.Skin];
            theme.Apply();
            _card.Background = theme.Card(s.PanelOpacity);
            _barFill.Background = theme.Progress();
            _themeButton.ToolTip = "当前：" + theme.Name + " · 点击切换为" + Theme.All[(s.Skin + 1) % Theme.All.Length].Name;

            Opacity = Math.Max(0.2, Math.Min(1, s.WindowOpacity));
            Topmost = s.Topmost;
            _pinLabel.Foreground = s.Topmost ? Ui.Accent : Ui.Gray;
            _full.Visibility = s.Mini ? Visibility.Collapsed : Visibility.Visible;
            _mini.Visibility = s.Mini ? Visibility.Visible : Visibility.Collapsed;
            _root.CornerRadius = new CornerRadius(s.Mini ? 15 : 16);
            ApplyMask();
            ApplyScale();
        }

        void ApplyMask()
        {
            bool m = _s.Masked;
            _odo.Visibility = m ? Visibility.Hidden : Visibility.Visible;
            _miniOdo.Visibility = m ? Visibility.Hidden : Visibility.Visible;
            _mask.Text = m ? _s.Currency + " ****.**" : "";
            _miniMask.Text = _mask.Text;
            _mask.Visibility = _miniMask.Visibility = m ? Visibility.Visible : Visibility.Collapsed;
        }

        void Animate()
        {
            if (_motionLayer == null) return;
            double ph = _sw.Elapsed.TotalSeconds;
            bool run = !_paused && _snap != null &&
                (_snap.State == WorkState.Working || _snap.State == WorkState.Overtime || _snap.State == WorkState.Lunch);
            if (run)
            {
                _donkeyTf.Y = Math.Sin(ph * 6.2) * 1.6;
                _carrotTfY.Y = Math.Sin(ph * 3.4) * 2.4;
                _carrotRot.Angle = 8 + Math.Sin(ph * 3.4) * 5;
                _motionLayer.Opacity = 0.85;
            }
            else
            {
                _donkeyTf.Y = 0;
                _carrotTfY.Y = 0;
                _carrotRot.Angle = 8;
                _motionLayer.Opacity = 0.2;
            }
        }

        void OnRender(object sender, EventArgs e)
        {
            if (!IsVisible || _snap == null) return;
            Animate();
            var r = Engine.Compute(_s, Now());
            _snap = r;
            if (_s.Masked) return;
            if (_s.Mini) _miniOdo.SetValue(r.Earned, _s.Decimals, r.Speed);
            else
            {
                _odo.SetValue(r.Earned, _s.Decimals, r.Speed);
                if (_barTrack.ActualWidth > 0) _barFill.Width = Math.Max(0, Math.Min(1, r.Progress)) * _barTrack.ActualWidth;
            }
        }

        static readonly string[] Week = { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };

        string CaptionText(Snapshot r)
        {
            if (_paused) return "小驴歇会儿 · 萝卜跑不掉";
            switch (r.State)
            {
                case WorkState.RestDay: return "今天不追萝卜 · 小驴也在休息";
                case WorkState.BeforeWork: return "小驴已就位 · 等开工铃响";
                case WorkState.Lunch: return "午休中 · 小驴也去吃草了";
                case WorkState.Overtime: return "加班追第二个萝卜 · 越跑越远";
                case WorkState.Working:
                    double p = r.Progress;
                    if (p < 0.12) return "小驴刚上路 · 慢慢跑";
                    if (p < 0.5) return "小驴小跑中 · 稳住节奏";
                    if (p < 0.88) return "小驴追萝卜 · 越追越近";
                    return "小驴追萝卜 · 就差一点点";
                default: return "今天收工了";
            }
        }

        string SubText(Snapshot r)
        {
            bool hide = _s.HideCountdown;
            switch (r.State)
            {
                case WorkState.RestDay: return "今天不用追萝卜，好好休息。";
                case WorkState.BeforeWork: return hide ? "开工前的最后一点自由时间。" : "距离上班还有 " + Engine.Duration(r.SecondsToNext, true);
                case WorkState.Lunch: return hide ? "小驴也在吃饭，别急着跑。" : "午休还剩 " + Engine.Duration(r.SecondsToNext, true) + "，暂停计薪";
                case WorkState.Working: return hide ? "专注一会儿，时间会自己走。" : "距离下班还有 " + Engine.Duration(r.SecondsToNext, true);
                case WorkState.Overtime: return hide ? "加班中，萝卜加倍。" : "已加班 " + Engine.Duration(r.OvertimeSeconds, true) + "，按 " + _s.OvertimeRate + " 倍计";
                default: return "今天的班已经上完了，明天继续";
            }
        }

        void Tick(bool force)
        {
            var now = Now();
            long sec = (long)(now - DateTime.MinValue).TotalSeconds;
            if (!force && sec == _lastSecond) return;
            _lastSecond = sec;
            var r = Engine.Compute(_s, now);
            _snap = r;
            string c = _s.Currency;

            _clock.Text = now.ToString("yyyy-MM-dd") + "  " + Week[(int)now.DayOfWeek] + now.ToString("HH:mm:ss");

            string st, mini;
            Brush color;
            switch (r.State)
            {
                case WorkState.RestDay: st = "休息日"; color = Ui.B("#7E9A63"); mini = "休息"; break;
                case WorkState.BeforeWork: st = "还没上班"; color = Ui.Muted; mini = "未上班"; break;
                case WorkState.Lunch: st = "午休中"; color = Ui.B("#6E8F52"); mini = "午休"; break;
                case WorkState.Working: st = "上班中"; color = Ui.Blue; mini = Engine.Duration(r.SecondsToNext, false) + "后下班"; break;
                case WorkState.Overtime: st = "加班中"; color = Ui.Carrot; mini = "加班中"; break;
                default: st = "已下班"; color = Ui.MutedStrong; mini = "已下班"; break;
            }
            _status.Text = st; _status.Foreground = color;
            _sub.Text = SubText(r);
            _miniStatus.Text = mini; _miniDot.Background = color;

            _caption.Text = CaptionText(r);
            _countdownLabel.Text = _s.HideCountdown ? "显示倒计时" : "隐藏倒计时 · 专注一会儿";

            _wish.Text = Engine.WishText(_s, r);

            double pc = r.Progress * 100;
            _progressText.Text = "今日工时进度 " + (pc > 99.95 ? "100" : pc.ToString("F1")) + "%";
            _progressRight.Text = r.Progress >= 0.999 ? "今日完成" : "还剩 " + ((1 - Math.Min(1, r.Progress)) * 100).ToString("F0") + "%";
            _hoursText.Text = "已工作 " + (r.PaidElapsed / 3600).ToString("F2") + " / " + (r.PaidSeconds / 3600).ToString("F2") + " 小时";
            _barTrack.Visibility = r.State == WorkState.RestDay ? Visibility.Collapsed : Visibility.Visible;

            double month = Engine.MonthEarned(_s, now, r.Earned);
            _month.Text = _s.Masked ? c + "****" : c + month.ToString("N2");

            if (_lastState.HasValue && _lastState != r.State && _s.OffWorkNotify)
            {
                if (r.State == WorkState.AfterWork || (r.State == WorkState.Overtime && _lastState == WorkState.Working))
                    Notify("下班啦！", "今天赚到 " + c + r.Earned.ToString("F2") + "，牛马可以下线了。");
                else if (r.State == WorkState.Working && _lastState == WorkState.BeforeWork)
                    Notify("开始计薪", "新的一天，钱开始到账了。");
            }
            _lastState = r.State;
            if (_s.Masked) { _mask.Text = c + " ****.**"; _miniMask.Text = _mask.Text; }
            Animate();
            SyncAnimation();
            OnRender(null, null);
        }

        void Notify(string title, string text)
        {
            try { _tray.ShowBalloonTip(5000, title, text, Forms.ToolTipIcon.Info); } catch { }
        }

        void SnapToEdges()
        {
            ApplyScale();
            var wa = CurrentWorkArea();
            const double d = 18;
            if (Math.Abs(Left - wa.Left) < d) Left = wa.Left;
            if (Math.Abs(Top - wa.Top) < d) Top = wa.Top;
            if (Math.Abs(Left + ActualWidth - wa.Right) < d) Left = wa.Right - ActualWidth;
            if (Math.Abs(Top + ActualHeight - wa.Bottom) < d) Top = wa.Bottom - ActualHeight;
        }

        void ToggleMini()
        {
            double right = Left + ActualWidth;
            _s.Mini = !_s.Mini;
            ApplyVisual(_s);
            Tick(true);
            UpdateLayout();
            if (right >= CurrentWorkArea().Right - 30) Left = right - ActualWidth;
            KeepOnScreen();
            SavePosition();
        }

        void CycleTheme()
        {
            _s.Skin = (Theme.Normalize(_s.Skin) + 1) % Theme.All.Length;
            ApplyVisual(_s);
            Tick(true);
            _s.Save();
        }

        void ToggleMask() { _s.Masked = !_s.Masked; ApplyMask(); Tick(true); _s.Save(); }

        void ToggleTopmost()
        {
            _s.Topmost = !_s.Topmost;
            ApplyVisual(_s);
            _s.Save();
        }

        void TogglePause()
        {
            _paused = !_paused;
            _pauseLabel.Text = _paused ? "继续" : "暂停";
            _pauseGlyph.Text = Glyph(_paused ? 0xE768 : 0xE769);
            Tick(true);
        }

        void ToggleCountdown()
        {
            _s.HideCountdown = !_s.HideCountdown;
            _s.Save();
            Tick(true);
        }

        void ToggleClickThrough()
        {
            _s.ClickThrough = !_s.ClickThrough;
            Native.SetExStyle(this, Native.WS_EX_TRANSPARENT, _s.ClickThrough);
            _s.Save();
            Notify(_s.ClickThrough ? "鼠标穿透已开启" : "鼠标穿透已关闭",
                _s.ClickThrough ? "窗口不再响应鼠标，按 Ctrl+Alt+T 或托盘菜单恢复。" : "可以拖动和点击窗口了。");
        }

        void ToggleVisible() { if (IsVisible) HideToTray(); else ShowFromTray(); }
        void HideToTray() { Hide(); SyncAnimation(); }

        void SyncAnimation()
        {
            if (_anim == null) return;
            _anim.SetActive(!_paused && !_s.Mini && IsVisible);
        }

        void ShowFromTray()
        {
            Show();
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Activate();
            Tick(true);
        }

        void OpenSettings()
        {
            if (_settingsWin != null) { _settingsWin.Activate(); return; }
            _settingsWin = new SettingsWindow(_s.Clone(), this);
            _settingsWin.Closed += (o, e) => _settingsWin = null;
            _settingsWin.Show();
            _settingsWin.Activate();
        }

        public AppSettings Current { get { return _s; } }

        void CopyReport()
        {
            var now = Now();
            var r = Engine.Compute(_s, now);
            string c = _s.Currency;
            string txt = "【" + _title.Text + " · 今日战报】" + now.ToString("yyyy-MM-dd") + "\n" +
                "状态：" + _status.Text + "\n今日已赚：" + c + r.Earned.ToString("F2") + " / " + c + r.DailyPay.ToString("F2") +
                "（进度 " + (r.Progress * 100).ToString("F1") + "%）\n" + "时薪：" + c + r.HourlyPay.ToString("F2") + "\n" + _wish.Text;
            try { Clipboard.SetText(txt); Notify("战报已复制", "可以直接粘贴给同事了。"); } catch { }
        }

        void ExitApp()
        {
            _exiting = true;
            SavePosition();
            _tray.Visible = false;
            _tray.Dispose();
            var h = new WindowInteropHelper(this).Handle;
            for (int i = 1; i <= 3; i++) Native.UnregisterHotKey(h, i);
            Application.Current.Shutdown();
        }
    }
}
