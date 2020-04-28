﻿using System.Collections;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

  public enum DrawMode {
    NoiseMap,
    ColourMap,
    Mesh
  }

  public DrawMode drawMode;

  public const int mapChunkSize = 241; // 240 is a multiple of all even numbers in range [1, 12]
  [Range(0, 6)]
  public int levelOfDetail;
  public float noiseScale;

  public int octaves;
  [Range(0, 1)]
  public float persistence;
  public float lacunarity;

  public int seed;
  public Vector2 offset;

  public float meshHeightMultiplier;
  public AnimationCurve meshHeightCurve;

  public bool autoUpdate;

  public TerrainType[] regions;

  public void GenerateMap() {
    float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, offset);

    Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
    for (int y = 0; y < mapChunkSize; y++) {
      for (int x = 0; x < mapChunkSize; x++) {
        float currentHeight = noiseMap[x, y];
        foreach (TerrainType region in regions) {
          if (currentHeight <= region.height) {
            colourMap[y * mapChunkSize + x] = region.colour;
            break;
          }
        }
      }
    }

    MapDisplay display = FindObjectOfType<MapDisplay>();
    if (drawMode == DrawMode.NoiseMap) {
      display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
    } else if (drawMode == DrawMode.ColourMap) {
      display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
    } else if (drawMode == DrawMode.Mesh) {
      display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColourMap(colourMap, mapChunkSize, mapChunkSize));
    }
  }

  private void OnValidate() {
    if (lacunarity < 1) {
      lacunarity = 1;
    }

    if (octaves < 0) {
      octaves = 0;
    }
  }

}

[System.Serializable]
public struct TerrainType {
  public string name;
  public float height;
  public Color colour;
}
