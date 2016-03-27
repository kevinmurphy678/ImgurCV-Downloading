using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Drawing.Drawing2D;

namespace puushSpy
{
    public partial class Form1 : Form
    {// <summary>Returns true if the current application has focus, false otherwise</summary>
        public static bool ApplicationIsActivated()
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
            {
                return false;       // No window is currently activated
            }
            var procId = Process.GetCurrentProcess().Id;
            int activeProcId;
            GetWindowThreadProcessId(activatedHandle, out activeProcId);
            return activeProcId == procId;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        Random random = new Random();

        Form form2;

        public Boolean dumping = false;


        private static FaceRecognizer face;
        public Form1()
        {
            InitializeComponent();
            form2 = new Form2();
          
            face = new FisherFaceRecognizer(0, 3500);

        }

        private string RandomString(int Size)
        {
            string input = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < Size; i++)
            {
                ch = input[random.Next(0, input.Length)];
                builder.Append(ch);
            }
            return builder.ToString();
        }


        delegate void AddBoxDel(PictureBox box);

        public void AddBox(PictureBox box)
        {
            if (panel1.InvokeRequired)
            {
                AddBoxDel method = new AddBoxDel(AddBox);
                panel1.Invoke(method, new object[] { box });
                return;
            }

            panel1.Controls.Add(box);
        }

        delegate void AppendMessageDel(string message);

        public void AppendMessage(string message)
        {
            if (debugBox.InvokeRequired)
            {
                AppendMessageDel method = new AppendMessageDel(AppendMessage);
                debugBox.Invoke(method, new object[] { message });
                return;
            }
            debugBox.AppendText(message);
            debugBox.SelectionStart = debugBox.Text.Length;
            debugBox.ScrollToCaret();
        }


