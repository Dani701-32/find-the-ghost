using UnityEngine;

public class RoomInstance : MonoBehaviour
{
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private RoomExit roomExit;

    public Transform PlayerSpawnPoint => playerSpawnPoint;

    public bool Initialize(RoomData roomData)
    {
        if (playerSpawnPoint == null)
        {
            Debug.LogError($"RoomInstance em '{name}' precisa de um ponto de spawn do jogador.", this);
            return false;
        }

        if (roomExit == null)
            roomExit = GetComponentInChildren<RoomExit>(true);

        if (roomExit == null)
        {
            Debug.LogError($"A sala '{name}' precisa de um RoomExit com collider Trigger.", this);
            return false;
        }

        Collider exitCollider = roomExit.GetComponent<Collider>();
        if (exitCollider == null || !exitCollider.isTrigger)
        {
            Debug.LogError($"O RoomExit da sala '{name}' precisa de um Collider com Is Trigger habilitado.", roomExit);
            return false;
        }

        DebuggingObject[] objects = GetComponentsInChildren<DebuggingObject>(true);
        int hauntedTargetCount = 0;

        foreach (DebuggingObject interactiveObject in objects)
        {
            if (interactiveObject.IsHauntedTarget)
                hauntedTargetCount++;
        }

        if (roomData.IsHaunted && hauntedTargetCount != 1)
        {
            Debug.LogError($"A sala assombrada '{roomData.name}' precisa ter exatamente um DebuggingObject marcado como Haunted Target; encontrados: {hauntedTargetCount}.", this);
            return false;
        }

        foreach (DebuggingObject interactiveObject in objects)
            interactiveObject.ConfigureAsHaunted(roomData.IsHaunted && interactiveObject.IsHauntedTarget);

        return true;
    }
}
