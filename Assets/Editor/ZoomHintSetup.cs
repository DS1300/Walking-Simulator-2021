using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WalkingSimulator.Tutorial;

// Temporary one-shot setup: adds the cutscene line to the Zoom Hint. Delete after it runs.
[InitializeOnLoad]
static class ZoomHintSetup
{
    const string Text =
        "RIGHT CLICK — ZOOM\n<size=20>You can zoom at any time, even during cutscenes.</size>";

    static ZoomHintSetup()
    {
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();

        if (scene.name != "The Game" || scene.isDirty)
        {
            Debug.Log("ZOOMHINT_LINE skipped: scene " + scene.name + " dirty=" + scene.isDirty);
            return;
        }

        TutorialManager manager = Object.FindObjectOfType<TutorialManager>();

        if (manager == null || manager.zoomHint == null)
        {
            Debug.Log("ZOOMHINT_LINE skipped: zoomHint missing");
            return;
        }

        TMP_Text label = manager.zoomHint.GetComponent<TMP_Text>();

        if (label.text == Text)
        {
            Debug.Log("ZOOMHINT_LINE skipped: already set");
            return;
        }

        Undo.RecordObjects(new Object[] { label, label.rectTransform }, "Zoom Hint Line");

        // Keep the first line where it was and let the second line grow downwards.
        label.text = Text;
        label.alignment = TextAlignmentOptions.Top;
        label.rectTransform.sizeDelta = new Vector2(900f, 70f);
        label.rectTransform.anchoredPosition = new Vector2(0f, 111f);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene);

        Debug.Log("ZOOMHINT_LINE done, saved=" + saved);
    }
}
