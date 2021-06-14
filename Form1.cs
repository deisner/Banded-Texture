using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using devDept.Graphics;


namespace Tubes_Demo
{
    public partial class Form1 : Form
    {
        private const string TextureName = "banded_texture";

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            model1.ShowFps = true;

            // could also be removed in control Smart Tag menu in designer
            model1.ActiveViewport.Grid.Visible = false;

            // turn off the thick black lines rendered around tubes
            model1.Rendered.SilhouettesDrawingMode = silhouettesDrawingType.Never;

            // turn off black edges on tube end caps
            model1.Rendered.ShowEdges = false;


            List<Color> colors = MakeColorList();
            Material mat = new Material(TextureName, MakeBandedColorBitmap(1, colors.Count, colors));
            mat.MagnifyingFunction = textureFilteringFunctionType.Nearest;
            mat.MinifyingFunction = textureFilteringFunctionType.Nearest;
            model1.Materials.Add(mat);

            pictureBox1.Image = MakeBandedColorBitmap(pictureBox1.Width, pictureBox1.Height, colors);
            model1.Entities.Add(GetColoredTubeMesh(0, 0, 0, 2, 100));
            model1.ZoomFit();
            base.OnLoad(e);
        }

        private List<Color> MakeColorList()
        {
            Color[] colors =
            {
                Color.FromArgb(255, 0, 0, 133),
                Color.FromArgb(255, 0, 0, 244),
                Color.FromArgb(255, 0, 101, 255),
                Color.FromArgb(255, 0, 213, 255),
                Color.FromArgb(255, 69, 255, 186),
                Color.FromArgb(255, 181, 255, 74),
                Color.FromArgb(255, 255, 218, 0),
                Color.FromArgb(255, 255, 106, 0),
                Color.FromArgb(255, 250, 0, 0),
                Color.FromArgb(255, 138, 0, 0)
            };
            return colors.ToList();
        }

        private static Bitmap MakeBandedColorBitmap(int width, int height, List<Color> colors)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int nBands = colors.Count;
            PixelFormat fmt = PixelFormat.Format24bppRgb;
            Bitmap bmp = new Bitmap(width, height, fmt);

            Rectangle lockRect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(lockRect, ImageLockMode.ReadWrite, fmt);

            IntPtr ptr = bmpData.Scan0;
            int nBytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbValues = new byte[nBytes];

            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, nBytes); // can't we omit this? 

            for (int y = 0; y < height; y++)
            {
                int band = (int) Math.Floor(1.0 * nBands * y / height);

                // I think this is not needed, but ...
                if (band >= nBands)
                {
                    band = nBands - 1;
                }

                Color c = colors[band];
                for (int x = 0; x < width; x++)
                {
                    // TODO: This code assumes bmpData.Stride is positive, i.e. bitmap is top-down. Should handle bottom-up case, too
                    int offset = y * bmpData.Stride + 3 * x;
                    rgbValues[offset] = c.B;
                    rgbValues[offset + 1] = c.G;
                    rgbValues[offset + 2] = c.R;
                }
            }

            // Console.WriteLine($"bitmap width x height: {bmp.Width} x {bmp.Height}, Stride: {bmpData.Stride}, Height: {bmpData.Height}, nBytes: {nBytes}");

            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, nBytes);
            bmp.UnlockBits(bmpData);

            sw.Stop();
            Console.WriteLine($@"Bitmap created in {sw.ElapsedMilliseconds} ms");
            return bmp;
        }

        private static Mesh GetColoredTubeMesh(double cx, double cy, double cz, double radius, double length)
        {
            Circle c = new Circle(Plane.XY, new Point2D(0, 0), radius);
            Mesh m = c.ExtrudeAsMesh(length * Vector3D.AxisZ, 0.02, Mesh.natureType.RichSmooth);
            m.ApplyMaterial(TextureName, textureMappingType.Cylindrical, 1, 1);
            m.Rotate(-Math.PI / 2, Vector3D.AxisY, Point3D.Origin);
            m.Translate(cx, cy, cz);
            return m;
        }
    }
}