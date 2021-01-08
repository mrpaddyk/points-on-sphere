using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    [SerializeField] private GameObject vertexPrefab;
    [SerializeField] private GameObject linePrefab;

    [SerializeField] private Button resetButton;
    [SerializeField] private Button subdivideButton;
    [SerializeField] private Button toSphereButton;

    [SerializeField] private Text verticesText;
    [SerializeField] private Text linesText;

    private readonly float phi = 1.618f;

    private List<GameObject> vertexGameObjects;
    private List<GameObject> lineGameObjects;
    private List<Vector3> points;

    private readonly int linesInPool = 2000;
    private readonly int verticesInPool = 700;

    private List<List<int>> faces;
    private List<List<int>> edges;

    private float shortSide;
    private ObjectPool objectPool;
    private int subdivideStage;

    private void InitObjectPool() {
        objectPool = GetComponent<ObjectPool>();
        objectPool.SetContainer(this.gameObject);
        objectPool.CreatePool(vertexPrefab, verticesInPool);
        objectPool.CreatePool(linePrefab, linesInPool);
    }

    void Start()
    {

        SetButtonStates(false, false, false);

        InitObjectPool();

        //(0, ±1, ± φ)
        //(±1, ± φ, 0) 
        //(± φ, 0, ±1)

        vertexGameObjects = new List<GameObject>();
        lineGameObjects = new List<GameObject>();
        points = new List<Vector3>();

        points.Add(new Vector3(0f, 1f, phi));
        points.Add(new Vector3(0f, -1f, phi));
        points.Add(new Vector3(0f, 1f, -phi));
        points.Add(new Vector3(0f, -1f, -phi));

        points.Add(new Vector3(1f, phi, 0f));
        points.Add(new Vector3(-1f, phi, 0f));
        points.Add(new Vector3(1f, -phi, 0f));
        points.Add(new Vector3(-1f, -phi, 0f));

        points.Add(new Vector3(phi, 0f, 1f));
        points.Add(new Vector3(-phi, 0f, 1f));
        points.Add(new Vector3(phi, 0f, -1f));
        points.Add(new Vector3(-phi, 0f, -1f));

        CreateBasicShape();

    }

    private void UpdateInfo() {
        verticesText.text = "POINTS = " + vertexGameObjects.Count;
        linesText.text = "EDGES = " + lineGameObjects.Count;
    }

    private void SetButtonStates(bool reset, bool subdivide, bool toSphere) {
        resetButton.interactable = reset;
        subdivideButton.interactable = subdivide;
        toSphereButton.interactable = toSphere;
    }

    private void CreateBasicShape() {
        SetButtonStates(false, false, false);
        ClearLines();
        ClearVertices();
        foreach (var p in points) {
            AddNewVertex(p);
        }
        UpdateShortSide();
        CalculateEdges();
        Subdivide();
        subdivideStage = 0;
        SetButtonStates(false, true, false);
        UpdateInfo();
    }

    private void ClearVertices() {
        foreach (var vertex in vertexGameObjects) {
            objectPool.ReturnToPool(vertexPrefab, vertex);
        }
        vertexGameObjects.Clear();
    }

    private void ClearLines() {
        foreach (var line in lineGameObjects) {
            objectPool.ReturnToPool(linePrefab, line);
        }
        lineGameObjects.Clear();
    }

    private void Subdivide() {
        ClearLines();
        foreach (var edge in edges) {
            var go1 = GetVertexGameObjectFromId(edge[0]);
            var go2 = GetVertexGameObjectFromId(edge[1]);
            ConnectWithLine(go1, go2);
        }
    }

    private void AddLevel() {
        var newEdges = new List<List<int>>();
        foreach (var edge in edges) {
            var go1 = GetVertexGameObjectFromId(edge[0]);
            var go2 = GetVertexGameObjectFromId(edge[1]);
            var pos = (go1.transform.position + go2.transform.position) / 2f;
            var newVertex = AddNewVertex(pos);
            var newVertexId = newVertex.GetComponent<Vertex>().Id;
            newEdges.Add(new List<int>{ edge[0], newVertexId });
            newEdges.Add(new List<int>{ newVertexId, edge[1] });
        }
        edges = newEdges;
    }

    private GameObject AddNewVertex(Vector3 pos) {
        var vertex = objectPool.GetFromPool(vertexPrefab);
        objectPool.ApplyPrefabScale(vertexPrefab, vertex);
        vertex.transform.position = pos;
        vertex.GetComponent<Vertex>().Id = vertexGameObjects.Count;
        vertexGameObjects.Add(vertex);
        return vertex;
    }

    private GameObject GetVertexGameObjectFromId(int id) {
        foreach (var go in vertexGameObjects) {
            if(go.GetComponent<Vertex>().Id == id){
                return go;
            }
        }
        return null;
    }

    private void CalculateEdges() {
        edges = new List<List<int>>();
        for (int i = 0; i < vertexGameObjects.Count; i++) {
            var goi = vertexGameObjects[i];
            for (int j = 0; j < vertexGameObjects.Count; j++) {
                var goj = vertexGameObjects[j];
                if (i != j) {
                    var dist = Vector3.Distance(goi.transform.position, goj.transform.position);
                    if (IsShortSide(dist)) {
                        var idi = goi.GetComponent<Vertex>().Id;
                        var idj = goj.GetComponent<Vertex>().Id;
                        var possibleEdge = new List<int> { idi, idj };
                        possibleEdge.Sort();
                        var alreadyExists = false;
                        foreach (var edge in edges) {
                            if (edge.SequenceEqual(possibleEdge)) {
                                alreadyExists = true;
                            }
                        }
                        if (!alreadyExists) {
                            edges.Add(possibleEdge);
                        }
                    }
                }
            }
        }
    }

    private void UpdateShortSide() {
        shortSide = 9999f;
        foreach (var go1 in vertexGameObjects) {
            foreach (var go2 in vertexGameObjects) {
                if (!go1.Equals(go2)) {
                    shortSide = Mathf.Min(shortSide, Vector3.Distance(go1.transform.position, go2.transform.position));
                }
            }
        }
    }

    private bool IsShortSide(float dist) {
        return (dist > shortSide * 0.95f && dist < shortSide * 1.05f);
    }

    private void ConnectWithLine(GameObject go1, GameObject go2) {
        var line = objectPool.GetFromPool(linePrefab);
        var scale = Vector3.one * 0.02f;
        scale.z = Vector3.Distance(go1.transform.position, go2.transform.position);
        line.transform.localScale = scale;
        line.transform.position = (go1.transform.position + go2.transform.position) * 0.5f;
        line.transform.LookAt(go1.transform.position);
        lineGameObjects.Add(line);
    }

    public void OnReset() {
        CreateBasicShape();
    }

    public void OnSubdivide() {
        SetButtonStates(false, false, false);
        AddLevel();
        UpdateShortSide();
        CalculateEdges();
        Subdivide();
        subdivideStage++;
        SetButtonStates(true, subdivideStage < 3, true);
        UpdateInfo();
    }

    public void OnToSphere() {
        var moved = false;
        SetButtonStates(false, false, false);
        ClearLines();
        foreach (var vertex in vertexGameObjects) {
            vertex.transform.DOLocalMove(vertex.transform.localPosition.normalized * 2.0f, 1.4f)
                .SetEase(Ease.OutCirc)
                .SetDelay(1.4f)
                .OnComplete(() => {
                    if (!moved) {
                        Subdivide();
                        moved = true;
                        SetButtonStates(true, false, false);
                    }
                });
        }
    }

    void Update() {
        this.transform.Rotate(0, 25 * Time.deltaTime, 0);
    }
}
