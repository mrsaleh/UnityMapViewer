using Mapsui;
using Mapsui.Geometries;
using Mapsui.Geometries.Utilities;
using Mapsui.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(SKCanvasView))]
public partial class MapControl : MonoBehaviour, IMapControl
{
    private SKCanvasView _canvas;
    private double _innerRotation;
    private double _previousAngle;
    private double _previousRadius = 1f;
    private TouchMode _mode = TouchMode.None;
    private ConcurrentQueue<Action> m_actionsQueue = new ConcurrentQueue<Action>();
    /// <summary>
    /// Saver for center before last pinch movement
    /// </summary>
    private Point _previousTouch = new Point();


    private float Width
    {
        get
        {
            return _canvas.Width;
        }
    }

    private float Height
    {
        get
        {
            return _canvas.Height;
        }
    }

    public void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        _canvas = this.GetComponent<SKCanvasView>();
        _canvas.IgnorePixelScaling = true;
        _canvas.PaintSurface += CanvasOnPaintSurface;

        SetViewportSize(); // todo: check if size is available, perhaps we need a load event

        Map = new Map();

    }

    public float PixelDensity => Screen.dpi;
   

    private void RunOnUIThread(Action action)
    {
        m_actionsQueue.Enqueue(action);
    }

    private void Update()
    {
        Action action;
        if (m_actionsQueue.TryDequeue(out action))
        {
            action();
        }

        CheckTouch();
    }

    private MotionEvent m;

    private void CheckTouch()
    {
        if (Input.GetMouseButtonDown(0))
        {
            m = new MotionEvent(new TouchEvent() {
                Index = 0,
                Position = Input.mousePosition,
                Status = TouchStatus.Down
            });
            m.ActionIndex = 0;
            m.Action = MotionEventActions.Down;
            HandleTouch(m);
            Debug.Log("Mouse down");
        }

        if (Input.GetMouseButton(0))
        {
            m.Action = MotionEventActions.Move;            
            HandleTouch(m);
            Debug.Log("Mouse move");
        }

        if (Input.GetMouseButtonUp(0))
        {
            m.Action = MotionEventActions.Up;
            HandleTouch(m);
            Debug.Log("Mouse up");
        }
    }


    private void CanvasOnPaintSurface(object sender, SKPaintSurfaceEventArgs args)
    {
        Debug.Log("Rendering map");
        Debug.Log($"{Viewport.Width},{Viewport.Height} with center at {Viewport.Center.X},{Viewport.Center.Y}");
        Renderer.Render(args.Surface.Canvas, Viewport, _map.Layers, _map.Widgets, _map.BackColor);
    }



    public void HandleTouch(MotionEvent Event)
    {

        var touchPoints = GetScreenPositions(Event, this.GetComponent<RectTransform>());

        switch (Event.Action)
        {
            case MotionEventActions.Up:
                Refresh();
                _mode = TouchMode.None;
                break;
            case MotionEventActions.Down:
            case MotionEventActions.Pointer1Down:
            case MotionEventActions.Pointer2Down:
            case MotionEventActions.Pointer3Down:
                if (touchPoints.Count >= 2)
                {
                    (_previousTouch, _previousRadius, _previousAngle) = GetPinchValues(touchPoints);
                    _mode = TouchMode.Zooming;
                    _innerRotation = Viewport.Rotation;
                }
                else
                {
                    Debug.Log("Start Dragging");
                    _mode = TouchMode.Dragging;
                    _previousTouch = touchPoints.First();
                }
                break;
            case MotionEventActions.Pointer1Up:
            case MotionEventActions.Pointer2Up:
            case MotionEventActions.Pointer3Up:
                // Remove the touchPoint that was released from the locations to reset the
                // starting points of the move and rotation
                touchPoints.RemoveAt(Event.ActionIndex);

                if (touchPoints.Count >= 2)
                {
                    (_previousTouch, _previousRadius, _previousAngle) = GetPinchValues(touchPoints);
                    _mode = TouchMode.Zooming;
                    _innerRotation = Viewport.Rotation;
                }
                else
                {
                    _mode = TouchMode.Dragging;
                    _previousTouch = touchPoints.First();
                }
                Refresh();
                break;
            case MotionEventActions.Move:
                switch (_mode)
                {
                    case TouchMode.Dragging:
                        {
                            if (touchPoints.Count != 1)
                                return;

                            var touch = touchPoints.First();
                            if (_previousTouch != null && !_previousTouch.IsEmpty())
                            {
                                Debug.Log($"Dragging from {touch} to {_previousTouch}");
                                //_viewport.Transform( touch, _previousTouch);
                                _viewport.Transform(new Point(0.0f,0.0f), new Point(20f, 0f));
                                Refresh();
                            }
                            _previousTouch = touch;
                        }
                        break;
                    case TouchMode.Zooming:
                        {
                            if (touchPoints.Count < 2)
                                return;

                            var (previousTouch, previousRadius, previousAngle) = (_previousTouch, _previousRadius, _previousAngle);
                            var (touch, radius, angle) = GetPinchValues(touchPoints);

                            double rotationDelta = 0;

                            if (!Map.RotationLock)
                            {
                                _innerRotation += angle - previousAngle;
                                _innerRotation %= 360;

                                if (_innerRotation > 180)
                                    _innerRotation -= 360;
                                else if (_innerRotation < -180)
                                    _innerRotation += 360;

                                if (Viewport.Rotation == 0 && Math.Abs(_innerRotation) >= Math.Abs(UnSnapRotationDegrees))
                                    rotationDelta = _innerRotation;
                                else if (Viewport.Rotation != 0)
                                {
                                    if (Math.Abs(_innerRotation) <= Math.Abs(ReSnapRotationDegrees))
                                        rotationDelta = -Viewport.Rotation;
                                    else
                                        rotationDelta = _innerRotation - Viewport.Rotation;
                                }
                            }

                            _viewport.Transform(touch, previousTouch, radius / previousRadius, rotationDelta);
                            RefreshGraphics();

                            (_previousTouch, _previousRadius, _previousAngle) = (touch, radius, angle);


                        }
                        break;
                }
                break;
        }
    }
    /// <summary>
    /// Gets the screen position in device independent units relative to the MapControl.
    /// </summary>
    /// <returns></returns>
    private List<Point> GetScreenPositions(MotionEvent motionEvent, RectTransform view)
    {
        var result = new List<Point>();
        for (var i = 0; i < motionEvent.PointerCount; i++)
        {
            var p = new Point((motionEvent.GetX(i) - view.rect.xMin ) / PixelDensity, (motionEvent.GetY(i) - view.rect.yMin) / PixelDensity);
            result.Add(p);/*.ToDeviceIndependentUnits(PixelDensity)*/
            Debug.Log($"new pt => {p.X},{p.Y}");

        }
        return result;
    }


    /// <summary>
    /// Gets the screen position in device independent units relative to the MapControl.
    /// </summary>
    /// <param name="motionEvent"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    private Point GetScreenPosition(MotionEvent motionEvent, RectTransform view)
    {
        return GetScreenPositionInPixels(motionEvent, view);
    }


    /// <summary>
    /// Gets the screen position in pixels relative to the MapControl.
    /// </summary>
    /// <param name="motionEvent"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    private static Point GetScreenPositionInPixels(MotionEvent motionEvent, RectTransform view)
    {
        return new Point(
            motionEvent.GetX(0) - view.rect.xMin,
            motionEvent.GetY(0) - view.rect.yMin)/*.ToMapsui()*/;
    }

    public void RefreshGraphics()
    {
        RunOnUIThread(RefreshGraphicsWithTryCatch);
    }

    private void RefreshGraphicsWithTryCatch()
    {
        try
        {
            // Bothe Invalidate and _canvas.Invalidate are necessary in different scenarios.
            _canvas?.Invalidate();
        }
        catch (ObjectDisposedException e)
        {
            // See issue: https://github.com/Mapsui/Mapsui/issues/433
            // What seems to be happening. The Activity is Disposed. Appently it's children get Disposed
            // explicitly by something in Xamarin. During this Dispose the MessageCenter, which is itself
            // not disposed gets another notification to call RefreshGraphics.
            Debug.LogWarning("This can happen when the parent Activity is disposing." + e);
        }
    }

    /*
    protected override void OnLayout(bool changed, int l, int t, int r, int b)
    {
        _canvas.Top = t;
        _canvas.Bottom = b;
        _canvas.Left = l;
        _canvas.Right = r;
    }
    */
    public void OpenBrowser(string url)
    {
        Application.OpenURL(url);
    }

    public void OnDestroy()
    {
        Unsubscribe();
    }


    private static (Point centre, double radius, double angle) GetPinchValues(List<Point> locations)
    {
        if (locations.Count < 2)
            throw new ArgumentException();

        double centerX = 0;
        double centerY = 0;

        foreach (var location in locations)
        {
            centerX += location.X;
            centerY += location.Y;
        }

        centerX = centerX / locations.Count;
        centerY = centerY / locations.Count;

        var radius = Algorithms.Distance(centerX, centerY, locations[0].X, locations[0].Y);

        var angle = Math.Atan2(locations[1].Y - locations[0].Y, locations[1].X - locations[0].X) * 180.0 / Math.PI;

        return (new Point(centerX, centerY), radius, angle);
    }

    private float ViewportWidth => ToDeviceIndependentUnits(Width);
    private float ViewportHeight => ToDeviceIndependentUnits(Height);

    /// <summary>
    /// In native Android touch positions are in pixels whereas the canvas needs
    /// to be drawn in device independent units (otherwise labels on raster tiles will be unreadable
    /// and symbols will be too small). This method converts pixels to device independent units.
    /// </summary>
    /// <returns>The pixels given as input translated to device independent units.</returns>
    private float ToDeviceIndependentUnits(float pixelCoordinate)
    {
        return pixelCoordinate /*/ PixelDensity*/;
    }
}