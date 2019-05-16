using SkiaSharp;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(RawImage))]
class SKCanvasView : MonoBehaviour
{

    public float Width
    {
        get
        {
            return m_rawImage.rectTransform.rect.width;
        }
    }
    public float Height
    {
        get
        {
            return m_rawImage.rectTransform.rect.height;
        }
    }

    private bool m_ignorePixelScaling;

    private RawImage m_rawImage;
    private SKSurface m_surface;
    private SKCanvas m_canvas;
    private Texture2D m_texture;
    private Color32[] m_textureColors;
    private byte[] m_buffer;

    private SKImageInfo m_imageInfo;

    public event EventHandler<SKPaintSurfaceEventArgs> PaintSurface;

    private void Awake()
    {
        //Initializing canvas       
        m_rawImage = this.GetComponent<RawImage>();
        var size = m_rawImage.GetPixelAdjustedRect();
        m_imageInfo = new SKImageInfo((int)size.width, (int)size.height, SKColorType.Rgba8888);
        Debug.Log("SKInfo: " + m_imageInfo);
        // Create the Skia drawing surface and canvas.
        m_surface = SKSurface.Create(m_imageInfo);
        m_canvas = m_surface.Canvas;

        TextureFormat format = (m_imageInfo.ColorType == SKColorType.Rgba8888) ? TextureFormat.RGBA32 : TextureFormat.BGRA32;
        m_texture = new Texture2D(m_imageInfo.Width, m_imageInfo.Height, format, false, true);
        m_texture.wrapMode = TextureWrapMode.Clamp;
        m_textureColors = m_texture.GetPixels32();
        m_buffer = new byte[m_textureColors.Length * 4];

    }

    protected virtual void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        PaintSurface?.Invoke(this, e);
    }


    public bool IgnorePixelScaling
    {
        get { return m_ignorePixelScaling; }
        set
        {
            m_ignorePixelScaling = value;
            Invalidate();
        }
    }

    public void Invalidate()
    {
        Debug.Log("Drawing..");
        OnPaintSurface(new SKPaintSurfaceEventArgs(m_surface, m_imageInfo));
        // Pull a Skia image object out of the canvas...
        using (var pixmap = m_surface.PeekPixels())
        {
            //Debug.LogError(pixmap.ColorType.ToString());
            IntPtr pColors = IntPtr.Zero;

            // Copy it to the Unity texture...
            var pixels = pixmap.GetPixels();

            using (var handle = new RAIICGHandle(m_textureColors))
            {
                pColors = handle.Address;
                Marshal.Copy(pixels, m_buffer, 0, m_buffer.Length);
                Marshal.Copy(m_buffer, 0, pColors, m_buffer.Length);
                m_texture.SetPixels32(m_textureColors);
                m_texture.Apply(false);
            }
        }

        // And drop it into the RawImage object.        
        m_rawImage.texture = m_texture;

        Debug.Log("Draw complete.");
    }

    private void OnDestroy()
    {
        // Dispose all our (disposable) Skia objects.

        m_canvas.Dispose();
        m_surface.Dispose();
    }

}

