using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class StellarHiltScreenshot
{
    [MenuItem("Tools/Hilt Screenshot")]
    public static void TakeHiltScreenshots()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 相机
        var camGO = new GameObject("Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.fieldOfView = 45f;

        // 加载 EnergyHilt GLB
        var hiltModel = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/CosmicSlice/Art/Models/EnergyHilt.glb");
        if (hiltModel == null)
        {
            Debug.LogError("[HiltShot] Cannot load EnergyHilt.glb");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        // Shader fallback（batchmode 下 URP shader 可能不可用）
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Standard")
                        ?? Shader.Find("Diffuse");

        Material MakeMat(Color c)
        {
            if (litShader == null) return null;
            var m = new Material(litShader);
            m.color = c;
            return m;
        }

        // ---- 左手（青色） ----
        var leftRoot = new GameObject("LeftTool");
        leftRoot.transform.localScale = Vector3.one * 0.72f;

        var leftHilt = Object.Instantiate(hiltModel, leftRoot.transform);
        leftHilt.transform.localPosition = new Vector3(0f, 0f, -0.09f);
        leftHilt.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var bladeL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bladeL.transform.SetParent(leftRoot.transform);
        bladeL.transform.localPosition = new Vector3(0f, 0f, 0.18f);
        bladeL.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        bladeL.transform.localScale = new Vector3(0.02f, 0.18f, 0.02f);
        var mL = MakeMat(new Color(0f, 0.8f, 1f, 0.9f));
        if (mL != null) bladeL.GetComponent<Renderer>().material = mL;

        leftRoot.transform.position = new Vector3(-0.2f, 0f, 0f);

        // ---- 右手（琥珀色） ----
        var rightRoot = new GameObject("RightTool");
        rightRoot.transform.localScale = Vector3.one * 0.72f;

        var rightHilt = Object.Instantiate(hiltModel, rightRoot.transform);
        rightHilt.transform.localPosition = new Vector3(0f, 0f, -0.09f);
        rightHilt.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        var bladeR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bladeR.transform.SetParent(rightRoot.transform);
        bladeR.transform.localPosition = new Vector3(0f, 0f, 0.18f);
        bladeR.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        bladeR.transform.localScale = new Vector3(0.02f, 0.18f, 0.02f);
        var mR = MakeMat(new Color(1f, 0.6f, 0f, 0.9f));
        if (mR != null) bladeR.GetComponent<Renderer>().material = mR;

        rightRoot.transform.position = new Vector3(0.2f, 0f, 0f);

        // 光源
        var lightGO = new GameObject("Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGO.transform.rotation = Quaternion.Euler(45f, -45f, 0f);
        light.intensity = 1.5f;

        // 渲染工具函数
        void Shoot(Vector3 pos, string filename)
        {
            camGO.transform.position = pos;
            camGO.transform.LookAt(Vector3.zero);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            rt.Release();
            File.WriteAllBytes(filename, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log($"[HiltShot] Saved {filename}");
        }

        Directory.CreateDirectory("/tmp/stellar_hilt_v2");
        Shoot(new Vector3(0f, 0.05f, 0.5f),  "/tmp/stellar_hilt_v2/unity_hilt_side.png");
        Shoot(new Vector3(0.3f, 0.2f, 0.4f), "/tmp/stellar_hilt_v2/unity_hilt_iso.png");

        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    // batchmode 入口
    static void RunFromBatch()
    {
        TakeHiltScreenshots();
    }
}
