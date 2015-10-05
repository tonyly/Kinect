using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Diagnostics;

namespace KinectDataManagement.DVR
{
    
    public class KinectRecorder : IDisposable
    {
        #region Attributes

        private BinaryWriter _colorWriter;
        private BinaryWriter _depthWriter;
        private BinaryWriter _infraredWriter;
        private BinaryWriter _fileMetaDataWriter;

        private SemaphoreSlim _colorSemaphore = new SemaphoreSlim(1);
        private SemaphoreSlim _depthSemaphore = new SemaphoreSlim(1);
        private SemaphoreSlim _infraredSemaphore = new SemaphoreSlim(1);
        private SemaphoreSlim _generalSemaphore = new SemaphoreSlim(1);
  
        private KinectSensor _sensor;

        private ColorRecorder _colorRecorder;
        private DepthRecorder _depthRecorder;
        private InfraredRecorder _infraredRecorder;

        private ColorFrameReader _colorReader;
        private DepthFrameReader _depthReader;
        private InfraredFrameReader _infraredReader;

        private ConcurrentQueue<RecordFrame> _recordColorQueue = new ConcurrentQueue<RecordFrame>();
        private ConcurrentQueue<RecordFrame> _recordDepthQueue = new ConcurrentQueue<RecordFrame>();
        private ConcurrentQueue<RecordFrame> _recordInfraredQueue = new ConcurrentQueue<RecordFrame>();

        private bool _isStarted = false;
        private bool _isStopped = false;
        
        private Task _processColorFramesTask = null;
        private Task _processDepthFramesTask = null;
        private Task _processInfraredFramesTask = null;
        private CancellationTokenSource _processFramesCancellationTokenSource = new CancellationTokenSource();

        private DateTime _previousFlushTime;

        private bool _enableColorRecorder;
        private bool _enableDepthRecorder;        
        private bool _enableInfraredRecorder;

        private string _colorLocation;
        private string _depthLocation;
        private string _infraredLocation;

        private int _fpsColor;
        private int _fpsDepth;
        private int _fpsInfrared;

        private int colorFpsCounter = 100;
        private int depthFpsCounter = 100;
        private int infraredFpsCounter = 100;

        private int colorCounter = 0;
        private int depthCounter = 0;
        private int infraredCounter = 0;

       

        //Stopwatch
        /*
        Stopwatch colorSw = new Stopwatch();
        Stopwatch depthSw = new Stopwatch();
        Stopwatch infraredSw = new Stopwatch();
        private double colorSum = 0;
        private double depthSum = 0;
        private double infraredSum = 0;
         */

        DateTime laterColor;
        DateTime laterDepth;
        DateTime laterInfrared;
        DateTime nowColor;
        DateTime nowDepth;
        DateTime nowInfrared;

        private double displayColorFrames = 30;
        private double displayDepthFrames = 30;
        private double displayInfraredFrames = 30;
        int colorFrames = 0;
        int depthFrames = 0;
        int infraredFrames = 0;
        int timeElapsedColor = 0;
        int timeElapsedDepth = 0;
        int timeElapsedInfrared = 0;
        private void TimeCheckColor()
        {
            nowColor = DateTime.Now;
            int result = DateTime.Compare(laterColor, nowColor);        
            if (result <= 0)
            {

                timeElapsedColor++;
                DisplayColorFrames = colorFrames / timeElapsedColor;
                colorFrames = 0;
                timeElapsedColor = 0;            
                nowColor = DateTime.Now;
                laterColor = nowColor.AddSeconds(1);
            }
        }

        private void TimeCheckDepth()
        {
            nowDepth = DateTime.Now;
            int result = DateTime.Compare(laterDepth, nowDepth);
            if (result <= 0)
            {

                timeElapsedDepth++;
                DisplayDepthFrames = depthFrames / timeElapsedDepth;
                depthFrames = 0;
                timeElapsedDepth = 0;
                nowDepth = DateTime.Now;
                laterDepth = nowDepth.AddSeconds(1);
            }
        }
        private void TimeCheckInfrared()
        {
            nowInfrared = DateTime.Now;
            int result = DateTime.Compare(laterInfrared, nowInfrared);
            if (result <= 0)
            {

                timeElapsedInfrared++;
                DisplayInfraredFrames = infraredFrames / timeElapsedInfrared;
                infraredFrames = 0;
                timeElapsedInfrared = 0;
                nowInfrared = DateTime.Now;
                laterInfrared = nowInfrared.AddSeconds(1);
            }
        }
        //int color_delay, depth_delay, infrared_delay;

