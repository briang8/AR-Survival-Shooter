// Assets/Editor/ModelSwapTool.cs
// Editor window that fixes FBX import settings, generates a simple Idle/Walk
// Animator Controller, and swaps a placeholder model inside an enemy prefab.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

public class ModelSwapTool : EditorWindow
{
    private const string LogPrefix = "[ModelSwapTool] ";
    private const string AnimationsFolder = "Assets/Animations";
    private const string SessionKeyPrefix = "ModelSwapTool.Controller.";
    private const string UrpLitShaderName = "Universal Render Pipeline/Lit";
    private const string BodyRendererFieldName = "bodyRenderer";
    private const string OldModelName = "Model";
    private const string NewModelName = "Model_New";
    private const string NoneOption = "(none)";

    // Section 1 state.
    [SerializeField] private GameObject importFbx;
    [SerializeField] private bool setPointFilter = true;
    [SerializeField] private int pointFilterMaxPixels = 256 * 256;
    [SerializeField] private bool extractEmbeddedMaterials = true;

    // Section 2 state.
    [SerializeField] private bool useSectionOneFbx = true;
    [SerializeField] private GameObject animFbxOverride;
    [SerializeField] private int idleOption;
    [SerializeField] private int walkOption;
    private AnimationClip[] clips = new AnimationClip[0];
    private string[] clipOptions = new[] { NoneOption };
    private GameObject scannedFbx;

    // Section 3 state.
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private GameObject replacementModel;
    [SerializeField] private Vector3 modelScale = Vector3.one;
    [SerializeField] private float yRotationOffset;

    private Vector2 scroll;

    [MenuItem("Tools/AR Shooter/Model Swap Tool")]
    public static void Open()
    {
        var window = GetWindow<ModelSwapTool>("Model Swap Tool");
        window.minSize = new Vector2(380, 520);
        window.Show();
    }

    // ------------------------------------------------------------------
    // GUI
    // ------------------------------------------------------------------

    private void OnGUI()
    {
        // Clip list is rebuilt during Layout so the control count stays stable between events.
        if (Event.current.type == EventType.Layout && CurrentAnimFbx() != scannedFbx)
            RefreshClips(CurrentAnimFbx());

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawImportSection();
        EditorGUILayout.Space(14);
        DrawAnimatorSection();
        EditorGUILayout.Space(14);
        DrawSwapSection();
        EditorGUILayout.EndScrollView();
    }

    // Draws section 1: FBX import fixup.
    private void DrawImportSection()
    {
        EditorGUILayout.LabelField("1. Model Import Fixup", EditorStyles.boldLabel);

        importFbx = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("FBX Asset", "FBX model from the Project window. Its import settings and materials are checked."),
            importFbx, typeof(GameObject), false);

        extractEmbeddedMaterials = EditorGUILayout.Toggle(
            new GUIContent("Extract Embedded Materials",
                "Embedded FBX materials cannot hold edits across reimports. When enabled, they are extracted to a 'Materials' folder next to the FBX before textures are assigned."),
            extractEmbeddedMaterials);

        setPointFilter = EditorGUILayout.Toggle(
            new GUIContent("Point Filter Small Atlas",
                "Sets the assigned atlas texture to Point filtering when its pixel count is small (hard-edged color atlas)."),
            setPointFilter);

        using (new EditorGUI.DisabledScope(!setPointFilter))
        {
            pointFilterMaxPixels = EditorGUILayout.IntField(
                new GUIContent("Max Pixels", "Textures with width x height at or below this value count as small. 65536 = 256x256."),
                pointFilterMaxPixels);
        }

