using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace Scurry.Editor
{
    public static class ColonyLayoutPreview
    {
        private static readonly Dictionary<int, Vector3> ColonyMapPositions = new Dictionary<int, Vector3>
        {
            // Defensive front (north)
            // Top row — above entrance
            { 41, new Vector3( -2.18f, 0, -10.33f) }, // Signal Tower
            { 42, new Vector3(  0.00f, 0, -10.33f) }, // Great City
            { 46, new Vector3(  2.20f, 0, -10.33f) }, // Throne Room
            { 43, new Vector3(  4.08f, 0, -10.33f) }, // Grand Stores

            // Colony Entrance
            { 25, new Vector3(  0.00f, 0,  -8.45f) }, // Colony Entrance

            // Tunnel Network
            { 28, new Vector3(  0.00f, 0,  -6.58f) }, // Tunnel Network

            // Left branch — storage/resources
            { 21, new Vector3( -4.05f, 0,  -4.70f) }, // Underground Storage
            { 23, new Vector3( -2.18f, 0,  -4.70f) }, // Seed Vault
            { 24, new Vector3( -0.30f, 0,  -4.70f) }, // Cold Storage

            // Right branch — military/training
            { 27, new Vector3(  2.20f, 0,  -4.70f) }, // Nursery
            { 37, new Vector3(  4.08f, 0,  -4.70f) }, // Training Ground
            { 29, new Vector3(  5.95f, 0,  -4.70f) }, // Workshop
            { 30, new Vector3(  7.83f, 0,  -4.70f) }, // Tailory

            // Mid-level
            { 39, new Vector3( -4.05f, 0,  -2.83f) }, // Mushroom Farm
            { 36, new Vector3(  4.08f, 0,  -2.83f) }, // Campfire
            { 40, new Vector3(  5.95f, 0,  -2.83f) }, // Fresh Water Pool

            // Core — Basic Burrow + War Hall
            { 26, new Vector3( -2.18f, 0,  -0.95f) }, // Basic Burrow
            { 38, new Vector3(  1.58f, 0,  -0.95f) }, // War Hall

            // Council Hall
            { 35, new Vector3(  0.00f, 0,   0.93f) }, // Council Hall

            // Defensive line
            { 31, new Vector3( -2.80f, 0,   2.80f) }, // Thorn Wall
            { 33, new Vector3( -0.93f, 0,   2.80f) }, // Pit Traps
            { 34, new Vector3(  0.95f, 0,   2.80f) }, // Wooden Gate
            { 32, new Vector3(  2.83f, 0,   2.80f) }, // Watchtower

            // Bottom row — advanced cards
            { 44, new Vector3( -2.80f, 0,   5.30f) }, // War Room
            { 45, new Vector3( -0.93f, 0,   5.30f) }, // Shrine of Heroes
            { 47, new Vector3(  0.95f, 0,   5.30f) }, // Forward Camp
            { 48, new Vector3(  2.83f, 0,   5.30f) }, // Messenger Post
            { 49, new Vector3(  4.70f, 0,   5.30f) }, // Forge
            { 50, new Vector3(  6.58f, 0,   5.30f) }, // Crafting Table
        };

        [MenuItem("Scurry/Create Colony Layout Preview Scene")]
        public static void CreatePreviewScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Background image as ground plane
            var bgTexture = Resources.Load<Texture2D>("Colony/rat_colony_board_background_2x");
            if (bgTexture != null)
            {
                var bgGO = new GameObject("ColonyBackground", typeof(MeshFilter), typeof(MeshRenderer));
                bgGO.GetComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                bgGO.transform.rotation = Quaternion.Euler(90, 0, 0);

                // Compute node extents to size and center the background
                float minX = float.MaxValue, maxX = float.MinValue;
                float minZ = float.MaxValue, maxZ = float.MinValue;
                foreach (var pos in ColonyMapPositions.Values)
                {
                    if (pos.x < minX) minX = pos.x;
                    if (pos.x > maxX) maxX = pos.x;
                    if (pos.z < minZ) minZ = pos.z;
                    if (pos.z > maxZ) maxZ = pos.z;
                }
                float centerX = (minX + maxX) * 0.5f;
                float centerZ = (minZ + maxZ) * 0.5f;
                float rangeX = maxX - minX;
                float rangeZ = maxZ - minZ;
                float imgAspect = (float)bgTexture.width / bgTexture.height;
                float padding = 1.35f;
                float neededHeight = rangeZ * padding;
                float quadHeight = neededHeight;
                float quadWidth = quadHeight * imgAspect;
                if (rangeX * padding / imgAspect > neededHeight)
                {
                    quadWidth = rangeX * padding;
                    quadHeight = quadWidth / imgAspect;
                }
                bgGO.transform.position = new Vector3(centerX, -0.5f, centerZ);
                bgGO.transform.localScale = new Vector3(quadWidth, quadHeight, 1);
                var bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                bgMat.mainTexture = bgTexture;
                bgGO.GetComponent<MeshRenderer>().material = bgMat;
            }
            else
            {
                // Fallback ground
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground";
                ground.transform.position = new Vector3(0, -0.55f, 0);
                ground.transform.localScale = new Vector3(25, 0.1f, 35);
                var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                groundMat.color = new Color(0.12f, 0.10f, 0.08f);
                ground.GetComponent<Renderer>().material = groundMat;
            }

            // Set up camera for isometric view
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 12f;
                // Position: 40 degree pitch, looking at center of colony layout
                float pitch = 40f * Mathf.Deg2Rad;
                float dist = 30f;
                Vector3 center = new Vector3(0, 0, -1.5f); // approximate center of all positions
                Vector3 offset = new Vector3(0, Mathf.Sin(pitch) * dist, -Mathf.Cos(pitch) * dist);
                cam.transform.position = center + offset;
                cam.transform.LookAt(center);
            }

            // Set up directional light
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                light.transform.rotation = Quaternion.Euler(50, -30, 0);
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.95f, 0.85f);
            }

            // Place all 30 colony card models
            var root = new GameObject("ColonyModels");
            int placed = 0;
            foreach (var kvp in ColonyMapPositions)
            {
                int cardId = kvp.Key;
                Vector3 pos = kvp.Value;

                string modelPath = $"Card Models/{cardId:D3}";
                var prefab = Resources.Load<GameObject>(modelPath);

                if (prefab != null)
                {
                    var instance = Object.Instantiate(prefab, root.transform);
                    instance.name = $"Colony_{cardId:D3}";
                    instance.transform.localPosition = pos + new Vector3(0, 0.5f, 0);
                    // Apply same rotation as ColonyOverlayUI
                    instance.transform.localRotation = instance.transform.localRotation * Quaternion.Euler(0, 0, 180);
                    instance.transform.localScale = Vector3.one * 0.8f;
                    placed++;
                    Debug.Log($"[ColonyLayoutPreview] Placed model {cardId:D3} at {pos}");
                }
                else
                {
                    // Fallback cube
                    var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    fallback.name = $"Colony_{cardId:D3}_Missing";
                    fallback.transform.SetParent(root.transform);
                    fallback.transform.localPosition = pos + new Vector3(0, 0.5f, 0);
                    fallback.transform.localScale = new Vector3(1.5f, 0.8f, 1.5f);
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = new Color(0.8f, 0.2f, 0.2f);
                    fallback.GetComponent<Renderer>().material = mat;
                    Debug.LogWarning($"[ColonyLayoutPreview] No model found for card {cardId}, placed red fallback cube");
                }
            }

            Debug.Log($"[ColonyLayoutPreview] Scene created with {placed} models placed");

            // Save scene
            string savePath = "Assets/Scenes/ColonyLayoutPreview.unity";
            EditorSceneManager.SaveScene(scene, savePath);
            Debug.Log($"[ColonyLayoutPreview] Scene saved to {savePath}");
        }
    }
}
