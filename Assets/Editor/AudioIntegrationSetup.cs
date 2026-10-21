using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using WalkingSimulator.Presentation;
using UnityEngine.UI;
using TMPro;

public static class AudioIntegrationSetup
{
    public static void FinalizeAndVerify()
    {
        Verify();
        var steps = UnityEngine.Object.FindObjectOfType<SurfaceFootstepAudio>();
        var unknown = UnityEngine.Object.FindObjectsOfType<Collider>()
            .Where(c => !c.isTrigger && c.GetComponent<Renderer>() != null)
            .SelectMany(c => c.GetComponent<Renderer>().sharedMaterials
                .Where(m => m != null && !steps.stoneMaterials.Contains(m) && !steps.woodMaterials.Contains(m))
                .Select(m => new { material = m, path = Path(c.transform) }))
            .GroupBy(x => x.material).Select(g => g.First()).ToArray();
        foreach (var entry in unknown)
            Debug.Log("AUDIO UNMAPPED MATERIAL " + entry.material.name + " | " +
                AssetDatabase.GetAssetPath(entry.material) + " | " + entry.path);
        foreach (var source in UnityEngine.Object.FindObjectsOfType<SpatialEnvironmentAudio>())
            Debug.Log("AUDIO SOURCE " + Path(source.transform) + " | activity=" +
                Path(source.activity.transform) + " | " + source.minimumDistance + "-" + source.maximumDistance);
        foreach (string script in new[] { "SurfaceFootstepAudio", "SpatialEnvironmentAudio", "ForestOwlAudio",
            "MovingBatAudio", "AudioPreferences", "SoundtrackPlayback" })
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scripts/Audio"))
                AssetDatabase.CreateFolder("Assets/Scripts", "Audio");
            string from = "Assets/Presentation/" + script + ".cs";
            string to = "Assets/Scripts/Audio/" + script + ".cs";
            if (AssetDatabase.LoadAssetAtPath<MonoScript>(from) == null) continue;
            string guid = AssetDatabase.AssetPathToGUID(from);
            string error = AssetDatabase.MoveAsset(from, to);
            if (error.Length > 0 || AssetDatabase.AssetPathToGUID(to) != guid)
                throw new Exception("Audio script move failed: " + script + " " + error);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("AUDIO FINAL: audio scripts organized with original GUIDs; no Play or build.");
    }

    public static void FinishEnvironment()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/The Game.unity");
        var protectedImpacts = UnityEngine.Object.FindObjectsOfType<PickupImpactAudio>()
            .ToDictionary(x => x, x => EditorJsonUtility.ToJson(x));
        var bible = UnityEngine.Object.FindObjectOfType<Bible1>();
        var fountain = UnityEngine.Object.FindObjectOfType<Bible2>();
        if (bible == null || fountain == null) throw new Exception("Missing existing Bible events.");
        var fire = (ParticleSystem)new SerializedObject(bible).FindProperty("Fire1").objectReferenceValue;
        var water = (ParticleSystem)new SerializedObject(fountain).FindProperty("Water1").objectReferenceValue;
        if (fire == null || water == null) throw new Exception("Missing event particle references.");
        bible.fireSound = EnvironmentSource(fire.transform, "Cross Fire Audio", "CrossFireLoop", fire, .4f, 3, 32);
        bible.fireSound.ignitionClip = Clip("CrossIgnition");
        fountain.fountainSound = EnvironmentSource(water.transform, "Fountain Audio", "FountainLoop", water, .35f, 4, 30);
        EditorUtility.SetDirty(bible);
        EditorUtility.SetDirty(fountain);

