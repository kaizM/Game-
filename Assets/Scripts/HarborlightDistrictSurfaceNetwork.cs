using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Harborlight
{
    public static class HarborlightDistrictSurfaceNetwork
    {
        public const float WorldMinX = -3000f;
        public const float WorldMaxX = 3000f;
        public const float WorldMinZ = -4050f;
        public const float WorldMaxZ = 66f;
        public const float CoreRoadSouthStartZ = -148f;
        public const float PlayableMinX = WorldMinX + 4f;
        public const float PlayableMaxX = WorldMaxX - 4f;
        public const float PlayableMinZ = WorldMinZ + 4f;
        public const float PlayableMaxZ = 48f;
        public const float VehicleMaxZ = 38f;
        public const float OuterCityGridSouthZ = -800f;
        public const float OuterRoadBoundaryClearance = 32f;
        private const float SouthernTaperStartZ = -1350f;
        private const float SouthernHalfWidth = 2050f;
        public static readonly float[] EastWestRoads = { -136f, -90f, -44f, -1f };
        public static readonly float[] NorthSouthRoads = { -100f, -68f, -20f, 18f, 68f, 100f };
        public static readonly float[] OuterEastWestRoads = BuildOuterEastWestRoads();
        public static readonly float[] OuterNorthSouthRoads = BuildOuterNorthSouthRoads();
        public static readonly Vector2[] BaylineParkway =
        {
            new Vector2(-2940f, -1010f), new Vector2(-2480f, -1190f),
            new Vector2(-1900f, -1390f), new Vector2(-1250f, -1550f),
            new Vector2(-560f, -1500f), new Vector2(180f, -1580f),
            new Vector2(920f, -1610f), new Vector2(1640f, -1450f),
            new Vector2(2300f, -1230f), new Vector2(2940f, -1010f)
        };
        public static readonly Vector2[] SouthgateConnector =
        {
            new Vector2(-2940f, -515f), new Vector2(-2500f, -680f),
            new Vector2(-1940f, -830f), new Vector2(-1370f, -790f),
            new Vector2(-760f, -930f), new Vector2(-110f, -1050f),
            new Vector2(610f, -1010f), new Vector2(1320f, -860f),
            new Vector2(2070f, -660f), new Vector2(2940f, -535f)
        };
        public static readonly Vector2[] CedarValeScenicRoute =
        {
            new Vector2(-1670f, -2660f), new Vector2(-1850f, -2910f),
            new Vector2(-1780f, -3190f), new Vector2(-1390f, -3390f),
            new Vector2(-820f, -3510f), new Vector2(-160f, -3430f),
            new Vector2(470f, -3540f), new Vector2(1080f, -3430f),
            new Vector2(1510f, -3230f), new Vector2(1620f, -2950f),
            new Vector2(1320f, -2710f)
        };
        public static readonly Vector2[] OuterBuildingLocations = BuildOuterBuildingLocations();
        public static readonly Vector2[] MultiplayerEntryPoints = BuildMultiplayerEntryPoints();

        private const float TextureWorldSize = 2.5f;
        private const float SidewalkWidth = 3.2f;
        private const float CurbWidth = 0.22f;
        private const float RoadY = 0.055f;
        private const float WalkY = 0.18f;
        private const float MaximumRoadMeshSegmentLength = 40f;
        private const float MultiplayerSpawnPadOffset = 20f;
        public const int MultiplayerEntryColumns = 6;
        public const int MultiplayerEntryRows = 6;
        public const int MultiplayerEntrySlotsPerZone = 25;
        private const float TerrainTransitionStartZ = -58f;
        private const float TerrainTransitionEndZ = -800f;

        public static float TerrainHeight(float z)
        {
            var grade = Mathf.Clamp01((TerrainTransitionStartZ - z) /
                (TerrainTransitionStartZ - TerrainTransitionEndZ));
            return Mathf.SmoothStep(0f, 4.2f, grade);
        }

        public static float WorldCenterXAtZ(float z)
        {
            var taper = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(WorldMinZ, SouthernTaperStartZ, z));
            return Mathf.Lerp(165f, 0f, taper);
        }

        public static float WorldHalfWidthAtZ(float z)
        {
            var taper = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(WorldMinZ, SouthernTaperStartZ, z));
            return Mathf.Lerp(SouthernHalfWidth, (WorldMaxX - WorldMinX) * 0.5f, taper);
        }

        public static float WorldMinXAtZ(float z)
        {
            return WorldCenterXAtZ(z) - WorldHalfWidthAtZ(z);
        }

        public static float WorldMaxXAtZ(float z)
        {
            return WorldCenterXAtZ(z) + WorldHalfWidthAtZ(z);
        }

        public static bool IsWithinPlayableLand(Vector2 position, float margin = 0f)
        {
            return position.y >= WorldMinZ + margin &&
                   position.y <= CoastlineZ(position.x) - margin &&
                   position.x >= WorldMinXAtZ(position.y) + margin &&
                   position.x <= WorldMaxXAtZ(position.y) - margin;
        }

        public static Vector2 ClampToPlayableLand(Vector2 position, float margin = 4f)
        {
            var z = Mathf.Clamp(position.y, WorldMinZ + margin, PlayableMaxZ);
            var x = Mathf.Clamp(position.x, WorldMinXAtZ(z) + margin, WorldMaxXAtZ(z) - margin);
            z = Mathf.Min(z, CoastlineZ(x) - margin);
            z = Mathf.Max(z, WorldMinZAtX(x) + margin);
            x = Mathf.Clamp(x, WorldMinXAtZ(z) + margin, WorldMaxXAtZ(z) - margin);
            return new Vector2(x, z);
        }

        public static float WorldMinZAtX(float x)
        {
            if (x >= WorldMinXAtZ(WorldMinZ) && x <= WorldMaxXAtZ(WorldMinZ)) return WorldMinZ;
            var low = WorldMinZ;
            var high = SouthernTaperStartZ;
            for (var iteration = 0; iteration < 16; iteration++)
            {
                var middle = (low + high) * 0.5f;
                if (x >= WorldMinXAtZ(middle) && x <= WorldMaxXAtZ(middle)) high = middle;
                else low = middle;
            }
            return high;
        }

        public static float CoastlineZ(float x)
        {
            var swell = Mathf.Sin(x * 0.085f + 0.7f) * 1.05f + Mathf.Sin(x * 0.19f) * 0.38f;
            var westInlet = 400f * Mathf.Exp(-Mathf.Pow((x + 1850f) / 430f, 2f));
            var eastInlet = 300f * Mathf.Exp(-Mathf.Pow((x - 2080f) / 500f, 2f));
            var centralHeadland = 20f * Mathf.Exp(-Mathf.Pow(x / 620f, 2f));
            return 35f + swell - westInlet + centralHeadland - eastInlet;
        }

        private static Vector2[] BuildOuterBuildingLocations()
        {
            var locations = new List<Vector2>();
            for (var z = -225f; z >= WorldMinZ + 105f; z -= 150f)
            for (var x = WorldMinX + 50f; x <= WorldMaxX - 50f; x += 250f)
            {
                AddOuterBuildingSite(locations, new Vector2(x - 35f, z));
                AddOuterBuildingSite(locations, new Vector2(x + 35f, z));
            }
            return locations.ToArray();
        }

        private static void AddOuterBuildingSite(List<Vector2> locations, Vector2 site)
        {
            if (!IsWithinPlayableLand(site, 22f) || IsNearBaylineParkway(site, 48f) ||
                IsNearDistrictConnector(site, 48f) || IsNearOuterStreet(site, 28f)) return;
            locations.Add(site);
        }

        private static bool IsNearDistrictConnector(Vector2 point, float minimumDistance)
        {
            return IsNearRoute(point, SouthgateConnector, minimumDistance) ||
                   IsNearRoute(point, CedarValeScenicRoute, minimumDistance);
        }

        private static bool IsNearRoute(Vector2 point, IReadOnlyList<Vector2> route, float minimumDistance)
        {
            var minimumDistanceSquared = minimumDistance * minimumDistance;
            for (var i = 0; i + 1 < route.Count; i++)
            {
                var start = route[i];
                var segment = route[i + 1] - start;
                var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
                if ((point - (start + segment * t)).sqrMagnitude < minimumDistanceSquared)
                    return true;
            }
            return false;
        }

        private static bool IsNearOuterStreet(Vector2 point, float minimumDistance)
        {
            if (Mathf.Abs(point.y - OuterCityGridSouthZ) < minimumDistance &&
                point.x >= WorldMinXAtZ(OuterCityGridSouthZ) + 10f &&
                point.x <= WorldMaxXAtZ(OuterCityGridSouthZ) - 10f)
                return true;

            for (var row = 0; row < OuterEastWestRoads.Length; row++)
            {
                if (!IsOuterEastWestRoadActive(row)) continue;
                var z = OuterEastWestRoads[row];
                if (Mathf.Abs(point.y - z) < minimumDistance &&
                    point.x >= WorldMinXAtZ(z) + 10f && point.x <= WorldMaxXAtZ(z) - 10f)
                    return true;
            }

            for (var column = 0; column < OuterNorthSouthRoads.Length; column++)
            {
                var x = OuterNorthSouthRoads[column];
                if (!IsOuterNorthSouthRoadActive(column, point.y)) continue;
                if (Mathf.Abs(point.x - x) < minimumDistance &&
                    point.y >= WorldMinZAtX(x) + OuterRoadBoundaryClearance &&
                    point.y <= CoreRoadSouthStartZ + 1f)
                    return true;
            }

            return false;
        }

        public static bool IsOuterEastWestRoadActive(int row)
        {
            var z = OuterEastWestRoads[row];
            if (z >= OuterCityGridSouthZ || row % 3 == 0) return true;

            for (var spawnRow = 0; spawnRow < MultiplayerEntryRows; spawnRow++)
            {
                var entryRoadRow = 2 + (OuterEastWestRoads.Length - 5) * spawnRow /
                    (MultiplayerEntryRows - 1);
                if (row == entryRoadRow) return true;
            }
            return false;
        }

        public static bool IsOuterNorthSouthRoadActive(int column, float z)
        {
            return z >= WorldMinZAtX(OuterNorthSouthRoads[column]) + OuterRoadBoundaryClearance &&
                   (z >= OuterCityGridSouthZ || column % 3 == 1);
        }

        private static bool IsNearBaylineParkway(Vector2 point, float minimumDistance)
        {
            for (var i = 0; i + 1 < BaylineParkway.Length; i++)
            {
                var start = BaylineParkway[i];
                var end = BaylineParkway[i + 1];
                var segment = end - start;
                var t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
                if ((point - (start + segment * t)).sqrMagnitude < minimumDistance * minimumDistance)
                    return true;
            }
            return false;
        }

        private static float[] BuildOuterEastWestRoads()
        {
            var roads = new List<float> { -152f };
            var offsets = new[] { -18f, 22f, -7f, 28f, -24f, 12f, -31f, 17f };
            var row = 0;
            for (var z = -300f; z >= WorldMinZ + 30f; z -= 150f, row++)
                roads.Add(z + offsets[row % offsets.Length]);
            return roads.ToArray();
        }

        private static float[] BuildOuterNorthSouthRoads()
        {
            var roads = new List<float>();
            var offsets = new[] { -52f, 24f, -16f, 61f, -39f, 36f, -68f, 12f, 48f, -28f, 5f, 65f };
            var column = 0;
            for (var x = WorldMinX + 175f; x <= WorldMaxX - 175f; x += 250f, column++)
                roads.Add(x + offsets[column % offsets.Length]);
            return roads.ToArray();
        }

        private static Vector2[] BuildMultiplayerEntryPoints()
        {
            const int columns = MultiplayerEntryColumns;
            const int rows = MultiplayerEntryRows;
            var points = new List<Vector2>(columns * rows);
            for (var row = 0; row < rows; row++)
            {
                var roadRow = 2 + (OuterEastWestRoads.Length - 5) * row / (rows - 1);
                var spawnZ = OuterEastWestRoads[roadRow] + MultiplayerSpawnPadOffset;
                var availableRoads = new List<float>();
                for (var column = 0; column < OuterNorthSouthRoads.Length; column++)
                {
                    var roadX = OuterNorthSouthRoads[column];
                    if (IsOuterNorthSouthRoadActive(column, spawnZ) &&
                        IsWithinPlayableLand(new Vector2(roadX + MultiplayerSpawnPadOffset, spawnZ), 8f))
                        availableRoads.Add(roadX);
                }
                for (var column = 0; column < columns; column++)
                {
                    var roadColumn = (availableRoads.Count - 1) * column / (columns - 1);
                    points.Add(new Vector2(
                        availableRoads[roadColumn] + MultiplayerSpawnPadOffset,
                        spawnZ));
                }
            }
            return points.ToArray();
        }

        public static void Build(Transform parent, Material asphaltSource, Material pavingSource,
            Material markingSource, Func<float, float> terrainHeight)
        {
            if (parent == null || asphaltSource == null || pavingSource == null || markingSource == null || terrainHeight == null)
                return;

            var asphalt = TiledMaterial("Harborlight district asphalt", asphaltSource);
            var paving = TiledMaterial("Harborlight district paving", pavingSource);
            var curbs = TiledMaterial("Harborlight district concrete curbs", pavingSource);
            var markings = new Material(markingSource) { name = "Harborlight district road paint" };
            markings.mainTexture = null;
            markings.mainTextureScale = Vector2.one;

            var roads = new MeshBuilder();
            var walks = new MeshBuilder();
            var curbFaces = new MeshBuilder();
            var paint = new MeshBuilder();

            AddHorizontalRoad(roads, walks, curbFaces, paint, -104f, -44f, -1f, 10f, terrainHeight, true);
            AddHorizontalRoad(roads, walks, curbFaces, paint, 44f, 104f, -1f, 10f, terrainHeight, true);
            foreach (var z in new[] { -44f, -90f, -136f })
                AddHorizontalRoad(roads, walks, curbFaces, paint, -104f, 104f, z, 9f, terrainHeight, false);

            AddVerticalRoad(roads, walks, curbFaces, paint, -100f, CoreRoadSouthStartZ, 30f, 8f, terrainHeight);
            AddVerticalRoad(roads, walks, curbFaces, paint, -68f, CoreRoadSouthStartZ, 30f, 8f, terrainHeight);
            AddVerticalRoad(roads, walks, curbFaces, paint, 68f, CoreRoadSouthStartZ, 30f, 8f, terrainHeight);
            AddVerticalRoad(roads, walks, curbFaces, paint, 100f, CoreRoadSouthStartZ, 30f, 8f, terrainHeight);
            AddVerticalRoad(roads, walks, curbFaces, paint, -20f, CoreRoadSouthStartZ, -32.5f, 7f, terrainHeight);
            AddVerticalRoad(roads, walks, curbFaces, paint, 18f, CoreRoadSouthStartZ, -34f, 8f, terrainHeight);

            AddPolylineRoad(roads, walks, paint, BaylineParkway, 18f, terrainHeight);
            AddPolylineRoad(roads, walks, paint, SouthgateConnector, 13f, terrainHeight);
            AddPolylineRoad(roads, walks, paint, CedarValeScenicRoute, 12f, terrainHeight);

            for (var row = 0; row < OuterEastWestRoads.Length; row++)
            {
                if (!IsOuterEastWestRoadActive(row)) continue;
                var arterial = row % 5 == 0;
                var z = OuterEastWestRoads[row];
                AddHorizontalRoad(roads, walks, curbFaces, paint, WorldMinXAtZ(z) + 15f, WorldMaxXAtZ(z) - 15f,
                    z, arterial ? 16f : 10f, terrainHeight, arterial);
            }
            AddHorizontalRoad(roads, walks, curbFaces, paint,
                WorldMinXAtZ(OuterCityGridSouthZ) + 15f,
                WorldMaxXAtZ(OuterCityGridSouthZ) - 15f,
                OuterCityGridSouthZ, 16f, terrainHeight, true);
            for (var column = 0; column < OuterNorthSouthRoads.Length; column++)
            {
                var x = OuterNorthSouthRoads[column];
                var southStart = WorldMinZAtX(x) + OuterRoadBoundaryClearance;
                var cityStart = Mathf.Max(southStart, OuterCityGridSouthZ);
                var arterial = column % 3 == 1;
                if (cityStart < CoreRoadSouthStartZ + 1f)
                    AddVerticalRoad(roads, walks, curbFaces, paint, x, cityStart,
                        CoreRoadSouthStartZ + 1f, arterial ? 14f : 8.5f, terrainHeight);
                if (arterial && southStart < OuterCityGridSouthZ)
                    AddVerticalRoad(roads, walks, curbFaces, paint, x, southStart,
                        OuterCityGridSouthZ, 14f, terrainHeight);
            }
            for (var row = 0; row < OuterEastWestRoads.Length; row += 5)
            for (var column = 2; column < OuterNorthSouthRoads.Length; column += 5)
                if (IsOuterEastWestRoadActive(row) &&
                    IsOuterNorthSouthRoadActive(column, OuterEastWestRoads[row]) &&
                    IsWithinPlayableLand(new Vector2(OuterNorthSouthRoads[column], OuterEastWestRoads[row]), 10f))
                    AddCrosswalk(paint, OuterNorthSouthRoads[column], OuterEastWestRoads[row],
                        14f, 16f, terrainHeight);

            foreach (var z in new[] { -44f, -90f, -136f })
            foreach (var x in new[] { -100f, -68f, -20f, 18f, 68f, 100f })
                AddCrosswalk(paint, x, z, x == -20f ? 7f : 8f, 9f, terrainHeight);

            walks.AddTop(-58f, -26f, -125f, -123f, WalkY, terrainHeight);
            walks.AddTop(-58f, -26f, -101f, -99f, WalkY, terrainHeight);
            walks.AddTop(-58f, -56f, -123f, -101f, WalkY, terrainHeight);
            walks.AddTop(-28f, -26f, -123f, -101f, WalkY, terrainHeight);
            walks.AddTop(-43f, -41f, -123f, -101f, WalkY, terrainHeight);

            CreateMeshObject(parent, "Playable district road network", roads, asphalt, true);
            CreateMeshObject(parent, "Playable district textured sidewalks", walks, paving, true);
            CreateMeshObject(parent, "Raised concrete curb edges", curbFaces, curbs, false);
            CreateMeshObject(parent, "Road lane and crosswalk paint", paint, markings, false);
        }

        private static void AddPolylineRoad(MeshBuilder roads, MeshBuilder walks, MeshBuilder paint,
            IReadOnlyList<Vector2> points, float width, Func<float, float> height)
        {
            var halfWidth = width * 0.5f;
            const float shoulderWidth = 1.7f;
            for (var segmentIndex = 0; segmentIndex + 1 < points.Count; segmentIndex++)
            {
                var start = points[segmentIndex];
                var end = points[segmentIndex + 1];
                var delta = end - start;
                var length = delta.magnitude;
                var direction = delta / length;
                var normal = new Vector2(-direction.y, direction.x);
                var subdivisions = Mathf.CeilToInt(length / MaximumRoadMeshSegmentLength);
                for (var subdivision = 0; subdivision < subdivisions; subdivision++)
                {
                    var from = Vector2.Lerp(start, end, (float)subdivision / subdivisions);
                    var to = Vector2.Lerp(start, end, (float)(subdivision + 1) / subdivisions);
                    roads.AddTopQuad(from + normal * halfWidth, to + normal * halfWidth,
                        from - normal * halfWidth, to - normal * halfWidth, RoadY + 0.04f, height);
                    walks.AddTopQuad(from + normal * halfWidth, to + normal * halfWidth,
                        from + normal * (halfWidth + shoulderWidth), to + normal * (halfWidth + shoulderWidth),
                        WalkY + 0.04f, height);
                    walks.AddTopQuad(from - normal * halfWidth, to - normal * halfWidth,
                        from - normal * (halfWidth + shoulderWidth), to - normal * (halfWidth + shoulderWidth),
                        WalkY + 0.04f, height);
                }

                for (var distance = 4f; distance < length - 2f; distance += 11f)
                {
                    var dashStart = start + direction * distance;
                    var dashEnd = start + direction * Mathf.Min(distance + 3.5f, length - 0.5f);
                    var dashNormal = normal * 0.09f;
                    paint.AddTopQuad(dashStart + dashNormal, dashEnd + dashNormal,
                        dashStart - dashNormal, dashEnd - dashNormal, RoadY + 0.065f, height);
                }
            }
        }

        private static void AddHorizontalRoad(MeshBuilder roads, MeshBuilder walks, MeshBuilder curbs,
            MeshBuilder paint, float startX, float endX, float z, float width,
            Func<float, float> height, bool mainRoad)
        {
            for (var segmentStartX = startX; segmentStartX < endX; segmentStartX += MaximumRoadMeshSegmentLength)
            {
                var segmentEndX = Mathf.Min(segmentStartX + MaximumRoadMeshSegmentLength, endX);
                roads.AddTop(segmentStartX, segmentEndX, z - width * 0.5f, z + width * 0.5f, RoadY, height);
                AddHorizontalSidewalk(walks, curbs, segmentStartX, segmentEndX, z, width, height, true);
                AddHorizontalSidewalk(walks, curbs, segmentStartX, segmentEndX, z, width, height, false);
            }

            var dashLength = mainRoad ? 3.2f : 2.8f;
            for (var x = startX + 3.5f; x < endX - 1f; x += mainRoad ? 8.5f : 8f)
                paint.AddTop(x, x + dashLength, z - 0.075f, z + 0.075f, RoadY + 0.014f, height);
        }

        private static void AddVerticalRoad(MeshBuilder roads, MeshBuilder walks, MeshBuilder curbs,
            MeshBuilder paint, float x, float startZ, float endZ, float width,
            Func<float, float> height)
        {
            for (var z = startZ; z < endZ; z += 4f)
            {
                var nextZ = Mathf.Min(z + 4f, endZ);
                roads.AddTop(x - width * 0.5f, x + width * 0.5f, z, nextZ, RoadY, height);
                AddVerticalSidewalk(walks, curbs, x, z, nextZ, width, height, true);
                AddVerticalSidewalk(walks, curbs, x, z, nextZ, width, height, false);
            }

            for (var z = startZ + 3f; z < endZ - 1f; z += 8f)
                paint.AddTop(x - 0.075f, x + 0.075f, z, z + 2.8f, RoadY + 0.014f, height);
        }

        private static void AddHorizontalSidewalk(MeshBuilder walks, MeshBuilder curbs,
            float startX, float endX, float z, float roadWidth, Func<float, float> height, bool northSide)
        {
            var edge = z + (northSide ? 1f : -1f) * roadWidth * 0.5f;
            var curbOuter = edge + (northSide ? 1f : -1f) * CurbWidth;
            var walkOuter = curbOuter + (northSide ? 1f : -1f) * SidewalkWidth;
            var walkMin = Mathf.Min(curbOuter, walkOuter);
            var walkMax = Mathf.Max(curbOuter, walkOuter);

            walks.AddTop(startX, endX, walkMin, walkMax, WalkY, height);
            walks.AddTop(startX, endX, Mathf.Min(edge, curbOuter), Mathf.Max(edge, curbOuter), WalkY, height);
            curbs.AddFaceAlongX(startX, endX, edge, RoadY, WalkY, height, northSide);
            curbs.AddFaceAlongX(startX, endX, walkOuter, 0.02f, WalkY, height, !northSide);
        }

        private static void AddVerticalSidewalk(MeshBuilder walks, MeshBuilder curbs,
            float x, float startZ, float endZ, float roadWidth, Func<float, float> height, bool eastSide)
        {
            var edge = x + (eastSide ? 1f : -1f) * roadWidth * 0.5f;
            var curbOuter = edge + (eastSide ? 1f : -1f) * CurbWidth;
            var walkOuter = curbOuter + (eastSide ? 1f : -1f) * SidewalkWidth;
            var walkMin = Mathf.Min(curbOuter, walkOuter);
            var walkMax = Mathf.Max(curbOuter, walkOuter);

            walks.AddTop(walkMin, walkMax, startZ, endZ, WalkY, height);
            walks.AddTop(Mathf.Min(edge, curbOuter), Mathf.Max(edge, curbOuter), startZ, endZ, WalkY, height);
            curbs.AddFaceAlongZ(edge, startZ, endZ, RoadY, WalkY, height, eastSide);
            curbs.AddFaceAlongZ(walkOuter, startZ, endZ, 0.02f, WalkY, height, !eastSide);
        }

        private static void AddCrosswalk(MeshBuilder paint, float x, float z, float verticalWidth,
            float horizontalWidth, Func<float, float> height)
        {
            for (var stripe = -4; stripe <= 4; stripe++)
            {
                var stripeX = x + stripe * 0.78f;
                paint.AddTop(stripeX - 0.22f, stripeX + 0.22f,
                    z - horizontalWidth * 0.5f + 0.5f, z + horizontalWidth * 0.5f - 0.5f,
                    RoadY + 0.025f, height);

                var stripeZ = z + stripe * 0.78f;
                paint.AddTop(x - verticalWidth * 0.5f + 0.5f, x + verticalWidth * 0.5f - 0.5f,
                    stripeZ - 0.22f, stripeZ + 0.22f, RoadY + 0.025f, height);
            }
        }

        private static Material TiledMaterial(string name, Material source)
        {
            var material = new Material(source) { name = name };
            material.mainTextureScale = Vector2.one;
            return material;
        }

        private static void CreateMeshObject(Transform parent, string name, MeshBuilder builder,
            Material material, bool addCollider)
        {
            var mesh = builder.Build(name + " mesh");
            if (mesh == null) return;
            var surface = new GameObject(name);
            surface.transform.SetParent(parent, false);
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = surface.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.receiveShadows = true;
            if (addCollider) surface.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private sealed class MeshBuilder
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();

            public void AddTop(float minX, float maxX, float minZ, float maxZ,
                float yOffset, Func<float, float> height)
            {
                var sw = new Vector3(minX, height(minZ) + yOffset, minZ);
                var se = new Vector3(maxX, height(minZ) + yOffset, minZ);
                var nw = new Vector3(minX, height(maxZ) + yOffset, maxZ);
                var ne = new Vector3(maxX, height(maxZ) + yOffset, maxZ);
                AddQuad(sw, se, nw, ne,
                    new Vector2(minX / TextureWorldSize, minZ / TextureWorldSize),
                    new Vector2(maxX / TextureWorldSize, minZ / TextureWorldSize),
                    new Vector2(minX / TextureWorldSize, maxZ / TextureWorldSize),
                    new Vector2(maxX / TextureWorldSize, maxZ / TextureWorldSize), true);
            }

            public void AddTopQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d,
                float yOffset, Func<float, float> height)
            {
                var vertexA = new Vector3(a.x, height(a.y) + yOffset, a.y);
                var vertexB = new Vector3(b.x, height(b.y) + yOffset, b.y);
                var vertexC = new Vector3(c.x, height(c.y) + yOffset, c.y);
                var vertexD = new Vector3(d.x, height(d.y) + yOffset, d.y);
                var index = _vertices.Count;
                _vertices.Add(vertexA); _vertices.Add(vertexB); _vertices.Add(vertexC); _vertices.Add(vertexD);
                _uvs.Add(new Vector2(a.x / TextureWorldSize, a.y / TextureWorldSize));
                _uvs.Add(new Vector2(b.x / TextureWorldSize, b.y / TextureWorldSize));
                _uvs.Add(new Vector2(c.x / TextureWorldSize, c.y / TextureWorldSize));
                _uvs.Add(new Vector2(d.x / TextureWorldSize, d.y / TextureWorldSize));
                if (Vector3.Cross(vertexB - vertexA, vertexC - vertexA).y >= 0f)
                    _triangles.AddRange(new[] { index, index + 1, index + 2, index + 1, index + 3, index + 2 });
                else
                    _triangles.AddRange(new[] { index, index + 2, index + 1, index + 1, index + 2, index + 3 });
            }

            public void AddFaceAlongX(float startX, float endX, float z,
                float bottomOffset, float topOffset, Func<float, float> height, bool faceNorth)
            {
                var y = height(z);
                var a = new Vector3(startX, y + bottomOffset, z);
                var b = new Vector3(endX, y + bottomOffset, z);
                var c = new Vector3(startX, y + topOffset, z);
                var d = new Vector3(endX, y + topOffset, z);
                AddQuad(a, b, c, d,
                    new Vector2(startX / TextureWorldSize, bottomOffset / TextureWorldSize),
                    new Vector2(endX / TextureWorldSize, bottomOffset / TextureWorldSize),
                    new Vector2(startX / TextureWorldSize, topOffset / TextureWorldSize),
                    new Vector2(endX / TextureWorldSize, topOffset / TextureWorldSize), !faceNorth);
            }

            public void AddFaceAlongZ(float x, float startZ, float endZ,
                float bottomOffset, float topOffset, Func<float, float> height, bool faceEast)
            {
                var a = new Vector3(x, height(startZ) + bottomOffset, startZ);
                var b = new Vector3(x, height(endZ) + bottomOffset, endZ);
                var c = new Vector3(x, height(startZ) + topOffset, startZ);
                var d = new Vector3(x, height(endZ) + topOffset, endZ);
                AddQuad(a, b, c, d,
                    new Vector2(startZ / TextureWorldSize, bottomOffset / TextureWorldSize),
                    new Vector2(endZ / TextureWorldSize, bottomOffset / TextureWorldSize),
                    new Vector2(startZ / TextureWorldSize, topOffset / TextureWorldSize),
                    new Vector2(endZ / TextureWorldSize, topOffset / TextureWorldSize), faceEast);
            }

            private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD, bool reverse)
            {
                var index = _vertices.Count;
                _vertices.Add(a); _vertices.Add(b); _vertices.Add(c); _vertices.Add(d);
                _uvs.Add(uvA); _uvs.Add(uvB); _uvs.Add(uvC); _uvs.Add(uvD);
                if (!reverse)
                    _triangles.AddRange(new[] { index, index + 1, index + 2, index + 1, index + 3, index + 2 });
                else
                    _triangles.AddRange(new[] { index, index + 2, index + 1, index + 1, index + 2, index + 3 });
            }

            public Mesh Build(string name)
            {
                if (_vertices.Count == 0) return null;
                var mesh = new Mesh { name = name };
                if (_vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(_vertices);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
