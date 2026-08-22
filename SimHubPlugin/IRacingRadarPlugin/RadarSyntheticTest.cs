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
        MethodInfo isQualifying = pluginType.GetMethod("IsQualifyingSessionName", BindingFlags.NonPublic | BindingFlags.Static);
        object settings = settingsType.GetMethod("Default", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        settingsType.GetProperty("DisplayMode").GetSetMethod(true).Invoke(settings, new object[] { "None" });
        object pluginInstance = Activator.CreateInstance(pluginType);
        pluginType.GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pluginInstance, settings);
        MethodInfo buildDisplayText = pluginType.GetMethod("BuildDisplayText", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo shouldTrigger = pluginType.GetMethod("ShouldTrigger", BindingFlags.NonPublic | BindingFlags.Static);
        bool noneModePass = (string)normalizeMode.Invoke(null, new object[] { "none" }) == "None" &&
            (string)normalizeMode.Invoke(null, new object[] { "invalid" }) == "Both" &&
            (string)buildDisplayText.Invoke(pluginInstance, new object[] { "F", 12.0, 0.4 }) == string.Empty &&
            (bool)shouldTrigger.Invoke(null, new object[] { settings, 100.0, 0.5, 400.0 }) &&
            (bool)shouldTrigger.Invoke(null, new object[] { settings, 60.0, 1.0, 200.0 }) &&
            !(bool)shouldTrigger.Invoke(null, new object[] { settings, 100.0, 1.0, 200.0 });
        bool greenArcSwitchPass = Math.Abs((double)settingsType.GetProperty("RadarFadeBandPercent").GetValue(settings, null) - 15.0) < 0.001 &&
            (bool)settingsType.GetProperty("FrontGreenArcEnabled").GetValue(settings, null) &&
            (bool)settingsType.GetProperty("RearGreenArcEnabled").GetValue(settings, null) &&
            (bool)settingsType.GetProperty("CatchEstimateEnabled").GetValue(settings, null) &&
            (bool)settingsType.GetProperty("OvertakePredictionEnabled").GetValue(settings, null) &&
            (bool)settingsType.GetProperty("HideInQualifying").GetValue(settings, null) &&
            (bool)settingsType.GetProperty("TrackBackgroundEnabled").GetValue(settings, null) &&
            !(bool)settingsType.GetProperty("TrackBackgroundAlwaysVisible").GetValue(settings, null) &&
            Math.Abs((double)settingsType.GetProperty("TrackScalePixelsPerMeter").GetValue(settings, null) - 3.5) < 0.001 &&
            Math.Abs((double)settingsType.GetProperty("ReferenceTrackWidthMeters").GetValue(settings, null) - 10.5) < 0.001 &&
            Math.Abs((double)settingsType.GetProperty("PlayerMarkerScalePercent").GetValue(settings, null) - 100.0) < 0.001 &&
            !(bool)parseBoolean.Invoke(null, new object[] { "false", true }) &&
            !(bool)parseBoolean.Invoke(null, new object[] { "off", true }) &&
            (bool)parseBoolean.Invoke(null, new object[] { "yes", false }) &&
            (bool)parseBoolean.Invoke(null, new object[] { "invalid", true });

        bool qualifyingSessionPass =
            (bool)isQualifying.Invoke(null, new object[] { "Lone Qualify" }) &&
            (bool)isQualifying.Invoke(null, new object[] { "Open Qualifying" }) &&
            !(bool)isQualifying.Invoke(null, new object[] { "Race" }) &&
            !(bool)isQualifying.Invoke(null, new object[] { "Practice" });

        MethodInfo usableTimeGap = pluginType.GetMethod("IsUsableTimeGap", BindingFlags.NonPublic | BindingFlags.Static);
        bool staleGapPass =
            !(bool)usableTimeGap.Invoke(null, new object[] { 300.0, 0.0, 250.0 }) &&
            !(bool)usableTimeGap.Invoke(null, new object[] { -280.0, 0.1, 250.0 }) &&
            (bool)usableTimeGap.Invoke(null, new object[] { 80.0, 0.7, 300.0 }) &&
            !(bool)shouldTrigger.Invoke(null, new object[] { settings, 300.0, 0.0, 250.0 }) &&
            (bool)shouldTrigger.Invoke(null, new object[] { settings, 60.0, 0.0, 250.0 });

        bool distantMatrixPass = true;
        string[] triggerModes = new[] { "None", "Both", "Time", "Distance" };
        foreach (string mode in triggerModes)
        {
            settingsType.GetProperty("DisplayMode").GetSetMethod(true).Invoke(settings, new object[] { mode });
            for (double speed = 0.0; speed <= 450.0; speed += 50.0)
            {
                for (double distance = 200.0; distance <= 1000.0; distance += 40.0)
                {
                    for (double staleSeconds = 0.0; staleSeconds <= 0.7001; staleSeconds += 0.1)
                    {
                        if ((bool)shouldTrigger.Invoke(null,
                            new object[] { settings, distance, staleSeconds, speed }) ||
                            (bool)shouldTrigger.Invoke(null,
                            new object[] { settings, -distance, -staleSeconds, speed }))
                            distantMatrixPass = false;
                    }
                }
            }
        }
        settingsType.GetProperty("DisplayMode").GetSetMethod(true).Invoke(settings, new object[] { "None" });

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
        FakeStatus raceStart = new FakeStatus();
        SetStatusDouble(raceStart, "SpeedKmh", 250.0);
        Opponent spreadingFront = AddWithGap(raceStart, -12.0, 0.2);
        Opponent spreadingRear = AddWithGap(raceStart, 14.0, 0.2);
        bool startFrontDetected = nearestOpponent.Invoke(null, new object[] { raceStart, settings, true }) != null;
        bool startRearDetected = nearestOpponent.Invoke(null, new object[] { raceStart, settings, false }) != null;
        spreadingFront.RelativeDistanceToPlayer = -320.0;
        spreadingFront.RelativeGapToPlayer = 0.0;
        spreadingRear.RelativeDistanceToPlayer = 280.0;
        spreadingRear.RelativeGapToPlayer = 0.1;
        bool fieldSpreadPass = startFrontDetected && startRearDetected &&
            nearestOpponent.Invoke(null, new object[] { raceStart, settings, true }) == null &&
            nearestOpponent.Invoke(null, new object[] { raceStart, settings, false }) == null;

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
        MethodInfo greenPredictionStage = pluginType.GetMethod("IsGreenPredictionStage", BindingFlags.NonPublic | BindingFlags.Static);
        double fastCatch = (double)catchSeconds.Invoke(null, new object[] { -20.0, 5.0 });
        double slowCatch = (double)catchSeconds.Invoke(null, new object[] { -20.0, 1.0 });
        double rearCatch = (double)catchSeconds.Invoke(null, new object[] { 20.0, 5.0 });
        double distantCatch = (double)catchSeconds.Invoke(null, new object[] { -100.0, 5.0 });
        bool catchEstimatePass = Math.Abs(fastCatch - 4.0) < 0.001 &&
            double.IsNaN(slowCatch) && double.IsNaN(rearCatch) && double.IsNaN(distantCatch);
        bool greenPredictionPass =
            (bool)greenPredictionStage.Invoke(null, new object[] { true, false }) &&
            !(bool)greenPredictionStage.Invoke(null, new object[] { true, true }) &&
            !(bool)greenPredictionStage.Invoke(null, new object[] { false, false });
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

        Type trackFrameType = plugin.GetType("User.IRacingRadarPlugin.TrackVisualFrame", true);
        MethodInfo emptyTrackFrame = trackFrameType.GetMethod("Empty", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo opponentMarkerOpacity = trackFrameType.GetMethod("OpponentMarkerOpacity", BindingFlags.NonPublic | BindingFlags.Static);
        Type trackMapType = plugin.GetType("User.IRacingRadarPlugin.TrackMapGeometry", true);
        MethodInfo displayedOpponentDistance = trackMapType.GetMethod("CalculateDisplayedOpponentDistance",
            BindingFlags.NonPublic | BindingFlags.Static);
        object greenFrame = emptyTrackFrame.Invoke(null, new object[] { 10.5, 1.75, 100.0 });
        object redFrame = emptyTrackFrame.Invoke(null, new object[] { 10.5, 3.5, 100.0 });
        object halfScaleFrame = emptyTrackFrame.Invoke(null, new object[] { 10.5, 1.75, 50.0 });
        double greenWidth = (double)trackFrameType.GetField("PlayerWidthPixels", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(greenFrame);
        double greenLength = (double)trackFrameType.GetField("PlayerLengthPixels", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(greenFrame);
        double redWidth = (double)trackFrameType.GetField("PlayerWidthPixels", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(redFrame);
        double redLength = (double)trackFrameType.GetField("PlayerLengthPixels", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(redFrame);
        double halfLength = (double)trackFrameType.GetField("PlayerLengthPixels", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(halfScaleFrame);
        bool markerGeometryPass = greenLength >= 24.0 && redLength > greenLength &&
            greenLength >= 27.0 &&
            Math.Abs(greenWidth / greenLength - 0.48) < 0.001 &&
            Math.Abs(redWidth / redLength - 0.48) < 0.001 &&
            Math.Abs(halfLength - greenLength * 0.5) < 0.001 &&
            Math.Abs((double)opponentMarkerOpacity.Invoke(null, new object[] { 0.0, greenLength }) - 45.0) < 0.001 &&
            Math.Abs((double)opponentMarkerOpacity.Invoke(null, new object[] { greenLength, greenLength }) - 100.0) < 0.001;
        double mappedRearContact = (double)displayedOpponentDistance.Invoke(null,
            new object[] { 4.5, 1.75, greenLength });
        double mappedFrontContact = (double)displayedOpponentDistance.Invoke(null,
            new object[] { -4.5, 1.75, greenLength });
        double mappedRearGap = (double)displayedOpponentDistance.Invoke(null,
            new object[] { 10.0, 1.75, greenLength });
        double contactPixels = mappedRearContact * 1.75;
        double displayedGapPixels = mappedRearGap * 1.75 - greenLength;
        bool bodyGapMappingPass = Math.Abs(contactPixels - greenLength) < 0.001 &&
            Math.Abs(mappedFrontContact + mappedRearContact) < 0.001 &&
            Math.Abs(displayedGapPixels - (10.0 - 4.5) * 1.75) < 0.001;

        object syntheticTrack = Activator.CreateInstance(trackMapType, true);
        Type worldPointType = trackMapType.GetNestedType("WorldPoint", BindingFlags.NonPublic);
        ConstructorInfo worldPointConstructor = worldPointType.GetConstructor(BindingFlags.NonPublic |
            BindingFlags.Instance, null, new[] { typeof(double), typeof(double) }, null);
        System.Collections.IList trackPoints = (System.Collections.IList)trackMapType
            .GetField("points", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(syntheticTrack);
        List<double> trackProgress = (List<double>)trackMapType
            .GetField("recordedProgress", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(syntheticTrack);
        List<double> trackCumulative = (List<double>)trackMapType
            .GetField("cumulative", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(syntheticTrack);
        double[,] syntheticPoints = { { 0, 0 }, { 100, 0 }, { 100, 100 }, { 0, 100 }, { 0, 10 } };
        double[] syntheticProgress = { 0.02, 0.25, 0.50, 0.75, 0.98 };
        double[] syntheticCumulative = { 0, 100, 200, 300, 390 };
        for (int i = 0; i < syntheticProgress.Length; i++)
        {
            trackPoints.Add(worldPointConstructor.Invoke(new object[] { syntheticPoints[i, 0], syntheticPoints[i, 1] }));
            trackProgress.Add(syntheticProgress[i]);
            trackCumulative.Add(syntheticCumulative[i]);
        }
        trackMapType.GetField("totalLength", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(syntheticTrack, 400.0);
        MethodInfo distanceAtProgress = trackMapType.GetMethod("DistanceAtProgress", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo sampleAtDistance = trackMapType.GetMethod("SampleAtDistance", BindingFlags.NonPublic | BindingFlags.Instance);
        double beforeFinishDistance = (double)distanceAtProgress.Invoke(syntheticTrack, new object[] { 0.99 });
        double afterFinishDistance = (double)distanceAtProgress.Invoke(syntheticTrack, new object[] { 0.01 });
        object beforeFinish = sampleAtDistance.Invoke(syntheticTrack, new object[] { beforeFinishDistance });
        object afterFinish = sampleAtDistance.Invoke(syntheticTrack, new object[] { afterFinishDistance });
        double beforeX = (double)worldPointType.GetField("X", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(beforeFinish);
        double beforeZ = (double)worldPointType.GetField("Z", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(beforeFinish);
        double afterX = (double)worldPointType.GetField("X", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(afterFinish);
        double afterZ = (double)worldPointType.GetField("Z", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(afterFinish);
        bool finishLineContinuityPass = beforeFinishDistance < afterFinishDistance &&
            Math.Sqrt((afterX - beforeX) * (afterX - beforeX) + (afterZ - beforeZ) * (afterZ - beforeZ)) < 6.0;

        settingsType.GetProperty("DynamicRadarRangeEnabled").GetSetMethod(true).Invoke(settings, new object[] { true });
        settingsType.GetProperty("RadarRangeMeters").GetSetMethod(true).Invoke(settings, new object[] { 70.0 });
        settingsType.GetProperty("DynamicRadarRangeMinimumMeters").GetSetMethod(true).Invoke(settings, new object[] { 35.0 });
        settingsType.GetProperty("NearDistanceMeters").GetSetMethod(true).Invoke(settings, new object[] { 20.0 });
        MethodInfo effectiveRange = pluginType.GetMethod("CalculateEffectiveRadarRange", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo smoothTrackScale = pluginType.GetMethod("SmoothTrackScale", BindingFlags.NonPublic | BindingFlags.Static);
        double nearRange = (double)effectiveRange.Invoke(null, new object[] { settings, 20.0 });
        double middleRange = (double)effectiveRange.Invoke(null, new object[] { settings, 26.0 });
        double farRange = (double)effectiveRange.Invoke(null, new object[] { settings, 32.0 });
        double oneFrameScale = (double)smoothTrackScale.Invoke(null, new object[] { 1.75, 3.5, 0.016 });
        bool fieldOfViewTransitionPass = Math.Abs(nearRange - 35.0) < 0.001 &&
            Math.Abs(middleRange - 52.5) < 0.001 && Math.Abs(farRange - 70.0) < 0.001 &&
            oneFrameScale > 1.75 && oneFrameScale < 3.5;

        Console.WriteLine(pass ? "PASS synthetic radar positions" : "FAIL synthetic radar positions");
        Console.WriteLine(smoothPass ? "PASS side position smoothing" : "FAIL side position smoothing");
        Console.WriteLine(transitionPass ? "PASS green-to-red transition" : "FAIL green-to-red transition");
        Console.WriteLine(motionPass ? "PASS closing/separating direction" : "FAIL closing/separating direction");
        Console.WriteLine(noneModePass ? "PASS None display mode" : "FAIL None display mode");
        Console.WriteLine(greenArcSwitchPass ? "PASS green arc switches" : "FAIL green arc switches");
        Console.WriteLine(qualifyingSessionPass ? "PASS qualifying-session detection" : "FAIL qualifying-session detection");
        Console.WriteLine(staleGapPass ? "PASS stale time-gap rejection after field spread" : "FAIL stale time-gap rejection after field spread");
        Console.WriteLine(fieldSpreadPass ? "PASS start-grid opponents clear after spreading hundreds of metres" : "FAIL start-grid opponents clear after spreading hundreds of metres");
        Console.WriteLine(distantMatrixPass ? "PASS 200-1000m stale-gap stress matrix in all display modes" : "FAIL 200-1000m stale-gap stress matrix in all display modes");
        Console.WriteLine(radarOpacityPass ? "PASS distance/time radar opacity" : "FAIL distance/time radar opacity");
        Console.WriteLine(catchEstimatePass ? "PASS front catch-time estimate" : "FAIL front catch-time estimate");
        Console.WriteLine(greenPredictionPass ? "PASS green-only overtake prediction" : "FAIL green-only overtake prediction");
        Console.WriteLine(markerGeometryPass ? "PASS unified readable vehicle geometry and close-overlap fade" : "FAIL vehicle geometry or overlap fade");
        Console.WriteLine(fieldOfViewTransitionPass ? "PASS smooth field-of-view and map-scale transition" : "FAIL field-of-view or map-scale transition");
        Console.WriteLine(bodyGapMappingPass ? "PASS physical body-gap marker mapping" : "FAIL physical body-gap marker mapping");
        Console.WriteLine(finishLineContinuityPass ? "PASS finish-line and irregular-progress continuity" : "FAIL finish-line continuity");
        return pass && smoothPass && transitionPass && motionPass && noneModePass && greenArcSwitchPass && qualifyingSessionPass && staleGapPass && fieldSpreadPass && distantMatrixPass && radarOpacityPass && catchEstimatePass && greenPredictionPass && markerGeometryPass && fieldOfViewTransitionPass && bodyGapMappingPass && finishLineContinuityPass ? 0 : 2;
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

    private static Opponent AddWithGap(FakeStatus data, double meters, double seconds)
    {
        Opponent opponent = CreateOpponent(meters);
        opponent.RelativeGapToPlayer = seconds;
        data.Opponents.Add(opponent);
        if (meters < 0.0) data.Ahead.Add(opponent);
        else data.Behind.Add(opponent);
        return opponent;
    }

    private static void SetStatusDouble(FakeStatus data, string property, double value)
    {
        typeof(StatusDataBase).GetProperty(property).GetSetMethod(true)
            .Invoke(data, new object[] { value });
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
