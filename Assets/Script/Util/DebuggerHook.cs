#if UNITY_EDITOR

using UnityEditor;
using System.Diagnostics;
using System.IO;

[InitializeOnLoad]
public class DebuggerHook
{
    const bool disable = false;

    static DebuggerHook() => EditorApplication.update += Update;

    static bool checkedFile = false;
    static bool startedFromDebugger = false;
    static bool dontPlay = EditorApplication.isPlaying && Debugger.IsAttached;

    static void Update()
    {
        if (disable)
            return;

        if (Debugger.IsAttached)
        {
            if (EditorApplication.isPlaying)
            {
                if (!startedFromDebugger)
                {
                    checkedFile = true;
                    startedFromDebugger = true;
                }
            }
            else if (!dontPlay)
            {
                Touch();
                EditorApplication.EnterPlaymode();
            }
        }
        else if (!Debugger.IsAttached && EditorApplication.isPlaying)
        {
            if (!checkedFile)
            {
                startedFromDebugger = Exists();
                checkedFile = true;
                if (startedFromDebugger)
                    Delete();
            }

            if (startedFromDebugger)
            {
                startedFromDebugger = false;
                dontPlay = false;
                EditorApplication.ExitPlaymode();
            }
        }
    }

    private const string tmpName = "bb_debugger_hook.bool";
    private static readonly string tmpFile = Path.GetTempPath() + tmpName;

    private static void Touch()
    {
        FileStream myFileStream = File.Open(tmpFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        myFileStream.Close();
        myFileStream.Dispose();
    }

    private static void Delete() => File.Delete(tmpFile);
    private static bool Exists() => File.Exists(tmpFile); 
}

#endif