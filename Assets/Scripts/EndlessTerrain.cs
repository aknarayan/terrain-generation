﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

  public const float maxViewDistance = 450;
  public Transform viewer;

  public static Vector2 viewerPosition;
  int chunkSize;
  int chunksVisibleInViewDistance;

  Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
  List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

  private void Start() {
    chunkSize = MapGenerator.mapChunkSize - 1;
    chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
  }

  private void Update() {
    viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
    UpdateVisibleChunks();
  }

  void UpdateVisibleChunks() {

    foreach (TerrainChunk chunk in terrainChunksVisibleLastUpdate) {
      chunk.SetVisible(false);
    }
    terrainChunksVisibleLastUpdate.Clear();

    int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
    int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

    for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
      for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
        Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

        if (terrainChunkDict.ContainsKey(viewedChunkCoord)) {
          // terrain chunk already generated
          TerrainChunk chunk = terrainChunkDict[viewedChunkCoord];
          chunk.UpdateTerrainChunk();
          if (chunk.isVisible()) {
            terrainChunksVisibleLastUpdate.Add(chunk);
          }
        } else {
          // terrain chunk not generated yet
          terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform));
        }

      }
    }

  }

  public class TerrainChunk {

    GameObject meshObject;
    Vector2 position;
    Bounds bounds;

    public TerrainChunk(Vector2 coord, int size, Transform parent) {
      position = coord * size;
      bounds = new Bounds(position, Vector2.one * size);
      Vector3 positionV3 = new Vector3(position.x, 0, position.y);

      meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
      meshObject.transform.position = positionV3;
      meshObject.transform.localScale = Vector3.one * size / 10f; // primitive plane is 10 units wide/long by default
      meshObject.transform.parent = parent;
      SetVisible(false);
    }

    public void UpdateTerrainChunk() {
      float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
      bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;
      SetVisible(visible);
    }

    public void SetVisible(bool visible) {
      meshObject.SetActive(visible);
    }

    public bool isVisible() {
      return meshObject.activeSelf;
    }

  }


}
