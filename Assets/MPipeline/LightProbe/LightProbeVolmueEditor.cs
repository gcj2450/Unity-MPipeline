﻿//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;
//using System.Linq;
//using System.Xml.Serialization;
//using System.IO;
//using System.Reflection;
//using UnityEditorInternal;
//using UnityEngine.SceneManagement;

//namespace MPipeline
//{

//    class PointEditr : IEditablePoint
//    {
//        private bool m_Editing;

//        private List<Vector3> m_SourcePositions;
//        private List<int> m_Selection = new List<int>();

//        private SMPSelection m_SerializedSelectedProbes;

//        private readonly LightProbeVolmue m_Group;
//        private bool m_ShouldRecalculateTetrahedra;
//        private Vector3 m_LastPosition = Vector3.zero;
//        private Quaternion m_LastRotation = Quaternion.identity;
//        private Vector3 m_LastScale = Vector3.one;
//        private LightProbeVolumeEditor m_Inspector;
//        private LPSystemManager m_probeSystem;

//        public PointEditr(LightProbeVolmue group, LightProbeVolumeEditor inspector)
//        {
//            m_Group = group;
//            m_LastPosition = group.transform.position;
//            m_LastRotation = group.transform.rotation;
//            m_LastScale = group.transform.localScale;
//            m_SerializedSelectedProbes = ScriptableObject.CreateInstance<SMPSelection>();
//            m_SerializedSelectedProbes.hideFlags = HideFlags.HideAndDontSave;
//            m_Inspector = inspector;
//            try {
//                m_probeSystem = GameObject.Find("LPSystem").GetComponent<LPSystemManager>();
//            }
//            catch (System.Exception) { }
//            if (m_probeSystem == null) {
//                GameObject lpsys = new GameObject("LPSystem");
//                m_probeSystem = lpsys.AddComponent<LPSystemManager>();
//                m_probeSystem.sceneLightProbeFile = LPSceneFile.CreateAsset(SceneManager.GetActiveScene().name);
//            }
//        }

//        public void SetEditing(bool editing)
//        {
//            m_Editing = editing;
//        }

//        public void AddProbe(Vector3 position)
//        {
//            Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { m_Group, m_SerializedSelectedProbes }, "Add Probe");
//            m_SourcePositions.Add(position);
//            SelectProbe(m_SourcePositions.Count - 1);

//            MarkDirtyChunk();
//        }

//        private void SelectProbe(int i)
//        {
//            if (!m_Selection.Contains(i))
//                m_Selection.Add(i);
//        }

//        public void SelectAllProbes()
//        {
//            DeselectProbes();

//            var count = m_SourcePositions.Count;
//            for (var i = 0; i < count; i++)
//                m_Selection.Add(i);
//        }

//        public void DeselectProbes()
//        {
//            m_Selection.Clear();
//            m_SerializedSelectedProbes.m_Selection = m_Selection;
//        }

//        private IEnumerable<Vector3> SelectedProbePositions()
//        {
//            return m_Selection.Select(t => m_SourcePositions[t]).ToList();
//        }

//        public void DuplicateSelectedProbes()
//        {
//            var selectionCount = m_Selection.Count;
//            if (selectionCount == 0) return;

//            Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { m_Group, m_SerializedSelectedProbes }, "Duplicate Probes");

//            foreach (var position in SelectedProbePositions())
//            {
//                m_SourcePositions.Add(position);
//            }

//            MarkDirtyChunk();
//        }

//        private void CopySelectedProbes()
//        {
//            //Convert probes to world position for serialization
//            var localPositions = SelectedProbePositions();

//            var serializer = new XmlSerializer(typeof(Vector3[]));
//            var writer = new StringWriter();

//            serializer.Serialize(writer, localPositions.Select(pos => m_Group.transform.TransformPoint(pos)).ToArray());
//            writer.Close();
//            GUIUtility.systemCopyBuffer = writer.ToString();
//        }

//        private static bool CanPasteProbes()
//        {
//            try
//            {
//                var deserializer = new XmlSerializer(typeof(Vector3[]));
//                var reader = new StringReader(GUIUtility.systemCopyBuffer);
//                deserializer.Deserialize(reader);
//                reader.Close();
//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        private bool PasteProbes()
//        {
//            //If we can't paste / paste buffer is bad do nothing
//            try
//            {
//                var deserializer = new XmlSerializer(typeof(Vector3[]));
//                var reader = new StringReader(GUIUtility.systemCopyBuffer);
//                var pastedProbes = (Vector3[])deserializer.Deserialize(reader);
//                reader.Close();

//                if (pastedProbes.Length == 0) return false;

