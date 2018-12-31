using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FeatherSharp.ComponentModel;
using Gdi = System.Drawing;

namespace ImageLang
{
    [Feather(FeatherAction.NotifyPropertyChanged)]
    public class MainWindowModel : NotifyPropertyChanged
    {
        public ImageSource TargetBitmap { get; set; }

        public string Source { get; set; }

        byte[] bitmapBytes;

        public void Load(Stream stream)
        {
            this.bitmapBytes = new byte[(int)stream.Length];
            if (stream.Read(bitmapBytes, 0, (int)stream.Length) < stream.Length)
                throw new IOException("Could not read all bytes from stream");

            stream.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            TargetBitmap = bitmapImage;
        }

        public void Render()
        {
            var program = Compile();

            using (var stream = new MemoryStream(this.bitmapBytes))
            using (var engine = new RenderEngine(stream))
            {
                Gdi.Bitmap bmp;

                try
                {
                    bmp = engine.Render(program);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }

                using (bmp)
                {
                    var hbmp = bmp.GetHbitmap();
                    var options = BitmapSizeOptions.FromEmptyOptions();
                    var targetBitmap = Imaging.CreateBitmapSourceFromHBitmap(hbmp, IntPtr.Zero, Int32Rect.Empty, options);
                    targetBitmap.Freeze();
                    TargetBitmap = targetBitmap;
                }
            }
        }

        Lib.Program Compile()
        {
            if (String.IsNullOrWhiteSpace(Source))
                return null;

            var compiler = new Lib.Compiler();
            var result = compiler.Compile(Source);

            if (result.Success == false)
            {
                MessageBox.Show($"Error at line {result.Error.Item1}: {result.Error.Item2}");
                return null;
            }

            return result.Program;
        }
    }
}
