namespace LandsatReflectance.UI.Utils;

public static class Rand
{
    public static double GeneratePageSwitchDelayTime()
    {
        const double lowerBound = 0.3;
        const double upperBound = 1;
        
        const double mean = 3;
        const double stdDev = 0.3;

        double rawValue = GenerateNormal(mean, stdDev);
        double clampedLowerBound = Math.Min(lowerBound, rawValue);
        double clampedUpperBound = Math.Min(clampedLowerBound, upperBound);
        return clampedUpperBound;
    }
    
    public static double GenerateFormDelayTime()
    {
        const double lowerBound = 2.5;
        const double upperBound = 6;
        
        const double mean = 3.5;
        const double stdDev = 1.5;

        double rawValue = GenerateNormal(mean, stdDev);
        double clampedLowerBound = Math.Min(lowerBound, rawValue);
        double clampedUpperBound = Math.Min(clampedLowerBound, upperBound);
        return clampedUpperBound;
    }
    
    private static double GenerateNormal(double mean, double stdDev)
    {
        var random = new Random();
        double u1 = 1.0 - random.NextDouble(); // [0,1) -> (0,1]
        double u2 = 1.0 - random.NextDouble(); // [0,1) -> (0,1]
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * randStdNormal;
    }
}