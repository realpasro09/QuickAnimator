using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace QuickAnimator
{
    public sealed partial class MainPage : Page
    {
        InkManager m_InkManager = new InkManager();
        InkManager m_InkManagerFrame = new InkManager();
        Canvas _mCurrentCanvasFrame = new Canvas();
        InkManager m_HighLightManager = new InkManager();
        private int _selectedFrame = 1;

        private uint m_PenId;
        private uint _touchID;
        private Point _previousContactPt;
        private Point currentContactPt;
        private double x1;
        private double y1;
        private double x2;
        private double y2;
        private int _frames = 1;

        private Color m_CurrentDrawingColor = Colors.Black;
        private double m_CurrentDrawingSize = 4;
        private Color m_CurrentHighlightColor = Colors.Yellow;
        private double m_CurrentHighlightSize = 8;
        private string m_CurrentMode = "Ink";
        private bool m_IsRecognizing = false;
        private Dictionary<int,Canvas> _smallFrameContainer = new Dictionary<int, Canvas>();  
        private List<FramesCollection> _framesArray = new List<FramesCollection>();
        readonly DispatcherTimer _timer = new DispatcherTimer();
        private DispatcherTimer _mycontroltimer = new DispatcherTimer();
        private FileOpenPicker _openProject;
        private StorageFile _projectfile;
        private int _currentOpenFrame = 0;
        private bool _loadterminado = false;

        public InkManager CurrentManager
        {
            get
            {
                if (m_CurrentMode == "Ink") return m_InkManager;

                return m_HighLightManager;
            }
            set { m_InkManager = value; }
        }

        public InkManager CurrentManagerFrame
        {
            get
            {
                if (m_CurrentMode == "Ink") return m_InkManagerFrame;

                return m_HighLightManager;
            }
            set { m_InkManagerFrame = value; }
        }

        public MainPage()
        {
            this.InitializeComponent();

            InkCanvas.PointerPressed += new PointerEventHandler(OnCanvasPointerPressed);
            InkCanvas.PointerMoved += new PointerEventHandler(OnCanvasPointerMoved);
            InkCanvas.PointerReleased += new PointerEventHandler(OnCanvasPointerReleased);
            InkCanvas.PointerExited += new PointerEventHandler(OnCanvasPointerReleased);

        }

        #region Pointer Event Handlers


        public void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (m_InkManager.GetStrokes().Count > 0)
            {
                RenderStrokesFrame(CurrentManager, _mCurrentCanvasFrame);
            }
            //OnCanvasPointerReleasedFrame(sender, e);
            if (e.Pointer.PointerId == m_PenId)
            {
                PointerPoint pt = e.GetCurrentPoint(InkCanvas);

                if (m_CurrentMode == "Erase")
                {
                    System.Diagnostics.Debug.WriteLine("Erasing : Pointer Released");

                    m_InkManager.ProcessPointerUp(pt);
                    m_HighLightManager.ProcessPointerUp(pt);
                }
                else
                {
                    // Pass the pointer information to the InkManager. 
                    CurrentManager.ProcessPointerUp(pt);
                }
            }
            else if (e.Pointer.PointerId == _touchID)
            {
                // Process touch input 
            }

            _touchID = 0;
            m_PenId = 0;

            // Call an application-defined function to render the ink strokes. 

            RefreshCanvas();

            e.Handled = true;
        }

        private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            //OnCanvasPointerMovedFrame(sender, e);
            if (e.Pointer.PointerId == m_PenId)
            {
                PointerPoint pt = e.GetCurrentPoint(InkCanvas);

                // Render a red line on the canvas as the pointer moves.  
                // Distance() is an application-defined function that tests 
                // whether the pointer has moved far enough to justify  
                // drawing a new line. 
                currentContactPt = pt.Position;
                x1 = _previousContactPt.X;
                y1 = _previousContactPt.Y;
                x2 = currentContactPt.X;
                y2 = currentContactPt.Y;

                var color = m_CurrentMode == "Ink" ? m_CurrentDrawingColor : m_CurrentHighlightColor;
                var size = m_CurrentMode == "Ink" ? m_CurrentDrawingSize : m_CurrentHighlightSize;

                if (Distance(x1, y1, x2, y2) > 2.0 && m_CurrentMode != "Erase")
                {
                    Line line = new Line()
                    {
                        X1 = x1,
                        Y1 = y1,
                        X2 = x2,
                        Y2 = y2,
                        StrokeThickness = size,
                        Stroke = new SolidColorBrush(color)
                    };


                    if (m_CurrentMode == "Highlight") line.Opacity = 0.4;
                    _previousContactPt = currentContactPt;

                    // Draw the line on the canvas by adding the Line object as 
                    // a child of the Canvas object. 
                    InkCanvas.Children.Add(line);
                }

                if (m_CurrentMode == "Erase")
                {
                    System.Diagnostics.Debug.WriteLine("Erasing : Pointer Update");

                    m_InkManager.ProcessPointerUpdate(pt);
                    m_HighLightManager.ProcessPointerUpdate(pt);
                }
                else
                {
                    // Pass the pointer information to the InkManager. 
                    CurrentManager.ProcessPointerUpdate(pt);
                }
            }

            else if (e.Pointer.PointerId == _touchID)
            {
                // Process touch input 
            }


        }

        private double Distance(double x1, double y1, double x2, double y2)
        {
            double d = 0;
            d = Math.Sqrt(Math.Pow((x2 - x1), 2) + Math.Pow((y2 - y1), 2));
            return d;
        }

        public void OnCanvasPointerPressed(object sender, PointerRoutedEventArgs e)
        {

            // OnCanvasPointerPressedFrame(sender, e);
            // Get information about the pointer location. 
            PointerPoint pt = e.GetCurrentPoint(InkCanvas);
            _previousContactPt = pt.Position;

            // Accept input only from a pen or mouse with the left button pressed.  
            PointerDeviceType pointerDevType = e.Pointer.PointerDeviceType;
            if (pointerDevType == PointerDeviceType.Pen ||
                    pointerDevType == PointerDeviceType.Mouse &&
                    pt.Properties.IsLeftButtonPressed)
            {
                if (m_CurrentMode == "Erase")
                {
                    System.Diagnostics.Debug.WriteLine("Erasing : Pointer Pressed");

                    m_InkManager.ProcessPointerDown(pt);
                    m_HighLightManager.ProcessPointerDown(pt);
                }
                else
                {
                    // Pass the pointer information to the InkManager. 
                    CurrentManager.ProcessPointerDown(pt);
                }

                m_PenId = pt.PointerId;
                
                e.Handled = true;
            }

            else if (pointerDevType == PointerDeviceType.Touch)
            {
                // Process touch input 
            }
        }

        #endregion 

        #region Mode Functions

        // Change the color and width in the default (used for new strokes) to the values
        // currently set in the current context.
        private void SetDefaults(double strokeSize, Color color)
        {
            var newDrawingAttributes = new InkDrawingAttributes();
            newDrawingAttributes.Size = new Size(strokeSize, strokeSize);
            newDrawingAttributes.Color = color;
            newDrawingAttributes.FitToCurve = true;
            CurrentManager.SetDefaultDrawingAttributes(newDrawingAttributes);
        }

        private void HighlightMode()
        {
            m_CurrentMode = "Highlight";
            m_HighLightManager.Mode = Windows.UI.Input.Inking.InkManipulationMode.Inking;
            SetDefaults(m_CurrentHighlightSize, m_CurrentHighlightColor);
        }

        private void InkMode()
        {
            m_CurrentMode = "Ink";
            m_InkManager.Mode = Windows.UI.Input.Inking.InkManipulationMode.Inking;
            SetDefaults(m_CurrentDrawingSize, m_CurrentDrawingColor);
        }

        private void SelectMode()
        {
            m_InkManager.Mode = Windows.UI.Input.Inking.InkManipulationMode.Selecting;
            
        }

        private void EraseMode()
        {
            m_InkManager.Mode = Windows.UI.Input.Inking.InkManipulationMode.Erasing;
            m_HighLightManager.Mode = Windows.UI.Input.Inking.InkManipulationMode.Erasing;
            m_CurrentMode = "Erase";
            //selCanvas.style.cursor = "url(images/erase.cur), auto";
        }

        //function tempEraseMode()
        //{
        //    saveMode();
        //    selContext.strokeStyle = "rgba(255,255,255,0.0)";
        //    context = selContext;
        //    inkManager.mode = inkManager.mode = Windows.UI.Input.Inking.InkManipulationMode.erasing;
        //    selCanvas.style.cursor = "url(images/erase.cur), auto";
        //}

        #endregion

        #region Rendering Functions

        private async void RestoreData(InkManager current)
        {
            if (m_InkManager.GetStrokes().Count > 0)
            {
                var newCanvas = NewCanvas();

                newCanvas.Children.Add(FrameId(_frames));
                RenderStrokesFrame(CurrentManager, newCanvas);
                _mCurrentCanvasFrame = newCanvas;
                FramesContainer.Children.Add(_mCurrentCanvasFrame);
                _smallFrameContainer.Add(_frames, _mCurrentCanvasFrame);
                scroll1.ScrollToHorizontalOffset(MaxWidth);
                _selectedFrame = _frames;
                InkMode();

                var strokesFromLastCurrentManager = CurrentManager.GetStrokes();
                var newCurrentManager = new InkManager();
                foreach (var stroke in strokesFromLastCurrentManager)
                {
                    newCurrentManager.AddStroke(stroke.Clone());
                }

                var uniqueFrame = new FramesCollection() { Key = _frames, Manager = newCurrentManager };
                _framesArray.Add(uniqueFrame);
                _frames = _frames + 1;

                RefreshCanvas();
            }
        }
        private async void ReadInk(StorageFile storageFile)
        {
            if (storageFile != null)
            {
                using (var stream = await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    m_InkManager = new InkManager();
                    m_InkManager.LoadAsync(stream).Completed = (s,a) => RestoreData(m_InkManager);
                }
            }
        }

        private async void ReadInkFrame(StorageFile storageFile)
        {
            if (storageFile != null)
            {
                using (var stream = await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    await m_InkManagerFrame.LoadAsync(stream);

                    if (m_InkManagerFrame.GetStrokes().Count > 0)
                    {
                        RenderStrokes(CurrentManagerFrame);
                    }
                }
            }
        }

        private void RenderStroke(InkStroke stroke, Color color, double width,  Canvas PInkCanvas, double opacity = 1)
        {
            var renderingStrokes = stroke.GetRenderingSegments();
            var path = new Windows.UI.Xaml.Shapes.Path();
            path.Data = new PathGeometry();
            ((PathGeometry)path.Data).Figures = new PathFigureCollection();
            var pathFigure = new PathFigure();
            pathFigure.StartPoint = renderingStrokes.First().Position;
            ((PathGeometry)path.Data).Figures.Add(pathFigure);
            foreach (var renderStroke in renderingStrokes)
            {
                pathFigure.Segments.Add(new BezierSegment()
                {
                    Point1 = renderStroke.BezierControlPoint1,
                    Point2 = renderStroke.BezierControlPoint2,
                    Point3 = renderStroke.Position
                });
            }

            path.StrokeThickness = width;
            path.Stroke = new SolidColorBrush(color);

            path.Opacity = opacity;

            PInkCanvas.Children.Add(path);
        }

        private void RenderStrokeFrame(InkStroke stroke, Color color, double width, Canvas PInkCanvas, double opacity = 1)
        {
            var renderingStrokes = stroke.GetRenderingSegments();
            var path = new Windows.UI.Xaml.Shapes.Path();
            path.Data = new PathGeometry();
            ((PathGeometry)path.Data).Figures = new PathFigureCollection();
            var pathFigure = new PathFigure();
            var startX = (renderingStrokes.First().Position.X / InkCanvas.ActualWidth) * _mCurrentCanvasFrame.Width;
            var startY = (renderingStrokes.First().Position.Y / InkCanvas.ActualHeight) * _mCurrentCanvasFrame.Height;
            pathFigure.StartPoint = new Point(startX,startY);
            ((PathGeometry)path.Data).Figures.Add(pathFigure);

            foreach (var renderStroke in renderingStrokes)
            {
                var b = new BezierSegment();
                var x = (renderStroke.BezierControlPoint1.X/InkCanvas.ActualWidth)*_mCurrentCanvasFrame.Width ;
                var y = (renderStroke.BezierControlPoint1.Y/InkCanvas.ActualHeight)*_mCurrentCanvasFrame.Height ;
                var point1 = new Point(x, y);
                x = (renderStroke.BezierControlPoint2.X / InkCanvas.ActualWidth) * _mCurrentCanvasFrame.Width;
                y = (renderStroke.BezierControlPoint2.Y / InkCanvas.ActualHeight) * _mCurrentCanvasFrame.Height;
                var point2 = new Point(x, y);
                x = (renderStroke.Position.X / InkCanvas.ActualWidth) * _mCurrentCanvasFrame.Width;
                y = (renderStroke.Position.Y / InkCanvas.ActualHeight) * _mCurrentCanvasFrame.Height;
                var point3 = new Point(x, y);
                
                b.Point1 = point1;
                b.Point2 = point2;
                b.Point3 = point3;
                
                pathFigure.Segments.Add(b);
            }

            
            switch ((int)width)
            {
                case 2:
                    path.StrokeThickness = 1;
                    break;
                case 4:
                    path.StrokeThickness = 2;
                    break;
                case 10:
                    path.StrokeThickness = 4;
                    break;
                default:
                    path.StrokeThickness = 1;
                    break;
            }
            path.Stroke = new SolidColorBrush(color);

            path.Opacity = opacity;
            PInkCanvas.Children.Add(path);
          
        }

        private void RenderStrokes(InkManager Pm_InkManager)
        {
            var strokes = Pm_InkManager.GetStrokes();

            foreach (var stroke in strokes)
            {
                RenderStroke(stroke, stroke.DrawingAttributes.Color, stroke.DrawingAttributes.Size.Width, InkCanvas);
            }
        }

        private void RenderStrokesFrame(InkManager Pm_InkManager, Canvas myCanvas)
        {
            var strokes = Pm_InkManager.GetStrokes();
            foreach (var stroke in strokes)
            {
                RenderStrokeFrame(stroke, stroke.DrawingAttributes.Color, stroke.DrawingAttributes.Size.Width, myCanvas);
            }
        }

        private void RefreshCanvas()
        {
            InkCanvas.Children.Clear();

            RenderStrokes(CurrentManager);
        }
        
        #endregion

        #region Additional Commands

        private async void Load(object sender, RoutedEventArgs e)
        {
            try
            {
                var savedimage =
                await ApplicationData.Current.LocalFolder.GetFileAsync(_projectfile.DisplayName + "_" + _currentOpenFrame +".png");
                ReadInk(savedimage);
            }
            catch (Exception exception)
            {
                _loadterminado = true;
            }
        }

        #region Quick Commands
        private async void Save(object sender, RoutedEventArgs e)
        {
            var existeFrame = _framesArray.Any(framesCollection => framesCollection.Key == _frames);
            if (!existeFrame)
            {
                var framenuevo = new FramesCollection() { Key = _frames, Manager = CurrentManager };
                _framesArray.Add(framenuevo);
            }

            var animatorSave = new AnimatorSave {CantidadFrames = _frames};
            var saveAnimation = new Windows.Storage.Pickers.FileSavePicker();
            saveAnimation.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            saveAnimation.DefaultFileExtension = ".xml";
            saveAnimation.FileTypeChoices.Add("XML", new string[] { ".xml" });
            var file2 = await saveAnimation.PickSaveFileAsync();
            await WriteXml(animatorSave, file2);

            //StorageFile filereader = await ApplicationData.Current.LocalFolder.GetFileAsync("data.xml");
            //Data = await ReadXml<List<AnimatorSave>>(file);
           
                foreach (var frame in _framesArray)
                {
                    try
                    {
                        var saveFrame = new Windows.Storage.Pickers.FileSavePicker
                        {
                            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop,
                            DefaultFileExtension = ".png"
                        };
                        saveFrame.FileTypeChoices.Add("PNG", new string[] { ".png" });
                        var path = file2.DisplayName + "_" + frame.Key + ".png";//file2.Path.Replace(file2.DisplayName+".xml",file2.DisplayName+"_"+frame.Key+".png");
                        var framefilesave = await ApplicationData.Current.LocalFolder.CreateFileAsync(path);
                        using (IOutputStream ab = await framefilesave.OpenAsync(FileAccessMode.ReadWrite))
                        {
                            if (ab != null)
                                await frame.Manager.SaveAsync(ab);
                        }
                    }
                    catch (Exception)
                    {
                    }
                    
                }
        }

        private async Task<T> ReadXml<T>(StorageFile xmldata)
        {
            var xmlser = new XmlSerializer(typeof(List<AnimatorSave>));
            T data;
            using (var strm = await xmldata.OpenStreamForReadAsync())
            {
                TextReader reader = new StreamReader(strm);
                data = (T)xmlser.Deserialize(reader);
            }
            return data;
        }

        private async Task WriteXml<T>(T data, StorageFile file)
        {
            try
            {
                var sw = new StringWriter();
                var xmlser = new XmlSerializer(typeof(T));
                xmlser.Serialize(sw, data);

                using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (var outputStream = fileStream.GetOutputStreamAt(0))
                    {
                        using (var dataWriter = new DataWriter(outputStream))
                        {
                            dataWriter.WriteString(sw.ToString());
                            await dataWriter.StoreAsync();
                            dataWriter.DetachStream();
                        }

                        await outputStream.FlushAsync();
                    }
                }
            }
            catch (Exception e)
            {
                throw new NotImplementedException(e.Message.ToString());

            }

        }

        private async void Copy(object sender, RoutedEventArgs e)
        {
            var strokes = m_InkManager.GetStrokes();

            for (int i = 0; i < strokes.Count; i++)
            {
                strokes[i].Selected = true;
            }

            m_InkManager.CopySelectedToClipboard();

            var msgdlg = new MessageDialog("Copied to clipboard successfully");
            await msgdlg.ShowAsync();

        }

        private async void Paste(object sender, RoutedEventArgs e)
        {

            var canpaste = m_InkManager.CanPasteFromClipboard();
            if (canpaste)
            {
                // panelcanvas.Children.Clear(); 
                m_InkManager.PasteFromClipboard(_previousContactPt);
                var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
                if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Bitmap))
                {
                    await dataPackageView.GetBitmapAsync();
                    RandomAccessStreamReference rv = await dataPackageView.GetBitmapAsync();
                    IRandomAccessStream irac = await rv.OpenReadAsync();
                    BitmapImage img = new BitmapImage();
                    img.SetSource(irac);
                    image.Source = img;

                }
            }
            else
            {
                var msgdlg = new MessageDialog("Clipboard is empty or unable to paste from clipboard");
                msgdlg.ShowAsync();
            }
        }

        private void Clear(object sender, RoutedEventArgs e)
        {
            m_InkManager.Mode = Windows.UI.Input.Inking.InkManipulationMode.Erasing;

            var strokes = m_InkManager.GetStrokes();

            foreach (var t in strokes)
            {
                t.Selected = true;
            }

            m_InkManager.DeleteSelected();

            InkCanvas.Background = new SolidColorBrush(Colors.White);
            InkCanvas.Children.Clear();

            m_InkManager.Mode = InkManipulationMode.Inking;
            _mCurrentCanvasFrame.Children.Clear();
            _mCurrentCanvasFrame.Children.Add(FrameId(_selectedFrame));
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            RefreshCanvas();
        }
        #endregion
        #endregion

        #region Inking Functions
        
        private async void Select(object sender, RoutedEventArgs e)
        {
            SelectMode();
        }

        private void Erase(object sender, RoutedEventArgs e)
        {
            m_InkManager.Mode = Windows.UI.Input.Inking.InkManipulationMode.Erasing;

            var strokes = m_InkManager.GetStrokes();

            m_InkManager.DeleteSelected();

            EraseMode();
        }

        #endregion

        #region Flyout Context Menus

        Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        private async void SelectColor(object sender, RoutedEventArgs e)
        {
            try
            {
                var menu = new PopupMenu();
                menu.Commands.Add(new UICommand("Black", null, 1));
                //menu.Commands.Add(new UICommandSeparator());
                menu.Commands.Add(new UICommand("Blue", null, 2));
                //menu.Commands.Add(new UICommandSeparator());
                menu.Commands.Add(new UICommand("Red", null, 3));
                menu.Commands.Add(new UICommand("Green", null, 4));

                System.Diagnostics.Debug.WriteLine("Context Menu is opening");

                var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));

                if (chosenCommand != null)
                {
                    switch ((int)chosenCommand.Id)
                    {
                        case 1:
                            m_CurrentDrawingColor = Colors.Black;
                            break;
                        case 2:
                            m_CurrentDrawingColor = Colors.Blue;
                            break;
                        case 3:
                            m_CurrentDrawingColor = Colors.Red;
                            break;
                        case 4:
                            m_CurrentDrawingColor = Colors.Green;
                            break;
                    }

                    InkMode();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void SelectHighlightColor(object sender, RoutedEventArgs e)
        {
            try
            {
                var menu = new PopupMenu();
                menu.Commands.Add(new UICommand("Yellow", null, 1));
                //menu.Commands.Add(new UICommandSeparator());
                menu.Commands.Add(new UICommand("Aqua", null, 2));
                //menu.Commands.Add(new UICommandSeparator());
                menu.Commands.Add(new UICommand("Line", null, 3));

                System.Diagnostics.Debug.WriteLine("Context Menu is opening");

                var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));

                if (chosenCommand != null)
                {
                    switch ((int)chosenCommand.Id)
                    {
                        case 1:
                            m_CurrentHighlightColor = Colors.Yellow;
                            break;
                        case 2:
                            m_CurrentHighlightColor = Colors.Aqua;
                            break;
                        case 3:
                            m_CurrentHighlightColor = Colors.Lime;
                            break;
                    }

                    HighlightMode();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void SelectHighlightSize(object sender, RoutedEventArgs e)
        {
            try
            {
                var menu = new PopupMenu();
                menu.Commands.Add(new UICommand("Small", null, 0));
                menu.Commands.Add(new UICommand("Medium", null, 1));
                menu.Commands.Add(new UICommand("Large", null, 2));

                System.Diagnostics.Debug.WriteLine("Context Menu is opening");

                var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));

                if (chosenCommand != null)
                {
                    switch ((int)chosenCommand.Id)
                    {
                        case 0:
                            m_CurrentHighlightSize = 8;
                            break;
                        case 1:
                            m_CurrentHighlightSize = 12;
                            break;
                        case 2:
                            m_CurrentHighlightSize = 16;
                            break;
                    }

                    HighlightMode();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void SelectAction(object sender, RoutedEventArgs e)
        {
            try
            {
                var menu = new PopupMenu();
                menu.Commands.Add(new UICommand("Copy", null, 0));
                menu.Commands.Add(new UICommand("Paste", null, 1));
                menu.Commands.Add(new UICommand("Save", null, 2));
                menu.Commands.Add(new UICommand("Load", null, 3));
                menu.Commands.Add(new UICommand("Refresh", null, 4));
                menu.Commands.Add(new UICommand("Clear", null, 5));

                System.Diagnostics.Debug.WriteLine("Context Menu is opening");

                var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));

                if (chosenCommand != null)
                {
                    switch ((int)chosenCommand.Id)
                    {
                        case 0:
                            Copy(sender, e);
                            break;
                        case 1:
                            Paste(sender, e);
                            break;
                        case 2:
                            Save(sender, e);
                            break;
                        case 3:
                            Load(sender, e);
                            break;
                        case 4:
                            Refresh(sender, e);
                            break;
                        case 5:
                            Clear(sender, e);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        

        #endregion

        #region QuickFunctions

        private async void SelectSize(object sender, RoutedEventArgs e)
        {
            try
            {

                m_CurrentDrawingColor = Colors.Black;
                var menu = new PopupMenu();
                menu.Commands.Add(new UICommand("Pequeño", null, 0));
                ////menu.Commands.Add(new UICommand("Small", null, 1));
                ////menu.Commands.Add(new UICommandSeparator());
                menu.Commands.Add(new UICommand("Mediano", null, 2));
                ////menu.Commands.Add(new UICommandSeparator());
                ////menu.Commands.Add(new UICommand("Large", null, 3));
                menu.Commands.Add(new UICommand("Grande", null, 4));

                //System.Diagnostics.Debug.WriteLine("Context Menu is opening");

                var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));

                if (chosenCommand != null)
                {
                    switch ((int)chosenCommand.Id)
                    {
                        case 0:
                            m_CurrentDrawingSize = 2;
                            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 103);
                            break;
                        case 2:
                            m_CurrentDrawingSize = 4;
                            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 102);
                            break;
                        case 4:
                            m_CurrentDrawingSize = 10;
                            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 101);
                            break;
                    }

                    InkMode();
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void SelectEraserSize(object sender, RoutedEventArgs e)
        {
            try
            {
                m_CurrentDrawingColor = Colors.White;
                var menu = new PopupMenu();
                menu.Commands.Add(new UICommand("Pequeño", null, 0));
                //menu.Commands.Add(new UICommand("Small", null, 1));
                ////menu.Commands.Add(new UICommandSeparator());
                //menu.Commands.Add(new UICommand("Medium", null, 2));
                ////menu.Commands.Add(new UICommandSeparator());
                //menu.Commands.Add(new UICommand("Large", null, 3));
                menu.Commands.Add(new UICommand("Grande", null, 4));
                menu.Commands.Add(new UICommand("Lienzo", null, 6));

                System.Diagnostics.Debug.WriteLine("Context Menu is opening");

                var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));

                if (chosenCommand != null)
                {
                    switch ((int)chosenCommand.Id)
                    {
                        case 0:
                            m_CurrentDrawingSize = 10;
                            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 104);
                            InkMode();
                            break;
                        case 1:
                            m_CurrentDrawingSize = 4;
                            break;
                        case 2:
                            EraseMode();
                            break;
                        case 3:
                            m_CurrentDrawingSize = 8;
                            break;
                        case 4:
                            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 105);
                            m_CurrentDrawingSize = 40;
                            InkMode();
                            break;
                    }

                    
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void Toolbox_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 101);
        }

        private void InkCanvas_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (m_CurrentDrawingColor == Colors.White)
            {
                switch ((int)m_CurrentDrawingSize)
                {
                    case 10:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 104);
                        break;
                    default:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 105);
                        break;
                }   
            }
            else
            {
                switch ((int)m_CurrentDrawingSize)
                {
                    case 2:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 103);
                        break;
                    case 4:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 102);
                        break;
                    case 10:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 101);
                        break;
                    default:
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 101);
                        break;
                }
            }
            
        }

        private void NewProject(object sender, RoutedEventArgs e)
        {
            New.Visibility = Visibility.Collapsed;
            Open.Visibility = Visibility.Collapsed;
            Lapiz.Visibility = Visibility.Visible;
            Borrador.Visibility = Visibility.Visible;
            Seleccion.Visibility = Visibility.Visible;
            BorrarTodo.Visibility = Visibility.Visible;
            logo.Visibility = Visibility.Collapsed;
            InkCanvas.Visibility = Visibility.Visible;
            image.Visibility = Visibility.Visible;
            BottomToolbar.Visibility = Visibility.Visible;
            Reproducir.Visibility = Visibility.Visible;
            Salvar.Visibility = Visibility.Visible;

            var newCanvas = NewCanvas();

            _mCurrentCanvasFrame = newCanvas;

            newCanvas.Children.Add(FrameId(_frames));
            FramesContainer.Children.Add(_mCurrentCanvasFrame);
            _smallFrameContainer.Add(_frames, _mCurrentCanvasFrame);

            InkMode();
        }

        private void BottomToolbar_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            //Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 101);
        }

        private void NewFrame(object sender, RoutedEventArgs e)
        {
            var uniqueFrame = new FramesCollection() {Key = _frames, Manager = CurrentManager};
            _framesArray.Add(uniqueFrame);
            _frames = _frames + 1;
            CurrentManager = new InkManager();
            RefreshCanvas();
            var newCanvas = NewCanvas();

            newCanvas.Children.Add(FrameId(_frames));
            _mCurrentCanvasFrame = newCanvas;
            FramesContainer.Children.Add(_mCurrentCanvasFrame);
            _smallFrameContainer.Add(_frames, _mCurrentCanvasFrame);
            scroll1.ScrollToHorizontalOffset(MaxWidth);
            _selectedFrame = _frames;
            InkMode();
        }

        private void DuplicateFrame(object sender, RoutedEventArgs e)
        {
            var uniqueFrame = new FramesCollection() { Key = _frames, Manager = CurrentManager };
            _framesArray.Add(uniqueFrame);
            _frames = _frames + 1;
            var strokesFromLastCurrentManager = CurrentManager.GetStrokes();
            CurrentManager = new InkManager();

            foreach (var stroke in strokesFromLastCurrentManager)
            {
                CurrentManager.AddStroke(stroke.Clone());
            }
            RefreshCanvas();

            
            var newCanvas = NewCanvas();

            newCanvas.Children.Add(FrameId(_frames));
            RenderStrokesFrame(CurrentManager, newCanvas);
            _mCurrentCanvasFrame = newCanvas;
            FramesContainer.Children.Add(_mCurrentCanvasFrame);
            _smallFrameContainer.Add(_frames, _mCurrentCanvasFrame);
            scroll1.ScrollToHorizontalOffset(MaxWidth);
            _selectedFrame = _frames;
            InkMode();
        }

        private TextBox FrameId(int frameid)
        {
            var idframe = new TextBox
            {
                Text = frameid.ToString(),
                MaxWidth = 2,
                FontSize = 40,
                Foreground = new SolidColorBrush(Colors.Red),
                IsReadOnly = true
            };
            return idframe;
        }

        private Canvas NewCanvas()
        {
            var newCanvas = new Canvas
                {
                    Name = _frames.ToString(),
                    Height = 120,
                    Width = 200,
                    Margin = new Thickness(10),
                    Background = new SolidColorBrush(Colors.White)
                };
            newCanvas.PointerMoved += newCanvas_PointerMoved;
            newCanvas.PointerExited += newCanvas_PointerExited;
            newCanvas.PointerReleased += newCanvas_PointerReleased;
            return newCanvas;
        }

        void newCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var existeFrame = _framesArray.Any(framesCollection => framesCollection.Key == _frames);
            if (!existeFrame)
            {
                var framenuevo = new FramesCollection() {Key = _frames, Manager = CurrentManager};
                _framesArray.Add(framenuevo);
            }
            var clickedFrame = (Canvas) sender;
            var id = Convert.ToInt32(clickedFrame.Name);
            _selectedFrame = id;

            var current = (from framesCollection in _framesArray where framesCollection.Key == id select framesCollection.Manager).FirstOrDefault();

            CurrentManager = current;

            foreach (var smallFrame in _smallFrameContainer.Where(smallFrame => smallFrame.Key == id))
            {
                var tempSmallFrame = smallFrame;
                _mCurrentCanvasFrame = smallFrame.Value;
                _mCurrentCanvasFrame.Children.Clear();
                RenderStrokesFrame(CurrentManager,_mCurrentCanvasFrame);
                _mCurrentCanvasFrame = smallFrame.Value;
                _mCurrentCanvasFrame.Children.Add(FrameId(id));
                RenderStrokesFrame(CurrentManager, _mCurrentCanvasFrame);
                continue;
            }

            RefreshCanvas();
        }

        void newCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
        {

            var currentFrame = (Canvas)sender;
            if (currentFrame.Name != _selectedFrame.ToString())
            {
                currentFrame.Width = 200;
                currentFrame.Height = 120;
            }
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 101);
        }

        void newCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var currentFrame = (Canvas) sender;
            
            currentFrame.Width = 210;
            currentFrame.Height = 130;
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 101);
        }

        private void ReproducirFrame(object sender, RoutedEventArgs e)
        {
            var existeFrame = _framesArray.Any(framesCollection => framesCollection.Key == _frames);
            if (!existeFrame)
            {
                var framenuevo = new FramesCollection() { Key = _frames, Manager = CurrentManager };
                _framesArray.Add(framenuevo);
            }
            Toolbox.IsOpen = false;
            BottomToolbar.IsOpen = false;
            var canvasTemp = InkCanvas;
            InkCanvas.Children.Clear();
            _selectedFrame = 1;
            
            _timer.Tick += DispatcherTimerEventHandler;
            _timer.Interval = new TimeSpan(0, 0, 0, 1);
            _timer.Start();
        }

        private void DispatcherTimerEventHandler(object sender, object e)
        {
            if (_selectedFrame > _frames)
            {
                _timer.Stop();
                Toolbox.IsOpen = true;
                BottomToolbar.IsOpen = true;
                _selectedFrame = _frames;
                scroll1.ScrollToHorizontalOffset(MaxWidth);
                foreach (var frame in _framesArray.Where(frame => frame.Key == _frames))
                {
                    var strokesFromLastCurrentManager = frame.Manager.GetStrokes();
                    InkCanvas.Children.Clear();
                    CurrentManager = new InkManager();
                    foreach (var stroke in strokesFromLastCurrentManager)
                    {
                        CurrentManager.AddStroke(stroke.Clone());
                    }
                    RefreshCanvas();
                }
                return;
            }
            InkCanvas.Children.Clear();
            foreach (var frame in _framesArray.Where(frame => frame.Key == _selectedFrame))
            {
                var strokesFromLastCurrentManager = frame.Manager.GetStrokes();
                InkCanvas.Children.Clear();
                CurrentManager = new InkManager();
                foreach (var stroke in strokesFromLastCurrentManager)
                {
                    CurrentManager.AddStroke(stroke.Clone());
                }
                RefreshCanvas();
            }

            _selectedFrame = _selectedFrame + 1;
        }

        private void SalvarAnimacion(object sender, RoutedEventArgs e)
        {
            Save(sender, e);
        }

        private async void OpenProject(object sender, RoutedEventArgs e)
        {
            New.Visibility = Visibility.Collapsed;
            Open.Visibility = Visibility.Collapsed;
            Lapiz.Visibility = Visibility.Visible;
            Borrador.Visibility = Visibility.Visible;
            Seleccion.Visibility = Visibility.Visible;
            BorrarTodo.Visibility = Visibility.Visible;
            logo.Visibility = Visibility.Collapsed;
            InkCanvas.Visibility = Visibility.Visible;
            image.Visibility = Visibility.Visible;
            BottomToolbar.Visibility = Visibility.Visible;
            Reproducir.Visibility = Visibility.Visible;
            Salvar.Visibility = Visibility.Visible;

            _openProject = new Windows.Storage.Pickers.FileOpenPicker();
            _openProject.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            _openProject.FileTypeFilter.Add(".xml");
            _projectfile = await _openProject.PickSingleFileAsync();
            //var animatorSaveClass = await ReadXml<AnimatorSave>(projectfile);

            _mycontroltimer.Tick += DispatcherTimerEventHandler2;
            _mycontroltimer.Interval = new TimeSpan(0, 0, 0, 1,500);
            _mycontroltimer.Start();
            //small change
      }

#endregion

        private void DispatcherTimerEventHandler2(object sender, object e)
        {
            if (!_loadterminado)
            {
                _currentOpenFrame = _currentOpenFrame + 1;
                Load(sender, new RoutedEventArgs());
            }
            else
            {
                _mycontroltimer.Stop();
            }
            
        }
    }
}
