using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;

namespace WindowsFormsApp1
{
    abstract class Filters
    {
        protected abstract Color calculateNewPixelColor(Bitmap sourceImage, int x, int y);
        public int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        public Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);

            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                if (worker.CancellationPending)
                {
                    return null;
                }
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }

            return resultImage;
        }
    }
    class InvertFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor = Color.FromArgb(255 - sourceColor.R, 255 - sourceColor.G, 255 - sourceColor.B);
            return resultColor;
        }
    }
    class MatrixFilter : Filters
    {
        protected float[,] kernel = null;
        protected MatrixFilter() { }
        public MatrixFilter(float[,] kernel)
        {
            this.kernel = kernel;
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++)
            {
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + 1, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }
            }
            return Color.FromArgb(
                Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255)
                );
        }
    }
    class BlurFilter : MatrixFilter
    {
        public BlurFilter()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    kernel[i, j] = 1.0f / (float)(sizeX * sizeY);
                }
            }
        }
    }
    class GaussFilter : MatrixFilter
    {
        public void createGaussianKernel(int radius, float sigma)
        {
            int size = 2 * radius + 1;
            kernel = new float[size, size];
            float norm = 0;
            for (int i = -radius; i <= radius; i++)
                for (int j = -radius; j <= radius; j++)
                {
                    kernel[i + radius, j + radius] = (float)(Math.Exp(-(i * i + j * j) / (2 * sigma * sigma)));
                    norm += kernel[i + radius, j + radius];
                }

            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    kernel[i, j] /= norm;
                }

        }
        public GaussFilter()
        {
            createGaussianKernel(3, 2);
        }
    }
    class GrayScaleFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            double intensity = (0.299 * sourceColor.R) + (0.587 * sourceColor.G) + (0.114 * sourceColor.B);
            Color resultColor = Color.FromArgb((int)intensity, (int)intensity, (int)intensity);
            return resultColor;
        }
    }
    class SepiaFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int k = 10;
            Color sourceColor = sourceImage.GetPixel(x, y);
            double intensity = 0.299 * sourceColor.R + 0.587 * sourceColor.G + 0.114 * sourceColor.B;
            double resultR = intensity + 2 * k;
            double resultG = intensity + 0.5 * k;
            double resultB = intensity - 1 * k;
            return Color.FromArgb(Clamp((int)resultR, 0, 255), Clamp((int)resultG, 0, 255), Clamp((int)resultB, 0, 255));
        }
    }
    class AddBrightnessFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            int resultR = sourceColor.R + 30;
            int resultG = sourceColor.G + 30;
            int resultB = sourceColor.B + 30;
            return Color.FromArgb(Clamp((int)resultR, 0, 255), Clamp((int)resultG, 0, 255), Clamp((int)resultB, 0, 255));
        }
    }
    class SobelFilter : MatrixFilter
    {
        private float[,] kernelX = {
        { -1, 0, 1 },
        { -2, 0, 2 },
        { -1, 0, 1 }
    };

        private float[,] kernelY = {
        { -1, -2, -1 },
        { 0, 0, 0 },
        { 1, 2, 1 }
    };

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            float gradientX = CalculateGradient(sourceImage, x, y, kernelX);
            float gradientY = CalculateGradient(sourceImage, x, y, kernelY);

            float magnitude = (float)Math.Sqrt(gradientX * gradientX + gradientY * gradientY);

            int intensity = Clamp((int)magnitude, 0, 255);
            return Color.FromArgb(intensity, intensity, intensity);
        }

        private float CalculateGradient(Bitmap sourceImage, int x, int y, float[,] kernel)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float result = 0;

            for (int l = -radiusY; l <= radiusY; l++)
            {
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    float grayValue = (float)(0.299 * neighborColor.R + 0.587 * neighborColor.G + 0.114 * neighborColor.B);
                    result += grayValue * kernel[k + radiusX, l + radiusY];
                }
            }

            return result;
        }
    }
    class SharpenFilter : MatrixFilter
    {
        public SharpenFilter()
        {
            kernel = new float[,]
            {
            { 0, -1, 0 },
            { -1, 5, -1 },
            { 0, -1, 0 }
            };
        }
    }
    class VerticalWaveFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int k = Clamp(x + (int)(Math.Sin(2 * Math.PI * y / 60.0) * 20), 0, sourceImage.Width - 1);

            Color neighborColor = sourceImage.GetPixel(k, y);
            return neighborColor;
        }
    }

    class HorizontalWaveFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int k = Clamp(y + (int)(Math.Sin(2 * Math.PI * x / 60.0) * 20), 0, sourceImage.Height - 1);

            Color neighborColor = sourceImage.GetPixel(x, k);
            return neighborColor;
        }
    }
    class GlassFilter : Filters
    {
        private Random random = new Random();
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int offsetX = (int)(random.NextDouble() * 20 - 10);
            int offsetY = (int)(random.NextDouble() * 20 - 10);

            int newX = Clamp(x + offsetX, 0, sourceImage.Width - 1);
            int newY = Clamp(y + offsetY, 0, sourceImage.Height - 1);

            Color resultColor = sourceImage.GetPixel(newX, newY);
            return resultColor;
        }
    }
    class RotationFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int i, int j)
        {
            int x0 = sourceImage.Width / 2;
            int y0 = sourceImage.Height / 2;
            double corner = 1;
            int idI = Clamp((int)((i - x0) * Math.Cos(corner) - (j - y0) * Math.Sin(corner)) + x0, 0, sourceImage.Width - 1);
            int idJ = Clamp((int)((i - x0) * Math.Sin(corner) + (j - y0) * Math.Cos(corner)) + y0, 0, sourceImage.Height - 1);
            if (idJ == sourceImage.Height - 1) { return Color.FromArgb(0, 0, 0); }
            Color resultColor = sourceImage.GetPixel(idI, idJ);
            return resultColor;
        }
    }
    class TransferFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int i, int j)
        {
            int idI = Clamp(i, 0, sourceImage.Width - 1);
            int idJ = Clamp(j + 50, 0, sourceImage.Height - 1);
            if (idJ == sourceImage.Height - 1) { return Color.FromArgb(0, 0, 0); }
            Color resultColor = sourceImage.GetPixel(idI, idJ);
            return resultColor;
        }
    }
    class EmbossingFilter : Filters
    {
        protected float[,] kernel;
        public EmbossingFilter()
        {
            kernel = new float[,] {
            { 0, 1, 0},
            { 1, 0, -1},
            { 0, -1, 0}
        };
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int i, int j)
        {
            float resultBrightness = 0;
            int radiusJ = 3 / 2;
            int radiusI = 3 / 2;
            for (int l = -radiusJ; l <= radiusJ; l++)
            {
                for (int k = -radiusI; k <= radiusI; k++)
                {
                    int idI = Clamp(i + k, 0, sourceImage.Width - 1);
                    int idJ = Clamp(j + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idI, idJ);
                    float brightness = (neighborColor.R + neighborColor.G + neighborColor.B) / 3.0f;
                    resultBrightness += brightness * kernel[k + radiusI, l + radiusJ];
                }
            }
            resultBrightness += 128;
            int grayValue = Clamp((int)resultBrightness, 0, 255);
            return Color.FromArgb(grayValue, grayValue, grayValue);
        }
    }

    class SharraFilter : MatrixFilter
    {
        protected float[,] kernelX;
        protected float[,] kernelY;
        public SharraFilter()
        {
            kernelX = new float[,]
            {
                { 3, 0, -3 },
                { 10, 0, -10 },
                { 3, 0, -3 }
            };
            kernelY = new float[,]
            {
                { 3, 10, 3 },
                { 0, 0, 0 },
                { -3, -10, -3 }
            };
        }


        protected override Color calculateNewPixelColor(Bitmap sourceImage, int i, int j)
        {
            float resultRX = 0;
            float resultGX = 0;
            float resultBX = 0;
            float resultRY = 0;
            float resultGY = 0;
            float resultBY = 0;
            int radiusJ = 3 / 2;
            int radiusI = 3 / 2;
            for (int l = -radiusJ; l <= radiusJ; l++)
            {
                for (int k = -radiusI; k <= radiusI; k++)
                {
                    int idI = Clamp(i + k, 0, sourceImage.Width - 1);
                    int idJ = Clamp(j + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idI, idJ);
                    resultRX += neighborColor.R * kernelX[k + radiusI, l + radiusJ];
                    resultGX += neighborColor.G * kernelX[k + radiusI, l + radiusJ];
                    resultBX += neighborColor.B * kernelX[k + radiusI, l + radiusJ];
                    resultRY += neighborColor.R * kernelY[k + radiusI, l + radiusJ];
                    resultGY += neighborColor.G * kernelY[k + radiusI, l + radiusJ];
                    resultBY += neighborColor.B * kernelY[k + radiusI, l + radiusJ];
                }
            }
            float resultR = (float)Math.Sqrt(Math.Pow(resultRX, 2) + Math.Pow(resultRY, 2));
            float resultG = (float)Math.Sqrt(Math.Pow(resultGX, 2) + Math.Pow(resultGY, 2));
            float resultB = (float)Math.Sqrt(Math.Pow(resultBX, 2) + Math.Pow(resultBY, 2));
            return Color.FromArgb(
                Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255));
            throw new NotImplementedException();
        }
    }
    class PruittFilter : MatrixFilter
    {
        protected float[,] kernelX;
        protected float[,] kernelY;
        public PruittFilter()
        {
            kernelX = new float[,]
            {
                { -1, 0, 1 },
                { -1, 0, 1 },
                { -1, 0, 1 }
            };
            kernelY = new float[,]
            {
                { -1, -1, -1 },
                { 0, 0, 0 },
                { 1, 1, 1 }
            };
        }


        protected override Color calculateNewPixelColor(Bitmap sourceImage, int i, int j)
        {
            float resultRX = 0;
            float resultGX = 0;
            float resultBX = 0;
            float resultRY = 0;
            float resultGY = 0;
            float resultBY = 0;
            int radiusJ = 3 / 2;
            int radiusI = 3 / 2;
            for (int l = -radiusJ; l <= radiusJ; l++)
            {
                for (int k = -radiusI; k <= radiusI; k++)
                {
                    int idI = Clamp(i + k, 0, sourceImage.Width - 1);
                    int idJ = Clamp(j + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idI, idJ);
                    resultRX += neighborColor.R * kernelX[k + radiusI, l + radiusJ];
                    resultGX += neighborColor.G * kernelX[k + radiusI, l + radiusJ];
                    resultBX += neighborColor.B * kernelX[k + radiusI, l + radiusJ];
                    resultRY += neighborColor.R * kernelY[k + radiusI, l + radiusJ];
                    resultGY += neighborColor.G * kernelY[k + radiusI, l + radiusJ];
                    resultBY += neighborColor.B * kernelY[k + radiusI, l + radiusJ];
                }
            }
            float resultR = (float)Math.Sqrt(Math.Pow(resultRX, 2) + Math.Pow(resultRY, 2));
            float resultG = (float)Math.Sqrt(Math.Pow(resultGX, 2) + Math.Pow(resultGY, 2));
            float resultB = (float)Math.Sqrt(Math.Pow(resultBX, 2) + Math.Pow(resultBY, 2));
            return Color.FromArgb(
                Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255));
            throw new NotImplementedException();
        }
    }
    

}