//                Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { m_Group, m_SerializedSelectedProbes }, "Paste Probes");

//                var oldLength = m_SourcePositions.Count;

//                //Need to convert into local space...
//                foreach (var position in pastedProbes)
//                {
//                    m_SourcePositions.Add(m_Group.transform.InverseTransformPoint(position));
//                }

//                //Change selection to be the newly pasted probes
//                DeselectProbes();
//                for (int i = oldLength; i < oldLength + pastedProbes.Length; i++)
//                {
//                    SelectProbe(i);
//                }
//                MarkDirtyChunk();

//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public bool InsertProbe()
//        {
//            try
//            {
//                Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { m_Group, m_SerializedSelectedProbes }, "Insert Probes");

//                var oldLength = m_SourcePositions.Count;

//                int idx = m_Selection[0];

//                m_SourcePositions.Insert(idx, m_SourcePositions[idx]);

//                //Change selection to be the newly pasted probes
//                DeselectProbes();

//                SelectProbe(idx);

//                return true;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public void RemoveSelectedProbes()
//        {
//            int selectionCount = m_Selection.Count;
//            if (selectionCount == 0)
//                return;

//            Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { m_Group, m_SerializedSelectedProbes }, "Delete Probes");

//            var reverseSortedIndicies = m_Selection.OrderByDescending(x => x);
//            foreach (var index in reverseSortedIndicies)
//            {
//                m_SourcePositions.RemoveAt(index);
//            }
//            DeselectProbes();
//            MarkDirtyChunk();
//        }

//        public void RebuildProbes()
//        {
//            Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { m_Group, m_SerializedSelectedProbes }, "Rebuild Probes");

//            m_SourcePositions.Clear();

//            var resources = AssetDatabase.LoadAssetAtPath<LPResources>("Assets/MPipeline/LightProbe/Resources/LPResources.asset");
//            var cs_GetSurfelIntersect = resources.GetSurfelIntersect;

//            ComputeBuffer cb_Intersect = new ComputeBuffer(1, sizeof(int));


//            Transform trans = m_Group.transform;
//            Vector3 max_size = m_Group.volumeSize / 2;
//            Vector3 probe_pos = max_size;
//            float cell_size = m_Group.cellSize;

//            probe_pos = probe_pos / cell_size;
//            probe_pos = - new Vector3(Mathf.Floor(probe_pos.x), Mathf.Floor(probe_pos.y), Mathf.Floor(probe_pos.z)) * cell_size;
//            Vector3 init_Pos = probe_pos;

//            GameObject go = new GameObject();
//            Camera cam = go.AddComponent<Camera>();
//            var info = go.AddComponent<BakeLightProbeInfomation>();
//            cam.cameraType = (CameraType)32;
//            go.SetActive(false);
//            cam.enabled = false;
//            cam.aspect = 1;
//            cam.transform.up = Vector3.up;
//            cam.transform.forward = Vector3.forward;
//            float distance = 30;
//            cam.orthographicSize = distance;
//            cam.farClipPlane = distance * 2;

//            RenderTextureDescriptor rtd = new RenderTextureDescriptor(128, 128, RenderTextureFormat.ARGBFloat, 24);
//            rtd.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
//            rtd.autoGenerateMips = false;
//            rtd.useMipMap = false;
//            info.rt2 = new RenderTexture(rtd); 
//            info.rt2.Create();
//            info.rt2.filterMode = FilterMode.Point;
//            info.rt3 = new RenderTexture(rtd);
//            info.rt3.Create();
//            info.rt3.filterMode = FilterMode.Point;

//            rtd = new RenderTextureDescriptor(128, 128, RenderTextureFormat.ARGBFloat, 0);
//            rtd.dimension = UnityEngine.Rendering.TextureDimension.Cube;
//            info.rt0 = new RenderTexture(rtd);
//            info.rt0.Create();
//            info.rt0.filterMode = FilterMode.Point;
//            info.rt1 = new RenderTexture(rtd);
//            info.rt1.Create();
//            info.rt1.filterMode = FilterMode.Point;

//            while (probe_pos.x < max_size.x)
//            {
//                while (probe_pos.y < max_size.y)
//                {
//                    while (probe_pos.z < max_size.z)
//                    {
//                        go.transform.position = trans.TransformPoint(probe_pos);

//                        cam.Render();

//                        cs_GetSurfelIntersect.SetTexture(0, "Cube0", info.rt0);
//                        cs_GetSurfelIntersect.SetTexture(0, "Cube1", info.rt1);
//                        cs_GetSurfelIntersect.SetVector("ProbePosition", probe_pos);
//                        cb_Intersect.SetData(new int[] { 0 });
//                        cs_GetSurfelIntersect.SetBuffer(0, "Result", cb_Intersect);
//                        cs_GetSurfelIntersect.Dispatch(0, 4, 4, 4);

