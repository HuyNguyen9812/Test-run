using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

//EMGU
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

//DiresctShow
using DirectShowLib;

namespace CameraCapture
{
    public partial class CameraCapture : Form
    {
        /*Hint use CTL+M and then CTL+O to callapse all fields*/
        #region Variables
        #region Camera Capture Variables
        private Capture _capture = null; //Camera
        private bool _captureInProgress = false; //Variable to track camera state
        int CameraDevice = 0; //Variable to track camera device selected
        Video_Device[] WebCams; //List containing all the camera available
        #endregion
        #region Camera Settings
        int Brightness_Store = 0;
        int Contrast_Store = 0;
        int Sharpness_Store = 0;
        #endregion
        #endregion

        public CameraCapture()
        {
            InitializeComponent();
            Slider_Enable(false); //Disable sliders untill capturing

            //-> Find systems cameras with DirectShow.Net dll
            //thanks to carles lloret
            DsDevice[] _SystemCamereas = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            WebCams = new Video_Device[_SystemCamereas.Length];
            for (int i = 0; i < _SystemCamereas.Length; i++)
            {
                WebCams[i] = new Video_Device(i, _SystemCamereas[i].Name, _SystemCamereas[i].ClassID); //fill web cam array
                Camera_Selection.Items.Add(WebCams[i].ToString());
            }
            if (Camera_Selection.Items.Count > 0)
            {
                Camera_Selection.SelectedIndex = 0; //Set the selected device the default
                captureButton.Enabled = true; //Enable the start
            }
            
        }

        /// <summary>
        /// What to do with each frame aquired from the camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        private void ProcessFrame(object sender, EventArgs arg)
        {
            //***If you want to access the image data the use the following method call***/
            //Image<Bgr, Byte> frame = new Image<Bgr,byte>(_capture.RetrieveBgrFrame().ToBitmap());

            if (RetrieveBgrFrame.Checked)
            {
                Image<Bgr, Byte> frame = _capture.RetrieveBgrFrame();
                //because we are using an autosize picturebox we need to do a thread safe update
                DisplayImage(frame.ToBitmap());
            }
            else if (RetrieveGrayFrame.Checked)
            {
                Image<Gray, Byte> frame = _capture.RetrieveGrayFrame();
                //because we are using an autosize picturebox we need to do a thread safe update
                DisplayImage(frame.ToBitmap());
            }
        }

        /// <summary>
        /// Thread safe method to display image in a picturebox that is set to automatic sizing
        /// </summary>
        /// <param name="Image"></param>
        private delegate void DisplayImageDelegate(Bitmap Image);
        private void DisplayImage(Bitmap Image)
        {
            if (captureBox.InvokeRequired)
            {
                try
                {
                    DisplayImageDelegate DI = new DisplayImageDelegate(DisplayImage);
                    this.BeginInvoke(DI, new object[] { Image });
                }
                catch (Exception ex)
                {
                }
            }
            else
            {
                captureBox.Image = Image;
            }
        }

        /// <summary>
        /// Start/Stop the camera aquasition
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void captureButtonClick(object sender, EventArgs e)
        {
            if (_capture != null)
            {
                if (_captureInProgress)
                {  
                    //stop the capture
                    captureButton.Text = "Start Capture"; //Change text on button
                    Slider_Enable(false); 
                    _capture.Pause(); //Pause the capture
                    _captureInProgress = false; //Flag the state of the camera
                }
                else
                {
                    //Check to see if the selected device has changed
                    if (Camera_Selection.SelectedIndex != CameraDevice)
                    {
                        SetupCapture(Camera_Selection.SelectedIndex); //Setup capture with the new device
                    }

                    RetrieveCaptureInformation(); //Get Camera information
                    captureButton.Text = "Stop"; //Change text on button
                    StoreCameraSettings(); //Save Camera Settings
                    Slider_Enable(true);  //Enable User Controls
                    _capture.Start(); //Start the capture
                    _captureInProgress = true; //Flag the state of the camera
                }
                
            }
            else 
            {
                //set up capture with selected device
                SetupCapture(Camera_Selection.SelectedIndex);
                //Be lazy and Recall this method to start camera
                captureButtonClick(null, null);
            }
        }

        /// <summary>
        /// Sets up the _capture variable with the selected camera index
        /// </summary>
        /// <param name="Camera_Identifier"></param>
        private void SetupCapture(int Camera_Identifier)
        {
            //update the selected device
            CameraDevice = Camera_Identifier;

           //Dispose of Capture if it was created before
            if (_capture != null) _capture.Dispose();
            try
            {
                //Set up capture device
                _capture = new Capture(CameraDevice);
                _capture.ImageGrabbed += ProcessFrame;
            }
            catch (NullReferenceException excpt)
            {
                MessageBox.Show(excpt.Message);
            }
        }