        int lastx = 0;
        private void getRandomPuushImage()
        {
            Image img=null;
            string link=null;
            bool worked = false;
            String name = null;
            try
            {
                String rand = RandomString(5);
                link = ("http://www.imgur.com/" + rand + ".jpg");
                WebRequest req = WebRequest.Create(link);
                req.Proxy = null;
                name = rand;
                WebResponse response = req.GetResponse();
                
                if(!response.ResponseUri.AbsoluteUri.ToString().Contains("removed") && !response.ContentType.Contains("gif"))
                {
                    Stream stream = req.GetResponse().GetResponseStream();
                    img = Image.FromStream(stream);
                    AppendMessage("\n" + link + ": success");
                    worked = true;
                    response.Close();
                }
                else
                {
                    AppendMessage("\n" + link + ": fail");
                    worked = false;
                    response.Close();
                    getRandomPuushImage();
                }

                response.Close();
            }
            catch
            {
                AppendMessage("\n" + link + ": fail");
                getRandomPuushImage();
            }

            if (worked == true)
            {
               

                    Rectangle[] faces=null;
                    Rectangle[] eyes=null;

                    if (checkBox1.Checked)
                    {
                        // Converting the master image to a bitmap
                        Bitmap masterImage = (Bitmap)img;

                        // Normalizing it to grayscale
                        Image<Gray, Byte> normalizedMasterImage = new Image<Gray, Byte>(masterImage);

                        float PERCENT = 0.1f;
                        int xSize = (int)(PERCENT * normalizedMasterImage.Size.Width);
                        int ySize = (int)(PERCENT * normalizedMasterImage.Size.Height);
                        int maxSize = Math.Max(xSize, ySize);
                        CascadeClassifier Classifier = new CascadeClassifier("haarcascade_frontalface_alt2.xml");
                        faces = Classifier.DetectMultiScale(normalizedMasterImage, 1.03, 10, new Size(maxSize, maxSize), Size.Empty);
                        CascadeClassifier ClassifierEyes = new CascadeClassifier("haarcascade_eye.xml");
                        eyes = ClassifierEyes.DetectMultiScale(normalizedMasterImage, 1.03, 10, new Size(maxSize, maxSize), Size.Empty);
                   }
       

                if (!dumping)
                {
                    if(faces!= null)
                    foreach (var face in faces)
                    {
                         AppendMessage("\nFACE DETECTED!");
                        using (var graphics = Graphics.FromImage(img))
                        {
                                Pen redPen = new Pen(Color.FromArgb(255, 255, 0, 0), 10);
                                redPen.Alignment = PenAlignment.Center;

                                graphics.DrawRectangle(redPen, face);
                        }
                    }
                    if(eyes!=null)
                    foreach (var eye in eyes)
                    {
                         AppendMessage("\nEYE DETECTED!");
                        using (var graphics = Graphics.FromImage(img))
                        {
                                Pen greenPen = new Pen(Color.FromArgb(255, 0, 255, 0), 10);
                                greenPen.Alignment = PenAlignment.Center;

                                graphics.DrawRectangle(greenPen, eye);
                            }
                    }

                    PictureBox box = new PictureBox();
                    if (lastx == 512) { lastx = 0; }
                    box.SetBounds(lastx, 0, 128, 128);
                    lastx += 128;
                    box.Image = img;
                    box.SizeMode = PictureBoxSizeMode.Zoom;
                    box.Tag = link;
                    AddBox(box);
                }
                else
                {
                    if (checkBox1.Checked)
                    {
                        if (faces.Length > 0 || eyes.Length > 0)
                        {
                            //save image
                            Console.WriteLine("saving image");

                            img.Save(Directory.GetCurrentDirectory().ToString() + "/images/Faces/" + name + ".jpg");

                        }
                    }

                    else
                    {
                        img.Save(Directory.GetCurrentDirectory().ToString() + "/images/Any/" + name + ".jpg");
                    }
                }

                       
            }

        }
        private void button1_Click(object sender, EventArgs e)//get images
        {
            dumping = false;
            for (int i = 0; i < numericUpDown1.Value; i++)
            {
                BackgroundWorker imageWorker = new BackgroundWorker();
                imageWorker.DoWork += new DoWorkEventHandler(imageWorker_DoWork);
                imageWorker.RunWorkerAsync();     
            }
        }
        private void imageWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            getRandomPuushImage();
        }

        bool oneclick = false;
        private void timer1_Tick(object sender, EventArgs e)
        {

            bool popup = false;
            foreach (PictureBox box in panel1.Controls)
            {
                Point pos = this.PointToClient(Cursor.Position);
                Rectangle boxrect = new Rectangle(box.Location.X+panel1.Location.X,box.Location.Y+panel1.Location.Y,box.Size.Width,box.Size.Height);
                bool hovering = boxrect.Contains(pos);
                bool hoveringForm = this.ClientRectangle.Contains(pos);
                bool hoveringpanel = panel1.ClientRectangle.Contains(pos);
                if (hovering&&hoveringForm&&hoveringpanel&&ApplicationIsActivated())
                {
                    popup = true;
                    form2.Show();

                    form2.SetBounds(0, 0, Clamp(box.Image.Width, 0, SystemInformation.PrimaryMonitorSize.Width), Clamp(box.Image.Height, 0, SystemInformation.PrimaryMonitorSize.Height));

                    foreach (PictureBox subbox in form2.Controls)//dunno how to access picturebox1 on form2 directly so im doing this hack
                    {
                        subbox.Image = box.Image;
                        subbox.SizeMode = PictureBoxSizeMode.Zoom;
                      
                    }
                    if ((Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left&&oneclick==false)
                    {
                        oneclick = true;
                        System.Diagnostics.Process.Start((string)box.Tag);
                      
                    }
                    if ((Control.MouseButtons & MouseButtons.Left) != MouseButtons.Left)
                    {
                        oneclick = false;
                    }
                }
            }
            if (popup == false) { form2.Hide(); }
        }

        private void button2_Click(object sender, EventArgs e)//clear images
        {
            
            lastx = 0;
            ArrayList controls = new ArrayList(panel1.Controls);
            foreach (Control c in controls)
            {
                panel1.Controls.Remove(c);
                c.Dispose();
            } 
        }

        private void button3_Click(object sender, EventArgs e)
        {
            dumping = true;
            for (int i = 0; i < numericUpDown2.Value; i++)
            {
                BackgroundWorker imageWorker = new BackgroundWorker();
                imageWorker.DoWork += new DoWorkEventHandler(imageWorker_DoWork);
                imageWorker.RunWorkerAsync();
            }
        }
    }
}