//                        int[] res = new int[1];
//                        cb_Intersect.GetData(res);

//                        if (res[0] != 0)
//                            m_SourcePositions.Add(probe_pos);
                        
//                        probe_pos.z += cell_size;
//                    }
//                    probe_pos.y += cell_size;
//                    probe_pos.z = init_Pos.z;
//                }
//                probe_pos.x += cell_size;
//                probe_pos.y = init_Pos.y;
//                probe_pos.z = init_Pos.z;
//            }

//            info.rt0.Release();
//            info.rt1.Release();
//            info.rt2.Release();
//            info.rt3.Release();
//            cb_Intersect.Dispose();
//            GameObject.DestroyImmediate(go);

//            DeselectProbes();
//            MarkDirtyChunk();
//        }


//        public void PullProbePositions()
//        {
//            if (m_Group != null && m_SerializedSelectedProbes != null)
//            {
//                m_SourcePositions = new List<Vector3>(m_Group.probePositions);
//                m_Selection = new List<int>(m_SerializedSelectedProbes.m_Selection);
//            }
//        }

//        public void PushProbePositions()
//        {
//            m_Group.probePositions = m_SourcePositions.ToArray();
//            m_SerializedSelectedProbes.m_Selection = m_Selection;
//        }

//        public void HandleEditMenuHotKeyCommands()
//        {
//            //Handle other events!
//            if (Event.current.type == EventType.ValidateCommand
//                || Event.current.type == EventType.ExecuteCommand)
//            {
//                bool execute = Event.current.type == EventType.ExecuteCommand;
//                switch (Event.current.commandName)
//                {
//                    case EventCommandNames.SoftDelete:
//                    case EventCommandNames.Delete:
//                        if (execute) RemoveSelectedProbes();
//                        Event.current.Use();
//                        break;
//                    case EventCommandNames.Duplicate:
//                        if (execute) DuplicateSelectedProbes();
//                        Event.current.Use();
//                        break;
//                    case EventCommandNames.SelectAll:
//                        if (execute)
//                            SelectAllProbes();
//                        Event.current.Use();
//                        break;
//                    case EventCommandNames.Cut:
//                        if (execute)
//                        {
//                            CopySelectedProbes();
//                            RemoveSelectedProbes();
//                        }
//                        Event.current.Use();
//                        break;
//                    case EventCommandNames.Copy:
//                        if (execute) CopySelectedProbes();
//                        Event.current.Use();
//                        break;
//                }
//            }
//        }


//        bool have_saved = false;
//        public bool OnSceneGUI(Transform transform)
//        {
//            if (!m_Group.enabled)
//                return m_Editing;

//            if (Event.current.type == EventType.Layout)
//            {
//                //If the group has moved / scaled since last frame need to retetra);)
//                if (m_LastPosition != m_Group.transform.position
//                    || m_LastRotation != m_Group.transform.rotation
//                    || m_LastScale != m_Group.transform.localScale)
//                {
//                    MarkDirtyChunk();
//                }

//                m_LastPosition = m_Group.transform.position;
//                m_LastRotation = m_Group.transform.rotation;
//                m_LastScale = m_Group.transform.localScale;
//            }

//            //See if we should enter edit mode!
//            bool firstSelect = false;
//            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
//            {
//                //We have no probes selected and have clicked the mouse... Did we click a probe
//                if (SelectedCount == 0)
//                {
//                    var selected = PointEditor.FindNearest(Event.current.mousePosition, transform, this);
//                    var clickedProbe = selected != -1;

//                    if (clickedProbe && !m_Editing)
//                    {
//                        m_Inspector.StartEditMode();
//                        m_Editing = true;
//                        firstSelect = true;
//                    }
//                }
//            }

//            //Need to cache this as select points will use it!
//            var mouseUpEvent = Event.current.type == EventType.MouseUp;

//            if (m_Editing)
//            {
//                if (PointEditor.SelectPoints(this, transform, ref m_Selection, firstSelect))
//                {
//                    Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { m_Group, m_SerializedSelectedProbes }, "Select Probes");
//                    m_Inspector.Repaint();
//                }
//                if (SelectedCount > 0)
//                {
//                    Transform trans = m_Group.transform;
//                    Vector3 pos = trans.TransformPoint(SelectedCount > 0 ? GetSelectedPositions()[0] : Vector3.zero);
//                    Vector3 newPosition = Handles.DoPositionHandle(pos, Quaternion.identity);

