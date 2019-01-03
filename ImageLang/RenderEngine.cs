using ImageLang.Lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Gdi = System.Drawing;

namespace ImageLang
{
    internal class RenderEngine : IDisposable
    {
        public static readonly Gdi.Imaging.PixelFormat RequiredPixelFormat = Gdi.Imaging.PixelFormat.Format32bppArgb;

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
                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

            return targetBitmap;
        }

        public void Dispose()
        {
            this.sourceBitmap?.Dispose();
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

            public ColorArgb GetPixel(int x, int y)
            {
                if (x >= 0 && x < this.sourceData.Width
                && y >= 0 && y < this.sourceData.Height)
                    return this.pSourcePixels[y * sourceData.Width + x];

                return ColorArgb.FromArgb(0, 0, 0, 0);
            }

            public void SetPixel(int x, int y, ColorArgb argb)
            {
                if (x >= 0 && x < this.targetData.Width
                && y >= 0 && y < this.targetData.Height)
                    this.pTargetPixels[y * targetData.Width + x] = argb;
            }

            public ColorArgb Convolute(int x, int y, int radius, int length, double[] kernel)
            {
                var kernelSum = 0.0;
                var r = 0.0;
                var g = 0.0;
                var b = 0.0;
                byte a = 255;
                var kernelIndex = 0;

                for (int kernelY = 0, sourceY = y - radius; kernelY < length; kernelY++, sourceY++)
                {
                    var pSource = this.pSourcePixels + (sourceY * sourceData.Width + x - radius);

                    for (int kernelX = 0, sourceX = x - radius; kernelX < length; kernelX++, sourceX++)
                    {
                        if (sourceX >= 0 && sourceX < this.source.Width
                        && sourceY >= 0 && sourceY < this.source.Height)
                        {
                            var value = kernel[kernelIndex];
                            var px = *pSource;
                            r += value * px.R;
                            g += value * px.G;
                            b += value * px.B;
                            kernelSum += value;

                            if (sourceX == x && sourceY == y)
                                a = px.A;
                        }

                        kernelIndex++;
                        pSource++;
                    }
                }

                if (kernelSum == 0.0)
                    return ColorArgb.FromArgb(a, Clamp(r), Clamp(g), Clamp(b));

                return ColorArgb.FromArgb(a, Clamp(r / kernelSum), Clamp(g / kernelSum), Clamp(b / kernelSum));
            }

            public void Blt(int x, int y, int width, int height)
            {
                if (x == 0 && y == 0 && width == this.sourceData.Width && height == this.sourceData.Height)
                {
                    var size = this.sourceData.Height * this.sourceData.Stride;
                    Buffer.MemoryCopy(this.pTargetPixels, this.pSourcePixels, size, size);
                }
                else
                {
                    for (var bottom = y + height; y < bottom; y++)
                    {
                        Buffer.MemoryCopy(
                            this.pTargetPixels + (y * this.targetData.Width + x),
                            this.pSourcePixels + (y * this.sourceData.Width + x),
                            width * sizeof(ColorArgb),
                            width * sizeof(ColorArgb));
                    }
                }
            }

            byte Clamp(double d)
            {
                if (d < 0)
                    return 0;
                if (d > 255)
                    return 255;
                return (byte)(d + 0.5);
            }
        }
    }
}
