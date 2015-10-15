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
            IsTrue(page.Contains("ConfigChecker(Error):	Error checking for issues: System.NullReferenceException: Object reference not set to an instance of an object."), "null pointer");
            IsTrue(page.Contains("You do not have any license keys installed."), "license missing but no way to add it?");
        }

        private static byte[] ScaleDownAndBrand(Config config) {
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
            wp.NamedWatermarks["foo"] = new Layer[] {watermark};
            // get source image
            var bytes = GetBytesFromBitmap(Resources.castrol_large, ImageFormat.Jpeg);
            var buffer = new byte[40000000]; // oversized buffer to avoid reallocations
            // transform
            using (var output = new MemoryStream(buffer)) {
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
        }

        private static Stopwatch StartStopwatch() {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            return stopwatch;
        }
    }
}