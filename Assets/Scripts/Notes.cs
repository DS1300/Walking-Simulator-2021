using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Notes : MonoBehaviour
{
    public float RayLength = 3f;
    public Canvas note;
    public TextMeshProUGUI noteTitle;
    public TextMeshProUGUI noteBody;
    public NotePage[] pages;
    public TextMeshProUGUI counter;
    public AudioClip paperOpen;
    [Range(0f, 1f)] public float paperVolume = .35f;
    AudioSource audioSource;
    private readonly HashSet<int> collected = new HashSet<int>();
    public event System.Action<int, bool> NoteClosed;
    int openNoteIndex = -1;
    bool openedInWorld;

    public bool IsReading { get { return note != null && note.enabled; } }
    public bool IsCollected(int index) { return collected.Contains(index); }
    public int CollectedCount { get { return collected.Count; } }

    public void OpenCollectedNote(int index)
    {
        if (index >= 0 && index < pages.Length && IsCollected(index)) ShowNote(pages[index], false);
    }

    public void CloseNote() { HideNote(); }

    void RefreshCounter()
    {
        if (counter != null) counter.text = "Notes: " + collected.Count + "/" + pages.Length;
    }

    private GameObject player;
    private GameObject playerCamera;
    private readonly List<Behaviour> pausedControls = new List<Behaviour>();

    [System.Serializable]
    public class NotePage
    {
        public GameObject noteObject;
        public string title;
        [TextArea(8, 20)] public string text;
    }

    void Start()
    {
        player = GameObject.Find("Player X");
        playerCamera = GameObject.Find("Main Camera");
        note.enabled = false;
        if (paperOpen == null) paperOpen = Resources.Load<AudioClip>("Audio/paperOpen");
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        RefreshCounter();
    }

    void Update()
    {
        if (note.enabled)
        {
            if (Input.GetKeyDown(KeyCode.E))
                HideNote();
            return;
        }

        if (Time.timeScale == 0f || Camera.main == null) return;
        Ray playerAim = Camera.main.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        RaycastHit hit;

        if (Input.GetMouseButtonDown(0) && Physics.Raycast(playerAim, out hit, RayLength))
        {
            if (!hit.collider.CompareTag("Note")) return;
            foreach (var page in pages)
            {
                if (page.noteObject != null && hit.transform.IsChildOf(page.noteObject.transform))
                {
                    ShowNote(page, true);
                    break;
                }
            }
        }
    }

    void ShowNote(NotePage page, bool inWorld)
    {
        openNoteIndex = System.Array.IndexOf(pages, page);
        openedInWorld = inWorld;
        collected.Add(System.Array.IndexOf(pages, page));
        RefreshCounter();
        noteTitle.text = page.title;
        noteBody.text = page.text;
        note.enabled = true;
        if (paperOpen != null)
            audioSource.PlayOneShot(paperOpen, paperVolume * WalkingSimulator.Presentation.AudioPreferences.SfxVolume);

        PauseControl(player.GetComponent<MouseLook>());
        PauseControl(playerCamera.GetComponent<MouseLook>());
        PauseControl(player.GetComponent<CrosshairGUI>());
        PauseControl(player.GetComponent<PlayerController>());
        PauseControl(player.GetComponent<PlayerMovement>());
        PauseControl(player.GetComponent<PlayerInput>());
        PauseControl(playerCamera.GetComponent<HeadBob>());
    }

    void PauseControl(Behaviour control)
    {
        if (control == null || !control.enabled) return;
        pausedControls.Add(control);
        control.enabled = false;
    }

    void HideNote()
    {
        bool wasReading = IsReading;
        note.enabled = false;
        foreach (var control in pausedControls)
            if (control != null) control.enabled = true;
        pausedControls.Clear();
        if (wasReading && NoteClosed != null)
            NoteClosed(openNoteIndex, openedInWorld);
        // A collected note leaves the world; it can still be reread from the journal.
        if (wasReading && openedInWorld && isActiveAndEnabled && pages[openNoteIndex].noteObject != null)
            pages[openNoteIndex].noteObject.SetActive(false);
        openNoteIndex = -1;
    }

    void OnDisable()
    {
        if (note != null && note.enabled) HideNote();
    }
}

