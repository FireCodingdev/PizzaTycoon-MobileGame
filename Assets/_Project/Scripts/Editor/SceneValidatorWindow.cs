using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PizzaTycoon.Map;
using PizzaTycoon.Customers;
using PizzaTycoon.Stations;
using PizzaTycoon.Player;
using PizzaTycoon.Camera;

namespace PizzaTycoon.Editor
{
    // Janela de validacao da cena — REPORTA problemas, nao corrige nada.
    // Cada linha tem botao [Select] para pular direto ao objeto problematico.
    public class SceneValidatorWindow : EditorWindow
    {
        private readonly List<Issue> _issues = new List<Issue>();
        private Vector2 _scroll;
        private bool    _hasRun;

        [MenuItem("PizzaTycoon/Validate Scene", priority = 0)]
        public static void Open()
        {
            var w = GetWindow<SceneValidatorWindow>("Validate Scene");
            w.minSize = new Vector2(420f, 300f);
            w.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run Validation", GUILayout.Height(28f)))
                    RunValidation();

                GUILayout.FlexibleSpace();
                if (_hasRun)
                {
                    int errs = 0, warns = 0;
                    foreach (var i in _issues) { if (i.Severity == Severity.Error) errs++; else warns++; }
                    var prevColor = GUI.color;
                    GUI.color = errs > 0 ? new Color(1f, 0.5f, 0.5f) : (warns > 0 ? new Color(1f, 0.85f, 0.4f) : new Color(0.6f, 1f, 0.6f));
                    EditorGUILayout.LabelField($"{errs} errors  |  {warns} warnings", GUILayout.Width(180f));
                    GUI.color = prevColor;
                }
            }

            EditorGUILayout.Space();

            if (!_hasRun)
            {
                EditorGUILayout.HelpBox("Clique em 'Run Validation' para checar a cena atual.", MessageType.Info);
                return;
            }

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Nenhum problema encontrado. Cena limpa!", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var issue in _issues)
                DrawIssue(issue);
            EditorGUILayout.EndScrollView();
        }

        private void DrawIssue(Issue issue)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                Color prev = GUI.color;
                GUI.color  = issue.Severity == Severity.Error ? new Color(1f, 0.55f, 0.55f) : new Color(1f, 0.85f, 0.4f);
                GUILayout.Label(issue.Severity == Severity.Error ? "ERRO" : "AVISO", GUILayout.Width(50f));
                GUI.color = prev;

                EditorGUILayout.LabelField(issue.Message, EditorStyles.wordWrappedLabel);

