/// <summary>
/// executes custom classifiers with Open CV in a multithreaded env
/// created by Dominik Müller
/// 2018 / 06 / 14
/// </summary>


using System.Collections.Generic;
using System.Threading;
using System;
using System.Drawing;
using UnityEngine;

using Vuforia;
using Emgu.CV;
using Emgu.CV.Structure;


public class VuforiaCVClassifierDetection : MonoBehaviour {

    [Tooltip("Folder from root in which your classifier files (xml files) reside")]
    public string classifierFolder = "Classifiers";

    [Tooltip("All the classifiers (xml files) you want to detect in the scene with this component")]
    public string[] classifiers;

    [Tooltip("The minimum neighbours a classifier must have to be a valid detection (sliding window approach)")]
    public int minNeighbours = 3;

    [Tooltip("The scaling factor for the image, shich gets resized smaller every iteration by this factor to detect classifiers (the bigger the faster, the smaller the more stable)")]
    public float scaleFactor = 1.2f;

    [Tooltip("Minimum size of a detected classifier, maximum size is infinite")]
    public Vector2Int minSize = new Vector2Int(30,30);

    // For the classification we'll need the grayscale video stream
    private Vuforia.Image.PIXEL_FORMAT mPixelFormat = Vuforia.Image.PIXEL_FORMAT.GRAYSCALE;
    private CameraDevice.FocusMode mFocusFormat = CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO;
    private bool mFormatRegistered = false;
    private int[] imageDimensions = new int[2];
    private Size minSizeObject;

    // Threading for multiscale detection
    // private Thread tDetection;
    private Rectangle[] currentDetectedRects = new Rectangle[0];
    private BasicBackgroundWorker bWorker;
    private System.Object lockDetetctedRects = new System.Object();
    private System.Object lockExportRects = new System.Object();

    // export element
    private Dictionary<string, Rectangle> exportDetectedRects = new Dictionary<string, Rectangle>();

    // here we have our variables for the classification itself
    private Dictionary<string, CascadeClassifier> registeredClassifiers = new Dictionary<string,CascadeClassifier>();

    void Start () {
        // register a background worker
        bWorker = new BasicBackgroundWorker(name: "ClassficationBackgroundWorker", prio: System.Threading.ThreadPriority.BelowNormal);

        // tDetection = new Thread(new ParameterizedThreadStart(DetectionPipeline)) { IsBackground=true, Name="Detection Pipeline" };
        minSizeObject = new Size(minSize.x, minSize.y);
        VuforiaARController.Instance.RegisterVuforiaStartedCallback(OnVuforiaStarted);
        VuforiaARController.Instance.RegisterTrackablesUpdatedCallback(OnTrackablesUpdated);
        VuforiaARController.Instance.RegisterOnPauseCallback(OnPause);
    }

