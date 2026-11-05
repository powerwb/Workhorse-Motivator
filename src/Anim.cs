using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NiuMa
{

    public sealed class AnimClip
    {
        const int CacheSlots = 32;

        readonly byte[] _data;
        readonly int[] _off, _len;
        readonly BitmapSource[] _cache = new BitmapSource[CacheSlots];
        readonly int[] _cacheId = new int[CacheSlots];

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int DelayMs { get; private set; }
        public int Count { get { return _len.Length; } }

        AnimClip(byte[] data, int w, int h, int delay, int[] off, int[] len)
        {
            _data = data; Width = w; Height = h; DelayMs = delay; _off = off; _len = len;
            for (int i = 0; i < CacheSlots; i++) _cacheId[i] = -1;
        }

        public static AnimClip Load(string res)
        {
            try
            {
                using (Stream s = Ui.Res(res))
                {
                    if (s == null) return null;
                    byte[] data = new byte[s.Length];
                    int read = 0;
                    while (read < data.Length)
                    {
                        int n = s.Read(data, read, data.Length - read);
                        if (n <= 0) break;
                        read += n;
                    }
                    if (read != data.Length || data.Length < 28) return null;
                    if (data[0] != (byte)'N' || data[1] != (byte)'M' || data[2] != (byte)'A' || data[3] != (byte)'F') return null;
                    if (BitConverter.ToInt32(data, 4) != 1) return null;

                    int w = BitConverter.ToInt32(data, 8), h = BitConverter.ToInt32(data, 12);
                    int delay = BitConverter.ToInt32(data, 16), count = BitConverter.ToInt32(data, 20);
                    if (count <= 0 || count > 5000 || w <= 0 || h <= 0) return null;

                    int head = 24 + count * 4;
                    if (data.Length < head) return null;
                    int[] len = new int[count], off = new int[count];
                    int p = head;
                    for (int i = 0; i < count; i++)
                    {
                        len[i] = BitConverter.ToInt32(data, 24 + i * 4);
                        if (len[i] <= 0) return null;
                        off[i] = p;
                        p += len[i];
                    }
                    if (p > data.Length) return null;
                    return new AnimClip(data, w, h, delay, off, len);
                }
            }
            catch { return null; }
        }

        public BitmapSource Frame(int i)
        {
            int n = _len.Length;
            if (n == 0) return null;
            i = ((i % n) + n) % n;
            int slot = i % CacheSlots;
            if (_cacheId[slot] == i) return _cache[slot];
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = new MemoryStream(_data, _off[i], _len[i], false);
                bi.EndInit();
                bi.Freeze();
                _cache[slot] = bi;
                _cacheId[slot] = i;
                return bi;
            }
            catch { return _cacheId[slot] == i ? _cache[slot] : null; }
        }
    }

    public sealed class AnimView : Grid
    {
        const int Cross = 5;

        readonly AnimClip _clip;
        readonly Border _base, _fade;
        readonly ImageBrush _baseBrush = new ImageBrush { Stretch = Stretch.UniformToFill };
        readonly ImageBrush _fadeBrush = new ImageBrush { Stretch = Stretch.UniformToFill };
        readonly DispatcherTimer _timer;
        int _i, _fadeLeft;

        public bool Ok { get { return _clip != null; } }

        public AnimView(string res)
        {
            _clip = AnimClip.Load(res);
            _base = new Border { CornerRadius = new CornerRadius(10), Background = _baseBrush };
            _fade = new Border { CornerRadius = new CornerRadius(10), Background = _fadeBrush, Opacity = 0, IsHitTestVisible = false };
            RenderOptions.SetBitmapScalingMode(_base, BitmapScalingMode.HighQuality);
            RenderOptions.SetBitmapScalingMode(_fade, BitmapScalingMode.HighQuality);

            if (_clip == null) { Visibility = Visibility.Collapsed; return; }

            Children.Add(_base);
            Children.Add(_fade);
            _baseBrush.ImageSource = _clip.Frame(0);

            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(Math.Max(20, _clip.DelayMs)),
            };
            _timer.Tick += (o, e) => Step();
            Unloaded += (o, e) => _timer.Stop();
        }

        public void SetActive(bool on)
        {
            if (_timer == null) return;
            if (on) { if (!_timer.IsEnabled) _timer.Start(); }
            else if (_timer.IsEnabled) _timer.Stop();
        }

        void Step()
        {
            _i++;
            if (_i >= _clip.Count)
            {
                _i = 0;
                _fadeLeft = Cross;
                _fadeBrush.ImageSource = _clip.Frame(_clip.Count - 1);
            }
            _baseBrush.ImageSource = _clip.Frame(_i);

            if (_fadeLeft > 0) { _fade.Opacity = (double)_fadeLeft / (Cross + 1); _fadeLeft--; }
            else if (_fade.Opacity > 0) _fade.Opacity = 0;
        }
    }
}
