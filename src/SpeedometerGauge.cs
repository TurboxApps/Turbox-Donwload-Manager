using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace IDMClone
{
    /// <summary>
    /// High-End Neon Gaming Speedometer Gauge matching sports-car digital tachometers.
    /// Features:
    /// - 270-degree sweeping circular track with glowing golden amber neon gradient
    /// - Intelligent non-linear dynamic range curve (0 to 100+ MB/s)
    /// - Precision speed markers (0, 1, 5, 15, 40, 100+ MB/s)
    /// - Silky-smooth 40 FPS interpolated sports-car tapered needle
    /// - Layered metallic golden pivot hub
    /// - Digital speed badge pill with live status (PAUSED, RESUMING, COMPLETED, or live MB/s)
    /// </summary>
    public class SpeedometerGauge : Control
    {
        private float _targetProgress = 0f;
        private float _currentProgress = 0f;
        private string _speedDisplay = "0 B/s";
        private Timer _animTimer;

        private struct DialMark
        {
            public string Label;
            public float Progress;
        }

        private static readonly DialMark[] DialMarks = new DialMark[]
        {
            new DialMark { Label = "0", Progress = 0.0f },
            new DialMark { Label = "1", Progress = 0.15f },
            new DialMark { Label = "5", Progress = 0.37f },
            new DialMark { Label = "15", Progress = 0.63f },
            new DialMark { Label = "40", Progress = 0.85f },
            new DialMark { Label = "100+", Progress = 1.0f }
        };

        public float MaxScale = 1000f;

        public SpeedometerGauge()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            this.Size = new Size(205, 215);
            this.BackColor = Color.FromArgb(11, 15, 23);

            _animTimer = new Timer();
            _animTimer.Interval = 25; // 40 FPS smooth needle animation
            _animTimer.Tick += (s, e) =>
            {
                float diff = _targetProgress - _currentProgress;
                if (Math.Abs(diff) < 0.003f)
                {
                    _currentProgress = _targetProgress;
                    _animTimer.Stop();
                }
                else
                {
                    _currentProgress += diff * 0.20f;
                }
                this.Invalidate();
            };
        }

        public void SetSpeed(string speedText)
        {
            if (string.IsNullOrWhiteSpace(speedText))
            {
                speedText = "0 B/s";
            }

            _speedDisplay = speedText.Trim();

            float mbSec = ParseSpeedToMbPerSec(_speedDisplay);
            _targetProgress = MapSpeedToProgress(mbSec);

            if (!_animTimer.Enabled)
            {
                _animTimer.Start();
            }
            this.Invalidate();
        }

        private float ParseSpeedToMbPerSec(string text)
        {
            try
            {
                string upper = text.ToUpperInvariant();
                if (upper.Contains("PAUSED") || upper.Contains("STOPPED")) return 0f;
                if (upper.Contains("100% DONE") || upper.Contains("FINISHED") || upper.Contains("COMPLETE")) return 100f;

                Match m = Regex.Match(text, @"([\d\.]+)");
                if (!m.Success) return 0f;

                float num = 0f;
                if (!float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out num))
                    return 0f;

                if (upper.Contains("GB/S") || upper.Contains("GIB/S")) return num * 1024f;
                if (upper.Contains("MB/S") || upper.Contains("MIB/S")) return num;
                if (upper.Contains("KB/S") || upper.Contains("KIB/S")) return num / 1024f;
                if (upper.Contains("B/S") || upper.Contains("B/SEC")) return num / (1024f * 1024f);

                // If in Mbps
                if (upper.Contains("GBPS")) return (num * 1000f) / 8f;
                if (upper.Contains("MBPS")) return num / 8f;
                if (upper.Contains("KBPS")) return (num / 1000f) / 8f;

                return num;
            }
            catch { return 0f; }
        }

        private float MapSpeedToProgress(float mbSec)
        {
            if (mbSec <= 0.01f) return 0f;
            if (mbSec < 1.0f) return mbSec * 0.15f; // 0 to 1 MB/s -> 0 to 15%
            if (mbSec < 5.0f) return 0.15f + ((mbSec - 1.0f) / 4.0f) * 0.22f; // 1 to 5 MB/s -> 15% to 37%
            if (mbSec < 15.0f) return 0.37f + ((mbSec - 5.0f) / 10.0f) * 0.26f; // 5 to 15 MB/s -> 37% to 63%
            if (mbSec < 40.0f) return 0.63f + ((mbSec - 15.0f) / 25.0f) * 0.22f; // 15 to 40 MB/s -> 63% to 85%
            return Math.Min(1.0f, 0.85f + ((mbSec - 40.0f) / 60.0f) * 0.15f); // 40 to 100+ MB/s -> 85% to 100%
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Fill background matching container
            using (Brush bgBrush = new SolidBrush(this.BackColor))
            {
                g.FillRectangle(bgBrush, this.ClientRectangle);
            }

            int w = this.Width;
            int h = this.Height;

            float cx = w / 2f;
            float cy = h / 2f - 14f;
            float radius = Math.Min(cx - 16f, cy - 8f);
            if (radius < 40f) radius = 40f;

            float startAngle = 135f;
            float sweepAngle = 270f;
            float trackWidth = 11f;

            RectangleF arcRect = new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2);

            // 1. Draw Inactive Dark Track Arc
            using (Pen trackPen = new Pen(Color.FromArgb(24, 32, 46), trackWidth))
            {
                trackPen.StartCap = LineCap.Round;
                trackPen.EndCap = LineCap.Round;
                g.DrawArc(trackPen, arcRect, startAngle, sweepAngle);
            }

            // 2. Draw Active Glowing Progress Arc (Neon Golden Amber Gradient)
            float activeSweep = sweepAngle * _currentProgress;
            if (activeSweep > 0.5f)
            {
                // Soft Neon Amber Aura Glow
                using (Pen glowPen = new Pen(Color.FromArgb(50, 245, 158, 11), trackWidth + 6))
                {
                    glowPen.StartCap = LineCap.Round;
                    glowPen.EndCap = LineCap.Round;
                    g.DrawArc(glowPen, arcRect, startAngle, activeSweep);
                }

                // Gradient Active Stroke (Golden Amber to Bright Warm Gold)
                using (LinearGradientBrush lgb = new LinearGradientBrush(
                    new PointF(cx - radius, cy + radius),
                    new PointF(cx + radius, cy - radius),
                    Color.FromArgb(245, 158, 11),
                    Color.FromArgb(251, 191, 36)))
                using (Pen activePen = new Pen(lgb, trackWidth))
                {
                    activePen.StartCap = LineCap.Round;
                    activePen.EndCap = LineCap.Round;
                    g.DrawArc(activePen, arcRect, startAngle, activeSweep);
                }
            }

            // 3. Draw Speed Dial Numbers (0, 1, 5, 15, 40, 100+ MB/s)
            using (Font numFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                float numRadius = radius - 18f;
                for (int i = 0; i < DialMarks.Length; i++)
                {
                    var mark = DialMarks[i];
                    float angleDeg = startAngle + sweepAngle * mark.Progress;
                    double angleRad = angleDeg * Math.PI / 180.0;

                    float nx = cx + (float)(numRadius * Math.Cos(angleRad));
                    float ny = cy + (float)(numRadius * Math.Sin(angleRad));

                    Color numCol = (mark.Progress <= _currentProgress && _currentProgress > 0.02f)
                        ? Color.FromArgb(254, 240, 138)
                        : Color.FromArgb(148, 163, 184);

                    using (Brush numBrush = new SolidBrush(numCol))
                    {
                        SizeF sz = g.MeasureString(mark.Label, numFont);
                        g.DrawString(mark.Label, numFont, numBrush, nx - sz.Width / 2f, ny - sz.Height / 2f);
                    }
                }
            }

            // Subtitle: MB/s unit indicator in upper dial
            using (var f = new Font("Segoe UI", 7F, FontStyle.Bold))
            using (var b = new SolidBrush(Color.FromArgb(100, 116, 139)))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString("SPEED (MB/s)", f, b, cx, cy - 24f, sf);
            }

            // 4. Draw Precision Tapered Sports Needle
            float needleAngleDeg = startAngle + activeSweep;
            double needleAngleRad = needleAngleDeg * Math.PI / 180.0;
            float needleLen = radius - 24f;

            float tipX = cx + (float)(needleLen * Math.Cos(needleAngleRad));
            float tipY = cy + (float)(needleLen * Math.Sin(needleAngleRad));

            double perpRad = (needleAngleDeg + 90) * Math.PI / 180.0;
            float baseHalf = 4.0f;
            float b1x = cx + (float)(baseHalf * Math.Cos(perpRad));
            float b1y = cy + (float)(baseHalf * Math.Sin(perpRad));
            float b2x = cx - (float)(baseHalf * Math.Cos(perpRad));
            float b2y = cy - (float)(baseHalf * Math.Sin(perpRad));

            PointF[] needlePts = new PointF[] {
                new PointF(b1x, b1y),
                new PointF(tipX, tipY),
                new PointF(b2x, b2y)
            };

            using (Brush needleBrush = new SolidBrush(Color.FromArgb(254, 240, 138)))
            {
                g.FillPolygon(needleBrush, needlePts);
            }
            using (Pen needleBorder = new Pen(Color.FromArgb(245, 158, 11), 1f))
            {
                g.DrawPolygon(needleBorder, needlePts);
            }

            // 5. Center Pivot / Hub
            float hubRadius = 12f;
            using (Brush hubShadow = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                g.FillEllipse(hubShadow, cx - hubRadius - 1, cy - hubRadius + 2, (hubRadius + 1) * 2, (hubRadius + 1) * 2);
            }
            using (Brush hubOuter = new SolidBrush(Color.FromArgb(30, 41, 59)))
            {
                g.FillEllipse(hubOuter, cx - hubRadius, cy - hubRadius, hubRadius * 2, hubRadius * 2);
            }
            using (Pen hubRing = new Pen(Color.FromArgb(245, 158, 11), 1.5f))
            {
                g.DrawEllipse(hubRing, cx - hubRadius, cy - hubRadius, hubRadius * 2, hubRadius * 2);
            }
            using (Brush hubInner = new SolidBrush(Color.White))
            {
                g.FillEllipse(hubInner, cx - (hubRadius - 4), cy - (hubRadius - 4), (hubRadius - 4) * 2, (hubRadius - 4) * 2);
            }

            // 6. Digital Speed Badge Pill (Bottom Center)
            string display = string.IsNullOrWhiteSpace(_speedDisplay) ? "0 B/s" : _speedDisplay;
            using (Font speedFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            {
                SizeF ssz = g.MeasureString(display, speedFont);
                float pillW = Math.Max(98f, ssz.Width + 24f);
                float pillH = ssz.Height + 8f;
                float pillX = cx - pillW / 2f;
                float pillY = cy + radius - 4f;

                using (GraphicsPath pillPath = new GraphicsPath())
                {
                    float pr = pillH / 2f;
                    pillPath.AddArc(pillX, pillY, pr * 2, pr * 2, 90, 180);
                    pillPath.AddArc(pillX + pillW - pr * 2, pillY, pr * 2, pr * 2, 270, 180);
                    pillPath.CloseFigure();

                    Color pillBgCol = Color.FromArgb(16, 22, 34);
                    Color pillBorderCol = Color.FromArgb(245, 158, 11);
                    Color textCol = Color.FromArgb(251, 191, 36);

                    if (display.IndexOf("PAUSED", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        pillBgCol = Color.FromArgb(32, 22, 10);
                        pillBorderCol = Color.FromArgb(245, 158, 11);
                        textCol = Color.FromArgb(251, 191, 36);
                    }
                    else if (display.IndexOf("DONE", StringComparison.OrdinalIgnoreCase) >= 0 || display.IndexOf("FINISH", StringComparison.OrdinalIgnoreCase) >= 0 || display.IndexOf("COMPLETE", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        pillBgCol = Color.FromArgb(10, 32, 20);
                        pillBorderCol = Color.FromArgb(34, 197, 94);
                        textCol = Color.FromArgb(74, 222, 128);
                    }
                    else if (display.IndexOf("RESUMING", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        pillBgCol = Color.FromArgb(10, 26, 44);
                        pillBorderCol = Color.FromArgb(14, 165, 233);
                        textCol = Color.FromArgb(56, 189, 248);
                    }

                    using (Brush pillBg = new SolidBrush(pillBgCol))
                    using (Pen pillBorder = new Pen(pillBorderCol, 1.3f))
                    {
                        g.FillPath(pillBg, pillPath);
                        g.DrawPath(pillBorder, pillPath);
                    }

                    using (Brush speedBrush = new SolidBrush(textCol))
                    {
                        g.DrawString(display, speedFont, speedBrush, cx - ssz.Width / 2f, pillY + 4f);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _animTimer != null)
            {
                _animTimer.Stop();
                _animTimer.Dispose();
                _animTimer = null;
            }
            base.Dispose(disposing);
        }
    }
}