                if (issue.Target != null && GUILayout.Button("Select", GUILayout.Width(70f)))
                {
                    Selection.activeObject       = issue.Target;
                    EditorGUIUtility.PingObject(issue.Target);
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
            }
        }

        // ── Checks ──────────────────────────────────────────────────────────

        private void RunValidation()
        {
            _issues.Clear();
            _hasRun = true;

            CheckPlayer();
            CheckCamera();
            CheckCustomerSystem();
            CheckDriveThru();
            CheckStations();

            Debug.Log($"[Validate Scene] {_issues.Count} problema(s) encontrado(s).");
        }

        private void CheckPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                AddError("Nenhum GameObject com tag 'Player' encontrado.", null);
                return;
            }

            if (player.GetComponent<Rigidbody>() == null)
                AddError("Player: falta Rigidbody.", player);

            if (player.GetComponent<CapsuleCollider>() == null && player.GetComponent<CharacterController>() == null)
                AddWarn("Player: sem CapsuleCollider nem CharacterController.", player);

            if (player.GetComponent<PlayerController>() == null)
                AddError("Player: falta componente PlayerController.", player);

            var pa = player.GetComponent<PlayerAnimator>();
            if (pa == null)
            {
                AddWarn("Player: falta componente PlayerAnimator.", player);
            }
            else
            {
                var anim = player.GetComponentInChildren<Animator>();
                if (anim == null || anim.runtimeAnimatorController == null)
                    AddWarn("Player: PlayerAnimator sem Animator filho ou sem AnimatorController.", player);
            }
        }

        private void CheckCamera()
        {
            var cam = Object.FindObjectOfType<IsometricCameraFollow>();
            if (cam == null)
            {
                AddWarn("Cena sem IsometricCameraFollow — a camera nao vai seguir o player.", null);
                return;
            }

            var so = new SerializedObject(cam);
            var target = so.FindProperty("_target");
            if (target == null || target.objectReferenceValue == null)
                AddWarn("IsometricCameraFollow: _target nao atribuido (vai tentar encontrar o Player em runtime).", cam.gameObject);
        }

        private void CheckCustomerSystem()
        {
            var spawner = Object.FindObjectOfType<CustomerSpawner>();
            if (spawner == null)
            {
                AddWarn("Cena sem CustomerSpawner — nenhum cliente vai aparecer.", null);
            }
            else
            {
                var so = new SerializedObject(spawner);
                var prefab    = so.FindProperty("_customerPrefab");
                var variants  = so.FindProperty("_customerPrefabVariants");
                var queueRef  = so.FindProperty("_customerQueue");
                var spawnPt   = so.FindProperty("_spawnPoint");

                bool hasPrefab = (prefab != null && prefab.objectReferenceValue != null)
                              || (variants != null && variants.arraySize > 0);
                if (!hasPrefab)
                    AddError("CustomerSpawner: sem _customerPrefab nem _customerPrefabVariants.", spawner.gameObject);
                if (queueRef == null || queueRef.objectReferenceValue == null)
                    AddError("CustomerSpawner: _customerQueue nao atribuido.", spawner.gameObject);
                if (spawnPt == null || spawnPt.objectReferenceValue == null)
                    AddError("CustomerSpawner: _spawnPoint nao atribuido.", spawner.gameObject);
            }

            var queue = Object.FindObjectOfType<CustomerQueue>();
            if (queue == null)
            {
                AddWarn("Cena sem CustomerQueue — clientes nao terao posicao de fila.", null);
            }
            else
            {
                var so = new SerializedObject(queue);
                var qs = so.FindProperty("_queueStart");
                if (qs == null || qs.objectReferenceValue == null)
                    AddError("CustomerQueue: _queueStart NAO atribuido — clientes vao ficar parados no spawner (bug do giro).", queue.gameObject);
            }
        }

        private void CheckDriveThru()
        {
            var dt = Object.FindObjectOfType<DriveThruSystem>();
            if (dt == null)
            {
                AddWarn("Cena sem DriveThruSystem — sem carros no drive-thru.", null);
                return;
            }

            var so = new SerializedObject(dt);
            CheckRef(so, "_spawnPoint",  "DriveThru", dt.gameObject);
            CheckRef(so, "_orderPoint",  "DriveThru", dt.gameObject);
            CheckRef(so, "_exitPoint",   "DriveThru", dt.gameObject);

            var queueArr = so.FindProperty("_queuePositions");
            if (queueArr == null || queueArr.arraySize == 0)
                AddError("DriveThru: _queuePositions vazio (precisa de pelo menos 1 slot).", dt.gameObject);
            else
                for (int i = 0; i < queueArr.arraySize; i++)
                    if (queueArr.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        AddError($"DriveThru: _queuePositions[{i}] esta null.", dt.gameObject);

            var cars = so.FindProperty("_carPrefabs");
            if (cars == null || cars.arraySize == 0)
                AddError("DriveThru: _carPrefabs vazio — nenhum carro vai aparecer.", dt.gameObject);
        }

        private void CheckStations()
        {
            var stations = Object.FindObjectsOfType<BaseStation>(true);
            foreach (var st in stations)
            {
                var col = st.GetComponent<BoxCollider>();
                if (col == null)
                {
                    AddWarn($"{st.gameObject.name}: sem BoxCollider — player nao consegue interagir.", st.gameObject);
                    continue;
                }
                if (!col.isTrigger)
                    AddWarn($"{st.gameObject.name}: BoxCollider nao e Trigger (isTrigger=false).", st.gameObject);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private void CheckRef(SerializedObject so, string propName, string ctx, GameObject target)
        {
            var p = so.FindProperty(propName);
            if (p == null || p.objectReferenceValue == null)
                AddError($"{ctx}: {propName} nao atribuido.", target);
        }

        private void AddError(string msg, Object target) =>
            _issues.Add(new Issue { Severity = Severity.Error,   Message = msg, Target = target });

        private void AddWarn(string msg, Object target) =>
            _issues.Add(new Issue { Severity = Severity.Warning, Message = msg, Target = target });

        private enum Severity { Error, Warning }

        private class Issue
        {
            public Severity Severity;
            public string   Message;
            public Object   Target;
        }
    }
}