//                    if (mouseUpEvent)
//                    {
//                        have_saved = false;
//                    }
//                    if (newPosition != pos)
//                    {
//                        newPosition = trans.InverseTransformPoint(newPosition);
//                        pos = trans.InverseTransformPoint(pos);
//                        if (!have_saved)
//                        {
//                            Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { m_Group, m_SerializedSelectedProbes }, "Move Probes");
//                            have_saved = true;
//                        }
//                        Vector3[] selectedPositions = GetSelectedPositions();
//                        Vector3 delta = newPosition - pos;
//                        for (int i = 0; i < selectedPositions.Length; i++)
//                            UpdateSelectedPosition(i, selectedPositions[i] + delta);
//                        MarkDirtyChunk();
//                    }
//                }
//            }

//            //Special handling for paste (want to be able to paste when not in edit mode!)

//            if ((Event.current.type == EventType.ValidateCommand || Event.current.type == EventType.ExecuteCommand)
//                && Event.current.commandName == EventCommandNames.Paste)
//            {
//                if (Event.current.type == EventType.ValidateCommand)
//                {
//                    if (CanPasteProbes())
//                        Event.current.Use();
//                }
//                if (Event.current.type == EventType.ExecuteCommand)
//                {
//                    if (PasteProbes())
//                    {
//                        Event.current.Use();
//                        m_Editing = true;
//                    }
//                }
//            }

//            PointEditor.Draw(this, transform, m_Selection, true);


//            //volume size
//            {
//                var color = Handles.color;

//                Handles.color = Color.yellow;

//                var trans = m_Group.transform;
//                var cellSize = m_Group.cellSize / 2;
//                bool changed = false;


//                Vector3 half_size = m_Group.volumeSize / 2;

//                {
//                    var handle_pos = trans.TransformPoint(Vector3.right * half_size.x);
//                    var poss = Handles.Slider(handle_pos, trans.right, 0.1f, Handles.CubeHandleCap, 0);
//                    float new_v = trans.InverseTransformPoint(poss).x;
//                    new_v = new_v < cellSize ? cellSize : new_v;
//                    if (new_v != half_size.x) { changed = true; half_size.x = new_v; }
//                }
//                {
//                    var handle_pos = trans.TransformPoint(Vector3.left * half_size.x);
//                    var poss = Handles.Slider(handle_pos, -trans.right, 0.1f, Handles.CubeHandleCap, 0);
//                    float new_v = -trans.InverseTransformPoint(poss).x;
//                    new_v = new_v < cellSize ? cellSize : new_v;
//                    if (new_v != half_size.x) { changed = true; half_size.x = new_v; }
//                }
//                {
//                    var handle_pos = trans.TransformPoint(Vector3.up * half_size.y);
//                    var poss = Handles.Slider(handle_pos, trans.up, 0.1f, Handles.CubeHandleCap, 0);
//                    float new_v = trans.InverseTransformPoint(poss).y;
//                    new_v = new_v < cellSize ? cellSize : new_v;
//                    if (new_v != half_size.y) { changed = true; half_size.y = new_v; }
//                }
//                {
//                    var handle_pos = trans.TransformPoint(Vector3.down * half_size.y);
//                    var poss = Handles.Slider(handle_pos, -trans.up, 0.1f, Handles.CubeHandleCap, 0);
//                    float new_v = -trans.InverseTransformPoint(poss).y;
//                    new_v = new_v < cellSize ? cellSize : new_v;
//                    if (new_v != half_size.y) { changed = true; half_size.y = new_v; }
//                }
//                {
//                    var handle_pos = trans.TransformPoint(Vector3.forward * half_size.z);
//                    var poss = Handles.Slider(handle_pos, trans.forward, 0.1f, Handles.CubeHandleCap, 0);
//                    float new_v = trans.InverseTransformPoint(poss).z;
//                    new_v = new_v < cellSize ? cellSize : new_v;
//                    if (new_v != half_size.z) { changed = true; half_size.z = new_v; }
//                }
//                {
//                    var handle_pos = trans.TransformPoint(Vector3.back * half_size.z);
//                    var poss = Handles.Slider(handle_pos, -trans.forward, 0.1f, Handles.CubeHandleCap, 0);
//                    float new_v = -trans.InverseTransformPoint(poss).z;
//                    new_v = new_v < cellSize ? cellSize : new_v;
//                    if (new_v != half_size.z) { changed = true; half_size.z = new_v; }
//                }

//                if (mouseUpEvent)
//                {
//                    have_saved = false;
//                }
//                if (changed)
//                {
//                    if (!have_saved)
//                    {
//                        Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { m_Group }, "Change probe volume size");
//                        have_saved = true;
//                    }
//                    m_Group.volumeSize = half_size * 2;
//                }
                
