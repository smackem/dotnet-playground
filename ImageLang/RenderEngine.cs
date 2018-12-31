using ImageLang.Lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gdi = System.Drawing;

namespace ImageLang
{
    internal class RenderEngine : IDisposable
    {
        public readonly static Gdi.Imaging.PixelFormat RequiredPixelFormat = Gdi.Imaging.PixelFormat.Format32bppArgb;

        readonly Gdi.Bitmap sourceBitmap;

        public RenderEngine(Stream sourceBitmapStream)
        {
            var bitmap = new Gdi.Bitmap(sourceBitmapStream);
            this.sourceBitmap = BltConvert(bitmap);
        }

        public Gdi.Bitmap Render(Program program)
        {
            if (program == null)
                return BltConvert(this.sourceBitmap);

            var bmp = new Gdi.Bitmap(this.sourceBitmap.Width, this.sourceBitmap.Height);

            using (var renderTarget = new RenderSurface(this.sourceBitmap, bmp))
            {
                program.Run(renderTarget);
            }

            return bmp;
        }

        static Bitmap BltConvert(Bitmap bitmap)
        {
            var targetBitmap = new Bitmap(bitmap.Width, bitmap.Height, RequiredPixelFormat);

            using (var g = Graphics.FromImage(targetBitmap))
                g.DrawImageUnscaled(bitmap, Point.Empty);

            return targetBitmap;
        }

        public void Dispose()
        {
            if (this.sourceBitmap != null)
                this.sourceBitmap.Dispose();
        }

        unsafe class RenderSurface : IBitmap, IDisposable
        {
            readonly Gdi.Bitmap source;
            readonly Gdi.Imaging.BitmapData sourceData;
            readonly ColorArgb* pSourcePixels;

            readonly Gdi.Bitmap target;
            readonly Gdi.Imaging.BitmapData targetData;
            readonly ColorArgb* pTargetPixels;

            public RenderSurface(Gdi.Bitmap source, Gdi.Bitmap target)
            {
                this.source = source;
                this.sourceData = source.LockBits(new Gdi.Rectangle(0, 0, source.Width, source.Height), Gdi.Imaging.ImageLockMode.ReadWrite, RequiredPixelFormat);
                this.pSourcePixels = (ColorArgb*)this.sourceData.Scan0;

                this.target = target;
                this.targetData = target.LockBits(new Gdi.Rectangle(0, 0, target.Width, target.Height), Gdi.Imaging.ImageLockMode.ReadWrite, RequiredPixelFormat);
                this.pTargetPixels = (ColorArgb*)this.targetData.Scan0;
            }

            public int Width => this.source.Width;

            public int Height => this.source.Height;

            public void Dispose()
            {
                this.source.UnlockBits(this.sourceData);
                this.target.UnlockBits(this.targetData);
            }

            public unsafe ColorArgb GetPixel(int x, int y)
            {
                if (x >= 0 && x < this.sourceData.Width
                && y >= 0 && y < this.sourceData.Height)
                    return this.pSourcePixels[y * sourceData.Width + x];

                return ColorArgb.FromArgb(0, 0, 0, 0);
            }

            public unsafe void SetPixel(int x, int y, ColorArgb argb)
            {
                if (x >= 0 && x < this.targetData.Width
                && y >= 0 && y < this.targetData.Height)
                    this.pTargetPixels[y * targetData.Width + x] = argb;
            }
        }
    }
}
