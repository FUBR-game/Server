﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;


public class DungeonGenerator : MonoBehaviour
{
    [Range(0.0f, 0.25f)] public float maxSplitOffset;
    [Range(0, 50)] public int splitDepth;
    public int levelWidth;
    public int levelHeight;

    public int seed = 0;
    public bool generateNewOnPlay;

    // Lower = more randomness, higher = less randomless, more fairness
    [Range(0.0f, 1.0f)] public float minRoomRatio;

    public bool visualize;
    public bool buildBlocks;
    public GameObject floorBlock;
    public GameObject wallBlock;

    List<GameObject> splitCubes;

    Assets.BlockGrid grid;

    void Start()
    {
        grid = new Assets.BlockGrid(levelWidth, levelHeight, new Vector2((floorBlock.transform.localScale.x * levelWidth), (floorBlock.transform.localScale.z * levelHeight)));

        if (generateNewOnPlay) seed = System.Environment.TickCount;
        UnityEngine.Random.InitState(seed);

        LevelSplit[] splits = new LevelSplit[] { new LevelSplit { start = new Vector2Int(0, 0), delta = new Vector2Int(levelWidth, levelHeight) } };
        for (int splitLevel = 1; splitLevel <= splitDepth; splitLevel++)
        {
            int newSplitNo = splits.Length*2;
            LevelSplit[] newSplits = new LevelSplit[newSplitNo];

            splitCubes = new List<GameObject>();

            int currentIndex = 0;
            foreach(LevelSplit split in splits)
            {
                bool splitHorizontal = DoISplitHorizontally(split);
                int splitRange = 0;
                if (splitHorizontal)
                {
                    splitRange = Mathf.RoundToInt(split.delta.x * maxSplitOffset);
                } 
                else
                {
                    splitRange = Mathf.RoundToInt(split.delta.y * maxSplitOffset);
                }
                LevelSplit[] newSplit = SplitLevel(split, UnityEngine.Random.Range(-splitRange, splitRange), splitHorizontal);
                newSplits[currentIndex] = newSplit[0];
                currentIndex++;
                newSplits[currentIndex] = newSplit[1];
                currentIndex++;
            }

            splits = newSplits;
        }

        if (visualize)
        {
            CreateGrid(splits);
        }

        if (buildBlocks) {
            foreach (LevelSplit split in splits)
            {
                int maxHorOffSet = 6;
                int topOffset = UnityEngine.Random.Range(0, maxHorOffSet);
                maxHorOffSet =- topOffset;
                int bottomOffset = UnityEngine.Random.Range(0, maxHorOffSet);
                int leftOffset = UnityEngine.Random.Range(0, 3);
                int rightOffset = UnityEngine.Random.Range(0, 3);

                for (int x = 0; x < split.delta.x; x++)
                {
                    for (int y = 0; y < split.delta.y; y++)
                    {
                        int coordX = split.start.x + x;
                        int coordY = split.start.y + y;

                        Vector2 gridPos = grid.GridPosToRealPos(new Vector2Int(coordX, coordY));
                        GameObject cube = Instantiate(wallBlock, transform);
                        cube.transform.localPosition = new Vector3(gridPos.x, 2, gridPos.y);

                        if (coordX == 0 || coordY == 0 || coordX == levelWidth - 1 || coordY == levelHeight - 1) // edge wall
                        {
                            cube.GetComponent<Renderer>().material.SetColor("_Color", new Color(0, 0, 0));
                            continue;
                        }

                        if (x <= topOffset || split.delta.x - x <= bottomOffset || y <= leftOffset || split.delta.y - y <= rightOffset) // wall
                        {
                            cube.GetComponent<Renderer>().material.SetColor("_Color", new Color(0, 0, 0));
                        }
                        else
                        {
                            cube.transform.localScale -= new Vector3(0, 7.1f, 0);
                            cube.transform.position -= new Vector3(0, 7.1f, 0);
                        }
                    }
                }
            }
        }
    }

    public struct LevelSplit
    {
        public Vector2Int start;
        public Vector2Int delta;
    }

    int GetLevelDepth(int levels)
    {
        if (levels == 0)
        {
            return 0;
        }
        else
        {
            return GetLevelDepth(levels - 1) + 2^levels;
        }
    }

    void PlaceCubesByCoords(Vector2Int start, Vector2Int delta, int Y, string name)
    {
        GameObject newCube = Instantiate(floorBlock, new Vector3(start.x + (delta.x/2), Y, start.y + (delta.y/2)), new Quaternion(), transform);
        newCube.name = name;
        newCube.transform.localScale = new Vector3(delta.x, 1, delta.y);

    }

    void CreateGrid(LevelSplit[] levelSplits)
    {
        float rangeX = (floorBlock.transform.localScale.x * levelWidth) / 2.0f;
        float rangeY = (floorBlock.transform.localScale.z * levelHeight) / 2.0f;

        foreach (LevelSplit split in levelSplits)
        {
            GameObject splitCube = Instantiate(floorBlock, transform);

            Vector2 gridPos = grid.GridPosToRealPos(split.start);
            Vector2 gridScale = grid.GetRealLength(split.delta);

            Vector3 pos = new Vector3(gridPos.x + (gridScale.x/2), 0, gridPos.y + (gridScale.y/2));
            Vector3 scale = new Vector3(gridScale.x, 1, gridScale.y);

            splitCube.GetComponent<Renderer>().material.SetColor("_Color", UnityEngine.Random.ColorHSV(0.0f, 1.0f, 0.999f, 1.0f, 0.999f, 1.0f));
            splitCube.transform.localScale = scale;
            splitCube.transform.position = pos;

            splitCubes.Add(splitCube);
        }
    }

    LevelSplit[] SplitLevel(LevelSplit levelSplit, int splitOffset, bool horizontal)
    {
        LevelSplit[] splitResult = new LevelSplit[2];

        if (horizontal) // Horizontal split (so in width/x)
        {
            int newWidth = Mathf.RoundToInt(levelSplit.delta.x / 2) + splitOffset;
            splitResult[0] = new LevelSplit { start = levelSplit.start, delta = new Vector2Int(newWidth, levelSplit.delta.y) };
            splitResult[1] = new LevelSplit { start = new Vector2Int(levelSplit.start.x + newWidth, levelSplit.start.y), delta = new Vector2Int(levelSplit.delta.x - newWidth, levelSplit.delta.y) };
        }
        else // Vertical split (so in height/y)
        {
            int newHeigth = Mathf.RoundToInt(levelSplit.delta.y / 2) + splitOffset;
            splitResult[0] = new LevelSplit { start = levelSplit.start, delta = new Vector2Int(levelSplit.delta.x, newHeigth) };
            splitResult[1] = new LevelSplit { start = new Vector2Int(levelSplit.start.x, levelSplit.start.y + newHeigth), delta = new Vector2Int(levelSplit.delta.x, levelSplit.delta.y - newHeigth) };
        }

        return splitResult;
    }

    bool DoISplitHorizontally(LevelSplit split)
    {
        if (split.delta.x / split.delta.y < minRoomRatio)
        {
            return false;
        }
        else if (split.delta.y / split.delta.x < minRoomRatio)
        {
            return true;
        }
        else return UnityEngine.Random.Range(0, 2) == 1;
    }
}