//                Handles.color = color;
//            }

//            while (m_Group.showBakeInfoInScene)
//            {
//                if (m_Group.probePositions.Length == 0) break;

//                Vector3 p0, p1, p2, p3, p4, p5, p6, p7;
//                Vector3 halfVolumeSize = m_Group.volumeSize / 2;
//                Transform trans = m_Group.transform;
//                p0 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(-1, -1, -1)));
//                p1 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(1, -1, -1)));
//                p2 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(1, -1, 1)));
//                p3 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(-1, -1, 1)));
//                p4 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(-1, 1, -1)));
//                p5 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(1, 1, -1)));
//                p6 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(1, 1, 1)));
//                p7 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(-1, 1, 1)));
//                Vector3 a, b;
//                a = Vector3.Min(p0, Vector3.Min(p1, Vector3.Min(p2, Vector3.Min(p3, Vector3.Min(p4, Vector3.Min(p5, Vector3.Min(p6, p7)))))));
//                b = Vector3.Max(p0, Vector3.Max(p1, Vector3.Max(p2, Vector3.Max(p3, Vector3.Max(p4, Vector3.Max(p5, Vector3.Max(p6, p7)))))));
//                Vector2 minV = new Vector2(a.x, a.z), maxV = new Vector2(b.x, b.z);
//                minV /= 64; maxV /= 64;

//                Vector2Int minChunkId = new Vector2Int(Mathf.FloorToInt(minV.x), Mathf.FloorToInt(minV.y)), maxChunkId = new Vector2Int(Mathf.FloorToInt(maxV.x), Mathf.FloorToInt(maxV.y));

//                for (int i = minChunkId.x; i <= maxChunkId.x; i++)
//                    for (int j = minChunkId.y; j <= maxChunkId.y; j++)
//                    {
//                        var chunk = m_probeSystem.GetChunk(new Vector2Int(i, j));
//                        if (chunk != null)
//                        {
//                            DrawChunkInfo(chunk);
//                        }
//                    }
//                break;
//            }


//            if (!m_Editing)
//                return m_Editing;

//            HandleEditMenuHotKeyCommands();

//            return m_Editing;
//        }

//        void DrawChunkInfo(LPChunk chunk)
//        {
//            Handles.matrix = Matrix4x4.identity;
//            foreach (var surfel in chunk.surfels)
//            {
//                Handles.color = new Color(surfel.albedo.x, surfel.albedo.y, surfel.albedo.z);
//                Handles.Slider(surfel.position, surfel.normal, 0.2f, Handles.ArrowHandleCap, 0);
//            }
//        }

//        public void MarkDirtyChunk()
//        {
//            if (m_Group.probePositions.Length == 0) return;

//            Vector3 p0, p1, p2, p3, p4, p5, p6, p7;
//            Vector3 halfVolumeSize = m_Group.volumeSize / 2;
//            Transform trans = m_Group.transform;
//            p0 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(-1, -1, -1)));
//            p1 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(1, -1, -1)));
//            p2 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(1, -1, 1)));
//            p3 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(-1, -1, 1)));
//            p4 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(-1, 1, -1)));
//            p5 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(1, 1, -1)));
//            p6 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(1, 1, 1)));
//            p7 = trans.TransformPoint(Vector3.Scale(halfVolumeSize, new Vector3(-1, 1, 1)));
//            Vector3 a, b;
//            a = Vector3.Min(p0, Vector3.Min(p1, Vector3.Min(p2, Vector3.Min(p3, Vector3.Min(p4, Vector3.Min(p5, Vector3.Min(p6, p7)))))));
//            b = Vector3.Max(p0, Vector3.Max(p1, Vector3.Max(p2, Vector3.Max(p3, Vector3.Max(p4, Vector3.Max(p5, Vector3.Max(p6, p7)))))));
//            Vector2 minV = new Vector2(a.x, a.z), maxV = new Vector2(b.x, b.z);
//            minV /= 64; maxV /= 64;

//            Vector2Int minChunkId = new Vector2Int(Mathf.FloorToInt(minV.x), Mathf.FloorToInt(minV.y)), maxChunkId = new Vector2Int(Mathf.FloorToInt(maxV.x), Mathf.FloorToInt(maxV.y));

//            for (int i = minChunkId.x; i <= maxChunkId.x; i++)
//                for (int j = minChunkId.y; j <= maxChunkId.y; j++)
//                    m_probeSystem.MarkDirt(new Vector2Int(i,j), m_Group);
//        }