        int waterfalls = 0;
        foreach (var p in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())
        {
            if (p.name != "WaterfallEffect") continue;
            var splash = p.GetComponentsInChildren<ParticleSystem>().FirstOrDefault(x => x.name == "WaterRipples");
            var anchor = splash == null ? p : splash;
            EnvironmentSource(anchor.transform, "River Waterfall Audio", "RiverWaterfallLoop", anchor, .5f, 6, 65);
            waterfalls++;
        }
        var swarms = UnityEngine.Object.FindObjectsOfType<RoamingBatSwarm>();
        foreach (var swarm in swarms)
        {
            var audio = swarm.GetComponent<MovingBatAudio>();
            if (audio == null) audio = swarm.gameObject.AddComponent<MovingBatAudio>();
            audio.swarmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Environment/BatSwarm.mp3");
            audio.swarm = swarm.GetComponent<ParticleSystem>();
            audio.volume = .16f;
            var source = audio.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 3;
            source.maxDistance = 32;
            EditorUtility.SetDirty(audio);
        }
        var owl = UnityEngine.Object.FindObjectOfType<ForestOwlAudio>();
        if (owl == null) owl = new GameObject("Forest Owl Audio").AddComponent<ForestOwlAudio>();
        owl.call = Clip("OwlCall");
        if (owl.perches == null || owl.perches.Length == 0)
        {
            var root = new GameObject("Owl Perches");
            owl.perches = swarms.Where(x => x.waypoints != null && x.waypoints.Length > 0 && x.waypoints[0] != null)
                .Take(4).Select((x, i) =>
                {
                    var perch = new GameObject("Forest Perch " + (i + 1));
                    perch.transform.SetParent(root.transform);
                    perch.transform.position = x.waypoints[0].position + Vector3.up * 4f;
                    return perch.transform;
                }).ToArray();
        }
        owl.GetComponent<AudioSource>().playOnAwake = false;
        EditorUtility.SetDirty(owl);
        foreach (string path in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX/Environment" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var settings = importer.defaultSampleSettings;
            settings.loadType = path.Contains("Loop") ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .85f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }
        foreach (var pair in protectedImpacts)
            if (EditorJsonUtility.ToJson(pair.Key) != pair.Value)
                throw new Exception("Pickup impact settings changed; refusing to save.");
        EditorSceneManager.SaveScene(scene);
        Debug.Log("AUDIO ENVIRONMENT SAVED fire/fountain event references; waterfalls=" + waterfalls +
            " roamingSwarms=" + swarms.Length + " owlPerches=" + owl.perches.Length +
            " protectedPickups=" + protectedImpacts.Count);
        Verify();
    }

    static AudioClip Clip(string name)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Environment/" + name + ".wav");
        if (clip == null) throw new Exception("Missing prepared effect " + name);
        return clip;
    }

    static SpatialEnvironmentAudio EnvironmentSource(Transform parent, string name, string clip,
        ParticleSystem activity, float volume, float min, float max)
    {
        Transform existing = parent.Find(name);
        var go = existing == null ? new GameObject(name) : existing.gameObject;
        if (existing == null) go.transform.SetParent(parent, false);
        var effect = go.GetComponent<SpatialEnvironmentAudio>();
        if (effect == null) effect = go.AddComponent<SpatialEnvironmentAudio>();
        effect.loopClip = Clip(clip);
        effect.activity = activity;
        effect.volume = volume;
        effect.minimumDistance = min;
        effect.maximumDistance = max;
        var source = effect.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.minDistance = min;
        source.maxDistance = max;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        EditorUtility.SetDirty(effect);
        return effect;
    }

    public static void Verify()
    {
        foreach (string path in new[] { "Assets/Scenes/Main Menu.unity", "Assets/Scenes/The Game.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path);
            foreach (var menu in Resources.FindObjectsOfTypeAll<MainMenu>().Where(m => m.gameObject.scene == scene))
            {
                var so = new SerializedObject(menu);
                foreach (string field in new[] { "volume", "musicVolume", "sfxVolume", "sensitivity",
                    "volumeValue", "musicVolumeValue", "sfxVolumeValue", "sensitivityValue" })
                    if (so.FindProperty(field).objectReferenceValue == null)
                        throw new Exception(path + " missing " + field);
            }
            int listeners = UnityEngine.Object.FindObjectsOfType<AudioListener>().Count(a => a.enabled);
            if (listeners != 1) throw new Exception(path + " active listeners=" + listeners);
            if (path.EndsWith("The Game.unity"))
            {
                var steps = UnityEngine.Object.FindObjectOfType<SurfaceFootstepAudio>();
                if (steps == null || steps.stone.Count != 5 || steps.wood.Count != 11 || steps.grass.Count != 12)
                    throw new Exception("Footstep references incomplete.");
                foreach (var effect in UnityEngine.Object.FindObjectsOfType<SpatialEnvironmentAudio>())
                    if (effect.loopClip == null || effect.activity == null)
                        throw new Exception("Incomplete environment source " + effect.name);
                if (UnityEngine.Object.FindObjectOfType<Bible1>().fireSound == null ||
                    UnityEngine.Object.FindObjectOfType<Bible2>().fountainSound == null)
                    throw new Exception("Missing activation link.");
                foreach (var swarm in UnityEngine.Object.FindObjectsOfType<RoamingBatSwarm>())
                    if (swarm.GetComponent<MovingBatAudio>() == null ||
                        swarm.GetComponent<MovingBatAudio>().swarmClip == null)
                        throw new Exception("Missing bat sound.");
            }
            Debug.Log("AUDIO VERIFIED " + path + ": serialized references and single listener. No Play test performed.");
        }
    }

    public static void FinishMenus()
    {
        foreach (string scenePath in new[] { "Assets/Scenes/Main Menu.unity", "Assets/Scenes/The Game.unity" })
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            foreach (var menu in Resources.FindObjectsOfTypeAll<MainMenu>().Where(m => m.gameObject.scene == scene))
            {
                var so = new SerializedObject(menu);
                var settings = (GameObject)so.FindProperty("settings").objectReferenceValue;
                var master = (Slider)so.FindProperty("volume").objectReferenceValue;
                var value = (TMP_Text)so.FindProperty("volumeValue").objectReferenceValue;
                if (settings == null || master == null || value == null)
                    throw new Exception("Missing original menu controls: " + scenePath);
                if (so.FindProperty("musicVolume").objectReferenceValue != null &&
                    so.FindProperty("sfxVolume").objectReferenceValue != null)
                    continue;
                var caption = settings.GetComponentsInChildren<TMP_Text>(true).First(t => t.text == "Master Volume");
                float baseY = -caption.rectTransform.anchoredPosition.y;
                // Fit the two new rows into the existing spacing, preserving fonts and styling.
                foreach (RectTransform child in settings.transform)
                {
                    float y = -child.anchoredPosition.y;
                    if (y >= baseY + 85 && y < 950)
                        child.anchoredPosition = new Vector2(child.anchoredPosition.x,
                            -(baseY + 210 + (y - (baseY + 100)) * .72f));
                }
                AddSlider(so, master, caption, value, settings.transform, "Music Volume", "musicVolume", "musicVolumeValue", 70);
                AddSlider(so, master, caption, value, settings.transform, "Sound Effects", "sfxVolume", "sfxVolumeValue", 140);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(menu);
                Debug.Log("AUDIO MENU SAVED " + scenePath + " Music/SFX serialized and assigned.");
            }
            EditorSceneManager.SaveScene(scene);
        }
    }

    static void AddSlider(SerializedObject so, Slider template, TMP_Text caption, TMP_Text value,
        Transform parent, string title, string field, string valueField, float offset)
    {
        if (so.FindProperty(field).objectReferenceValue != null) return;
        var slider = UnityEngine.Object.Instantiate(template, parent);
        slider.name = title;
        slider.GetComponent<RectTransform>().anchoredPosition = template.GetComponent<RectTransform>().anchoredPosition - Vector2.up * offset;
        slider.onValueChanged = new Slider.SliderEvent();
        var label = UnityEngine.Object.Instantiate(caption, parent);
        label.name = title + " Label";
        label.text = title;
        label.rectTransform.anchoredPosition = caption.rectTransform.anchoredPosition - Vector2.up * offset;
        var number = UnityEngine.Object.Instantiate(value, parent);
        number.name = title + " Value";
        number.text = "100%";
        number.rectTransform.anchoredPosition = value.rectTransform.anchoredPosition - Vector2.up * offset;
        slider.value = 1f;
        so.FindProperty(field).objectReferenceValue = slider;
        so.FindProperty(valueField).objectReferenceValue = number;
    }

    public static void FinishFootsteps()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/The Game.unity");
        var player = GameObject.Find("Player X");
        if (player == null || player.GetComponent<CharacterController>() == null)
            throw new Exception("Player controller not found.");
        var steps = player.GetComponent<SurfaceFootstepAudio>();
        if (steps == null) steps = player.AddComponent<SurfaceFootstepAudio>();
        steps.wood = Clips("Assets/Audio/SFX/Footsteps/Prepared", "Wood_").ToList();
        steps.grass = Clips("Assets/Audio/SFX/Footsteps/Prepared", "GravelLeaves_").ToList();
        steps.stone = Clips("Assets/Audio/SFX/Footsteps/CreativeCore", "cavestep").ToList();
        steps.water = Clips("Assets/Audio/SFX/Footsteps/CreativeCore", "waterstep").ToList();
        steps.stoneLayers = new[] { AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/Terrain/Alley.terrainlayer") };
        var materials = UnityEngine.Object.FindObjectsOfType<Renderer>()
            .SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToArray();
        steps.woodMaterials = materials.Where(m => AssetDatabase.GetAssetPath(m).Contains("/Cabin/Cabin.fbx") ||
            AssetDatabase.GetAssetPath(m).Contains("wood_plank")).ToArray();
        steps.stoneMaterials = materials.Where(m =>
        {
            string p = AssetDatabase.GetAssetPath(m);
            return p.Contains("/Bridge/") || p.Contains("/Tower/Tower.fbx") ||
                p.Contains("/Chapel/Chapel.fbx") || p.Contains("/Fountain/") ||
                p.Contains("Cobblestone") || p.Contains("Alley blocks");
        }).ToArray();
        if (steps.wood.Count == 0 || steps.grass.Count == 0 || steps.stone.Count != 5 ||
            steps.stoneLayers[0] == null || steps.woodMaterials.Length == 0 || steps.stoneMaterials.Length == 0)
            throw new Exception("Incomplete footstep references; scene not saved.");
        foreach (var clip in steps.wood.Concat(steps.grass).Concat(steps.stone).Concat(steps.water))
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }
        EditorUtility.SetDirty(steps);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("AUDIO FOOTSTEPS SAVED wood=" + steps.wood.Count + " grass=" + steps.grass.Count +
            " stone=" + steps.stone.Count + " materials=" + steps.stoneMaterials.Length +
            "/" + steps.woodMaterials.Length + " terrain=Alley");
    }

    static AudioClip[] Clips(string folder, string prefix)
    {
        return AssetDatabase.FindAssets("t:AudioClip", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p)
            .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
            .Where(c => c.name.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
    }

    public static void Inspect()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/The Game.unity");
        foreach (var terrain in UnityEngine.Object.FindObjectsOfType<Terrain>())
            Debug.Log("AUDIO TERRAIN " + terrain.name + ": " + string.Join(", ",
                terrain.terrainData.terrainLayers.Select(x => x == null ? "null" : x.name).ToArray()));
        foreach (var c in UnityEngine.Object.FindObjectsOfType<Collider>())
        {
            string path = Path(c.transform);
            string lower = path.ToLowerInvariant();
            if (!new[] {"cabin", "bridge", "chapel", "tower", "cross monument", "alley"}.Any(lower.Contains))
                continue;
            var r = c.GetComponent<Renderer>();
            if (r == null) r = c.GetComponentInParent<Renderer>();
            Debug.Log("AUDIO SURFACE " + path + " | " + c.GetType().Name + " | " +
                (r == null ? "-" : string.Join(", ", r.sharedMaterials.Select(m => m == null ? "-" : AssetDatabase.GetAssetPath(m)).ToArray())));
        }
        foreach (var p in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())
        {
            string path = Path(p.transform);
            if (path.ToLowerInvariant().Contains("waterfall"))
                Debug.Log("AUDIO WATERFALL " + path);
        }
        foreach (var menu in UnityEngine.Object.FindObjectsOfType<MainMenu>())
            Debug.Log("AUDIO MENU " + Path(menu.transform));
    }

    static string Path(Transform t)
    {
        return t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
