using GameReaderCommon;
using System;
using System.Collections.Generic;
using System.Reflection;

internal sealed class FakeStatus : StatusDataBase
{
    public FakeStatus()
    {
        SetList("Opponents");
        SetList("OpponentsAheadOnTrack");
        SetList("OpponentsBehindOnTrack");
    }

    public List<Opponent> Ahead { get { return OpponentsAheadOnTrack; } }
    public List<Opponent> Behind { get { return OpponentsBehindOnTrack; } }
    public override object GetRawDataObject() { return null; }

    private void SetList(string property)
    {
        typeof(StatusDataBase).GetProperty(property).GetSetMethod(true)
            .Invoke(this, new object[] { new List<Opponent>() });
    }
}

internal static class RadarSyntheticTest
{
    private static int Main()
    {
        Assembly plugin = Assembly.LoadFrom("User.IRacingRadarPlugin.dll");
        Type pluginType = plugin.GetType("User.IRacingRadarPlugin.IRacingRadarPlugin", true);
        MethodInfo select = pluginType.GetMethod("GetRelativeDistances", BindingFlags.NonPublic | BindingFlags.Static);
        Type settingsType = plugin.GetType("User.IRacingRadarPlugin.RadarSettings", true);
        MethodInfo normalizeMode = settingsType.GetMethod("NormalizeDisplayMode", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo parseBoolean = settingsType.GetMethod("ParseBoolean", BindingFlags.NonPublic | BindingFlags.Static);
        object settings = settingsType.GetMethod("Default", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        settingsType.GetProperty("DisplayMode").GetSetMethod(true).Invoke(settings, new object[] { "None" });
        object pluginInstance = Activator.CreateInstance(pluginType);
        pluginType.GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pluginInstance, settings);
        MethodInfo buildDisplayText = pluginType.GetMethod("BuildDisplayText", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo shouldTrigger = pluginType.GetMethod("ShouldTrigger", BindingFlags.NonPublic | BindingFlags.Static);
        bool noneModePass = (string)normalizeMode.Invoke(null, new object[] { "none" }) == "None" &&
            (string)normalizeMode.Invoke(null, new object[] { "invalid" }) == "Both" &&
            (string)buildDisplayText.Invoke(pluginInstance, new object[] { "F", 12.0, 0.4 }) == string.Empty &&
            (bool)shouldTrigger.Invoke(null, new object[] { settings, 100.0, 0.5 }) &&
            (bool)shouldTrigger.Invoke(null, new object[] { settings, 60.0, 1.0 }) &&
            !(bool)shouldTrigger.Invoke(null, new object[] { settings, 100.0, 1.0 });
        bool greenArcSwitchPass = Math.Abs((double)settingsType.GetProperty("RadarFadeBandPercent").GetValue(settings, null) - 15.0) < 0.001 &&
            (bool)settingsType.GetProperty("FrontGreenArcEnabled").GetValue(settings, null) &&
            (bool)settingsType.GetProperty("RearGreenArcEnabled").GetValue(settings, null) &&
            (bool)settingsType.GetProperty("CatchEstimateEnabled").GetValue(settings, null) &&
            !(bool)parseBoolean.Invoke(null, new object[] { "false", true }) &&
            !(bool)parseBoolean.Invoke(null, new object[] { "off", true }) &&
            (bool)parseBoolean.Invoke(null, new object[] { "yes", false }) &&
            (bool)parseBoolean.Invoke(null, new object[] { "invalid", true });

        MethodInfo thresholdOpacity = pluginType.GetMethod("CalculateThresholdOpacity", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo directionalOpacity = pluginType.GetMethod("CalculateDirectionalRadarOpacity", BindingFlags.NonPublic | BindingFlags.Static);
        double edgeOpacity = (double)thresholdOpacity.Invoke(null, new object[] { 70.0, 70.0, 15.0 });
        double proportionalOpacity = (double)thresholdOpacity.Invoke(null, new object[] { 65.0, 70.0, 15.0 });
        double fullOpacity = (double)thresholdOpacity.Invoke(null, new object[] { 59.0, 70.0, 15.0 });
        double timeProportionalOpacity = (double)thresholdOpacity.Invoke(null, new object[] { 0.65, 0.7, 15.0 });
        double greenEnabledFar = (double)directionalOpacity.Invoke(null, new object[] { true, true, false, 0.0, 60.0 });
        double greenDisabledFar = (double)directionalOpacity.Invoke(null, new object[] { true, false, false, 0.0, 60.0 });
        double greenDisabledNear = (double)directionalOpacity.Invoke(null, new object[] { true, false, true, 50.0, 80.0 });
        bool radarOpacityPass = edgeOpacity == 0.0 && proportionalOpacity > 0.0 &&
            proportionalOpacity < 100.0 && fullOpacity == 100.0 &&
            timeProportionalOpacity > 0.0 && timeProportionalOpacity < 100.0 &&
            Math.Abs(greenEnabledFar - 60.0) < 0.001 && greenDisabledFar == 0.0 &&
            Math.Abs(greenDisabledNear - 40.0) < 0.001;
        FakeStatus data = new FakeStatus();
        Add(data, 7.0);
        Add(data, -2.0);
        Add(data, 25.0);
        AddPit(data, 1.0);
        AddGhost(data, -1.0);

        double[] selected = (double[])select.Invoke(null, new object[] { data, 18.0 });
        if (selected.Length != 2 || selected[0] != -2.0 || selected[1] != 7.0)
        {
            Console.WriteLine("FAIL opponent selection: " + string.Join(",", selected));
            return 1;
        }

        FakeStatus qualifyingOnly = new FakeStatus();
        AddGhost(qualifyingOnly, -3.0);
        double[] qualifyingSelected = (double[])select.Invoke(null, new object[] { qualifyingOnly, 18.0 });
        MethodInfo nearestOpponent = pluginType.GetMethod("FindNearestOpponent", BindingFlags.NonPublic | BindingFlags.Static);
        object qualifyingFront = nearestOpponent.Invoke(null, new object[] { qualifyingOnly, settings, true });
        object qualifyingRear = nearestOpponent.Invoke(null, new object[] { qualifyingOnly, settings, false });
        if (qualifyingSelected.Length != 0 || qualifyingFront != null || qualifyingRear != null)
        {
            Console.WriteLine("FAIL qualifying ghost opponent was not excluded");
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

        MethodInfo catchSeconds = pluginType.GetMethod("CalculateCatchSeconds", BindingFlags.NonPublic | BindingFlags.Static);
        double fastCatch = (double)catchSeconds.Invoke(null, new object[] { -20.0, 5.0 });
        double slowCatch = (double)catchSeconds.Invoke(null, new object[] { -20.0, 1.0 });
        double rearCatch = (double)catchSeconds.Invoke(null, new object[] { 20.0, 5.0 });
        double distantCatch = (double)catchSeconds.Invoke(null, new object[] { -100.0, 5.0 });
        bool catchEstimatePass = Math.Abs(fastCatch - 4.0) < 0.001 &&
            double.IsNaN(slowCatch) && double.IsNaN(rearCatch) && double.IsNaN(distantCatch);
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
        Console.WriteLine(noneModePass ? "PASS None display mode" : "FAIL None display mode");
        Console.WriteLine(greenArcSwitchPass ? "PASS green arc switches" : "FAIL green arc switches");
        Console.WriteLine(radarOpacityPass ? "PASS distance/time radar opacity" : "FAIL distance/time radar opacity");
        Console.WriteLine(catchEstimatePass ? "PASS front catch-time estimate" : "FAIL front catch-time estimate");
        return pass && smoothPass && transitionPass && motionPass && noneModePass && greenArcSwitchPass && radarOpacityPass && catchEstimatePass ? 0 : 2;
    }

    private static void AddPit(FakeStatus data, double meters)
    {
        Opponent opponent = CreateOpponent(meters);
        opponent.IsCarInPitLane = true;
        data.Opponents.Add(opponent);
        data.Behind.Add(opponent);
    }

    private static void Add(FakeStatus data, double meters)
    {
        Opponent opponent = CreateOpponent(meters);
        data.Opponents.Add(opponent);
        if (meters < 0.0) data.Ahead.Add(opponent);
        else data.Behind.Add(opponent);
    }

    private static void AddGhost(FakeStatus data, double meters)
    {
        data.Opponents.Add(CreateOpponent(meters));
    }

    private static Opponent CreateOpponent(double meters)
    {
        Opponent opponent = new Opponent();
        opponent.IsConnected = true;
        opponent.IsPlayer = false;
        opponent.RelativeDistanceToPlayer = meters;
        return opponent;
    }
}
