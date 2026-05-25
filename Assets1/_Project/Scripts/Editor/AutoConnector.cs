#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using PizzaTycoon.Player;
using PizzaTycoon.Camera;
using PizzaTycoon.Input;
using PizzaTycoon.Customers;
using PizzaTycoon.Managers;
using PizzaTycoon.UI;
using PizzaTycoon.Stations;

namespace PizzaTycoon.Editor
{
    // Conecta automaticamente todas as referencias da cena
    // Menu: PizzaTycoon > 0. Auto Connect All References
    public static class AutoConnector
    {
        [MenuItem("PizzaTycoon/0. Auto Connect All References", priority = 0)]
        public static void ConnectAll()
        {
            int connected = 0;

            EnsureTags();

            connected += ConnectPlayer();
            connected += ConnectCamera();
            connected += ConnectCustomerSpawner();
            connected += ConnectStations();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            EditorUtility.DisplayDialog(
                "Pizza Tycoon - Auto Connect",
                $"[OK] {connected} referencias conectadas com sucesso!",
                "OK");
        }

        // ── Tags ────────────────────────────────────────────────────────────────
        private static void EnsureTags()
        {
            AddTagIfMissing("Player");
            AddTagIfMissing("Station");
            AddTagIfMissing("Customer");
        }

        private static void AddTagIfMissing(string tag)
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue == tag) return;
            }

            tagsProp.InsertArrayElementAtIndex(0);
            tagsProp.GetArrayElementAtIndex(0).stringValue = tag;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[AutoConnector] Tag '{tag}' adicionada ao projeto.");
        }

        // ── Player ─────────────────────────────────────────────────────────────
        private static int ConnectPlayer()
        {
            int n = 0;
            PlayerController player = Object.FindObjectOfType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[AutoConnector] PlayerController nao encontrado na cena.");
                return 0;
            }

            // Tag Player
            if (!player.gameObject.CompareTag("Player"))
            {
                player.gameObject.tag = "Player";
                n++;
            }

            // Joystick
            JoystickController joystick = Object.FindObjectOfType<JoystickController>(includeInactive: true);
            if (joystick != null)
            {
                SerializedObject so = new SerializedObject(player);
                SerializedProperty prop = so.FindProperty("_joystick");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    prop.objectReferenceValue = joystick;
                    so.ApplyModifiedProperties();
                    n++;
                }
            }

            // PlayerAnimator: body e headPivot
            PlayerAnimator anim = player.GetComponent<PlayerAnimator>();
            if (anim != null)
            {
                SerializedObject so = new SerializedObject(anim);
                Transform body = FindChildByName(player.transform, "Body");
                Transform head = FindChildByName(player.transform, "Head");

                SerializedProperty bodyProp = so.FindProperty("_body");
                SerializedProperty headProp = so.FindProperty("_headPivot");

                if (body != null && bodyProp != null && bodyProp.objectReferenceValue == null)
                {
                    bodyProp.objectReferenceValue = body;
                    n++;
                }
                if (head != null && headProp != null && headProp.objectReferenceValue == null)
                {
                    headProp.objectReferenceValue = head;
                    n++;
                }
                so.ApplyModifiedProperties();
            }

            return n;
        }

        // ── Camera ─────────────────────────────────────────────────────────────
        private static int ConnectCamera()
        {
            int n = 0;
            IsometricCameraFollow follow = Object.FindObjectOfType<IsometricCameraFollow>();
            PlayerController player = Object.FindObjectOfType<PlayerController>();

            if (follow != null && player != null)
            {
                SerializedObject so = new SerializedObject(follow);
                SerializedProperty target = so.FindProperty("_target");
                if (target != null && target.objectReferenceValue == null)
                {
                    target.objectReferenceValue = player.transform;
                    so.ApplyModifiedProperties();
                    n++;
                }
            }

            // CameraShake na Main Camera
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam != null && mainCam.GetComponent<CameraShake>() == null)
            {
                mainCam.gameObject.AddComponent<CameraShake>();
                n++;
            }

            return n;
        }

        // ── Customer Spawner ───────────────────────────────────────────────────
        private static int ConnectCustomerSpawner()
        {
            int n = 0;
            CustomerSpawner spawner = Object.FindObjectOfType<CustomerSpawner>();
            if (spawner == null) return 0;

            SerializedObject so = new SerializedObject(spawner);
            SerializedProperty prefabProp = so.FindProperty("_customerPrefab");

            if (prefabProp != null && prefabProp.objectReferenceValue == null)
            {
                // Tenta carregar de Resources e Prefabs/Customers/
                Customer prefab = Resources.Load<Customer>("Customer");
                if (prefab == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:Prefab Customer",
                        new[] { "Assets/_Project/Prefabs" });
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (obj != null && obj.GetComponent<Customer>() != null)
                        {
                            prefab = obj.GetComponent<Customer>();
                            break;
                        }
                    }
                }

                if (prefab != null)
                {
                    prefabProp.objectReferenceValue = prefab;
                    so.ApplyModifiedProperties();
                    n++;
                }
            }

            return n;
        }

        // ── Stations ───────────────────────────────────────────────────────────
        private static int ConnectStations()
        {
            int n = 0;
            BaseStation[] stations = Object.FindObjectsOfType<BaseStation>();
            foreach (BaseStation st in stations)
            {
                Collider col = st.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider box = st.gameObject.AddComponent<BoxCollider>();
                    box.size = new Vector3(3f, 2f, 3f);
                    box.center = new Vector3(0f, 1f, 0f);
                    box.isTrigger = true;
                    n++;
                }
                else if (!col.isTrigger)
                {
                    col.isTrigger = true;
                    n++;
                }
            }
            return n;
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private static Transform FindChildByName(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (child.name == name) return child;
            }
            return null;
        }
    }
}
#endif
