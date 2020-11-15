using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SafeExit : MonoBehaviour {

    [InitializeOnLoad]
    public static class StopEditorOnRecompile {
        static StopEditorOnRecompile() {
            // Rely on the fact that recompiling causes an assembly reload and that, in turn,
            // causes our static constructor to be called again.
            if (EditorApplication.isPlaying) {
                Debug.LogWarning("Stopping Editor because of AssemblyReload.");
                EditorApplication.isPlaying = false;
            }
        }
    }
}
