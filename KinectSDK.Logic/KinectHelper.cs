using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace KinectSDK.Logic {

    /// <summary>
    /// The Kinect Helper
    /// </summary>
    public class KinectHelper : IDisposable {

        #region Fields

        private readonly int screenWidth = (int) SystemParameters.PrimaryScreenWidth;
        private readonly int screenHeight = (int) SystemParameters.PrimaryScreenHeight;
        private int maxBodyCount = 6;

        private bool grippedHand = false;

        private List<KinectObject> kinectObjects;
        private List<Kinect3DObject> kinect3DObjects;

        private KinectSensor kinectSensor;
        private KinectAudioStream convertStream;
        private SpeechRecognitionEngine speechEngine;

        private BodyFrameReader kinectBody;
        private ColorFrameReader kinectColor;
        private DepthFrameReader kinectDepth;
        private InfraredFrameReader kinectInfrared;

        private Choices speechDictionary;
        private Dictionary<string, Action> speechToActionDictionary;

        #endregion Fields

        #region Auto-Properties

        /// <summary>
        /// The Image the Color images will be bound to
        /// </summary>
        public Image ColorView { private get; set; }

        /// <summary>
        /// The Image the Depth images will be bound to
        /// </summary>
        public Image DepthView { private get; set; }

        /// <summary>
        /// The Image the Infrared images will be bound to
        /// </summary>
        public Image InfraredView { private get; set; }

        /// <summary>
        /// THe ModelVisual3D the 3D models will be rendered on
        /// </summary>
        public ModelVisual3D ModelVisual { private get; set; }

        /// <summary>
        /// The canvas the skeleton will be drawn on
        /// </summary>
        public Canvas Canvas { private get; set; }

        /// <summary>
        /// The JointType the Mouse should react to
        /// </summary>
        public JointType Mouse { private get; set; } = JointType.HandRight;

        /// <summary>
        /// The Cursor that changes on 'click' with the hand
        /// </summary>
        public Cursor Cursor { private get; set; } = new Cursor(Application.GetResourceStream(new Uri(string.Format(@"pack://application:,,,/{0};component/Resources/cursorAni.ani", Assembly.GetExecutingAssembly().GetName().Name), UriKind.Absolute)).Stream);

        /// <summary>
        /// Should the kinect be used as a mouse control?
        /// </summary>
        public bool KinectAsMouse { private get; set; } = false;

        /// <summary>
        /// Should a skeleton be drawn?
        /// </summary>
        public bool DrawDots { private get; set; } = false;

        /// <summary>
        /// Should the images be drawn?
        /// </summary>
        public bool DrawImages { private get; set; } = false;

        /// <summary>
        /// Should the 3D models be rendered?
        /// </summary>
        public bool RenderObjects { private get; set; } = false;

        #endregion Auto-Properties

        #region Properties

        /// <summary>
        /// The maximum amount of bodies to be tracked (min. = 1, max. = 6)
        /// </summary>
        public int MaxBodyCount {
            get { return this.maxBodyCount; }
            set {
                if (value > 6) this.maxBodyCount = 6;
                if (value < 0) this.maxBodyCount = 0;
                this.maxBodyCount = value;
            }
        }

        /// <summary>
        /// Use the Body event
        /// </summary>
        public bool UseBody {
            set {
                if (value) kinectBody.FrameArrived += Reader_BodyFrameArrived;
                else kinectBody.FrameArrived -= Reader_BodyFrameArrived;
            }
        }

        /// <summary>
        /// Use the Color event
        /// </summary>
        public bool UseColor {
            set {
                if (value) kinectColor.FrameArrived += Reader_ColorFrameArrived;
                else kinectColor.FrameArrived -= Reader_ColorFrameArrived;
            }
        }

        /// <summary>
        /// Use the Depth event
        /// </summary>
        public bool UseDepth {
            set {
                if (value) kinectDepth.FrameArrived += Reader_DepthFrameArrived;
                else kinectDepth.FrameArrived -= Reader_DepthFrameArrived;
            }
        }

        /// <summary>
        /// Use the Infrared event
        /// </summary>
        public bool UseInfrared {
            set {
                if (value) kinectInfrared.FrameArrived += Reader_InfraredFrameArrived;
                else kinectInfrared.FrameArrived -= Reader_InfraredFrameArrived;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// The KinectHelper constructor
        /// </summary>
        public KinectHelper () {
            kinectSensor = KinectSensor.GetDefault();

            if (kinectSensor != null) {
                kinectSensor.Open();
                if (kinectSensor.IsOpen) {
                    // grab the audio stream
                    IReadOnlyList<AudioBeam> audioBeamList = this.kinectSensor.AudioSource.AudioBeams;
                    Stream audioStream = audioBeamList[0].OpenInputStream();
                    this.convertStream = new KinectAudioStream(audioStream);

                    kinectBody = kinectSensor.BodyFrameSource.OpenReader();
                    kinectColor = kinectSensor.ColorFrameSource.OpenReader();
                    kinectDepth = kinectSensor.DepthFrameSource.OpenReader();
                    kinectInfrared = kinectSensor.InfraredFrameSource.OpenReader();
                } else {
                    throw new Exception("Kinect - Cannot open the Kinect Sensor to receive data!");
                }
            } else {
                throw new Exception("Kinect - Cannot find a Kinect Sensor to open!");
            }
        }

        /// <summary>
        /// Initialise speech recognition
        /// </summary>
        public void InitiateSpeechRecognition () {
            RecognizerInfo ri = TryGetKinectRecognizer();

            if (null != ri) {
                speechEngine = new SpeechRecognitionEngine(ri.Id);
                speechEngine.SpeechRecognized += SpeechRecognized;

                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(speechDictionary);

                var g = new Grammar(gb);
                speechEngine.LoadGrammar(g);
                convertStream.SpeechActive = true;

                speechEngine.SetInputToAudioStream(convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            } else {
                throw new Exception("Speech recognition refused to activate");
            }
        }

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to process audio from Kinect device.
        /// </summary>
        /// <returns>RecognizerInfo if found, <code>null</code> otherwise.</returns>
        private static RecognizerInfo TryGetKinectRecognizer () {
            IEnumerable<RecognizerInfo> recognizers;

            // This is required to catch the case when an expected recognizer is not installed.
            // By default - the x86 Speech Runtime is always expected.
            try {
                recognizers = SpeechRecognitionEngine.InstalledRecognizers();
            } catch (COMException) {
                return null;
            }

            foreach (RecognizerInfo recognizer in recognizers) {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-GB".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase)) {
                    return recognizer;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a KinectObject (2D Images)
        /// </summary>
        /// <param name="kinectObject">A KinectObject</param>
        public void AddKinectObject (KinectObject kinectObject) {
            if (kinectObjects == null) kinectObjects = new List<KinectObject>();
            kinectObjects.Add(kinectObject);
        }

        /// <summary>
        /// Clear all the Kinect Objects
        /// </summary>
        public void ClearKinectObject (KinectObject k)
        {
            kinectObjects.Remove(k);
        }

        /// <summary>
        /// Adds a Kinect3DObject (3D Models)
        /// </summary>
        /// <param name="kinectObject">A Kinect3DObject</param>
        public void AddKinectObject (Kinect3DObject kinectObject) {
            if (kinect3DObjects == null) kinect3DObjects = new List<Kinect3DObject>();
            kinect3DObjects.Add(kinectObject);
            if (this.RenderObjects) KinectDraw.RenderKinect3DObjects(ModelVisual, kinect3DObjects);
        }

        /// <summary>
        /// Clear all the Kinect 3D Objects
        /// </summary>
        public void ClearKinect3DObject(Kinect3DObject k)
        {
            kinect3DObjects.Remove(k);
        }

        /// <summary>
        /// Adds a speech action + caption words
        /// </summary>
        /// <param name="categoryWord">Main category word</param>
        /// <param name="captionWords">Words to use to recognize speech</param>
        /// <param name="method">The method to execute</param>
        public void AddSpeechAction (string categoryWord, string[] captionWords, Action method) {
            if (speechDictionary == null) speechDictionary = new Choices();
            if (speechToActionDictionary == null) speechToActionDictionary = new Dictionary<string, Action>();

            foreach (string caption in captionWords) {
                speechDictionary.Add(new SemanticResultValue(caption, categoryWord));
            }
            speechToActionDictionary.Add(categoryWord, method);
        }

        /// <summary>
        /// Dispose the kinect and it's readers
        /// </summary>
        public void Dispose () {
            if (kinectBody != null) {
                kinectBody.Dispose();
                kinectBody = null;
            }

            if (kinectColor != null) {
                kinectColor.Dispose();
                kinectColor = null;
            }

            if (kinectDepth != null) {
                kinectDepth.Dispose();
                kinectDepth = null;
            }

            if (kinectInfrared != null) {
                kinectInfrared.Dispose();
                kinectInfrared = null;
            }

            if (convertStream != null) {
                convertStream.Close();
                convertStream.Dispose();
                convertStream = null;
            }

            if (kinectSensor != null) {
                kinectSensor.Close();
                kinectSensor = null;
            }

            if (speechEngine != null)
            {
                speechEngine.Dispose();
                speechEngine = null;
            }
        }

        #endregion Methods

        #region Events

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRecognized (object sender, SpeechRecognizedEventArgs e) {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;
            if (e.Result.Confidence >= ConfidenceThreshold) {
                if (speechToActionDictionary.ContainsKey(e.Result.Semantics.Value.ToString()))
                    speechToActionDictionary[e.Result.Semantics.Value.ToString()]();
            }
        }

        /// <summary>
        /// The Body reader evnet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Reader_BodyFrameArrived (object sender, BodyFrameArrivedEventArgs e) {
            using (var frame = e.FrameReference.AcquireFrame()) {
                if (frame != null && this.maxBodyCount > 0) {
                    var bodies = new Body[this.maxBodyCount];
                    frame.GetAndRefreshBodyData(bodies);

                    var closestBody = KinectUtils.GetClosestBody(frame);

                    Canvas.Children.Clear();

                    foreach (Body body in bodies) {
                        if (body.IsTracked && body != null) {
                            //Draw The Skeleton
                            if (this.DrawDots) KinectDraw.DrawDots(Canvas, kinectSensor, body);
                            //Draw the images
                            if (this.DrawImages) KinectDraw.DrawKinectObjects(Canvas, kinectSensor, body, kinectObjects);
                            //Render 3D Objects
                            if (this.RenderObjects) KinectDraw.UpdateKinect3DObjects(body, kinect3DObjects);
                            //Use the Kinect to control the mouse and it is the closest body
                            if (this.KinectAsMouse && closestBody != null && body.TrackingId == closestBody.TrackingId) {
                                CameraSpacePoint mousePoint = body.Joints[Mouse].Position;
                                CameraSpacePoint spinePoint = body.Joints[JointType.SpineBase].Position;
                                //Is the hand in front of the person?
                                if (mousePoint.Z - spinePoint.Z < -0.15f) {
                                    Point curPos = MouseOverride.GetCursorPosition();
                                    MouseOverride.SetCursorPos((int) (curPos.X + (mousePoint.X * 2f * (int) SystemParameters.PrimaryScreenWidth - curPos.X) * 0.4), (int) (curPos.Y + (((spinePoint.Y - mousePoint.Y + 0.5f) + 0.25f) * 2f * (int) SystemParameters.PrimaryScreenHeight - curPos.Y) * 0.4));

                                    if (body.HandRightState == HandState.Closed) {
                                        if (!grippedHand) {
                                            // If the hand is closed hold down the left mouse button
                                            MouseOverride.MouseLeftDown();
                                            grippedHand = true;
                                            System.Windows.Input.Mouse.OverrideCursor = Cursor;
                                        }
                                    } else if (body.HandRightState == HandState.Open) {
                                        if (grippedHand) {
                                            // If the hand is opened let go of the left mouse button
                                            MouseOverride.MouseLeftUp();
                                            grippedHand = false;
                                            System.Windows.Input.Mouse.OverrideCursor = null;
                                        }
                                    }
                                } else {
                                    grippedHand = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The Color reader event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Reader_ColorFrameArrived (object sender, ColorFrameArrivedEventArgs e) {
            using (var frame = e.FrameReference.AcquireFrame()) {
                if (frame != null) {
                    ColorView.Source = KinectUtils.ToBitmap(frame);
                }
            }
        }

        /// <summary>
        /// The Depth reader event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Reader_DepthFrameArrived (object sender, DepthFrameArrivedEventArgs e) {
            using (var frame = e.FrameReference.AcquireFrame()) {
                if (frame != null) {
                    DepthView.Source = KinectUtils.ToBitmap(frame);
                }
            }
        }

        /// <summary>
        /// The Infrared reader event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Reader_InfraredFrameArrived (object sender, InfraredFrameArrivedEventArgs e) {
            using (var frame = e.FrameReference.AcquireFrame()) {
                if (frame != null) {
                    InfraredView.Source = KinectUtils.ToBitmap(frame);
                }
            }
        }

        #endregion Events
    }
}