//        public Bounds selectedProbeBounds
//        {
//            get
//            {
//                List<Vector3> selectedPoints = new List<Vector3>();
//                foreach (var idx in m_Selection)
//                    selectedPoints.Add(m_SourcePositions[(int)idx]);
//                return GetBounds(selectedPoints);
//            }
//        }

//        public Bounds bounds
//        {
//            get { return GetBounds(m_SourcePositions); }
//        }

//        private Bounds GetBounds(List<Vector3> positions)
//        {
//            if (positions.Count == 0)
//                return new Bounds();

//            if (positions.Count == 1)
//                return new Bounds(m_Group.transform.TransformPoint(positions[0]), new Vector3(1f, 1f, 1f));

//            return GeometryUtility.CalculateBounds(positions.ToArray(), m_Group.transform.localToWorldMatrix);
//        }

//        /// Get the world-space position of a specific point
//        public Vector3 GetPosition(int idx)
//        {
//            return m_SourcePositions[idx];
//        }

//        public Vector3 GetWorldPosition(int idx)
//        {
//            return m_Group.transform.TransformPoint(m_SourcePositions[idx]);
//        }

//        public void SetPosition(int idx, Vector3 position)
//        {
//            if (m_SourcePositions[idx] == position)
//                return;

//            m_SourcePositions[idx] = position;
//        }

//        private static readonly Color kCloudColor = new Color(200f / 255f, 0, 20f / 255f, 0.75f);
//        private static readonly Color kSelectedCloudColor = new Color(.3f, 0, 1, 1);

//        public Color GetDefaultColor()
//        {
//            return kCloudColor;
//        }

//        public Color GetSelectedColor()
//        {
//            return kSelectedCloudColor;
//        }

//        public float GetPointScale()
//        {
//            var t = typeof(Lightmapping).Assembly.GetType("UnityEditor.AnnotationUtility").GetProperty("iconSize", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
//            return 10.0f * (float)(t);
//        }

//        public Vector3[] GetSelectedPositions()
//        {
//            var selectedCount = SelectedCount;
//            var result = new Vector3[selectedCount];
//            for (int i = 0; i < selectedCount; i++)
//            {
//                result[i] = m_SourcePositions[m_Selection[i]];
//            }
//            return result;
//        }

//        public Vector3[] GetSelectedPositionsInSM()
//        {
//            var selectedCount = SelectedCount;
//            var result = new Vector3[selectedCount];
//            for (int i = 0; i < selectedCount; i++)
//            {
//                result[i] = m_Group.probePositions[m_Selection[i]];
//            }
//            return result;
//        }

//        public void UpdateSelectedPosition(int idx, Vector3 position)
//        {
//            if (idx > (SelectedCount - 1))
//                return;

//            m_SourcePositions[m_Selection[idx]] = position;
//        }

//        public IEnumerable<Vector3> GetPositions()
//        {
//            return m_SourcePositions;
//        }

//        public Vector3[] GetUnselectedPositions()
//        {
//            var totalProbeCount = Count;
//            var selectedProbeCount = SelectedCount;

//            if (selectedProbeCount == totalProbeCount)
//            {
//                return new Vector3[0];
//            }
//            else if (selectedProbeCount == 0)
//            {
//                return m_SourcePositions.ToArray();
//            }
//            else
//            {
//                var selectionList = new bool[totalProbeCount];

//                // Mark everything unselected
//                for (int i = 0; i < totalProbeCount; i++)
//                {
//                    selectionList[i] = false;
//                }

//                // Mark selected
//                for (int i = 0; i < selectedProbeCount; i++)
//                {
//                    selectionList[m_Selection[i]] = true;
//                }

//                // Get remaining unselected
//                var result = new Vector3[totalProbeCount - selectedProbeCount];
//                var unselectedCount = 0;
//                for (int i = 0; i < totalProbeCount; i++)
//                {
//                    if (selectionList[i] == false)
//                    {
//                        result[unselectedCount++] = m_SourcePositions[i];
//                    }
//                }

//                return result;
//            }
//        }

//        /// How many points are there in the array.
//        public int Count { get { return m_SourcePositions.Count; } }


//        /// How many points are selected in the array.
//        public int SelectedCount { get { return m_Selection.Count; } }
//    }




