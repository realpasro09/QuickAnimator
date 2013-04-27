using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Storage;
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
        private Dictionary<int,InkManager> _framesArray = new Dictionary<int, InkManager>(); 

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

            var newCanvas = new Canvas
            {
                Name = "frame" + _frames,
                Height = 120,
                Width = 200,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Colors.White)
            };

            _mCurrentCanvasFrame = newCanvas;
            var idframe = new TextBox
            {
                Text = _frames.ToString(),
                MaxWidth = 2,
                FontSize = 40,
                Foreground = new SolidColorBrush(Colors.Black),
                IsReadOnly = true
            };

            newCanvas.Children.Add(idframe);
            FramesContainer.Children.Add(_mCurrentCanvasFrame);
            
 
            InkMode();

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

        private async void ReadInk(StorageFile storageFile)
        {
            if (storageFile != null)
            {
                using (var stream = await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    await m_InkManager.LoadAsync(stream);

                    if (m_InkManager.GetStrokes().Count > 0)
                    {
                        RenderStrokes(CurrentManager);
                    }
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

            path.StrokeThickness = 1;
            path.Stroke = new SolidColorBrush(color);

            path.Opacity = opacity;
            PInkCanvas.Children.Add(path);
          
        }

        private void RenderStrokes(InkManager Pm_InkManager)
        {
            var strokes = Pm_InkManager.GetStrokes();

            var highlightStrokes = m_HighLightManager.GetStrokes();

            foreach (var stroke in strokes)
            {
                if (stroke.Selected)
                {
                    RenderStroke(stroke, stroke.DrawingAttributes.Color, stroke.DrawingAttributes.Size.Width * 2, InkCanvas);
                }
                else
                {
                    RenderStroke(stroke, stroke.DrawingAttributes.Color, stroke.DrawingAttributes.Size.Width, InkCanvas);
                }
            }

            foreach (var stroke in highlightStrokes)
            {
                if (stroke.Selected)
                {
                    RenderStroke(stroke, stroke.DrawingAttributes.Color, stroke.DrawingAttributes.Size.Width * 2, InkCanvas, 0.4);
                }
                else
                {
                    RenderStroke(stroke, stroke.DrawingAttributes.Color, stroke.DrawingAttributes.Size.Width, InkCanvas, 0.4);
                }
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

            if (m_IsRecognizing && m_InkManager.GetStrokes().Count > 0)
            {
                var recognizers = m_InkManager.GetRecognizers();

                m_InkManager.SetDefaultRecognizer(recognizers.First());

                var task = m_InkManager.RecognizeAsync(InkRecognitionTarget.All);

                task.Completed = new AsyncOperationCompletedHandler<IReadOnlyList<InkRecognitionResult>>((operation, status) =>
                {
                    double previousX = 0;
                    double previousY = 0;

                    var firstWord = true;

                    foreach (var result in operation.GetResults())
                    {
                        
                        var isNewWord = Math.Abs(result.BoundingRect.Left - previousX) > 10;
                        var isNewLine = Math.Abs(result.BoundingRect.Bottom - previousY) > 20;

                        previousX = result.BoundingRect.Right;
                        previousY = result.BoundingRect.Bottom;

                        firstWord = false;
                        m_IsRecognizing = false;
                    }
                });
            }
        }

        private void RefreshCanvasFrame()
        {
            _mCurrentCanvasFrame.Children.Clear();

            RenderStrokes(CurrentManagerFrame);

            if (m_IsRecognizing && m_InkManager.GetStrokes().Count > 0)
            {
                var recognizers = m_InkManager.GetRecognizers();

                m_InkManager.SetDefaultRecognizer(recognizers.First());

                var task = m_InkManager.RecognizeAsync(InkRecognitionTarget.All);

                task.Completed = new AsyncOperationCompletedHandler<IReadOnlyList<InkRecognitionResult>>((operation, status) =>
                {
                    double previousX = 0;
                    double previousY = 0;

                    var firstWord = true;

                    foreach (var result in operation.GetResults())
                    {

                        var isNewWord = Math.Abs(result.BoundingRect.Left - previousX) > 10;
                        var isNewLine = Math.Abs(result.BoundingRect.Bottom - previousY) > 20;

                        previousX = result.BoundingRect.Right;
                        previousY = result.BoundingRect.Bottom;

                        firstWord = false;
                        m_IsRecognizing = false;
                    }
                });
            }
        }

        #endregion

        #region Additional Commands

        private async void Load(object sender, RoutedEventArgs e)
        {
            try
            {
                Windows.Storage.Pickers.FileOpenPicker open = new Windows.Storage.Pickers.FileOpenPicker();
                open.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                open.FileTypeFilter.Add(".png");
                StorageFile filesave = await open.PickSingleFileAsync();

                ReadInk(filesave);
            }
            catch (Exception ex)
            {

                var dlge = new MessageDialog(ex.Message);
                dlge.ShowAsync();
            }


        }

        private async void Save(object sender, RoutedEventArgs e)
        {
            try
            {
                Windows.Storage.Pickers.FileSavePicker save = new Windows.Storage.Pickers.FileSavePicker();
                save.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                save.DefaultFileExtension = ".png";
                save.FileTypeChoices.Add("PNG", new string[] { ".png" });
                StorageFile filesave = await save.PickSaveFileAsync();
                using (IOutputStream ab = await filesave.OpenAsync(FileAccessMode.ReadWrite))
                {
                    if (ab != null)
                        await m_InkManager.SaveAsync(ab);
                }
            }
            catch (Exception ex)
            {

                var dlge = new MessageDialog(ex.Message);
                dlge.ShowAsync();
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

            for (int i = 0; i < strokes.Count; i++)
            {
                strokes[i].Selected = true;
            }

            m_InkManager.DeleteSelected();

            InkCanvas.Background = new SolidColorBrush(Colors.White);
            InkCanvas.Children.Clear();

            m_InkManager.Mode = Windows.UI.Input.Inking.InkManipulationMode.Inking;
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            RefreshCanvas();
        }

        #endregion

        #region Inking Functions

        private void Recognize(object sender, RoutedEventArgs e)
        {
            m_IsRecognizing = !m_IsRecognizing;
            RefreshCanvas();
        }

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

        private async void SelectSize(object sender, RoutedEventArgs e)
        {
            try
            {

                m_CurrentDrawingColor = Colors.Black;
                //var menu = new PopupMenu();
                //menu.Commands.Add(new UICommand("Small", null, 0));
                ////menu.Commands.Add(new UICommand("Small", null, 1));
                ////menu.Commands.Add(new UICommandSeparator());
                //menu.Commands.Add(new UICommand("Medium", null, 2));
                ////menu.Commands.Add(new UICommandSeparator());
                ////menu.Commands.Add(new UICommand("Large", null, 3));
                //menu.Commands.Add(new UICommand("Large", null, 4));

                //System.Diagnostics.Debug.WriteLine("Context Menu is opening");

                //var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));

                //if (chosenCommand != null)
                //{
                //switch ((int)chosenCommand.Id)
                //{
                //case 0:
                //m_CurrentDrawingSize = 2;
                //Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 103);
                //break;
                //case 1:
                //m_CurrentDrawingSize = 4;
                //break;
                //case 2:
                m_CurrentDrawingSize = 4;
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 102);
                //break;
                //case 3:
                //    m_CurrentDrawingSize = 8;
                //    break;
                //case 4:
                //    m_CurrentDrawingSize = 10;
                //    Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Custom, 101);
                //    break;
                //}

                InkMode();
                //}
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
                menu.Commands.Add(new UICommand("Smallest", null, 0));
                //menu.Commands.Add(new UICommand("Small", null, 1));
                ////menu.Commands.Add(new UICommandSeparator());
                //menu.Commands.Add(new UICommand("Medium", null, 2));
                ////menu.Commands.Add(new UICommandSeparator());
                //menu.Commands.Add(new UICommand("Large", null, 3));
                menu.Commands.Add(new UICommand("Largest", null, 4));

                System.Diagnostics.Debug.WriteLine("Context Menu is opening");

                var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));

                if (chosenCommand != null)
                {
                    switch ((int)chosenCommand.Id)
                    {
                        case 0:
                            m_CurrentDrawingSize = 10;
                            break;
                        case 1:
                            m_CurrentDrawingSize = 4;
                            break;
                        case 2:
                            m_CurrentDrawingSize = 6;
                            break;
                        case 3:
                            m_CurrentDrawingSize = 8;
                            break;
                        case 4:
                            m_CurrentDrawingSize = 40;
                            break;
                    }

                    InkMode();
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

        private void NewProject(object sender, RoutedEventArgs e)
        {
            New.Visibility = Visibility.Collapsed;
            Lapiz.Visibility = Visibility.Visible;
            Borrador.Visibility = Visibility.Visible;
            Seleccion.Visibility = Visibility.Visible;
            BorrarTodo.Visibility = Visibility.Visible;
            logo.Visibility = Visibility.Collapsed;
            InkCanvas.Visibility = Visibility.Visible;
            image.Visibility = Visibility.Visible;
            BottomToolbar.Visibility = Visibility.Visible;
        }

        private void BottomToolbar_OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 101);
        }

        private void NewFrame(object sender, RoutedEventArgs e)
        {

            _frames += 1;
            _framesArray.Add(_frames, new InkManager());
            InkManager newCurrentManager;
            _framesArray.TryGetValue(_frames, out newCurrentManager);
            CurrentManager = newCurrentManager;
            RefreshCanvas();
            var newCanvas = new Canvas
            {
                Name = "frame" + _frames,
                Height = 120,
                Width = 200,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Colors.White)
            };

            var idframe = new TextBox
                {
                    Text = _frames.ToString(),
                    MaxWidth = 2,
                    FontSize = 40,
                    Foreground = new SolidColorBrush(Colors.Black),
                    IsReadOnly = true
                };

            newCanvas.Children.Add(idframe);
            _mCurrentCanvasFrame = newCanvas;
            FramesContainer.Children.Add(_mCurrentCanvasFrame);
            scroll1.ScrollToHorizontalOffset(MaxWidth);
        }
    }
}
