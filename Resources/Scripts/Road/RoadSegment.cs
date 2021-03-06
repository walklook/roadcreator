﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadSegment : MonoBehaviour
{

    public Material baseRoadMaterial;
    public Material overlayRoadMaterial;
    public PhysicMaterial roadPhysicsMaterial;
    public float startRoadWidth = 2;
    public float endRoadWidth = 2;
    public bool flipped = false;
    public float textureTilingY = 1;
    public bool curved = true;

    public enum TerrainOption { adapt, deform, ignore };
    public TerrainOption terrainOption;

    public enum BridgeGenerator { none, simple };
    public BridgeGenerator bridgeGenerator;
    public Material[] bridgeMaterials;

    public float yOffsetFirstStep = 0.25f;
    public float yOffsetSecondStep = 0.5f;
    public float widthPercentageFirstStep = 0.6f;
    public float widthPercentageSecondStep = 0.6f;
    public float extraWidth = 0.2f;

    public bool placePillars = true;
    public GameObject pillarPrefab;
    public float pillarGap = 5;
    public float pillarPlacementOffset = 5;
    public float extraPillarHeight = 0.2f;
    public float xzPillarScale = 1;

    public List<bool> extraMeshOpen = new List<bool>();
    public List<bool> extraMeshLeft = new List<bool>();
    public List<Material> extraMeshMaterial = new List<Material>();
    public List<PhysicMaterial> extraMeshPhysicMaterial = new List<PhysicMaterial>();
    public List<float> extraMeshWidth = new List<float>();
    public List<float> extraMeshYOffset = new List<float>();

    public Vector3[] startGuidelinePoints;
    public Vector3[] centerGuidelinePoints;
    public Vector3[] endGuidelinePoints;

    public void CreateRoadMesh(Vector3[] points, Vector3[] nextSegmentPoints, Vector3 previousPoint, Vector3[] previousVertices, float heightOffset, Transform segment, Transform previousSegment, int smoothnessAmount, RoadCreator roadCreator)
    {
        if (baseRoadMaterial == null)
        {
            baseRoadMaterial = Resources.Load("Materials/Low Poly/Roads/2 Lane Roads/2L Road") as Material;
        }

        if (bridgeMaterials == null || bridgeMaterials.Length == 0 || bridgeMaterials[0] == null)
        {
            bridgeMaterials = new Material[] { Resources.Load("Materials/Low Poly/Concrete") as Material };
        }

        if (pillarPrefab == null || pillarPrefab.GetComponent<MeshFilter>() == null)
        {
            pillarPrefab = Resources.Load("Prefabs/Low Poly/Bridges/Oval Bridge Pillar") as GameObject;
        }

        for (int i = 0; i < extraMeshOpen.Count; i++)
        {
            if (extraMeshMaterial[i] == null)
            {
                extraMeshMaterial[i] = Resources.Load("Materials/Low Poly/Asphalt") as Material;
            }
        }

        if (segment.GetSiblingIndex() == 0)
        {
            SetGuidelines(points, nextSegmentPoints, true);
        }
        else
        {
            SetGuidelines(points, nextSegmentPoints, false);
        }

        GenerateMesh(points, nextSegmentPoints, previousPoint, previousVertices, heightOffset, segment, previousSegment, transform.GetChild(1).GetChild(0), "Road", baseRoadMaterial, overlayRoadMaterial, smoothnessAmount, roadCreator, roadPhysicsMaterial);

        for (int i = 0; i < extraMeshOpen.Count; i++)
        {
            float leftYOffset = extraMeshYOffset[i];
            float xOffset = 0;
            float yOffset = heightOffset;
            if (i > 0)
            {
                bool foundLast = false;
                for (int j = i - 1; j > -1; j -= 1)
                {
                    if (extraMeshLeft[j] == extraMeshLeft[i] && j != i)
                    {
                        if (foundLast == false)
                        {
                            leftYOffset = extraMeshYOffset[j];
                            foundLast = true;
                        }

                        xOffset += extraMeshWidth[j];
                        yOffset += extraMeshYOffset[j];
                    }
                }
            }

            float currentHeight = heightOffset;
            for (int j = i - 1; j > -1; j -= 1)
            {
                if (extraMeshLeft[j] == extraMeshLeft[i] && j != i)
                {
                    currentHeight += extraMeshYOffset[j];
                }
            }

            GenerateMesh(points, nextSegmentPoints, previousPoint, previousVertices, heightOffset, segment, previousSegment, transform.GetChild(1).GetChild(i + 1), "Extra Mesh", extraMeshMaterial[i], null, smoothnessAmount, roadCreator, extraMeshPhysicMaterial[i], xOffset, extraMeshWidth[i], currentHeight + extraMeshYOffset[i], currentHeight, extraMeshLeft[i]);
        }

        if (transform.childCount == 3)
        {
            DestroyImmediate(transform.GetChild(2).gameObject);
        }

        if (bridgeGenerator == BridgeGenerator.simple)
        {
            float extraWidthLeft = 0;
            float extraWidthRight = 0;

            for (int i = 0; i < extraMeshLeft.Count; i++)
            {
                if (extraMeshLeft[i] == true)
                {
                    extraWidthLeft += extraMeshWidth[i];
                }
                else
                {
                    extraWidthRight += extraMeshWidth[i];
                }
            }

            extraWidthLeft += extraWidth;
            extraWidthRight += extraWidth;

            BridgeGeneration.GenerateSimpleBridge(points, nextSegmentPoints, previousPoint, this, extraWidthLeft, extraWidthRight, bridgeMaterials);
        }
    }

    private void SetGuidelines(Vector3[] currentPoints, Vector3[] nextPoints, bool first)
    {
        // Start Guidelines
        Vector3 left;
        int guidelineAmount = transform.parent.parent.GetComponent<RoadCreator>().globalSettings.amountRoadGuidelines;

        if (guidelineAmount > 0)
        {
            if (first == true)
            {
                left = Misc.CalculateLeft(currentPoints[0], currentPoints[1]);

                startGuidelinePoints = new Vector3[guidelineAmount * 2];
                for (int i = 0; i < (guidelineAmount * 2) - 1; i += 2)
                {
                    startGuidelinePoints[i] = transform.GetChild(0).GetChild(0).position + left * (i + 1);
                    startGuidelinePoints[i + 1] = transform.GetChild(0).GetChild(0).position - left * (i + 1);
                }
            }

            // Center Guidelines
            left = Misc.CalculateLeft(transform.GetChild(0).GetChild(0).position, transform.GetChild(0).GetChild(2).position);

            centerGuidelinePoints = new Vector3[guidelineAmount * 2];
            for (int i = 0; i < (guidelineAmount * 2) - 1; i += 2)
            {
                centerGuidelinePoints[i] = transform.GetChild(0).GetChild(1).position + left * (i + 1);
                centerGuidelinePoints[i + 1] = transform.GetChild(0).GetChild(1).position - left * (i + 1);
            }

            // End guidelines
            endGuidelinePoints = new Vector3[guidelineAmount * 2];

            if (nextPoints == null)
            {
                left = Misc.CalculateLeft(currentPoints[currentPoints.Length - 2], currentPoints[currentPoints.Length - 1]);
            }
            else if (nextPoints.Length > 1)
            {
                left = Misc.CalculateLeft(currentPoints[currentPoints.Length - 1], nextPoints[1]);
            }
            else
            {
                endGuidelinePoints = new Vector3[0];
                return;
            }

            for (int i = 0; i < (guidelineAmount * 2) - 1; i += 2)
            {
                endGuidelinePoints[i] = transform.GetChild(0).GetChild(2).position + left * (i + 1);
                endGuidelinePoints[i + 1] = transform.GetChild(0).GetChild(2).position - left * (i + 1);
            }
        }
        else
        {
            startGuidelinePoints = null;
            centerGuidelinePoints = null;
            endGuidelinePoints = null;
        }
    }

    private void GenerateMesh(Vector3[] points, Vector3[] nextSegmentPoints, Vector3 previousPoint, Vector3[] previousVertices, float heightOffset, Transform segment, Transform previousSegment, Transform mesh, string name, Material baseMaterial, Material overlayMaterial, int smoothnessAmount, RoadCreator roadCreator, PhysicMaterial physicMaterial, float xOffset = 0, float width = 0, float yOffset = 0, float leftYOffset = 0, bool extraMeshLeft = true)
    {
        Vector3[] vertices = new Vector3[points.Length * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int numTriangles = 2 * (points.Length - 1);
        int[] triangles = new int[numTriangles * 3];
        int verticeIndex = 0;
        int triangleIndex = 0;
        float totalDistance = 0;
        float currentDistance = 0;

        for (int i = 1; i < points.Length; i++)
        {
            totalDistance += Vector3.Distance(points[i - 1], points[i]);
        }

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 left = Misc.CalculateLeft(points, nextSegmentPoints, previousPoint, i);
            float correctedHeightOffset = heightOffset;

            if (i > 0)
            {
                currentDistance += Vector3.Distance(points[i - 1], points[i]);
            }

            float roadWidth = Mathf.Lerp(startRoadWidth, endRoadWidth, currentDistance / totalDistance);
            if (i == 0 && previousPoint != Misc.MaxVector3)
            {
                correctedHeightOffset = previousPoint.y - points[i].y;
            }
            else if (i == points.Length - 1 && nextSegmentPoints != null && nextSegmentPoints.Length == 1)
            {
                correctedHeightOffset = nextSegmentPoints[0].y - points[i].y;
            }

            if (name == "Road")
            {
                vertices[verticeIndex] = (points[i] + left * roadWidth) - segment.position;
                vertices[verticeIndex].y = correctedHeightOffset + points[i].y - segment.position.y;
                vertices[verticeIndex + 1] = (points[i] - left * roadWidth) - segment.position;
                vertices[verticeIndex + 1].y = correctedHeightOffset + points[i].y - segment.position.y;
            }
            else
            {
                float modifiedXOffset = xOffset + roadWidth;

                if (extraMeshLeft == true)
                {
                    vertices[verticeIndex] = (points[i] + left * -modifiedXOffset) - segment.position;
                    vertices[verticeIndex].y = correctedHeightOffset + leftYOffset + points[i].y - segment.position.y;
                    vertices[verticeIndex + 1] = (points[i] + left * (-modifiedXOffset - width)) - segment.position;
                    vertices[verticeIndex + 1].y = correctedHeightOffset + yOffset + points[i].y - segment.position.y;
                }
                else
                {
                    vertices[verticeIndex] = (points[i] + left * (modifiedXOffset + width)) - segment.position;
                    vertices[verticeIndex].y = correctedHeightOffset + yOffset + points[i].y - segment.position.y;
                    vertices[verticeIndex + 1] = (points[i] + left * modifiedXOffset) - segment.position;
                    vertices[verticeIndex + 1].y = correctedHeightOffset + leftYOffset + points[i].y - segment.position.y;
                }
            }

            if (i < points.Length - 1)
            {
                triangles[triangleIndex] = verticeIndex;
                triangles[triangleIndex + 1] = verticeIndex + 2;
                triangles[triangleIndex + 2] = verticeIndex + 1;

                triangles[triangleIndex + 3] = verticeIndex + 1;
                triangles[triangleIndex + 4] = verticeIndex + 2;
                triangles[triangleIndex + 5] = verticeIndex + 3;
            }

            verticeIndex += 2;
            triangleIndex += 6;
        }

        // Terrain deformation
        if (terrainOption == TerrainOption.deform)
        {
            float[,] modifiedHeights;
            RaycastHit raycastHit;
            if (Physics.Raycast(points[0] + new Vector3(0, 100, 0), Vector3.down, out raycastHit, Mathf.Infinity, ~(1 << roadCreator.globalSettings.roadLayer | 1 << roadCreator.globalSettings.ignoreMouseRayLayer)))
            {
                Terrain terrain = raycastHit.collider.GetComponent<Terrain>();
                if (terrain != null)
                {
                    TerrainData terrainData = terrain.terrainData;
                    modifiedHeights = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
                    float zDivisions = Vector3.Distance(points[0], points[1]);

                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector3 forward = (vertices[i * 2 + 1] - vertices[i * 2]).normalized;

                        for (float offset = 0; offset < 1; offset += 1 / zDivisions)
                        {
                            Vector3 leftVertex = vertices[i * 2];
                            Vector3 rightVertex = vertices[i * 2 + 1];
                            if (i > 0)
                            {
                                leftVertex = Vector3.Lerp(vertices[(i - 1) * 2], vertices[i * 2], offset);
                                rightVertex = Vector3.Lerp(vertices[(i - 1) * 2 + 1], vertices[i * 2 + 1], offset);
                            }

                            float roadWidth = Mathf.Lerp(startRoadWidth, endRoadWidth, (i / points.Length));
                            float divisions = Mathf.Max(2f, roadWidth * 10);

                            for (float t = 0; t <= 1; t += 1f / divisions)
                            {
                                Vector3 position = Vector3.Lerp(rightVertex + forward * 2f - new Vector3(0, heightOffset, 0) + segment.transform.position, leftVertex - forward * 2f - new Vector3(0, heightOffset, 0) + segment.transform.position, t);
                                Vector3 localTerrainPoint = position - terrain.transform.position;

                                int terrainPointX = (int)((localTerrainPoint.x / terrainData.size.x) * terrainData.heightmapWidth);
                                float terrainPointY = position.y / terrainData.size.y;
                                int terrainPointZ = (int)((localTerrainPoint.z / terrainData.size.z) * terrainData.heightmapHeight);

                                if (terrainPointX > terrainData.heightmapWidth || terrainPointZ > terrainData.heightmapHeight)
                                {
                                    continue;
                                }

                                modifiedHeights[terrainPointZ, terrainPointX] = Mathf.Clamp01(terrainPointY);
                            }
                        }
                    }

                    terrainData.SetHeights(0, 0, modifiedHeights);
                }
            }
        }

        // First
        if (previousVertices != null)
        {
            if (vertices.Length > 4 && previousVertices.Length > 3)
            {
                vertices[0] = previousVertices[previousVertices.Length - 2] + previousSegment.position - segment.position;
                vertices[1] = previousVertices[previousVertices.Length - 1] + previousSegment.position - segment.position;
                vertices = fixVertices(0, vertices, (vertices[2] - vertices[4]).normalized);
                vertices = fixVertices(1, vertices, (vertices[3] - vertices[5]).normalized);
            }
        }

        Mesh generatedMesh = new Mesh();
        generatedMesh.vertices = vertices;
        generatedMesh.triangles = triangles;
        generatedMesh.uv = uvs;

        if (name == "Road")
        {
            generatedMesh = GenerateUvs(generatedMesh, flipped);
        }
        else
        {
            generatedMesh = GenerateUvs(generatedMesh, extraMeshLeft);
        }

        mesh.GetComponent<MeshFilter>().sharedMesh = generatedMesh;
        mesh.GetComponent<MeshCollider>().sharedMesh = generatedMesh;
        mesh.GetComponent<MeshCollider>().sharedMaterial = physicMaterial;

        if (overlayMaterial == null)
        {
            mesh.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { baseMaterial };
        }
        else
        {
            mesh.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { baseMaterial, overlayMaterial };
        }
    }

    private Vector3[] fixVertices(int position, Vector3[] vertices, Vector3 forward)
    {
        int startVertex = 0;
        for (int i = position; startVertex == 0; i += 2)
        {
            if (i > vertices.Length - 1)
            {
                return vertices;
            }

            float direction = Vector3.Dot(forward.normalized, (vertices[position] - vertices[i]).normalized);
            if (direction >= 0)
            {
                startVertex = i;
            }
        }

        int amount = Mathf.Abs(startVertex - position) / 2;
        float part = 1f / amount;
        float index = 0;

        for (int i = startVertex; index < amount * 2; i -= 2)
        {
            vertices[i] = Vector3.Lerp(vertices[startVertex], vertices[position], part * index);
            index += 2;
        }

        return vertices;
    }

    private Mesh GenerateUvs(Mesh mesh, bool left)
    {
        Vector2[] uvs = mesh.uv;
        Vector2[] widths = new Vector2[uvs.Length];
        Vector3[] vertices = mesh.vertices;

        // Calculate total distance
        float totalDistanceLeft = 0;
        float totalDistanceRight = 0;
        float currentDistance = 0;

        for (int i = 2; i < vertices.Length; i += 2)
        {
            totalDistanceLeft += Vector3.Distance(vertices[i - 2], vertices[i]);
        }

        for (int i = 3; i < vertices.Length; i += 2)
        {
            totalDistanceRight += Vector3.Distance(vertices[i - 2], vertices[i]);
        }

        // Left
        for (int i = 0; i < uvs.Length; i += 2)
        {
            if (i > 0)
            {
                currentDistance += Vector3.Distance(vertices[i - 2], vertices[i]);
            }

            if (left == false)
            {
                uvs[i] = new Vector2(0, currentDistance / totalDistanceLeft);
            }
            else
            {
                uvs[i] = new Vector2(1, currentDistance / totalDistanceLeft);
            }
        }

        // Right
        for (int i = 1; i < uvs.Length; i += 2)
        {
            if (i > 1)
            {
                currentDistance += Vector3.Distance(vertices[i - 2], vertices[i]);
            }
            else
            {
                currentDistance = 0;
            }

            if (left == false)
            {
                uvs[i] = new Vector2(1, currentDistance / totalDistanceRight);
            }
            else
            {
                uvs[i] = new Vector2(0, currentDistance / totalDistanceRight);
            }
        }

        float totalWidth = endRoadWidth * 2;
        if (startRoadWidth > endRoadWidth)
        {
            totalWidth = startRoadWidth * 2;
        }

        for (int i = 0; i < widths.Length; i += 2)
        {
            float currentRoadWidth = Vector3.Distance(vertices[i], vertices[i + 1]);
            float currentLocalDistance = currentRoadWidth / totalWidth;
            uvs[i].x *= currentLocalDistance;
            uvs[i + 1].x *= currentLocalDistance;
            widths[i].x = currentLocalDistance;
            widths[i + 1].x = currentLocalDistance;
        }

        mesh.uv = uvs;
        mesh.uv2 = widths;
        return mesh;
    }
}