        using (new EditorGUI.DisabledScope(importFbx == null))
        {
            if (GUILayout.Button(new GUIContent("Fix Import Settings",
                    "Sets the rig to Create From This Model if needed and fills empty material texture slots from an atlas image.")))
                FixImportSettings(importFbx);
        }
    }

    // Draws section 2: Animator Controller generation.
    private void DrawAnimatorSection()
    {
        EditorGUILayout.LabelField("2. Animator Generation", EditorStyles.boldLabel);

        useSectionOneFbx = EditorGUILayout.Toggle(
            new GUIContent("Use FBX From Section 1", "When enabled, the FBX from section 1 is used."),
            useSectionOneFbx);

        if (useSectionOneFbx)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(new GUIContent("FBX Asset", "FBX taken from section 1."),
                    importFbx, typeof(GameObject), false);
            }
        }
        else
        {
            animFbxOverride = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("FBX Asset", "FBX whose AnimationClip sub-assets are listed below."),
                animFbxOverride, typeof(GameObject), false);
        }

        GameObject fbx = CurrentAnimFbx();
        if (fbx == null)
        {
            EditorGUILayout.HelpBox("Pick an FBX to list its animation clips.", MessageType.Info);
            return;
        }

        if (clips.Length == 0)
        {
            EditorGUILayout.HelpBox("No AnimationClip sub-assets were found in this FBX.", MessageType.Warning);
        }
        else
        {
            idleOption = EditorGUILayout.Popup(
                new GUIContent("Idle Clip", "Clip used by the default 'Idle' state."), idleOption, clipOptions);
            walkOption = EditorGUILayout.Popup(
                new GUIContent("Walk Clip", "Clip used by the 'Walk' state."), walkOption, clipOptions);
        }

        if (GUILayout.Button(new GUIContent("Refresh Clip List", "Rescans the FBX for AnimationClip sub-assets.")))
            RefreshClips(fbx);

        bool canGenerate = clips.Length > 0 && idleOption > 0 && walkOption > 0;
        using (new EditorGUI.DisabledScope(!canGenerate))
        {
            if (GUILayout.Button(new GUIContent("Generate Animator Controller",
                    "Creates Assets/Animations/<FBXName>Animator.controller with Idle, Walk, and an IsMoving bool.")))
                GenerateAnimator(fbx, clips[idleOption - 1], clips[walkOption - 1]);
        }
    }

    // Draws section 3: prefab swap.
    private void DrawSwapSection()
    {
        EditorGUILayout.LabelField("3. Prefab Swap", EditorStyles.boldLabel);

        targetPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Target Prefab", "Prefab asset to modify, e.g. Assets/Prefabs/MeleeEnemy.prefab or ShooterEnemy.prefab."),
            targetPrefab, typeof(GameObject), false);

        replacementModel = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Replacement Model", "FBX asset, or a scene instance of the FBX, that becomes the new 'Model_New' child."),
            replacementModel, typeof(GameObject), true);

        using (new EditorGUI.DisabledScope(importFbx == null))
        {
            if (GUILayout.Button(new GUIContent("Use FBX From Section 1", "Copies the section 1 FBX into Replacement Model.")))
                replacementModel = importFbx;
        }

        modelScale = EditorGUILayout.Vector3Field(
            new GUIContent("Scale", "Local scale applied to Model_New. Default (1,1,1)."), modelScale);
        yRotationOffset = EditorGUILayout.FloatField(
            new GUIContent("Y Rotation Offset", "Local Y rotation in degrees applied to Model_New to correct facing direction."),
            yRotationOffset);

        EditorGUILayout.HelpBox(
            "The existing 'Model' child is disabled, not deleted. Re-running replaces any existing 'Model_New' child.",
            MessageType.None);

        using (new EditorGUI.DisabledScope(targetPrefab == null || replacementModel == null))
        {
            if (GUILayout.Button(new GUIContent("Swap Model In Prefab",
                    "Edits the target prefab asset: disables 'Model', adds 'Model_New', rewires bodyRenderer, and assigns the Animator Controller if one exists.")))
                SwapModel();
        }
    }

    // ------------------------------------------------------------------
    // Section 1: import fixup
    // ------------------------------------------------------------------

    // Runs the rig fix and the material fix for one FBX and logs a summary.
    private void FixImportSettings(GameObject fbx)
    {
        var changes = new List<string>();
        var warnings = new List<string>();
        string fbxPath = AssetDatabase.GetAssetPath(fbx);

        try
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError(LogPrefix + "'" + fbxPath + "' has no ModelImporter. Pick an FBX asset from the Project window.");
                return;
            }

            FixRig(importer, changes, warnings);
            FixMaterials(importer, fbxPath, changes, warnings);
            AssetDatabase.SaveAssets();
        }
        catch (Exception e)
        {
            Debug.LogError(LogPrefix + "Fix Import Settings failed for '" + fbxPath + "': " + e);
            warnings.Add("The fixup stopped early because of an error. See the error above.");
        }

        Debug.Log(LogPrefix + "Fix Import Settings for '" + fbxPath + "':\n- "
                  + (changes.Count > 0 ? string.Join("\n- ", changes) : "No changes made."));

        foreach (string warning in warnings)
            Debug.LogWarning(LogPrefix + warning);

        if (warnings.Count == 0)
            Debug.Log(LogPrefix + "No unresolved issues were detected by this tool for '" + fbxPath + "'.");
    }

    // Switches a Generic rig with no avatar to Create From This Model and reimports.
    private static void FixRig(ModelImporter importer, List<string> changes, List<string> warnings)
    {
        if (importer.animationType == ModelImporterAnimationType.Generic
            && importer.avatarSetup == ModelImporterAvatarSetup.NoAvatar)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            changes.Add("Rig: Avatar Definition changed from No Avatar to Create From This Model and reimported.");
        }
        else
        {
            changes.Add("Rig: unchanged (animationType=" + importer.animationType
                        + ", avatarSetup=" + importer.avatarSetup + ").");
        }

        if (importer.animationType == ModelImporterAnimationType.None)
            warnings.Add("Rig: Animation Type is None. Set it to Generic or Humanoid manually if this model needs to animate.");
    }

    // Extracts embedded materials if requested, then fills empty texture slots.
    private void FixMaterials(ModelImporter importer, string fbxPath, List<string> changes, List<string> warnings)
    {
        string folder = NormalizePath(Path.GetDirectoryName(fbxPath));

        if (extractEmbeddedMaterials)
            ExtractEmbeddedMaterials(fbxPath, folder, changes, warnings);

        List<Material> materials = CollectMaterials(fbxPath);
        if (materials.Count == 0)
        {
            warnings.Add("Materials: none found on this FBX.");
            return;
        }

        string fbxName = Path.GetFileNameWithoutExtension(fbxPath);
        Texture2D atlas = FindAtlasTexture(folder, fbxName);
        bool atlasAssigned = false;

        foreach (Material mat in materials)
        {
            string matPath = AssetDatabase.GetAssetPath(mat);

            if (!matPath.StartsWith("Assets/"))
            {
                warnings.Add("Material '" + mat.name + "' is outside Assets (" + matPath + ") and was skipped.");
                continue;
            }

            if (matPath == fbxPath)
            {
                warnings.Add("Material '" + mat.name + "' is embedded in the FBX, so edits would be lost on reimport. "
                             + "Enable Extract Embedded Materials or extract it in the FBX Materials tab.");
                continue;
            }

            bool isUrp = mat.shader != null && mat.shader.name.StartsWith("Universal Render Pipeline/");
            string existingSlot = mat.HasProperty("_BaseMap") ? "_BaseMap" : (mat.HasProperty("_MainTex") ? "_MainTex" : null);
            Texture existing = existingSlot != null ? mat.GetTexture(existingSlot) : null;

            if (existing != null)
            {
                changes.Add("Material '" + mat.name + "': " + existingSlot + " already set to '" + existing.name + "'.");
                if (!isUrp)
                    warnings.Add("Material '" + mat.name + "' uses non-URP shader '" + mat.shader.name + "' and may render pink under URP.");
                continue;
            }

            if (atlas == null)
            {
                warnings.Add("Material '" + mat.name + "': texture slot is empty and no atlas image (name containing 'atlas' or matching '"
                             + fbxName + "', PNG/JPG) was found in " + folder + ". Assign a texture manually.");
                continue;
            }

            if (!isUrp)
            {
                Shader urp = Shader.Find(UrpLitShaderName);
                if (urp != null)
                {
                    string oldShader = mat.shader != null ? mat.shader.name : "none";
                    mat.shader = urp;
                    changes.Add("Material '" + mat.name + "': shader changed from '" + oldShader + "' to '" + UrpLitShaderName + "'.");
                }
                else
                {
                    warnings.Add("Material '" + mat.name + "': '" + UrpLitShaderName + "' was not found. Shader left unchanged.");
                }
            }

            string targetSlot = mat.HasProperty("_BaseMap") ? "_BaseMap" : (mat.HasProperty("_MainTex") ? "_MainTex" : null);
            if (targetSlot == null)
            {
                warnings.Add("Material '" + mat.name + "' has neither _BaseMap nor _MainTex. Assign a texture manually.");
                continue;
            }

            mat.SetTexture(targetSlot, atlas);
            EditorUtility.SetDirty(mat);
            atlasAssigned = true;
            changes.Add("Material '" + mat.name + "': " + targetSlot + " set to '" + AssetDatabase.GetAssetPath(atlas) + "'.");
        }

        if (atlasAssigned && setPointFilter)
            ApplyPointFilter(atlas, changes, warnings);
    }

    // Extracts materials embedded in the FBX to a Materials folder and reimports the FBX.
    private static void ExtractEmbeddedMaterials(string fbxPath, string folder, List<string> changes, List<string> warnings)
    {
        List<Material> embedded = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>().ToList();
        if (embedded.Count == 0)
            return;

        string materialsFolder = folder + "/Materials";
        if (!AssetDatabase.IsValidFolder(materialsFolder))
            AssetDatabase.CreateFolder(folder, "Materials");

        bool extractedAny = false;
        foreach (Material mat in embedded)
        {
            string target = AssetDatabase.GenerateUniqueAssetPath(materialsFolder + "/" + SanitizeFileName(mat.name) + ".mat");
            string error = AssetDatabase.ExtractAsset(mat, target);
            if (string.IsNullOrEmpty(error))
            {
                extractedAny = true;
                changes.Add("Materials: extracted embedded material '" + mat.name + "' to '" + target + "'.");
            }
            else
            {
                warnings.Add("Materials: could not extract '" + mat.name + "': " + error);
            }
        }

        if (extractedAny)
        {
            AssetDatabase.WriteImportSettingsIfDirty(fbxPath);
            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
        }
    }

    // Gathers materials that belong to the FBX, either as sub-assets or as renderer materials.
    private static List<Material> CollectMaterials(string fbxPath)
    {
        var result = new HashSet<Material>();

        foreach (Material m in AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>())
            result.Add(m);

        var root = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (root != null)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
                foreach (Material m in r.sharedMaterials)
                    if (m != null)
                        result.Add(m);
        }

        return result.ToList();
    }

    // Looks in the FBX folder only. Prefers a name containing "atlas", then an exact FBX name match, then a partial match.
    private static Texture2D FindAtlasTexture(string folder, string fbxName)
    {
        List<string> candidates = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => string.Equals(NormalizePath(Path.GetDirectoryName(p)), folder, StringComparison.OrdinalIgnoreCase))
            .Where(p =>
            {
                string ext = Path.GetExtension(p).ToLowerInvariant();
                return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
            })
            .ToList();

        string chosen =
            candidates.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).IndexOf("atlas", StringComparison.OrdinalIgnoreCase) >= 0)
            ?? candidates.FirstOrDefault(p => string.Equals(Path.GetFileNameWithoutExtension(p), fbxName, StringComparison.OrdinalIgnoreCase))
            ?? candidates.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p).IndexOf(fbxName, StringComparison.OrdinalIgnoreCase) >= 0);

        return chosen != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(chosen) : null;
    }

    // Sets Point filtering on the atlas if its pixel count is at or below the threshold.
    private void ApplyPointFilter(Texture2D atlas, List<string> changes, List<string> warnings)
    {
        string path = AssetDatabase.GetAssetPath(atlas);
        long pixels = (long)atlas.width * atlas.height;

        if (pixels > pointFilterMaxPixels)
        {
            changes.Add("Texture '" + path + "': " + pixels + " pixels exceeds the threshold of "
                        + pointFilterMaxPixels + ", filter mode left unchanged.");
            return;
        }

        var texImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        if (texImporter == null)
        {
            warnings.Add("Texture '" + path + "' has no TextureImporter. Filter mode not changed.");
            return;
        }

        if (texImporter.filterMode == FilterMode.Point)
        {
            changes.Add("Texture '" + path + "': filter mode already Point.");
            return;
        }

        FilterMode previous = texImporter.filterMode;
        texImporter.filterMode = FilterMode.Point;
        texImporter.SaveAndReimport();
        changes.Add("Texture '" + path + "': filter mode changed from " + previous + " to Point (" + pixels + " pixels).");
    }

    // ------------------------------------------------------------------
    // Section 2: animator generation
    // ------------------------------------------------------------------

    // Returns the FBX selected for animator generation.
    private GameObject CurrentAnimFbx()
    {
        return useSectionOneFbx ? importFbx : animFbxOverride;
    }

    // Rebuilds the clip list from the FBX sub-assets and preselects likely Idle and Walk clips.
    private void RefreshClips(GameObject fbx)
    {
        scannedFbx = fbx;
        clips = new AnimationClip[0];
        clipOptions = new[] { NoneOption };
        idleOption = 0;
        walkOption = 0;

        if (fbx == null)
            return;

        string path = AssetDatabase.GetAssetPath(fbx);
        clips = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__"))
            .OrderBy(c => c.name)
            .ToArray();

        clipOptions = new[] { NoneOption }.Concat(clips.Select(c => c.name)).ToArray();
        idleOption = GuessOption("idle");
        walkOption = GuessOption("walk", "crawl", "run");
    }

    // Returns the popup index of the first clip whose name contains a keyword, or 0 for none.
    private int GuessOption(params string[] keywords)
    {
        foreach (string keyword in keywords)
        {
            for (int i = 0; i < clips.Length; i++)
                if (clips[i].name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i + 1;
        }
        return 0;
    }

    // Creates the Animator Controller asset with Idle/Walk states, an IsMoving bool, and two transitions.
    private void GenerateAnimator(GameObject fbx, AnimationClip idleClip, AnimationClip walkClip)
    {
        try
        {
            string fbxPath = AssetDatabase.GetAssetPath(fbx);

            if (!AssetDatabase.IsValidFolder(AnimationsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Animations");
                Debug.Log(LogPrefix + "Created folder '" + AnimationsFolder + "'.");
            }

            string desiredPath = ControllerPathFor(fbxPath);
            string controllerPath = desiredPath;
            if (AssetDatabase.LoadAssetAtPath<Object>(desiredPath) != null)
            {
                controllerPath = AssetDatabase.GenerateUniqueAssetPath(desiredPath);
                Debug.LogWarning(LogPrefix + "'" + desiredPath + "' already exists and was not overwritten. Using '" + controllerPath + "' instead.");
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            AnimatorState idle = stateMachine.AddState("Idle");
            idle.motion = idleClip;
            AnimatorState walk = stateMachine.AddState("Walk");
            walk.motion = walkClip;
            stateMachine.defaultState = idle;

            AnimatorStateTransition toWalk = idle.AddTransition(walk);
            toWalk.hasExitTime = false;
            toWalk.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");

            AnimatorStateTransition toIdle = walk.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            SessionState.SetString(SessionKeyPrefix + fbxPath, controllerPath);

            Debug.Log(LogPrefix + "Created Animator Controller at '" + controllerPath + "' (Idle='" + idleClip.name
                      + "' default, Walk='" + walkClip.name + "', bool IsMoving, transitions without exit time).");

            WarnIfNotLooping(idleClip);
            WarnIfNotLooping(walkClip);
        }
        catch (Exception e)
        {
            Debug.LogError(LogPrefix + "Generate Animator Controller failed: " + e);
        }
    }

    // Warns when a clip is not set to loop, since Idle and Walk normally should.
    private static void WarnIfNotLooping(AnimationClip clip)
    {
        if (!AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            Debug.LogWarning(LogPrefix + "Clip '" + clip.name + "' does not have Loop Time enabled. Enable it in the FBX Animation tab if it should repeat.");
    }

    // Returns the default controller path for an FBX.
    private static string ControllerPathFor(string fbxPath)
    {
        return AnimationsFolder + "/" + Path.GetFileNameWithoutExtension(fbxPath) + "Animator.controller";
    }

    // Finds the controller generated for an FBX in this editor session, or the default path as a fallback.
    private static AnimatorController FindControllerFor(string fbxPath)
    {
        string stored = SessionState.GetString(SessionKeyPrefix + fbxPath, "");
        if (!string.IsNullOrEmpty(stored))
        {
            var fromSession = AssetDatabase.LoadAssetAtPath<AnimatorController>(stored);
            if (fromSession != null)
                return fromSession;
        }
        return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPathFor(fbxPath));
    }

    // ------------------------------------------------------------------
    // Section 3: prefab swap
    // ------------------------------------------------------------------

    // Applies the model swap to the target prefab asset and logs every action taken.
    private void SwapModel()
    {
        var actions = new List<string>();
        var warnings = new List<string>();
        GameObject root = null;
        string prefabPath = null;
        bool saved = false;

        try
        {
            if (!EditorUtility.IsPersistent(targetPrefab) || !PrefabUtility.IsPartOfPrefabAsset(targetPrefab))
            {
                Debug.LogError(LogPrefix + "Target Prefab must be a prefab asset from the Project window, not a scene object.");
                return;
            }

            GameObject sourceAsset = ResolveModelAsset(replacementModel);
            if (sourceAsset == null)
            {
                Debug.LogError(LogPrefix + "Replacement Model must be an FBX/prefab asset or a scene instance of one.");
                return;
            }

            prefabPath = AssetDatabase.GetAssetPath(targetPrefab);
            string sourcePath = AssetDatabase.GetAssetPath(sourceAsset);

            root = PrefabUtility.LoadPrefabContents(prefabPath);

            // Remove a Model_New left by a previous run so re-running does not create duplicates.
            Transform previousNew = root.transform.Find(NewModelName);
            if (previousNew != null)
            {
                Object.DestroyImmediate(previousNew.gameObject);
                actions.Add("Removed existing '" + NewModelName + "' child from a previous run.");
            }

            // Disable the placeholder.
            Transform oldModel = root.transform.Find(OldModelName);
            if (oldModel != null)
            {
                bool wasActive = oldModel.gameObject.activeSelf;
                oldModel.gameObject.SetActive(false);
                actions.Add("Set '" + OldModelName + "' child inactive (was " + (wasActive ? "active" : "already inactive") + "). It was not deleted.");
            }
            else
            {
                warnings.Add("No child named '" + OldModelName + "' was found on the prefab root. Nothing was disabled.");
            }

            // Add the replacement.
            var newModel = (GameObject)PrefabUtility.InstantiatePrefab(sourceAsset, root.transform);
            newModel.name = NewModelName;
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.Euler(0f, yRotationOffset, 0f);
            newModel.transform.localScale = modelScale;
            actions.Add("Instantiated '" + sourcePath + "' as child '" + NewModelName + "' at local position (0,0,0), rotation Y="
                        + yRotationOffset + ", scale " + modelScale + ".");

            // Point bodyRenderer at the new renderer.
            Renderer bodyRenderer = FindBodyRenderer(newModel, warnings);
            if (bodyRenderer != null)
                AssignBodyRenderer(root, bodyRenderer, actions, warnings);
            else
                warnings.Add("No SkinnedMeshRenderer or MeshRenderer was found under '" + NewModelName + "'. bodyRenderer was not changed.");

            // Assign the Animator Controller if one was generated for this FBX.
            AssignAnimator(newModel, sourcePath, actions, warnings);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out saved);
            if (saved)
                actions.Add("Saved prefab '" + prefabPath + "'.");
            else
                Debug.LogError(LogPrefix + "Saving '" + prefabPath + "' failed. No changes were written to the prefab.");
        }
        catch (Exception e)
        {
            saved = false;
            Debug.LogError(LogPrefix + "Swap Model In Prefab failed: " + e + "\nThe prefab asset was not saved, so it is unchanged.");
        }
        finally
        {
            if (root != null)
                PrefabUtility.UnloadPrefabContents(root);
        }

        if (!saved)
            return;

        Debug.Log(LogPrefix + "Swap summary for '" + prefabPath + "':\n- " + string.Join("\n- ", actions));

        foreach (string warning in warnings)
            Debug.LogWarning(LogPrefix + warning);

        Debug.Log(LogPrefix + "Next steps: verify facing direction, scale, and vertical offset in Play mode. "
                  + "The old '" + OldModelName + "' child is disabled but still present; delete it manually once the result is confirmed. "
                  + "Changes can be reviewed or reverted through version control.");
    }

    // Returns the asset behind the field value: the asset itself, or the source of a scene instance.
    private static GameObject ResolveModelAsset(GameObject go)
    {
        if (go == null)
            return null;
        if (EditorUtility.IsPersistent(go))
            return go;

        GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
        return instanceRoot != null ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(instanceRoot) : null;
    }

    // Picks the first SkinnedMeshRenderer, falling back to the first MeshRenderer.
    private static Renderer FindBodyRenderer(GameObject newModel, List<string> warnings)
    {
        SkinnedMeshRenderer[] skinned = newModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (skinned.Length > 0)
        {
            if (skinned.Length > 1)
                warnings.Add("Found " + skinned.Length + " SkinnedMeshRenderers under '" + NewModelName
                             + "'. The first one was used, so hit-flash will only affect that renderer.");
            return skinned[0];
        }

        MeshRenderer[] meshes = newModel.GetComponentsInChildren<MeshRenderer>(true);
        if (meshes.Length > 1)
            warnings.Add("Found " + meshes.Length + " MeshRenderers under '" + NewModelName
                         + "'. The first one was used, so hit-flash will only affect that renderer.");
        return meshes.Length > 0 ? meshes[0] : null;
    }

    // Sets the private "bodyRenderer" field on any script on the root that exposes it to serialization.
    private static void AssignBodyRenderer(GameObject root, Renderer renderer, List<string> actions, List<string> warnings)
    {
        string rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, root.transform);
        bool assigned = false;

        foreach (MonoBehaviour behaviour in root.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null)
                continue;

            var serialized = new SerializedObject(behaviour);
            SerializedProperty property = serialized.FindProperty(BodyRendererFieldName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                continue;

            Object previous = property.objectReferenceValue;
            property.objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (property.objectReferenceValue != renderer)
            {
                warnings.Add("'" + behaviour.GetType().Name + "." + BodyRendererFieldName + "' rejected a " + renderer.GetType().Name
                             + ". Check the field's declared type.");
                continue;
            }

            assigned = true;
            actions.Add("Set " + behaviour.GetType().Name + "." + BodyRendererFieldName + " to '" + rendererPath + "' ("
                        + renderer.GetType().Name + "), previously "
                        + (previous != null ? "'" + previous.name + "'" : "empty") + ".");
        }

        if (!assigned)
            warnings.Add("No script on the prefab root has a serialized '" + BodyRendererFieldName + "' field. "
                         + "The field must be serialized (public or [SerializeField]) to be set. Assign the Body Renderer manually.");
    }

    // Finds or adds an Animator on the new model and assigns the generated controller if there is one.
    private static void AssignAnimator(GameObject newModel, string sourcePath, List<string> actions, List<string> warnings)
    {
        AnimatorController controller = FindControllerFor(sourcePath);
        if (controller == null)
        {
            actions.Add("No generated Animator Controller found for '" + sourcePath + "'. Animator not changed.");
            return;
        }

        Animator animator = newModel.GetComponent<Animator>();
        bool added = false;
        if (animator == null)
        {
            animator = newModel.AddComponent<Animator>();
            added = true;
        }

        animator.runtimeAnimatorController = controller;
        actions.Add((added ? "Added Animator to '" : "Found Animator on '") + NewModelName + "' and assigned controller '"
                    + AssetDatabase.GetAssetPath(controller) + "'.");

        if (animator.avatar == null)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<Avatar>().FirstOrDefault();
            if (avatar != null)
            {
                animator.avatar = avatar;
                actions.Add("Assigned avatar '" + avatar.name + "' from '" + sourcePath + "' to the Animator.");
            }
            else
            {
                warnings.Add("The Animator has no avatar and none was found in '" + sourcePath + "'. Run Fix Import Settings or check the FBX Rig tab.");
            }
        }

        actions.Add("Animator Apply Root Motion is " + (animator.applyRootMotion ? "enabled" : "disabled")
                    + ". Disable it if movement is driven by script.");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    // Converts backslashes to forward slashes for asset paths.
    private static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? "" : path.Replace('\\', '/');
    }

    // Replaces characters that are invalid in file names.
    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