        #endregion

        #region Properties

        public bool IsStarted
        {
            get { return _isStarted; }
            set { _isStarted = value; }
        }
        public bool IsStopped
        {
            get { return _isStopped; }
            set { _isStopped = value; }
        }
        public bool EnableColorRecorder
        {
            get { return _enableColorRecorder; }
            set { _enableColorRecorder = value; }
        }
        public bool EnableDepthRecorder
        {
            get { return _enableDepthRecorder; }
            set { _enableDepthRecorder = value; }
        }
        public bool EnableInfraredRecorder
        {
            get { return _enableInfraredRecorder; }
            set { _enableInfraredRecorder = value; }
        }
        public ICodec ColorRecorderCodec
        {
            get { return _colorRecorder.Codec; }
            set { _colorRecorder.Codec = value; }
        }
        public ICodec DepthRecorderCodec
        {
            get { return _depthRecorder.Codec; }
            set { _depthRecorder.Codec = value; }
        }
        public ICodec InfraredRecorderCodec
        {
            get { return _infraredRecorder.Codec; }
            set { _infraredRecorder.Codec = value; }
        }
        public String ColorLocation
        {
            get { return _colorLocation; }
            set { _colorLocation = value; }
        }
        public String DepthLocation
        {
            get { return _depthLocation; }
            set { _depthLocation = value; }
        }
        public String InfraredLocation
        {
            get { return _infraredLocation; }
            set { _infraredLocation = value; }
        }
        public int ColorFramerate
        {
            get { return _fpsColor; }
            set { _fpsColor = value; }
        }
        public int DepthFramerate
        {
            get { return _fpsDepth; }
            set { _fpsDepth = value; }
        }
        public int InfraredFramerate
        {
            get { return _fpsInfrared; }
            set { _fpsInfrared = value; }
        }

        public double DisplayColorFrames
        {
            get
            {
                return displayColorFrames;
            }

            set
            {
                displayColorFrames = value;
            }
        }

        public double DisplayDepthFrames
        {
            get
            {
                return displayDepthFrames;
            }

            set
            {
                displayDepthFrames = value;
            }
        }

        public double DisplayInfraredFrames
        {
            get
            {
                return displayInfraredFrames;
            }

            set
            {
                displayInfraredFrames = value;
            }
        }

        #endregion

        #region constructor
        public KinectRecorder( string location, KinectSensor sensor = null)
        {
            this.ColorLocation = (location + "COLOR");
            this.DepthLocation = (location + "DEPTH");
            this.InfraredLocation = (location + "INFRARED");

            Stream fmdStream = File.Open((location+"METADATA"), FileMode.Create);
            Stream colorStream = File.Open(ColorLocation, FileMode.Create);
            Stream depthStream = File.Open(DepthLocation, FileMode.Create);
            Stream infraredStream = File.Open(InfraredLocation, FileMode.Create);

            _fileMetaDataWriter = new BinaryWriter(fmdStream);
            _colorWriter = new BinaryWriter(colorStream);
            _depthWriter = new BinaryWriter(depthStream);
            _infraredWriter = new BinaryWriter(infraredStream);

            _sensor = sensor;
            _colorRecorder = new ColorRecorder(_colorWriter);
            _depthRecorder = new DepthRecorder(_depthWriter);
            _infraredRecorder = new InfraredRecorder(_infraredWriter);

            //DisplayColorFrames = 0;
        }
        #endregion

        #region Dispose

        ~KinectRecorder()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_colorReader != null)
                {
                    _colorReader.FrameArrived -= _colorReader_FrameArrived;
                    _colorReader.Dispose();
                    _colorReader = null;
                }

