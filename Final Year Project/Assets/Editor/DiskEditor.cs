using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Points))]
public class DiskEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Points points = (Points)target;
        base.OnInspectorGUI();
        //DrawDefaultInspector();
        points.GenerateDisk();
    }
}
