using System.Collections.Generic;
using UnityEngine;

public class PlateController : MonoBehaviour
{
    [Header("Dirt")]
    public FrontDirtSpawner spawner;
    public int startDirtCount = 10;
    public bool autoSpawnOnEnable = true;

    [Header("Clean logic")]
    public float visibleAlphaThreshold = 0.08f;
    public int cleanToleranceSpots = 1;
    public float polishFadeTime = 0.15f;


    public bool IsClean { get; private set; }
    public event System.Action<PlateController> OnCleaned;

    List<DirtSpot> _spots = new List<DirtSpot>();


    void Awake() { if (!spawner) spawner = GetComponent<FrontDirtSpawner>(); }

    void OnEnable()
    {
        IsClean = false;
        if (autoSpawnOnEnable) RespawnDirt();
    }

    public void RespawnDirt()
    {
        foreach (var s in _spots) if (s) Destroy(s.gameObject);
        _spots.Clear();

        if (spawner)
        {
            spawner.count = startDirtCount;
            spawner.Spawn();
            _spots.AddRange(GetComponentsInChildren<DirtSpot>(true));
        }
        IsClean = false;
    }

    void Update()
    {
        if (IsClean) return;

        int visible = 0;
        for (int i = 0; i < _spots.Count; i++)
        {
            var s = _spots[i];
            if (s == null) continue;
            if (s.Alpha > visibleAlphaThreshold) visible++;
            if (visible > cleanToleranceSpots) return; // still too dirty
        }

        // threshold check
        IsClean = true;

        // clean remaining dirt after threshold
        for (int i = 0; i < _spots.Count; i++)
            if (_spots[i] != null)
                _spots[i].ForceClean(polishFadeTime);

        OnCleaned?.Invoke(this);
    }
}