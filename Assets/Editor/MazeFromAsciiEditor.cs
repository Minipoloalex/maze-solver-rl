#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// Creates the "Build from Config" button that allows creating the maze during edit time
/// </summary>
[CustomEditor(typeof(MazeFromAscii))]
public class MazeFromAsciiEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MazeFromAscii maze = (MazeFromAscii)target;
        if (GUILayout.Button("Build From Config"))
        {
            maze.Build();                            // builds the maze
        }
    }
}
