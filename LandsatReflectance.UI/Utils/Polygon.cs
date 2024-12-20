// using LandsatReflectance.Models;

namespace LandsatReflectance.UI.Utils;

public static class Polygon
{
    // TODO: Try re-writing
    /*
    public static bool IsPointInPolygon(List<LatLong> polygonPoints, (double, double) point)
    {
        int n = polygonPoints.Count;
        bool isInside = false;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var xi = polygonPoints[i].Latitude;
            var yi = polygonPoints[i].Longitude;
            var xj = polygonPoints[j].Latitude;
            var yj = polygonPoints[j].Longitude;

            bool intersect = ((yi > point.Item2) != (yj > point.Item2)) &&
                             (point.Item1 < (xj - xi) * (point.Item2 - yi) / (yj - yi) + xi);

            if (intersect)
            {
                isInside = !isInside;
            }
        }

        return isInside;
    }
     */
}