﻿using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

  public enum DrawMode {
    NoiseMap,
    ColourMap,
    Mesh,
    FalloffMap
  }

  public DrawMode drawMode;

  public Noise.NormaliseMode normaliseMode;

  public bool useFlatShading;

  [Range(0, 6)]
  public int editorPreviewLOD;
  public float noiseScale;

  public int octaves;
  [Range(0, 1)]
  public float persistence;
  public float lacunarity;

  public int seed;
  public Vector2 offset;

  public bool useFalloff;

  public float meshHeightMultiplier;
  public AnimationCurve meshHeightCurve;

  public bool autoUpdate;

  public TerrainType[] regions;
  static MapGenerator instance;

  float[,] falloffMap;

  Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
  Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

  private void Awake() {
    falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
  }

  public static int mapChunkSize {
    get {
      if (instance == null) {
        instance = FindObjectOfType<MapGenerator>();
      }
      return instance.useFlatShading ? 95 : 239;
    }
  }

  public void DrawMapInEditor() {
    MapData mapData = GenerateMapData(Vector2.zero);

    float[,] noiseMap = mapData.heightMap;
    Color[] colourMap = mapData.colourMap;

    MapDisplay display = FindObjectOfType<MapDisplay>();
    if (drawMode == DrawMode.NoiseMap) {
      display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
    } else if (drawMode == DrawMode.ColourMap) {
      display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
    } else if (drawMode == DrawMode.Mesh) {
      display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD, useFlatShading), TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
    } else if (drawMode == DrawMode.FalloffMap) {
      display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
    }
  }

  public void RequestMapData(Vector2 centre, Action<MapData> callback) {
    ThreadStart threadStart = delegate {
      MapDataThread(centre, callback);
    };

    new Thread(threadStart).Start();
  }

  void MapDataThread(Vector2 centre, Action<MapData> callback) {
    MapData mapData = GenerateMapData(centre);
    lock (mapDataThreadInfoQueue) {
      mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
    }
  }

  public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
    ThreadStart threadStart = delegate {
      MeshDataThread(mapData, lod, callback);
    };

    new Thread(threadStart).Start();
  }

  void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
    MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod, useFlatShading);
    lock (meshDataThreadInfoQueue) {
      meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
    }
  }

  private void Update() {
    if (mapDataThreadInfoQueue.Count > 0) {
      for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
        MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
        threadInfo.callback(threadInfo.parameter);
      }
    }

    if (meshDataThreadInfoQueue.Count > 0) {
      for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
        MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
        threadInfo.callback(threadInfo.parameter);
      }
    }
  }

  MapData GenerateMapData(Vector2 centre) {
    float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, seed, noiseScale, octaves, persistence, lacunarity, centre + offset, normaliseMode);

    Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
    for (int y = 0; y < mapChunkSize; y++) {
      for (int x = 0; x < mapChunkSize; x++) {
        if (useFalloff) {
          noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
        }

        float currentHeight = noiseMap[x, y];
        foreach (TerrainType region in regions) {
          if (currentHeight >= region.height) {
            colourMap[y * mapChunkSize + x] = region.colour;
          } else {
            break;
          }
        }
      }
    }

    return new MapData(noiseMap, colourMap);
  }

  private void OnValidate() {
    if (lacunarity < 1) {
      lacunarity = 1;
    }

    if (octaves < 0) {
      octaves = 0;
    }

    falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
  }

  struct MapThreadInfo<T> {
    public readonly Action<T> callback;
    public readonly T parameter;

    public MapThreadInfo(Action<T> callback, T parameter) {
      this.callback = callback;
      this.parameter = parameter;
    }
  }

}

[System.Serializable]
public struct TerrainType {
  public string name;
  public float height;
  public Color colour;
}

public struct MapData {
  public readonly float[,] heightMap;
  public readonly Color[] colourMap;

  public MapData(float[,] heightMap, Color[] colourMap) {
    this.heightMap = heightMap;
    this.colourMap = colourMap;
  }
}
