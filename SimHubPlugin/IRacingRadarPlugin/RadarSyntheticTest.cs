using GameReaderCommon;
using System;
using System.Collections.Generic;
using System.Reflection;

internal sealed class FakeStatus : StatusDataBase
{
    public FakeStatus() { typeof(StatusDataBase).GetProperty("Opponents").GetSetMethod(true).Invoke(this, new object[] { new List<Opponent>() }); }
    public override object GetRawDataObject() { return null; }
}

internal static class RadarSyntheticTest
{
    private static int Main()
    {
        Assembly plugin = Assembly.LoadFrom("User.IRacingRadarPlugin.dll");
        Type pluginType = plugin.GetType("User.IRacingRadarPlugin.IRacingRadarPlugin", true);
        MethodInfo select = pluginType.GetMethod("GetRelativeDistances", BindingFlags.NonPublic | BindingFlags.Static);

        FakeStatus data = new FakeStatus();
        Add(data, 7.0);
        Add(data, -2.0);
        Add(data, 25.0);
        AddPit(data, 1.0);

        double[] selected = (double[])select.Invoke(null, new object[] { data, 18.0 });
        if (selected.Length != 2 || selected[0] != -2.0 || selected[1] != 7.0)
        {
            Console.WriteLine("FAIL opponent selection: " + string.Join(",", selected));
            return 1;
        }

        Type math = plugin.GetType("User.IRacingRadarPlugin.RadarMath", true);
        MethodInfo top = math.GetMethod("CalculateTopFromRelativeMeters");
        double ahead = (double)top.Invoke(null, new object[] { -6.0, 66.0 });
        double beside = (double)top.Invoke(null, new object[] { 0.0, 66.0 });
        double behind = (double)top.Invoke(null, new object[] { 6.0, 66.0 });

        Console.WriteLine("ahead -6m top=" + ahead);
        Console.WriteLine("beside 0m top=" + beside);
        Console.WriteLine("behind +6m top=" + behind);
        bool pass = ahead < beside && beside < behind;
        Console.WriteLine(pass ? "PASS synthetic radar positions" : "FAIL synthetic radar positions");
        return pass ? 0 : 2;
    }

    private static void AddPit(StatusDataBase data, double meters)
    {
        Opponent opponent = new Opponent();
        opponent.IsConnected = true;
        opponent.IsPlayer = false;
        opponent.IsCarInPitLane = true;
        opponent.RelativeDistanceToPlayer = meters;
        data.Opponents.Add(opponent);
    }

    private static void Add(StatusDataBase data, double meters)
    {
        Opponent opponent = new Opponent();
        opponent.IsConnected = true;
        opponent.IsPlayer = false;
        opponent.RelativeDistanceToPlayer = meters;
        data.Opponents.Add(opponent);
    }
}