                if (_depthReader != null)
                {
                    _depthReader.FrameArrived -= _depthReader_FrameArrived;
                    _depthReader.Dispose();
                    _depthReader = null;
                }

                if (_infraredReader != null)
                {
                    _infraredReader.FrameArrived -= _infraredReader_FrameArrived;
                    _infraredReader.Dispose();
                    _infraredReader = null;
                }
                #region Color disposing
                try
                {
                    _colorSemaphore.Wait();
                    if (_colorWriter != null)
                    {
                        _colorWriter.Flush();

                        if (_colorWriter.BaseStream != null)
                        {
                            _colorWriter.BaseStream.Flush();
                        }

                        _colorWriter.Dispose();
                        _colorWriter = null;
                    }
                }
                catch (Exception ex)
                {
                    // TODO: Change to log the error
                    System.Diagnostics.Debug.WriteLine("Error Disposing Color: " + ex);
                }
                finally
                {
                    _colorSemaphore.Dispose();
                }
                #endregion

                #region Depth disposing
                try
                {
                    _depthSemaphore.Wait();
                    if (_depthWriter != null)
                    {
                        _depthWriter.Flush();

                        if (_depthWriter.BaseStream != null)
                        {
                            _depthWriter.BaseStream.Flush();
                        }

                        _depthWriter.Dispose();
                        _depthWriter = null;
                    }
                }
                catch (Exception ex)
                {
                    // TODO: Change to log the error
                    System.Diagnostics.Debug.WriteLine("Error Disposing Depth: " + ex);
                }
                finally
                {
                    _depthSemaphore.Dispose();
                }
                #endregion

