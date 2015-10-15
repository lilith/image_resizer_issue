using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ImageResizer.Configuration;
using ImageResizer.Plugins.FastScaling;
using ImageResizer.Plugins.SimpleFilters;
using ImageResizer.Plugins.Watermark;
using NLog;
using NUnit.Framework;
using ImageResizer.Plugins.Basic;

namespace ImageResizer.Demo
{
    internal class ImageTests : Assert
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static byte[] GetBytesFromBitmap(Bitmap bitmap, ImageFormat format) {
            using (var mem = new MemoryStream()) {
                bitmap.Save(mem, format);
                return mem.ToArray();
            }
        }

        /// <summary>
        /// this is not working but SHOULD work - alpha blending will NOT be applied to the watermark
        /// 
        /// the operation takes around 860ms without fastscale and 
        /// </summary>
        [Test]
        public void AlphaChannelNOTWorking() {
            File.WriteAllBytes("alpha-not-working-in-watermark.jpg", ScaleDownAndBrand(new Config()));
        }

        [Test]
        public void AlphaChannelWorkingProperly() {
            File.WriteAllBytes("alpha-working-in-watermark.jpg", ScaleDownAndBrand(Config.Current));
        }

        /// <summary>
        /// the diagnostics has the following issues:
        /// 
        /// - there is no license (and we don't know how to add it via the managed API)
        /// - it contains an
        /// 
        /// </summary>
        [Test]
        public void DiagnosticsContainsErrors() {
            var config = new Config();
            new FastScalingPlugin().Install(config);
            new SimpleFilters().Install(config);
            new WatermarkPlugin().Install(config);
            var page = config.GetDiagnosticsPage();
            IsFalse(page.Contains("Error checking for issues"), "null pointer");
            IsTrue(page.Contains("You do not have any license keys installed."), "license missing but no way to add it?");

            new StaticLicenseProvider("resizer.apphb.com(R4Performance includes R4Performance):RG9tYWluOiByZXNpemVyLmFwcGhiLmNvbQpPd25lcjogTmF0aGFuYWVsIEpvbmVzCklzc3VlZDogMjAxNS0wNS0wMVQxNTowNzo1NloKRm" + 
                "VhdHVyZXM6IFI0UGVyZm9ybWFuY2U=:oWv2YlAkzTEWcaJ6fPMEsweTNh9Bt5evhjWVNHuXtiRNl22sSS3OB/XE69NsSx8kEs1ExSwzvjwPx95paQyxGsTDigdh/UCkh7TCUyIECX7pI2JtA5f3KkFzfwmISIE8d14Kyf3ijO6s2HI1A1obbH5Iuc" + 
                "yaDJLQBCSrykxJK6JM4NOM82UbAUfwXRCnjWw2frwtBDp9rezJ46iQ80BXxTJ1LXlSqBry5z7bdSZtcP2k8L+Zp3t+9Blfl2k6z0um06kDa7RkPnmfwKCYTU+HbPQ2qDfGvcNaRC6XEa17ztTn52T6hErS7AJKIZ4OKxvw3olLmmVjEg+LiuKo7NVmmQ==").Install(config);

            IsFalse(config.GetDiagnosticsPage().Contains("You do not have any license keys installed."), "license missing but no way to add it?");

        }

        private static byte[] ScaleDownAndBrand(Config config)
        {
            new FastScalingPlugin().Install(config);
            new SimpleFilters().Install(config);
            var wp = new WatermarkPlugin();
            wp.Install(config);
            // get and configure watermark
            File.WriteAllBytes("wm.png", GetBytesFromBitmap(Resources.atp, ImageFormat.Png));
            var watermark = new ImageLayer(config);
            watermark.Path = "wm.png";
            watermark.ImageQuery["fastscale"] = "true";
            watermark.ImageQuery["s.alpha"] = "0.2";
            wp.NamedWatermarks["foo"] = new Layer[] { watermark };
            // get source image
            var bytes = GetBytesFromBitmap(Resources.castrol_large, ImageFormat.Jpeg);
            var buffer = new byte[40000000]; // oversized buffer to avoid reallocations
                                             // transform

            using (var output = new MemoryStream(buffer))
            {
                var at = StartStopwatch();
                var instructions = new Instructions();
                instructions.OutputFormat = OutputFormat.Jpeg;
                instructions["fastscale"] = "true";
                instructions.Width = 800;
                instructions.Height = 800;
                instructions.Watermark = "foo";
                config.Build(new ImageJob(new MemoryStream(bytes), output, instructions, true, false));
                Log.Debug(at.ElapsedMilliseconds);
                return output.ToArray();
            }

            return null;
        }

        private static Stopwatch StartStopwatch() {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            return stopwatch;
        }
    }
}