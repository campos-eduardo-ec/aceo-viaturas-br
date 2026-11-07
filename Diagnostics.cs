// Opt-in developer smoke test. Never opens a save or spawns a simulation vehicle.
// Runs only when ACEO_VIATURAS_TEST_DIR is set on the launching process.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AeroportosBR.Viaturas
{
    internal static class Diagnostics
    {
        private static Transform CopyGraphics(Transform source, Transform parent)
        {
            var node = new GameObject(source.name).transform;
            node.gameObject.tag = source.gameObject.tag;
            node.SetParent(parent, false);
            node.localPosition = source.localPosition;
            node.localRotation = source.localRotation;
            node.localScale = source.localScale;
            var sr = source.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var r = node.gameObject.AddComponent<SpriteRenderer>();
                r.sprite = sr.sprite; r.color = sr.color; r.sharedMaterial = sr.sharedMaterial;
                r.sortingLayerID = sr.sortingLayerID; r.sortingOrder = sr.sortingOrder;
                r.flipX = sr.flipX; r.flipY = sr.flipY; r.enabled = sr.enabled;
            }
            foreach (Transform child in source) CopyGraphics(child, node);
            node.gameObject.SetActive(source.gameObject.activeSelf);
            return node;
        }

        internal static IEnumerator Run(string directory)
        {
            // Load only the world scene resources. Suppress game bootstrap so no
            // airport, save, autosave or simulation is loaded or started.
            yield return new WaitForSecondsRealtime(3);
            var testHarmony = new Harmony(Plugin.Id + ".diagnostic");
            testHarmony.Patch(AccessTools.Method(typeof(GameController), "Start"),
                prefix: new HarmonyMethod(typeof(Diagnostics), nameof(SuppressGameStart)));
            var operation = SceneManager.LoadSceneAsync("GameWorld");
            while (!operation.isDone) yield return null;
            DataPlaceholderVehicles data = null;
            for (int i=0; i<120; i++)
            {
                data = Resources.FindObjectsOfTypeAll<DataPlaceholderVehicles>().FirstOrDefault(d=>d.airportPoliceCarPrefab != null);
                if (data != null && Resources.FindObjectsOfTypeAll<DataPlaceholderMaterials>().Length > 0 && Resources.FindObjectsOfTypeAll<AirportPoliceCarController>().Any(v=>v.transform.Find("Sprite")!=null)) break;
                yield return new WaitForSecondsRealtime(1);
            }
            Directory.CreateDirectory(directory);
            try { Execute(data, directory); }
            catch (Exception ex) { File.WriteAllText(Path.Combine(directory,"FAILED.txt"), ex.ToString()); Plugin.Log("DIAGNOSTIC FAILED: " + ex); }
            Application.Quit();
        }

        private static bool SuppressGameStart() => false;

        private static void Execute(DataPlaceholderVehicles data, string directory)
        {
            if (data == null) throw new Exception("Vehicle resource holder unavailable.");
            var checks = new List<string>();
            Action<bool,string> assert = (condition,label) => { if (!condition) throw new Exception(label); checks.Add("PASS " + label); };
            var roots = new List<GameObject>();
            var skins = new List<VehicleSkin>();
            var cameraObject = new GameObject("ViaturasBR_DiagnosticCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = 9;
            camera.aspect = 1;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f,.20f,.24f,1);
            camera.transform.position = new Vector3(50000,50000,-100);
            camera.nearClipPlane = .1f; camera.farClipPlane = 200;
            var target = new RenderTexture(1600,1600,24);
            camera.targetTexture = target;
            try
            {
                for (int i=0;i<4;i++)
                {
                    // DataPlaceholderVehicles contains UI previews, not the world prefabs.
                    var prefab = i<2 ? Resources.FindObjectsOfTypeAll<AirportPoliceCarController>().First(v=>v.transform.Find("Sprite")!=null).gameObject : i==2 ? Resources.FindObjectsOfTypeAll<FireTruckController>().First(v=>v.transform.Find("Sprite")!=null).gameObject : Resources.FindObjectsOfTypeAll<AmbulanceController>().First(v=>v.transform.Find("Sprite")!=null).gameObject;
                    var root = new GameObject("ViaturasBR_Diagnostic_" + i); roots.Add(root);
                    root.transform.position = new Vector3(50000,50006-i*4,0);
                    VehicleController vehicle = i<2 ? (VehicleController)root.AddComponent<AirportPoliceCarController>() : i==2 ? root.AddComponent<FireTruckController>() : (VehicleController)root.AddComponent<AmbulanceController>();
                    vehicle.enabled = false;
                    vehicle.spriteTransform = CopyGraphics(prefab.transform.Find("Sprite"), root.transform);
                    vehicle.vehicleModel = new VehicleModel { referenceID="diagnostic-"+i,vehicleName=i==0?"[PF] Test":"[RF] Test",vehicleColor=Color.white };
                    vehicle.allSprites = vehicle.spriteTransform.GetComponentsInChildren<SpriteRenderer>(true);
                    vehicle.colorableParts = new SpriteRenderer[0];
                    foreach (var sr in vehicle.allSprites) sr.sharedMaterial = SingletonNonDestroy<DataPlaceholderMaterials>.Instance.nonLitMateral;
                    if (i<2)
                    {
                        var main = vehicle.spriteTransform.Find("MainSprite");
                        main.gameObject.AddComponent<VehicleMaterialSetting>();
                        var combined = new CombinedVehicleSprite(main);
                        ((AirportPoliceCarController)vehicle).CombinedVehicleSprite = combined;
                        combined.SetComibinedSprite(new MaterialOffsetData {offset=new Vector2(.0066f,.1587f),boundary=new Vector4(0,1,.18f,.33f)}, data.serviceTexture2, true);
                    }
                    vehicle.isInside = true;
                    string before = JsonUtility.ToJson(vehicle.vehicleModel);
                    Plugin.Attach(vehicle);
                    Plugin.Attach(vehicle);
                    var skin = vehicle.GetComponent<VehicleSkin>(); skins.Add(skin);
                    assert(skin!=null,"skin created " + i);
                    assert(root.GetComponents<VehicleSkin>().Length==1,"idempotent attach " + i);
                    assert(before==JsonUtility.ToJson(vehicle.vehicleModel),"save data unchanged " + i);
                    AccessTools.Method(typeof(VehicleController),"SetMaterial").Invoke(vehicle,new object[]{SingletonNonDestroy<DataPlaceholderMaterials>.Instance.nonLitMateral});
                    assert(skin.CurrentLivery==(i==0?"pf":i==1?"rf":i==2?"fire":"ambulance"),"livery after material refresh " + i);
                    if(i==2) assert(vehicle.spriteTransform.Find("CraneBase/ExtensionBase/Extension")!=null,"articulated fire hose retained");
                    if(i==3) assert(vehicle.spriteTransform.Find("Chassie").GetComponent<SpriteRenderer>().sprite==Plugin.Art["ambulance"],"ambulance sprite replaced");
                    vehicle.spriteTransform.gameObject.SetActive(false);
                    assert(!skin.transform.Find("Sprite").gameObject.activeInHierarchy,"floor visibility inherited " + i);
                    vehicle.spriteTransform.gameObject.SetActive(true);
                }
                assert(Plugin.Choice("[RF]","same","PF")=="rf","explicit RF name overrides config");
                assert(Plugin.Choice("[PF]","same","RF")=="pf","explicit PF name overrides config");
                assert(Plugin.Choice("","stable-id","Mixed")==Plugin.Choice("","stable-id","Mixed"),"stable livery selection");
                assert(Enumerable.Range(0,100).Select(i=>Plugin.Choice("","vehicle-"+i,"Mixed")).Distinct().Count()==2,"mixed selection includes both liveries");
                camera.Render();
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var image = new Texture2D(1600,1600,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1600,1600),0,0); image.Apply();
                File.WriteAllBytes(Path.Combine(directory,"vehicles-unity.png"),ImageConversion.EncodeToPNG(image));
                UnityEngine.Object.Destroy(image); RenderTexture.active=previous;
                File.WriteAllText(Path.Combine(directory,"PASS.txt"),string.Join("\n",checks.ToArray()));
                Plugin.Log("DIAGNOSTIC PASS: " + checks.Count + " checks. " + directory);
            }
            finally
            {
                foreach(var root in roots) UnityEngine.Object.Destroy(root);
                camera.targetTexture=null; target.Release(); UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(cameraObject);
            }
        }
    }
}
