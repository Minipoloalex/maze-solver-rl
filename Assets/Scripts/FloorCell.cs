using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Controls clicks on empty cells (to create new walls)
/// </summary>
[RequireComponent(typeof(Collider))]
public class FloorCell : MonoBehaviour,
                         IPointerEnterHandler,
                         IPointerExitHandler,
                         IPointerClickHandler
{
    [Header("Prefabs")]
    public GameObject ghostPrefab;     // transparent cube shown on hover

    [HideInInspector] public MazeSpawner spawner;
    [HideInInspector] public Vector2Int posId;

    GameObject _ghost;

    void Awake()
    {
        _ghost = Instantiate(ghostPrefab, transform.position, Quaternion.identity, transform);
        _ghost.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData _) =>
        _ghost.SetActive(true);

    public void OnPointerExit(PointerEventData _) =>
        _ghost.SetActive(false);

    public void OnPointerClick(PointerEventData _)
    {
        spawner.SpawnWall(transform.position, posId);

        Destroy(_ghost);
        Destroy(gameObject);           // floor trigger gone: replaced by wall
    }
}
