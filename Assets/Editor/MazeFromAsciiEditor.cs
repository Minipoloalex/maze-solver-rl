#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[CustomEditor(typeof(MazeFromAscii))]
public class MazeFromAsciiEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();                      // shows asciiMaze & prefab fields

        MazeFromAscii maze = (MazeFromAscii)target;
        if (GUILayout.Button("Build From Config"))
            maze.Build();                            // calls the same method
    }
}
