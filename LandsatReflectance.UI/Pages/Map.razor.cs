using System.Collections.Concurrent;
using System.Diagnostics;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;
// using LandsatReflectance.Common.Models;
// using LandsatReflectance.Models;
using LandsatReflectance.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor;
using MouseEvent = GoogleMapsComponents.Maps.MouseEvent;
using Polygon = LandsatReflectance.UI.Utils.Polygon;

namespace LandsatReflectance.UI.Pages;

public partial class Map : ComponentBase
{
    [Inject]
    public required IWebAssemblyHostEnvironment Environment { get; set; } 
    
    [Inject]
    public required IJSRuntime JsRuntime { get; set; } 
    
    [Inject]
    public required ISnackbar Snackbar { get; set; } 
    
    [Inject]
    public required Wrs2AreasService Wrs2AreasService { get; set; } 
    
    
    private GoogleMap m_googleMap = null!;
    private MapOptions m_mapOptions = null!;

    private const string ParentDivHeight = "height: calc(100vh - (var(--mud-appbar-height) - var(--mud-appbar-height) / 4))";

    
    protected override void OnInitialized()
    {
        m_mapOptions = new MapOptions
        {
            Zoom = 2,
            Center = new LatLngLiteral
            {
                Lat = 0,
                Lng = 0 
            },
            MapTypeId = MapTypeId.Roadmap,
            DisableDefaultUI = true,
            ZoomControl = true,
        };
    }

    private async Task OnAfterMapRender()
    {
        if (Environment.IsDevelopment())
        {
            Snackbar.Add("Map loaded", Severity.Info);
        }
        
        await m_googleMap.InteropObject.AddListener<MouseEvent>("click", mouseEvents => { _ = OnClick(mouseEvents); });
    }

    private async Task OnClick(MouseEvent e)
    {
        if (Environment.IsDevelopment())
        {
            Snackbar.Add($"Left clicked: {e.LatLng.Lat:F}, {e.LatLng.Lng:F}", Severity.Info);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var pathAndRows = new List<(int, int)>();
            
            /*
            foreach (var area in Wrs2AreasService.Wrs2Areas)
            {
                const double tolerance = 5;
                var latTooFar = area.LatLongCoordinates.Select(latLong => latLong.Latitude).Any(lat => Math.Abs(lat - e.LatLng.Lat) >= tolerance);
                var longTooFar = area.LatLongCoordinates.Select(latLong => latLong.Longitude).Any(@long => Math.Abs(@long - e.LatLng.Lng) >= tolerance);
                if (latTooFar || longTooFar)
                    continue;

                if (Polygon.IsPointInPolygon(area.LatLongCoordinates, (e.LatLng.Lat, e.LatLng.Lng)))
                {
                    pathAndRows.Add((area.Metadata.Path, area.Metadata.Row));
                }
            }
             */

            
            stopwatch.Stop();
            Console.WriteLine($"[Map] {e.LatLng.Lat:F}, {e.LatLng.Lng:F}, finished in {stopwatch.Elapsed.TotalSeconds:F}s " +
                              $"({stopwatch.Elapsed.TotalMilliseconds}ms). Found {pathAndRows.Count} areas.");
            
            var pathsAndRowsStr = string.Join("\n", pathAndRows.Select((tuple, i) => $"{i}. {tuple.Item1}, {tuple.Item2}"));
            Console.WriteLine(pathsAndRowsStr);
        }
        
        var markerOptions = new MarkerOptions
        {
            Position = e.LatLng,
            Map = m_googleMap.InteropObject,
            // Title = "something",
            // Label = $"Marker @ {e.LatLng.Lat:F}, {e.LatLng.Lng:F}",
            Draggable = false,
            Icon = new Icon
            {
                Url = "http://maps.google.com/mapfiles/ms/icons/blue-dot.png"
            }
        };

        var newMarker = await Marker.CreateAsync(m_googleMap.JsRuntime, markerOptions);
        
        if (Environment.IsDevelopment())
        {
            Snackbar.Add($"Left clicked: {e.LatLng.Lat:F}, {e.LatLng.Lng:F}", Severity.Info);
            await newMarker.AddListener<MouseEvent>("click", async mouseEvent =>
            {
                await mouseEvent.Stop();
                // await OnMarkerClicked(newTarget, newMarker);
            });
        }
    }

    /*
    private async Task OnMarkerClicked(Target target, Marker marker)
    {
        string markerLabel = await marker.GetLabelText();
        string markerTitle = await marker.GetTitle();
        Snackbar.Add($"Marker clicked with: (Label: \"{markerLabel}\"), (Title: \"{markerTitle}\")", Severity.Info);
    }
     */
    
    // TODO: Re-write this
    public static List<T[]> SplitArray<T>(T[] array, int n)
    {
        var result = new List<T[]>();
        int chunkSize = array.Length / n;
        int remainder = array.Length % n;
        int startIndex = 0;

        for (int i = 0; i < n; i++)
        {
            // If there is a remainder, distribute the extra elements to the first few subarrays
            int currentChunkSize = chunkSize + (remainder > 0 ? 1 : 0);
            remainder--;

            // Create the subarray and copy elements from the original array
            var chunk = new T[currentChunkSize];
            Array.Copy(array, startIndex, chunk, 0, currentChunkSize);
            result.Add(chunk);

            startIndex += currentChunkSize;  // Move the starting index for the next subarray
        }

        return result;
    }
}