#if UNITY_EDITOR

using UnityEditor;
using System.Diagnostics;

[InitializeOnLoad]
public class DebuggerHook
{
    static DebuggerHook() => EditorApplication.update += Update;

    static void Update()
    {
        if (Debugger.IsAttached && !EditorApplication.isPlaying)
            EditorApplication.EnterPlaymode();
        else if (!Debugger.IsAttached && EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
    }
}

#endif