    /// <summary>
    /// the detection pipeline in a separate function to run in a thread and not block the main thread
    /// --> no Unity API methods can be used in separate threads, therefore no Debug.Log etc.
    /// </summary>
    /// <param name="grayPixels"></param>
    private void DetectionPipeline(object data) {

        byte[] grayPixels;
        try {
            grayPixels = (byte[])data;
        } catch (InvalidCastException exc) {
            throw (exc);
        }

        // keep the whole detection in sync!
        lock (lockDetetctedRects) {
            // detect the classifier in the image
            foreach (string key in registeredClassifiers.Keys) {
                CascadeClassifier cClass = registeredClassifiers[key];
                Image<Gray, byte> depthImage = new Image<Gray, byte>(imageDimensions[0], imageDimensions[1]) {
                    Bytes = grayPixels
                };

                // the detection happens here
                Rectangle[] detectedRectangles = cClass.DetectMultiScale(depthImage, scaleFactor: scaleFactor, minNeighbors: minNeighbours, minSize: minSizeObject);

                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    currentDetectedRects = detectedRectangles;
                });

            }
        }
    }
    

    /// <summary>
    /// on Vuforia initialization
    /// </summary>
    private void OnVuforiaStarted() {
        CameraDevice.Instance.SetFocusMode(mFocusFormat);
        if (CameraDevice.Instance.SetFrameFormat(mPixelFormat, true)) {
            Debug.Log("Successfully registered pixel format " + mPixelFormat.ToString());

            mFormatRegistered = true;
        } else {
            Debug.LogError(
           "Failed to register pixel format " + mPixelFormat.ToString() +
           "\n the format may be unsupported by your device;" +
           "\n consider using a different pixel format.");

            mFormatRegistered = false;
        }

        // create all classifiers
        foreach (string classifierName in classifiers) {
            CascadeClassifier cClass = new CascadeClassifier(classifierFolder + "/" + classifierName);
            registeredClassifiers.Add(classifierName,cClass);
        }
    }

    private void Update() {
        Debug.Log(currentDetectedRects.Length);
    }

    /// <summary>
    /// gets the stream image pixels in the given format
    /// </summary>
    /// <returns name="imagePixels">byte[] pixels</returns>
    private byte[] GetVuforiaStream() {
        Vuforia.Image image = CameraDevice.Instance.GetCameraImage(mPixelFormat);
        if (image != null) {
            imageDimensions[0] = image.Width;
            imageDimensions[1] = image.Height;
        }
        return image != null ? image.Pixels : null;
    }

    
    /// <summary>
    /// called every time the vuforia trackables get updated (kind of like update)
    /// </summary>
    void OnTrackablesUpdated() {
        if (mFormatRegistered) {
            byte[] grayPixels = GetVuforiaStream();
            if (grayPixels != null && grayPixels.Length > 0) {
                // start the detection thread
                bWorker.EnqueueWork(() => DetectionPipeline(grayPixels));
            }
        }
    }

    /// <summary>
    /// Called when app is paused / resumed
    /// </summary>
    void OnPause(bool paused) {
        if (paused) {
            Debug.Log("App was paused");
            UnregisterFormat();
        } else {
            Debug.Log("App was resumed");
            RegisterFormat();
        }
    }

    /// <summary>
    /// Register the camera pixel format
    /// </summary>
    void RegisterFormat() {
        if (CameraDevice.Instance.SetFrameFormat(mPixelFormat, true)) {
            Debug.Log("Successfully registered camera pixel format " + mPixelFormat.ToString());
            mFormatRegistered = true;
        } else {
            Debug.LogError("Failed to register camera pixel format " + mPixelFormat.ToString());
            mFormatRegistered = false;
        }
    }

    /// <summary>
    /// Unregister the camera pixel format (e.g. call this when app is paused)
    /// </summary>
    void UnregisterFormat() {
        Debug.Log("Unregistering camera pixel format " + mPixelFormat.ToString());
        CameraDevice.Instance.SetFrameFormat(mPixelFormat, false);
        mFormatRegistered = false;
    }
}


/// <summary>
/// BasicBackgroundWorker starts a thread that runs forever and takes Actions on a Queue to execute
/// </summary>
public class BasicBackgroundWorker {
    private readonly Thread _backgroundWorkThread;
    private readonly Queue<Action> _queue = new Queue<Action>();
    private readonly ManualResetEvent _workAvailable = new ManualResetEvent(false);

    public BasicBackgroundWorker(string name, System.Threading.ThreadPriority prio) {
        _backgroundWorkThread = new Thread(BackgroundThread) {
            IsBackground = true,
            Priority = prio,
            Name = name
        };
        _backgroundWorkThread.Start();
    }

    public void EnqueueWork(Action work) {
        lock (_queue) {
            _queue.Enqueue(work);
            _workAvailable.Set();
        }
    }

    private void BackgroundThread() {
        while (true) {
            _workAvailable.WaitOne();
            Action workItem;
            lock (_queue) {
                workItem = _queue.Dequeue();
                if (_queue.Count == 0) {
                    _workAvailable.Reset();
                }
            }
            try {
                workItem();
            } catch (Exception) {
                //Log exception that happened in backgroundWork
            }
        }
    }
}