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
        MethodInfo smooth = math.GetMethod("SmoothSideTop");
        double smoothed = (double)smooth.Invoke(null, new object[] { beside, behind, 0.016 });
        bool smoothPass = smoothed > beside && smoothed < behind;

        MethodInfo thresholdProgress = pluginType.GetMethod("CalculateThresholdProgress", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo farProgress = pluginType.GetMethod("CalculateFarProgress", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo nearProgress = pluginType.GetMethod("CalculateNearProgress", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo nearBlend = pluginType.GetMethod("CalculateNearBlend", BindingFlags.NonPublic | BindingFlags.Static);
        double triggerEdge = (double)thresholdProgress.Invoke(null, new object[] { 70.0, 70.0 });
        double midRange = (double)thresholdProgress.Invoke(null, new object[] { 45.0, 70.0 });
        double nearStart = (1.0 - 20.0 / 70.0) * 100.0;
        double greenFull = (double)farProgress.Invoke(null, new object[] { triggerEdge, nearStart });
        double greenShorter = (double)farProgress.Invoke(null, new object[] { midRange, nearStart });
        double redGrowing = (double)nearProgress.Invoke(null, new object[] { 85.0, nearStart });
        double blendBefore = (double)nearBlend.Invoke(null, new object[] { nearStart - 5.0, nearStart, 3.6 });
        double blendAfter = (double)nearBlend.Invoke(null, new object[] { nearStart + 5.0, nearStart, 3.6 });
        bool transitionPass = greenFull > greenShorter && greenShorter > 0.0 &&
            redGrowing > 0.0 && blendBefore == 0.0 && blendAfter == 100.0;

        MethodInfo closingSpeed = pluginType.GetMethod("CalculateClosingSpeed", BindingFlags.NonPublic | BindingFlags.Static);
        double rearClosing = (double)closingSpeed.Invoke(null, new object[] { 20.0, 15.0, 1.0 });
        double rearSeparating = (double)closingSpeed.Invoke(null, new object[] { 20.0, 25.0, 1.0 });
        double frontClosing = (double)closingSpeed.Invoke(null, new object[] { -20.0, -15.0, 1.0 });
        double frontSeparating = (double)closingSpeed.Invoke(null, new object[] { -20.0, -25.0, 1.0 });
        MethodInfo classifyMotion = pluginType.GetMethod("ClassifyMotion", BindingFlags.NonPublic | BindingFlags.Static);
        int closingState = (int)classifyMotion.Invoke(null, new object[] { 0.6, 0 });
        int separatingState = (int)classifyMotion.Invoke(null, new object[] { -0.6, 0 });
        int steadyState = (int)classifyMotion.Invoke(null, new object[] { 0.1, 1 });
        bool motionPass = rearClosing > 0.0 && rearSeparating < 0.0 &&
            frontClosing > 0.0 && frontSeparating < 0.0 &&
            closingState == 1 && separatingState == -1 && steadyState == 0;

        Console.WriteLine(pass ? "PASS synthetic radar positions" : "FAIL synthetic radar positions");
        Console.WriteLine(smoothPass ? "PASS side position smoothing" : "FAIL side position smoothing");
        Console.WriteLine(transitionPass ? "PASS green-to-red transition" : "FAIL green-to-red transition");
        Console.WriteLine(motionPass ? "PASS closing/separating direction" : "FAIL closing/separating direction");
        return pass && smoothPass && transitionPass && motionPass ? 0 : 2;
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
