using Mapsui;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Projection;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Utilities;
using Mapsui.Widgets.ScaleBar;
using Mapsui.Widgets.Zoom;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class OSMMap : MonoBehaviour
{

    [SerializeField]
    private Texture2D m_marker;
    // Use this for initialization
    void Start()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        //Marker layer
        map.Layers.Add(CreatePointLayer(59.51146, 36.31670));


        //map.CRS = "EPSG:3857";
        //map.Transformation = new MinimalTransformation();
        //map.Widgets.Add(new ScaleBarWidget(map) { TextAlignment = Mapsui.Widgets.Alignment.Center, HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Center, VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Top });
        //map.Widgets.Add(new ZoomInOutWidget { MarginX = 0, MarginY = 0 });
        //map.Home = n => n.NavigateTo(new Point(6599038.475580856,348231.48864817794 ),map.Resolutions[16]);       

        // Get the lon lat coordinates from somewhere (Mapsui can not help you there)
        var locationIranMashhad = new Point(59.51146, 36.31670);
        // OSM uses spherical mercator coordinates. So transform the lon lat coordinates to spherical mercator
        var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(locationIranMashhad.X, locationIranMashhad.Y);
        // Set the center of the viewport to the coordinate. The UI will refresh automatically
        // Additionally you might want to set the resolution, this could depend on your specific purpose
        map.Home = n => n.NavigateTo(sphericalMercatorCoordinate, map.Resolutions[17]);


        var mapControl = this.GetComponent<MapControl>();
        mapControl.Map = map;



        Debug.Log("loading map ....");
    }


    private MemoryLayer CreatePointLayer(double lon, double lat)
    {
        var feature = new Feature();
        feature.Geometry = SphericalMercator.FromLonLat(lon, lat);
        SymbolStyle style = CreateBitmapStyle();
        return new MemoryLayer
        {
            Name = "PlayerMarker",
            IsMapInfoLayer = true,
            DataSource = new MemoryProvider(feature),
            Style = style
        };
    }

    private class Marker
    {
        public string Name { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public static IEnumerable<T> DeserializeFromStream<T>(Stream stream)
    {
        var serializer = new JsonSerializer();

        using (var sr = new StreamReader(stream))
        using (var jsonTextReader = new JsonTextReader(sr))
        {
            return serializer.Deserialize<List<T>>(jsonTextReader);
        }
    }

    private SymbolStyle CreateBitmapStyle()
    {
        // For this sample we get the bitmap from an embedded resouce
        // but you could get the data stream from the web or anywhere
        // else.
        var bitmapId = GetBitmapId();
        var bitmapHeight = 176; // To set the offset correct we need to know the bitmap height
        return new SymbolStyle { BitmapId = bitmapId, SymbolScale = 0.4, SymbolOffset = new Offset(0, bitmapHeight * 0.5) };
        
    }

    private int GetBitmapId()
    {
        var imageBytes = ImageConversion.EncodeToPNG(m_marker);
        MemoryStream imageStream = new MemoryStream(imageBytes);
        return BitmapRegistry.Instance.Register(imageStream);
    }
}