//    [CustomEditor(typeof(LightProbeVolmue))]
//    class LightProbeVolumeEditor : Editor
//    {
//        private static class Styles
//        {
//            public static readonly GUIContent showVolume = EditorGUIUtility.TrTextContent("Show volume", "Display volume in scene view");
//            public static readonly GUIContent showBake = EditorGUIUtility.TrTextContent("Show bake info", "Display bake infomation in scene view");
//            public static readonly GUIContent cellSize = EditorGUIUtility.TrTextContent("Cell size");
//            public static readonly GUIContent volumeSize = EditorGUIUtility.TrTextContent("Volume size");
//            public static readonly GUIContent rebuild = EditorGUIUtility.TrTextContent("Rebuild probes automatically");
//            public static readonly GUIContent selectedProbePosition = EditorGUIUtility.TrTextContent("Selected Probe Position", "The local position of this ponit relative to parent.");
//            public static readonly GUIContent addPoint = EditorGUIUtility.TrTextContent("Add Probe");
//            public static readonly GUIContent deleteSelected = EditorGUIUtility.TrTextContent("Delete Selected");
//            public static readonly GUIContent selectAll = EditorGUIUtility.TrTextContent("Select All");
//            public static readonly GUIContent duplicateSelected = EditorGUIUtility.TrTextContent("Duplicate Selected");
//            public static readonly GUIContent rebake = EditorGUIUtility.TrTextContent("Rebake probes of scene", "Will rebake all probes changed in the scene, not only this light probe component.");
//            public static readonly GUIContent editModeButton;

//            static Styles()
//            {
//                editModeButton = EditorGUIUtility.IconContent("EditCollider");
//            }
//        }
//        private PointEditr m_Editor;
//        private LPSystemManager m_probeSystem;

//        public void OnEnable()
//        {
//            m_Editor = new PointEditr(target as LightProbeVolmue, this);
//            m_Editor.PullProbePositions();
//            m_Editor.DeselectProbes();
//            m_Editor.PushProbePositions();
//            SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
//            Undo.undoRedoPerformed += UndoRedoPerformed;
//            EditMode.onEditModeStartDelegate += OnEditModeStarted;
//            EditMode.onEditModeEndDelegate += OnEditModeEnded;
//            try {
//                m_probeSystem = GameObject.Find("LPSystem").GetComponent<LPSystemManager>();
//            }
//            catch (System.Exception) {}
//            if (m_probeSystem == null)
//            {
//                GameObject lpsys = new GameObject("LPSystem");
//                m_probeSystem = lpsys.AddComponent<LPSystemManager>();
//                m_probeSystem.sceneLightProbeFile = LPSceneFile.CreateAsset(SceneManager.GetActiveScene().name);
//            }
//        }

//        private void OnEditModeEnded(Editor owner)
//        {
//            if (owner == this)
//            {
//                EndEditProbes();
//            }
//        }

//        private void OnEditModeStarted(Editor owner, EditMode.SceneViewEditMode mode)
//        {
//            if (owner == this && mode == EditMode.SceneViewEditMode.LightProbeGroup)
//            {
//                StartEditProbes();
//            }
//        }

//        public void StartEditMode()
//        {
//            EditMode.ChangeEditMode(EditMode.SceneViewEditMode.LightProbeGroup, m_Editor.bounds, this);
//        }

//        private void StartEditProbes()
//        {
//            if (m_EditingProbes)
//                return;

//            m_EditingProbes = true;
//            m_Editor.SetEditing(true);
//            Tools.hidden = true;
//            SceneView.RepaintAll();
//        }

//        private void EndEditProbes()
//        {
//            if (!m_EditingProbes)
//                return;

//            m_Editor.DeselectProbes();
//            m_Editor.SetEditing(false);
//            m_EditingProbes = false;
//            Tools.hidden = false;
//            SceneView.RepaintAll();
//        }

//        public void OnDisable()
//        {
//            EndEditProbes();
//            Undo.undoRedoPerformed -= UndoRedoPerformed;
//            SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
//            if (target != null)
//            {
//                m_Editor.PushProbePositions();
//                m_Editor = null;
//            }
//        }

//        private void UndoRedoPerformed()
//        {
//            // Update the cached probe positions from the ones just restored in the LightProbeGroup
//            m_Editor.PullProbePositions();

//            m_Editor.MarkDirtyChunk();
//        }

//        private bool m_EditingProbes;
//        private bool m_ShouldFocus;
//        public override void OnInspectorGUI()
//        {
//            m_Editor.PullProbePositions();

//            var lp = target as LightProbeVolmue;
//            if (!lp) return;


//            lp.showVolumeInScene = EditorGUILayout.Toggle(Styles.showVolume, lp.showVolumeInScene);

//            lp.showBakeInfoInScene = EditorGUILayout.Toggle(Styles.showBake, lp.showBakeInfoInScene);

//            lp.cellSize = EditorGUILayout.FloatField(Styles.cellSize, lp.cellSize);
//            lp.volumeSize = EditorGUILayout.Vector3Field(Styles.volumeSize, lp.volumeSize);

