using UnityEngine;

[CreateAssetMenu(fileName = "NewRoom", menuName = "Find The Ghost/Room")]
public class RoomData : ScriptableObject
{
    [SerializeField] private bool isHaunted;
    [SerializeField] private GameObject roomPrefab;
    [SerializeField] private string roomName;

    public bool IsHaunted => isHaunted;
    public GameObject RoomPrefab => roomPrefab;
    public string RoomName => roomName;
}