        /// <summary>
        /// Flips the image along the Horizontal axis
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlipHorizontalButtonClick(object sender, EventArgs e)
        {
            if (_capture != null) _capture.FlipHorizontal = !_capture.FlipHorizontal;
        }

        /// <summary>
        /// Flips the image along the Vertical axis
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlipVerticalButtonClick(object sender, EventArgs e)
        {
            if (_capture != null) _capture.FlipVertical = !_capture.FlipVertical;
        }

        //TODO: Set up other Parameters
        /// <summary>
        /// Retrieves the settings from the camera
        /// </summary>
        private void RetrieveCaptureInformation()
        {
            richTextBox1.Clear();
            richTextBox1.AppendText("Camera: " + WebCams[CameraDevice].Device_Name + " (-1 = Unknown)\n\n");

            //Brightness
            richTextBox1.AppendText("Brightness: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_BRIGHTNESS).ToString() + "\n"); //get the value and add it to richtextbox
            Brigtness_SLD.Value = (int)_capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_BRIGHTNESS);  //Set the slider value
            Brigthness_LBL.Text = Brigtness_SLD.Value.ToString(); //set the slider text

            //Contrast
            richTextBox1.AppendText("Contrast: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_CONTRAST).ToString() + "\n");//get the value and add it to richtextbox
            Contrast_SLD.Value = (int)_capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_CONTRAST);  //Set the slider value
            Contrast_LBL.Text = Contrast_SLD.Value.ToString(); //set the slider text

            //Sharpness
            richTextBox1.AppendText("Sharpness: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_SHARPNESS).ToString() + "\n");
            Sharpness_SLD.Value = (int)_capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_CONTRAST);  //Set the slider value
            Sharpness_LBL.Text = Sharpness_SLD.Value.ToString(); //set the slider text

            //TODO: ALL These need sliders setting up on main form
            richTextBox1.AppendText("Convert RGB : " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_CONVERT_RGB).ToString() + "\n");
            richTextBox1.AppendText("Exposure control done by camera: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_AUTO_EXPOSURE).ToString() + "\n");
            richTextBox1.AppendText("Exposure: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_EXPOSURE).ToString() + "\n");
            richTextBox1.AppendText("Frame Height: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT).ToString() + "\n");
            richTextBox1.AppendText("Frame Width: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH).ToString() + "\n");
            richTextBox1.AppendText("Gain: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_GAIN).ToString() + "\n");
            richTextBox1.AppendText("Gamma: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_GAMMA).ToString() + "\n");
            richTextBox1.AppendText("Hue: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_HUE).ToString() + "\n");
            richTextBox1.AppendText("Saturation: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_SATURATION).ToString() + "\n");
            richTextBox1.AppendText("Sharpness: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_SHARPNESS).ToString() + "\n");
            richTextBox1.AppendText("Trigger: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_TRIGGER).ToString() + "\n");
            richTextBox1.AppendText("Trigger Delay: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_TRIGGER_DELAY).ToString() + "\n");
            richTextBox1.AppendText("White balance blue u : " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_WHITE_BALANCE_BLUE_U).ToString() + "\n");
            richTextBox1.AppendText("White balance red v : " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_WHITE_BALANCE_RED_V).ToString() + "\n");
            richTextBox1.AppendText("Max DC1394: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_MAX_DC1394).ToString() + "\n");
            richTextBox1.AppendText("Current Capture Mode: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_MODE).ToString() + "\n");
            richTextBox1.AppendText("Monocrome : " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_MONOCROME).ToString() + "\n");
            richTextBox1.AppendText("Rectification : " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_RECTIFICATION).ToString() + "\n");
            richTextBox1.AppendText("Preview (tricky property, returns cpnst char* indeed ): " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_SUPPORTED_PREVIEW_SIZES_STRING).ToString() + "\n");

            #region Unused
            /*
            //OpenNI
            richTextBox1.AppendText("\nOpen NI specific devices: \n");
            richTextBox1.AppendText("OpenNI map generators : " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_OPENNI_DEPTH_GENERATOR).ToString() + "\n");
            richTextBox1.AppendText("Depth generator baseline, in mm: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_OPENNI_DEPTH_GENERATOR_BASELINE).ToString() + "\n");
            richTextBox1.AppendText("Depth generator focal length, in pixels: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_OPENNI_DEPTH_GENERATOR_FOCAL_LENGTH).ToString() + "\n");
            richTextBox1.AppendText("OpenNI map generators: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_OPENNI_GENERATORS_MASK).ToString() + "\n");
            richTextBox1.AppendText("CV_CAP_OPENNI_IMAGE_GENERATOR: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_OPENNI_IMAGE_GENERATOR).ToString() + "\n");
            richTextBox1.AppendText("Image generator output mode : " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_OPENNI_IMAGE_GENERATOR_OUTPUT_MODE).ToString() + "\n");
            richTextBox1.AppendText("OpenNI Baseline mm: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_OPENNI_BASELINE).ToString() + "\n");
            richTextBox1.AppendText("OpenNI Focal Length, pixels: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_OPENNI_FOCAL_LENGTH).ToString() + "\n");
            richTextBox1.AppendText("OpenNI Max Depth mm: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_OPENNI_FRAME_MAX_DEPTH).ToString() + "\n");
            richTextBox1.AppendText("OpenNI Oputput Mode: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_OPENNI_OUTPUT_MODE).ToString() + "\n");
             
            //Android
            richTextBox1.AppendText("\nAndroid Only: \n");
            richTextBox1.AppendText("property for highgui class: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_AUTOGRAB).ToString() + "\n");
             
            //Video File
            richTextBox1.AppendText("\nVideo Files: \n");
            richTextBox1.AppendText("Format: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FORMAT).ToString() + "\n");
            richTextBox1.AppendText("4-character code of codec : " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FOURCC).ToString() + "\n");
            richTextBox1.AppendText("Frame rate : " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FPS).ToString() + "\n");
            richTextBox1.AppendText("Number of frames in video file : " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_COUNT).ToString() + "\n");
            richTextBox1.AppendText(" Relative position of the video file (0 - start of the film, 1 - end of the film): " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_AVI_RATIO).ToString() + "\n");
            richTextBox1.AppendText("0-based index of the frame to be decoded/captured next: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_FRAMES).ToString() + "\n");
            richTextBox1.AppendText("Film current position in milliseconds or video capture timestamp: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_POS_MSEC).ToString() + "\n");

            //GStreamer Device
            richTextBox1.AppendText("\nGStreamer: \n");
            richTextBox1.AppendText("Properties of cameras available through GStreamer interface: " + _capture.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_GSTREAMER_QUEUE_LENGTH).ToString() + "\n");*/
            #endregion

        }

        //TODO: Set up other Parameters
        /// <summary>
        /// Stores the Initial camera settings so they can be reset
        /// </summary>
        private void StoreCameraSettings()
        {
            Brightness_Store = Brigtness_SLD.Value;
            Contrast_Store = Contrast_SLD.Value;
            Sharpness_Store = Sharpness_SLD.Value;
        }

        /// <summary>
        /// Retrieves the camera information again to ensure it's been updated again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Refresh_BTN_Click(object sender, EventArgs e)
        {
            if (_capture != null)
            {
                RetrieveCaptureInformation();
            }
        }

        //TODO: Set up other Parameters
        /// <summary>
        /// Resets the camera setting to those saved or initially read
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reset_Cam_Settings_Click(object sender, EventArgs e)
        {
            if (_capture != null)
            {
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_BRIGHTNESS, Brightness_Store);
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_CONTRAST, Contrast_Store);
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_SHARPNESS, Sharpness_Store);
                RetrieveCaptureInformation(); // This will refresh the settings
            }
        }

        //TODO: Set up other Parameters
        #region Sliders
        /*These simply update the appropriate label and set the individual properties of the camera using _capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP)*/
        private void Brigtness_SLD_Scroll(object sender, EventArgs e)
        {
            Brigthness_LBL.Text = Brigtness_SLD.Value.ToString();
            if (_capture != null) _capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_BRIGHTNESS, Brigtness_SLD.Value);
        }
        private void Contrast_SLD_Scroll(object sender, EventArgs e)
        {
            Contrast_LBL.Text = Contrast_SLD.Value.ToString();
            if (_capture != null) _capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_CONTRAST, Contrast_SLD.Value);
        }
        private void Sharpness_SLD_Scroll(object sender, EventArgs e)
        {
            Sharpness_LBL.Text = Sharpness_SLD.Value.ToString();
            if (_capture != null) _capture.SetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_SHARPNESS, Sharpness_SLD.Value);
        }
        /// <summary>
        /// Allows the Sliders to be enable and disabled in one method call
        /// </summary>
        /// <param name="State"></param>
        private void Slider_Enable(bool State)
        {
            Brigtness_SLD.Enabled = State;
            Contrast_SLD.Enabled = State;
            Sharpness_SLD.Enabled = State;
        }
        #endregion

        /// <summary>
        /// Ensure that the Camera Setting are reset if the form is just clossed and the camera is released
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (Capture != null)
            {
                Reset_Cam_Settings_Click(null, null);
                _capture.Dispose();
            }

        }

        #region Ensures that bothe checkboxs can't be checked
        private void RetrieveGrayFrame_CheckedChanged(object sender, EventArgs e)
        {
            if (RetrieveGrayFrame.Checked)
            {
                RetrieveBgrFrame.Checked = !RetrieveGrayFrame.Checked;
            }
        }

        private void RetrieveBgrFrame_CheckedChanged(object sender, EventArgs e)
        {
            if (RetrieveBgrFrame.Checked)
            {
                RetrieveGrayFrame.Checked = !RetrieveBgrFrame.Checked;
            }
        }
        #endregion


        

        
    }
}