                #region Infrared disposing
                try
                {
                    _infraredSemaphore.Wait();
                    if (_infraredWriter != null)
                    {
                        _infraredWriter.Flush();

                        if (_infraredWriter.BaseStream != null)
                        {
                            _infraredWriter.BaseStream.Flush();
                        }

                        _infraredWriter.Dispose();
                        _infraredWriter = null;
                    }
                }
                catch (Exception ex)
                {
                    // TODO: Change to log the error
                    System.Diagnostics.Debug.WriteLine("Error Disposing Infrared: " + ex);
                }
                finally
                {
                    _infraredSemaphore.Dispose();
                }
                #endregion
                if (_processFramesCancellationTokenSource != null)
                {
                    _processFramesCancellationTokenSource.Dispose();
                    _processFramesCancellationTokenSource = null;
                }

            }
        }

        #endregion

        #region Methods

        #region Start Recording

        public void Start()
        {
            nowColor = DateTime.Now;
            nowDepth = DateTime.Now;
            nowInfrared = DateTime.Now;
            laterColor = nowColor.AddSeconds(1);
            laterDepth = nowDepth.AddSeconds(1);
            laterInfrared = nowInfrared.AddSeconds(1);
            if (_isStarted)
                return;

            if (_sensor != null)
            {
               
                if (EnableColorRecorder)
                {
                    _colorReader = _sensor.ColorFrameSource.OpenReader();
                    _colorReader.FrameArrived += _colorReader_FrameArrived;
                    
                }

                if (EnableDepthRecorder)
                {
                    _depthReader = _sensor.DepthFrameSource.OpenReader();
                    _depthReader.FrameArrived += _depthReader_FrameArrived;
                    
                }

                if (EnableInfraredRecorder)
                {
                    _infraredReader = _sensor.InfraredFrameSource.OpenReader();
                    _infraredReader.FrameArrived += _infraredReader_FrameArrived;
                   
                }

                if (!_sensor.IsOpen)
                    _sensor.Open();

            }

            _isStarted = true;

            try
            {
                _generalSemaphore.Wait();

                var metadata = new FileMetadata()
                {
                    Version = this.GetType().GetTypeInfo().Assembly.GetName().Version.ToString(),
                    HasColor = this.EnableColorRecorder,
                    HasDepth = this.EnableDepthRecorder,
                    HasInfrared = this.EnableInfraredRecorder,
                    ColorLocation = this.ColorLocation,
                    DepthLocation = this.DepthLocation,
                    InfraredLocation = this.InfraredLocation,
                    ColorCodecId = this.ColorRecorderCodec.ColorCodecId,
                    DepthCodecId = this.DepthRecorderCodec.DepthCodecId,
                    InfraredCodecId = this.InfraredRecorderCodec.InfraredCodecId,
                    FpsColor = 100-this.ColorFramerate,
                    FpsDepth = 100-this.DepthFramerate,
                    FpsInfrared = 100-this.InfraredFramerate
                  
                };
                _fileMetaDataWriter.Write(JsonConvert.SerializeObject(metadata));
            }
            catch (Exception ex)
            {
                
                System.Diagnostics.Debug.WriteLine("Error Saving MetaData: " + ex);
            }
            finally
            {
                _generalSemaphore.Release();
                if (_fileMetaDataWriter != null)
                {
                    _fileMetaDataWriter.Flush();

                    if (_fileMetaDataWriter.BaseStream != null)
                    {
                        _fileMetaDataWriter.BaseStream.Flush();
                    }

                    _fileMetaDataWriter.Dispose();
                    _fileMetaDataWriter = null;
                }


            }

            _processColorFramesTask = ProcessColorFramesAsync();
            _processDepthFramesTask = ProcessDepthFramesAsync();
            _processInfraredFramesTask = ProcessInfraredFramesAsync();
        }

        private async Task ProcessColorFramesAsync()
        {
            _previousFlushTime = DateTime.Now;
            var cancellationToken = _processFramesCancellationTokenSource.Token;
            await Task.Run(async () =>
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    RecordFrame frame;
                    if (_recordColorQueue.TryDequeue(out frame))
                    {
                        try
                        {
                            await _colorSemaphore.WaitAsync();

                            if (frame is RecordColorFrame)
                            {
                               
                                await _colorRecorder.RecordAsync((RecordColorFrame)frame);
                                
                               // System.Diagnostics.Debug.WriteLine("--- Processed Color Frame ({0})", _recordQueue.Count);
                            }
                            
                            frame = null;
                            ColorFlush();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error Processing Color frames: "+ex);
                        }
                        finally
                        {
                            _colorSemaphore.Release();
                        }
                    }
                    else
                    {
                        await Task.Delay(100);
                        if (_recordColorQueue.IsEmpty && _isStarted == false)
                        {
                            break;
                        }
                    }
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        private async Task ProcessDepthFramesAsync()
        {
            _previousFlushTime = DateTime.Now;
            var cancellationToken = _processFramesCancellationTokenSource.Token;
            await Task.Run(async () =>
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    RecordFrame frame;
                    if (_recordDepthQueue.TryDequeue(out frame))
                    {
                        try
                        {
                            await _depthSemaphore.WaitAsync();

                            
                            if (frame is RecordDepthFrame)
                            {
                                await _depthRecorder.RecordAsync((RecordDepthFrame)frame);

                                // System.Diagnostics.Debug.WriteLine("--- Processed Depth Frame ({0})", _recordQueue.Count);
                            }
                            
                            frame = null;
                            DepthFlush();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error Processing frames: " + ex);
                        }
                        finally
                        {
                            _depthSemaphore.Release();
                        }
                    }
                    else
                    {
                        await Task.Delay(100);
                        if (_recordDepthQueue.IsEmpty && _isStarted == false)
                        {
                            break;
                        }
                    }
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        private async Task ProcessInfraredFramesAsync()
        {
            _previousFlushTime = DateTime.Now;
            var cancellationToken = _processFramesCancellationTokenSource.Token;
            await Task.Run(async () =>
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    RecordFrame frame;
                    if (_recordInfraredQueue.TryDequeue(out frame))
                    {
                        try
                        {
                            await _infraredSemaphore.WaitAsync();

                            
                            if (frame is RecordInfraredFrame)
                            {
                                await _infraredRecorder.RecordAsync((RecordInfraredFrame)frame);

                                //  System.Diagnostics.Debug.WriteLine("--- Processed Infrared Frame ({0})", _recordQueue.Count);
                            }
                            frame = null;
                            InfraredFlush();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error Processing frames: " + ex);
                        }
                        finally
                        {
                            _infraredSemaphore.Release();
                        }
                    }
                    else
                    {
                        await Task.Delay(100);
                        if (_recordInfraredQueue.IsEmpty && _isStarted == false)
                        {
                            break;
                        }
                    }
                }
            }, cancellationToken).ConfigureAwait(false);
        }


        private void ColorFlush()
        {
            var now = DateTime.Now;

            if (now.Subtract(_previousFlushTime).TotalSeconds > 10)
            {
                _previousFlushTime = now;
                _colorWriter.Flush();
            }
        }
        private void DepthFlush()
        {
            var now = DateTime.Now;

            if (now.Subtract(_previousFlushTime).TotalSeconds > 10)
            {
                _previousFlushTime = now;
                _depthWriter.Flush();
            }
        }
        private void InfraredFlush()
        {
            var now = DateTime.Now;

            if (now.Subtract(_previousFlushTime).TotalSeconds > 10)
            {
                _previousFlushTime = now;
                _infraredWriter.Flush();
            }
        }

        #endregion

        #region Stop Recording
        public async Task StopAsync()
        {
            if (_isStopped)
                return;

            System.Diagnostics.Debug.WriteLine(">>> Attempt to Stop Recording (Color queue size {0})", _recordColorQueue.Count);
            System.Diagnostics.Debug.WriteLine(">>> Attempt to Stop Recording (Depth queue size {0})", _recordDepthQueue.Count);
            System.Diagnostics.Debug.WriteLine(">>> Attempt to Stop Recording (Infrared queue size {0})", _recordInfraredQueue.Count);

            _isStarted = false;
            _isStopped = true;

            if (_colorReader != null)
            {
                _colorReader.FrameArrived -= _colorReader_FrameArrived;
                _colorReader.Dispose();
                _colorReader = null;
            }

            if (_depthReader != null)
            {
                _depthReader.FrameArrived -= _depthReader_FrameArrived;
                _depthReader.Dispose();
                _depthReader = null;
            }

            if (_infraredReader != null)
            {
                _infraredReader.FrameArrived -= _infraredReader_FrameArrived;
                _infraredReader.Dispose();
                _infraredReader = null;
            }

            try
            {
                await _processColorFramesTask;
                await _processDepthFramesTask;
                await _processInfraredFramesTask;
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("!!! Process Canceled (in StopAsync)");
            }
            _processColorFramesTask = null;
            _processDepthFramesTask = null;
            _processInfraredFramesTask = null;

            await CloseWriterAsync();

            System.Diagnostics.Debug.WriteLine("<<< Stopping recording (DONE!) :D ");
            /*
            Console.WriteLine("oooo Color enqueue avg {0}",colorSum/colorCounter);
            Console.WriteLine("oooo Depth enqueue avg {0}", depthSum / depthCounter);
            Console.WriteLine("oooo Infrared enqueue avg {0}", infraredSum / infraredCounter);
             */
        }

        private async Task CloseWriterAsync()
        {
            try
            {
                await _generalSemaphore.WaitAsync();
                if (_colorWriter != null)
                {
                    _colorWriter.Flush();

                    if (_colorWriter.BaseStream != null)
                    {
                        _colorWriter.BaseStream.Flush();
                    }

                    _colorWriter.Dispose();
                    _colorWriter = null;
                }
                if (_depthWriter != null)
                {
                    _depthWriter.Flush();

                    if (_depthWriter.BaseStream != null)
                    {
                        _depthWriter.BaseStream.Flush();
                    }

                    _depthWriter.Dispose();
                    _depthWriter = null;
                }
                if (_infraredWriter != null)
                {
                    _infraredWriter.Flush();

                    if (_infraredWriter.BaseStream != null)
                    {
                        _infraredWriter.BaseStream.Flush();
                    }

                    _infraredWriter.Dispose();
                    _infraredWriter = null;
                }
            }
            catch (Exception ex)
            {
                // TODO: Change to log the error
                System.Diagnostics.Debug.WriteLine("Error Closing Writer: "+ex);
            }
            finally
            {
                _generalSemaphore.Release();
            }
        }

        #endregion

        #region Acquiring Frames
        
        private void _colorReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (_isStarted && colorFpsCounter > 0)
                {
                    if (frame != null)
                    {
                        
                        //colorSw.Start();
                        _recordColorQueue.Enqueue(new RecordColorFrame(frame));
                        //colorSw.Stop();
                        //colorSum += colorSw.Elapsed.TotalMilliseconds;
                        //colorSw.Reset();

                        TimeCheckColor();
                        colorFrames++;                       
                        colorCounter++;

                        //Console.WriteLine("Color Enqueue time = {0}", colorSw.Elapsed.TotalMilliseconds);                        
                        //System.Diagnostics.Debug.WriteLine("+++ Enqueued Color Frame ({0})", _recordQueue.Count);
                    }
                    
                    else
                    {
                       // System.Diagnostics.Debug.WriteLine("!!! FRAME SKIPPED (Color in KinectRecorder)");
                    }
                    colorFpsCounter -= ColorFramerate;
                }
                else if( colorFpsCounter == -50 || colorFpsCounter == -125)
                {
                    colorFpsCounter -= ColorFramerate;
                }
                else
                {
                    colorFpsCounter = 100;
                    
                   // System.Diagnostics.Debug.WriteLine("!!! FRAME SKIPPED ");
                }
            }
        }
        private void _depthReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (_isStarted && depthFpsCounter > 0)
                { 
                    if (frame != null)
                    {
                        //depthSw.Start();
                        _recordDepthQueue.Enqueue(new RecordDepthFrame(frame));                       
                        //depthSw.Stop();
                        //depthSum += depthSw.Elapsed.TotalMilliseconds;
                        //depthSw.Reset();
                        
                        TimeCheckDepth();
                        depthFrames++;
                        depthCounter++;

                        // Console.WriteLine("Depth Enqueue time = {0}", sw.Elapsed);                        
                        // System.Diagnostics.Debug.WriteLine("+++ Enqueued Depth Frame ({0})", _recordQueue.Count);
                    }
                    else
                    {
                      //  System.Diagnostics.Debug.WriteLine("!!! FRAME SKIPPED (Depth in KinectRecorder)");
                    }
                    depthFpsCounter -= DepthFramerate;
                }
                else if (depthFpsCounter == -50 || depthFpsCounter == -125)
                {
                    depthFpsCounter -= DepthFramerate;
                }
                else
                {
                    depthFpsCounter = 100;
                    //color_delay = global_delay;
                    // System.Diagnostics.Debug.WriteLine("!!! FRAME SKIPPED ");
                }
            }
        }
        
        private void _infraredReader_FrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (_isStarted && infraredFpsCounter > 0)
                {
                    if (frame != null)
                    {
                        //infraredSw.Start();
                        _recordInfraredQueue.Enqueue(new RecordInfraredFrame(frame));                      
                        //infraredSw.Stop();
                        //infraredSum += infraredSw.Elapsed.TotalMilliseconds;
                        //infraredSw.Reset();

                        TimeCheckInfrared();
                        infraredFrames++;
                        infraredCounter++;

                        // Console.WriteLine("Infrared Enqueue time = {0}", sw.Elapsed);                        
                        // System.Diagnostics.Debug.WriteLine("+++ Enqueued Infrared Frame ({0})", _recordQueue.Count);
                    }
                    else
                    {
                       // System.Diagnostics.Debug.WriteLine("!!! FRAME SKIPPED (Infrared in KinectRecorder)");
                    }
                    infraredFpsCounter -= InfraredFramerate; ;
                }
                else if (infraredFpsCounter == -50 || infraredFpsCounter == -125)
                {
                    infraredFpsCounter -= InfraredFramerate;
                }
                else
                {
                    infraredFpsCounter = 100;
                }
            }
        }

        #endregion

        #endregion


    }
}
