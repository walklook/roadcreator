﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabLineCreator : MonoBehaviour
{

    public GameObject prefab;
    public GameObject currentPoint;

    public float spacing = 1;
    public bool rotateAlongCurve = true;
    public enum RotationDirection { forward, backward, left, right };
    public RotationDirection rotationDirection;
    public float scale = 1;
    public int smoothnessAmount = 3;
    public bool offsetPrefabWidth = true;

    public GlobalSettings globalSettings;

}
