# Architecture

The project has three runtime parts:

1. `IRacingRadarPlugin.cs` reads normalized iRacing telemetry exposed by SimHub.
2. `RadarMath.cs` converts relative longitudinal distance into overlay positions.
3. The Dash Studio overlay renders side blocks and front/rear fan warnings.

Side detection deliberately uses only `SpotterCarLeft`, `SpotterCarRight`, and the raw `CarLeftRight` state. It does not infer a side from sound or longitudinal distance. This avoids false left/right alerts.

Front/rear opponents use `RelativeDistanceToPlayer` and `RelativeGapToPlayer`. Cars reported in the garage, pit lane, or standing still in pit lane are excluded before selection.

The synthetic test verifies front/side/rear ordering and confirms that a nearby pit-lane opponent is ignored.

