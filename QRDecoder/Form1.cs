using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Media;
using System.Windows.Forms;
using System.Diagnostics;

// Video Library
using AForge.Video.DirectShow;
using AForge.Video;

namespace QRDecoder
{
    public partial class Form1 : Form
    {
        // General Initialization
        // This is an initialization for synchronous textBox update
        delegate void SetTextCallback(string text);
        string filePath;

        // A stopWatch to test the processing speed.
        Stopwatch stopwatch = new Stopwatch();

        // Webcam variables to handle the image captured by the webcam.
        FilterInfoCollection videoSources;
        VideoCaptureDevice videoStream;

        // Bitmap buffers
        Bitmap streamBitmap;
        Bitmap snapShotBitmap;
        Bitmap safeTempstreamBitmap;

        // Sound to be played when successful detection take a place.
        SoundPlayer player = new SoundPlayer("Resources/connect.wav");

        // Thread for decoding in parallel with the webacm video streaming.
        Thread decodingThread;

        // The QR Decoder variable from ZXing
        MDecoder decoder;

        public Form1()
        {
            InitializeComponent();
        }

        // This function will put an image inside the pictureBox instead of streaming video and decode it statically.
        // Notice: You have to stop the recording before browse for a static image.
        private void decode_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowReadOnly = true;
            openFileDialog1.Filter = "bmp files (*.bmp)|*.bmp";

            if (DialogResult.OK == openFileDialog1.ShowDialog(this))
            {
                filePath = openFileDialog1.InitialDirectory + openFileDialog1.FileName;
                fileName.Text = filePath;
                decode.Enabled = true;
                pictureBox1.ImageLocation = filePath;
            }
        }

        // Manual decode
        private void decode_Click_1(object sender, EventArgs e)
        {
            Bitmap b = new Bitmap(filePath, false);

            stopwatch.Reset();
            stopwatch.Start();

            string decodeStr = decoder.MDecode(b);

            stopwatch.Stop();

            if (decodeStr == null)
            {
                result.Text = "There is no QR Code!";
            }
            else
            {
                result.Text = decodeStr;
                speed.Text = stopwatch.Elapsed.TotalMilliseconds.ToString();
                player.Play();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialize sound variable
            player.Stream = Properties.Resources.connect;

            decoder = new MDecoder();

            // Start a decoding process
            decodingThread = new Thread(new ThreadStart(decodeLoop));
            decodingThread.Start();

            try
            {
                // enumerate video devices
                videoSources = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                // create video source
                videoStream = new VideoCaptureDevice(videoSources[0].MonikerString);
                
                // set NewFrame event handler
                videoStream.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);

                // start the video source
                videoStream.Start();

            }catch(VideoException exp)
            {
                Console.Write(exp.Message);
            }
        }

        // This event will be triggered whenever a new image is being captured by the webcam, minimum 25 frame per minute
        // Depending in the webcam capability.
        void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                streamBitmap = (Bitmap)eventArgs.Frame.Clone();
                safeTempstreamBitmap = (Bitmap)streamBitmap.Clone();
                pictureBox1.Image = streamBitmap;
                pictureBox1.Refresh();

            }catch(Exception exp)
            {
                Console.Write(exp.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            videoStream.SignalToStop();
            videoStream.Stop();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            videoStream.Start();
        }

        // Decoding endless thread process
        public void decodeLoop()
        {
            while (true)
            {
                // 1 second pause for the thread. This could be changed manually to a prefereable decoding interval.
                Thread.Sleep(1000);
                if (streamBitmap != null)
                    snapShotBitmap = (Bitmap)safeTempstreamBitmap.Clone();
                else
                    return;

                // Reset watch before decoding the streamed image.
                stopwatch.Reset();
                stopwatch.Start();

                // Decode the snapshot.
                string decodeStr = decoder.MDecode(snapShotBitmap);

                stopwatch.Stop();
                //string decode = Detect(b);

                // If decodeStr is null then there was no QR detected, otherwise show the result of detection and play the sound.
                if (decodeStr == null)
                {
                    this.SetResultText("There is no QR Code!");
                }
                else
                {
                    this.SetResultText(decodeStr);
                    this.SetSpeedText( stopwatch.Elapsed.TotalMilliseconds.ToString());
                    player.Play();
                }
            }
        }

        private void SetResultText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.result.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetResultText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.result.Text = text;
            }
        }

        private void SetSpeedText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.result.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetSpeedText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.speed.Text = text;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            videoStream.SignalToStop();
            videoStream.Stop();
            decodingThread.Abort();

            this.Dispose();
        }

    }
}
