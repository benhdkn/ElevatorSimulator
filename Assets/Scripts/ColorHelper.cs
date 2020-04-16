using UnityEngine;

public static class ColorHelper 
{
    private static readonly string[] passengerRGBStringColors = 
    {
        "0.962, 0.962, 0.962",
        "1.000, 0.949, 0.929",
        "1.000, 0.949, 0.854",
        "0.330, 0.122, 0.107",
        "0.679, 0.472, 0.381",
        "0.160, 0.069, 0.030",
    };

    public static Color GetRandomPassengerColor() 
    {
        string randomRGBColorstring = passengerRGBStringColors[Random.Range(0, passengerRGBStringColors.Length)];
        return ParseRGBColorString(randomRGBColorstring);
    }

    public static Color ParseRGBColorString(string s) 
    {
        s.Replace(" ", string.Empty);
        string[] rgb = s.Split(',');

        Color ret = Color.white;
        for (var i = 0; i < rgb.Length; i++)
        {
            ret[i] = float.Parse(rgb[i]);
        }
        return ret;
    }
}