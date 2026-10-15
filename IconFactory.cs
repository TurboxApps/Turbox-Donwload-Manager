using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace IDMClone
{
    public static class IconFactory
    {
        public static Bitmap CreateAddUrlIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color green = Color.FromArgb(16, 163, 127);
                using (Pen p = new Pen(green, 3f))
                {
                    p.StartCap = LineCap.Round;
                    p.EndCap = LineCap.Round;
                    // Draw outer rounded box or plus
                    g.DrawLine(p, 16, 5, 16, 27);
                    g.DrawLine(p, 5, 16, 27, 16);
                }
                using (Pen pOuter = new Pen(Color.FromArgb(100, 16, 163, 127), 1.5f))
                {
                    g.DrawRectangle(pOuter, 2, 2, 27, 27);
                }
            }
            return bmp;
        }

        public static Bitmap CreateResumeIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color grey = Color.FromArgb(120, 144, 156);
                using (Pen p = new Pen(grey, 2.2f))
                {
                    g.DrawEllipse(p, 4, 4, 23, 23);
                    // Down arrow
                    g.DrawLine(p, 16, 9, 16, 21);
                    g.DrawLine(p, 11, 16, 16, 21);
                    g.DrawLine(p, 21, 16, 16, 21);
                }
            }
            return bmp;
        }

        public static Bitmap CreateStopIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color grey = Color.FromArgb(120, 144, 156);
                using (Pen p = new Pen(grey, 2.2f))
                {
                    g.DrawEllipse(p, 4, 4, 23, 23);
                    g.DrawRectangle(p, 11, 11, 9, 9);
                }
            }
            return bmp;
        }

        public static Bitmap CreateStopAllIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color grey = Color.FromArgb(120, 144, 156);
                using (Pen p = new Pen(grey, 2f))
                {
                    // Raised hand palm shape
                    g.DrawRectangle(p, 10, 13, 12, 12);
                    g.DrawLine(p, 11, 13, 11, 6);
                    g.DrawLine(p, 14, 13, 14, 5);
                    g.DrawLine(p, 18, 13, 18, 6);
                    g.DrawLine(p, 21, 13, 21, 9);
                }
            }
            return bmp;
        }

        public static Bitmap CreateDeleteIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color red = Color.FromArgb(225, 29, 72);
                using (Pen p = new Pen(red, 2f))
                {
                    // Trash can lid
                    g.DrawLine(p, 7, 9, 25, 9);
                    g.DrawLine(p, 13, 6, 19, 6);
                    // Trash can body
                    Point[] body = new Point[] {
                        new Point(9, 9),
                        new Point(11, 26),
                        new Point(21, 26),
                        new Point(23, 9)
                    };
                    g.DrawPolygon(p, body);
                    g.DrawLine(p, 14, 13, 14, 22);
                    g.DrawLine(p, 18, 13, 18, 22);
                }
            }
            return bmp;
        }

        public static Bitmap CreateDeleteCompletedIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color purple = Color.FromArgb(168, 85, 247);
                using (Pen p = new Pen(purple, 2f))
                {
                    // Trash can with checkmark
                    g.DrawLine(p, 7, 9, 25, 9);
                    g.DrawLine(p, 13, 6, 19, 6);
                    Point[] body = new Point[] {
                        new Point(9, 9),
                        new Point(11, 26),
                        new Point(21, 26),
                        new Point(23, 9)
                    };
                    g.DrawPolygon(p, body);
                    // Checkmark
                    g.DrawLine(p, 12, 17, 15, 20);
                    g.DrawLine(p, 15, 20, 20, 14);
                }
            }
            return bmp;
        }

        public static Bitmap CreateOptionsIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color purple = Color.FromArgb(139, 92, 246);
                using (Pen p = new Pen(purple, 2.2f))
                {
                    // Gear
                    g.DrawEllipse(p, 10, 10, 12, 12);
                    // Teeth
                    for (int i = 0; i < 8; i++)
                    {
                        double angle = i * Math.PI / 4.0;
                        int x1 = (int)(16 + 8 * Math.Cos(angle));
                        int y1 = (int)(16 + 8 * Math.Sin(angle));
                        int x2 = (int)(16 + 12 * Math.Cos(angle));
                        int y2 = (int)(16 + 12 * Math.Sin(angle));
                        g.DrawLine(p, x1, y1, x2, y2);
                    }
                }
            }
            return bmp;
        }

        public static Bitmap CreateSchedulerIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color orange = Color.FromArgb(249, 115, 22);
                using (Pen p = new Pen(orange, 2f))
                {
                    // Alarm clock
                    g.DrawEllipse(p, 7, 9, 18, 18);
                    // Bells
                    g.DrawArc(p, 5, 5, 8, 8, 180, 140);
                    g.DrawArc(p, 19, 5, 8, 8, 220, 140);
                    // Hands
                    g.DrawLine(p, 16, 18, 16, 13);
                    g.DrawLine(p, 16, 18, 20, 18);
                    // Feet
                    g.DrawLine(p, 9, 25, 7, 28);
                    g.DrawLine(p, 23, 25, 25, 28);
                }
            }
            return bmp;
        }

        public static Bitmap CreateStartQueueIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color blue = Color.FromArgb(14, 165, 233);
                using (Pen p = new Pen(blue, 2f))
                {
                    // Front sheet & back sheet
                    g.DrawRectangle(p, 6, 5, 15, 18);
                    g.DrawRectangle(p, 11, 10, 15, 18);
                    // Down arrow in front
                    g.DrawLine(p, 18, 14, 18, 22);
                    g.DrawLine(p, 15, 19, 18, 22);
                    g.DrawLine(p, 21, 19, 18, 22);
                }
            }
            return bmp;
        }

        public static Bitmap CreateStopQueueIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color purple = Color.FromArgb(168, 85, 247);
                using (Pen p = new Pen(purple, 2f))
                {
                    g.DrawRectangle(p, 6, 5, 15, 18);
                    g.DrawRectangle(p, 11, 10, 15, 18);
                    g.DrawRectangle(p, 15, 15, 7, 7);
                }
            }
            return bmp;
        }

        public static Bitmap CreateTellFriendIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Color blue = Color.FromArgb(59, 130, 246);
                using (Pen p = new Pen(blue, 2f))
                {
                    // Thumbs up
                    g.DrawRectangle(p, 6, 14, 6, 12);
                    Point[] thumb = new Point[] {
                        new Point(12, 17),
                        new Point(18, 17),
                        new Point(18, 10),
                        new Point(21, 10),
                        new Point(25, 16),
                        new Point(25, 24),
                        new Point(12, 24)
                    };
                    g.DrawPolygon(p, thumb);
                }
            }
            return bmp;
        }

        private static Image _cachedTurboXMaster = null;

        public static Image GetTurboXImage(int width = 24, int height = 24)
        {
            try
            {
                if (_cachedTurboXMaster == null)
                {
                    string p = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "turbox_icon.png");
                    if (System.IO.File.Exists(p))
                    {
                        using (var src = Image.FromFile(p))
                        {
                            _cachedTurboXMaster = new Bitmap(src);
                        }
                    }
                }

                if (_cachedTurboXMaster != null)
                {
                    Bitmap resized = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.Clear(Color.Transparent);
                        g.DrawImage(_cachedTurboXMaster, new Rectangle(0, 0, width, height));
                    }
                    return resized;
                }
            }
            catch { }

            // Fallback generated globe
            Bitmap fallback = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(fallback))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (Brush b = new SolidBrush(Color.FromArgb(6, 214, 250)))
                {
                    g.FillEllipse(b, 2, 2, width - 4, height - 4);
                }
            }
            return fallback;
        }

        public static Icon GetTurboXIcon()
        {
            try
            {
                string icoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (System.IO.File.Exists(icoPath))
                {
                    return new Icon(icoPath);
                }
                using (Bitmap bmp = (Bitmap)GetTurboXImage(32, 32))
                {
                    return Icon.FromHandle(bmp.GetHicon());
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
