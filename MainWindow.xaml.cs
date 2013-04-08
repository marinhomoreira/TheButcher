using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SharpGL.SceneGraph;
using SharpGL;

using System.IO;
using System.Drawing;


namespace TheButcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the OpenGLDraw event of the openGLControl1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="SharpGL.SceneGraph.OpenGLEventArgs"/> instance containing the event data.</param>
        private void openGLControl_OpenGLDraw(object sender, OpenGLEventArgs args)
        {
            //  Get the OpenGL object.
            OpenGL gl = openGLControl.OpenGL;
            gl.Enable(OpenGL.GL_TEXTURE_3D);
            //  Clear the color and depth buffer.
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            //  Load the identity matrix.
            gl.LoadIdentity();


            // rotate affects whatever happens after it is called (this will rotate the plane around the x-axis)
            gl.MatrixMode(OpenGL.GL_TEXTURE);
            gl.LoadIdentity();
            gl.Translate(0, 0.5, 0.5);
            gl.Rotate(rotation, 1, 0, 0);
            gl.Translate(0, -0.5, -0.5);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);

            // draw a square plane
            gl.Begin(OpenGL.GL_QUAD_STRIP);
            gl.TexCoord(0, 1, 1);
            gl.Vertex(-2, 2, 0);

            gl.TexCoord(1, 1, 1);
            gl.Vertex(2, 2, 0);

            gl.TexCoord(0, 0, .5);
            gl.Vertex(-2, -2, 0);

            gl.TexCoord(1, 0, .5);
            gl.Vertex(2, -2, 0);
            gl.End();

            // draw another square behind it
            gl.Disable(OpenGL.GL_TEXTURE_3D);
            gl.Color(.0, .0, 1);
            gl.Begin(OpenGL.GL_QUAD_STRIP);
            gl.Vertex(-2.1, 2.1, 0.1);
            gl.Vertex(2.1, 2.1, 0.1);
            gl.Vertex(-2.1, -2.1, 0.1);
            gl.Vertex(2.1, -2.1, 0.1);
            gl.End();

            //  Nudge the rotation.
            rotation += 1.0f;
        }

        /// <summary>
        /// Handles the OpenGLInitialized event of the openGLControl1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="SharpGL.SceneGraph.OpenGLEventArgs"/> instance containing the event data.</param>
        private void openGLControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            //  TODO: Initialise OpenGL here.

            //  Get the OpenGL object.
            OpenGL gl = openGLControl.OpenGL;

            //  Set the clear color.
            gl.ClearColor(0, 0, 0, 0);

            generateTexture(".\\data");
        }

        /// <summary>
        /// Handles the Resized event of the openGLControl1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="SharpGL.SceneGraph.OpenGLEventArgs"/> instance containing the event data.</param>
        private void openGLControl_Resized(object sender, OpenGLEventArgs args)
        {
            //  TODO: Set the projection matrix here.

            //  Get the OpenGL object.
            OpenGL gl = openGLControl.OpenGL;

            //  Set the projection matrix.
            gl.MatrixMode(OpenGL.GL_PROJECTION);

            //  Load the identity.
            gl.LoadIdentity();

            //  Create a perspective transformation.
            gl.Perspective(60.0f, (double)Width / (double)Height, 0.01, 100.0);

            //  Use the 'look at' helper function to position and aim the camera.
            gl.LookAt(0, 0, -5, 0, 0, 0, 0, 1, 0);
            // camera at 0, 0, -5
            // camera pointed towards 0,0,0
            // camera has 0,1,0 as up (i.e., the y-axis)

            //  Set the modelview matrix.
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
        }

        /// <summary>
        /// The current rotation.
        /// </summary>
        private float rotation = 0.0f;

        #region Texture Generation

        uint[] volTex = new uint[1];

        // generate texture from the files
        private void generateTexture(string imgDirectory)
        {
            //  Get the OpenGL object.
            OpenGL gl = openGLControl.OpenGL;

            string[] files = Directory.GetFiles(imgDirectory);
            Bitmap b = new Bitmap(files[0]);
            int w = b.Width;
            int h = b.Height;
            int depth = files.Length;
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, w, h);
            byte[] volume = new byte[w * h * depth * 3];

            for (int i = 0; i < files.Length; i++)
            {
                if (i != 0)
                    b = new Bitmap(files[i]);
                System.Drawing.Imaging.BitmapData bmd = b.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                // copy into volume byte buffer (e.g., our copy of all the slices into one big block of memory)
                System.Runtime.InteropServices.Marshal.Copy(bmd.Scan0, volume, i * w * h * 3, w * h * 3);
                b.Dispose();
            }

            // pin the array into memory . . . we have to do this so the GC doesn't disappear it when we are counting on it to be where we left it 
            System.Runtime.InteropServices.GCHandle pinnedVolume = System.Runtime.InteropServices.GCHandle.Alloc(volume, System.Runtime.InteropServices.GCHandleType.Pinned);
            IntPtr volPtr = pinnedVolume.AddrOfPinnedObject();

            // loading the volume into OpenGL

            gl.GenTextures(1, volTex);
            gl.BindTexture(OpenGL.GL_TEXTURE_3D, volTex[0]);
            gl.TexImage3D(OpenGL.GL_TEXTURE_3D, 0, 3, w, h, depth, 0, OpenGL.GL_BGR, OpenGL.GL_UNSIGNED_BYTE, volPtr);

            // set filtering & clamping texturing parameters
            gl.TexParameter(OpenGL.GL_TEXTURE_3D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR);
            gl.TexParameter(OpenGL.GL_TEXTURE_3D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);
            gl.TexParameter(OpenGL.GL_TEXTURE_3D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP);
            gl.TexParameter(OpenGL.GL_TEXTURE_3D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP);
            gl.TexParameter(OpenGL.GL_TEXTURE_3D, OpenGL.GL_TEXTURE_WRAP_R, OpenGL.GL_CLAMP);

            // now we can free the volume as it should now be in video texture memory
            pinnedVolume.Free();
            gl.TexEnv(OpenGL.GL_TEXTURE_ENV, OpenGL.GL_TEXTURE_ENV_MODE, OpenGL.GL_REPLACE);
        }

        #endregion

    }
}
