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
    internal class ImageTests
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