//            if (GUILayout.Button(Styles.rebuild))
//            {
//                m_Editor.RebuildProbes();
//            }

//            GUILayout.Space(10);

//            EditMode.DoEditModeInspectorModeButton(UnityEditorInternal.EditMode.SceneViewEditMode.LightProbeGroup, "Edit Probes manually", EditorGUIUtility.IconContent("EditCollider"), this.m_Editor.bounds, this);

//            GUILayout.Space(3);
//            EditorGUI.BeginDisabledGroup(EditMode.editMode != EditMode.SceneViewEditMode.LightProbeGroup);

//            //bool performDeringing = EditorGUILayout.Toggle(Styles.performDeringing, m_Editor.GetDeringProbes());
//            //m_Editor.SetDeringProbes(performDeringing);


//            EditorGUI.BeginChangeCheck();

//            EditorGUI.BeginDisabledGroup(m_Editor.SelectedCount == 0);
//            Vector3 pos = m_Editor.SelectedCount > 0 ? m_Editor.GetSelectedPositions()[0] : Vector3.zero;
//            Vector3 newPosition = EditorGUILayout.Vector3Field(Styles.selectedProbePosition, pos);
//            if (newPosition != pos)
//            {
//                Vector3[] selectedPositions = m_Editor.GetSelectedPositions();
//                Vector3 delta = newPosition - pos;
//                for (int i = 0; i < selectedPositions.Length; i++)
//                    m_Editor.UpdateSelectedPosition(i, selectedPositions[i] + delta);
//                m_Editor.MarkDirtyChunk();
//            }
//            EditorGUI.EndDisabledGroup();

//            GUILayout.Space(3);

//            GUILayout.BeginHorizontal();
//            GUILayout.BeginVertical();

//            if (GUILayout.Button(Styles.addPoint))
//            {
//                var position = Vector3.zero;
//                //if (SceneView.lastActiveSceneView)
//                //{
//                //    var probeGroup = target as SplineMesh;
//                //    if (probeGroup) position = probeGroup.transform.InverseTransformPoint(position);
//                //}
//                StartEditProbes();
//                m_Editor.DeselectProbes();
//                m_Editor.AddProbe(position);
//            }

//            if (GUILayout.Button(Styles.deleteSelected))
//            {
//                StartEditProbes();
//                m_Editor.RemoveSelectedProbes();
//            }
//            GUILayout.EndVertical();
//            GUILayout.BeginVertical();

//            if (GUILayout.Button(Styles.selectAll))
//            {
//                StartEditProbes();
//                m_Editor.SelectAllProbes();
//            }

//            if (GUILayout.Button(Styles.duplicateSelected))
//            {
//                StartEditProbes();
//                m_Editor.DuplicateSelectedProbes();
//            }

//            GUILayout.EndVertical();
//            GUILayout.EndHorizontal();

//            EditorGUI.EndDisabledGroup();

//            GUILayout.Space(10);

//            m_Editor.HandleEditMenuHotKeyCommands();
//            m_Editor.PushProbePositions();

//            if (GUILayout.Button(Styles.rebake))
//            {
//                m_probeSystem.RebakeDirtChunks();
//            }

//            SceneView.RepaintAll();
//        }

//        internal Bounds GetWorldBoundsOfTarget(UnityEngine.Object targetObject)
//        {
//            return m_Editor.bounds;
//        }

//        private void InternalOnSceneView()
//        {
//            if (SceneView.lastActiveSceneView != null)
//            {
//                if (m_ShouldFocus)
//                {
//                    m_ShouldFocus = false;
//                    SceneView.lastActiveSceneView.FrameSelected();
//                }
//            }

//            m_Editor.PullProbePositions();
//            var lpg = target as LightProbeVolmue;
//            if (lpg != null)
//            {
//                if (m_Editor.OnSceneGUI(lpg.transform))
//                    StartEditProbes();
//                else
//                    EndEditProbes();
//            }
//            m_Editor.PushProbePositions();
//        }

//        public void OnSceneGUI()
//        {
//            if (Event.current.type != EventType.Repaint)
//                InternalOnSceneView();
//        }

//        public void OnSceneGUIDelegate(SceneView sceneView)
//        {
//            if (Event.current.type == EventType.Repaint)
//                InternalOnSceneView();
//        }

//        public bool HasFrameBounds()
//        {
//            return m_Editor.SelectedCount > 0;
//        }

//        public Bounds OnGetFrameBounds()
//        {
//            return m_Editor.selectedProbeBounds;
//        }
//    }
//}