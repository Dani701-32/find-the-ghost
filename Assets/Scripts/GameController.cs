using System;
using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private RoomData startingRoom;
    [SerializeField] private RoomData[] challengeRooms;
    [SerializeField, Min(1)] private int attemptsPerRoom = 3;
    [SerializeField] private PlayerController player;

    private static GameController instance;
    private GameObject currentRoomObject;
    private RoomData currentRoom;
    private GhostBehavior looseGhost;
    private int attemptsRemaining;
    private int challengeRoomIndex = -1;
    private bool isStartingRoom;
    private bool ghostWasReleased;
    private bool ghostWasCaught;
    private bool transitioning;
    private bool gameCompleted;

    public static GameController Instance => instance;
    public int AttemptsRemaining => attemptsRemaining;
    public int AttemptsPerRoom => attemptsPerRoom;
    public RoomData CurrentRoom => currentRoom;

    public event Action<int, int> AttemptsChanged;
    public event Action<string> GameLost;
    public event Action GameCompleted;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("Há mais de um GameController ativo na cena.", this);
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            PlayerController scenePlayer = playerObject.GetComponent<PlayerController>();
            if (scenePlayer != null)
                player = scenePlayer;
        }

        if (player == null || !player.gameObject.scene.IsValid())
        {
            Debug.LogError("GameController precisa de um PlayerController em um objeto ativo da cena. Confira também a tag Player.", this);
            enabled = false;
            return;
        }

        if (startingRoom == null)
        {
            Debug.LogError("Configure a sala inicial no GameController.", this);
            enabled = false;
            return;
        }

        attemptsPerRoom = Mathf.Max(1, attemptsPerRoom);
        LoadRoom(startingRoom, true, -1);
    }

    public bool CanInteract(IInteractable target)
    {
        if (target == null || transitioning || gameCompleted || currentRoom == null)
            return false;

        return !ghostWasReleased || ghostWasCaught || target is GhostBehavior;
    }

    public void InteractWithObject(DebuggingObject target)
    {
        if (target == null || !CanInteract(target))
            return;

        if (target.IsHauntedTarget)
        {
            GhostBehavior ghost = target.SpawnGhost();
            if (ghost == null)
                return;

            looseGhost = ghost;
            ghostWasReleased = true;
            ghostWasCaught = false;
            target.RemoveObject();
            return;
        }

        target.BreakObject();
        attemptsRemaining--;
        AttemptsChanged?.Invoke(attemptsRemaining, attemptsPerRoom);
        Debug.Log($"Objeto comum quebrado. Tentativas restantes nesta sala: {attemptsRemaining}.", target);

        if (attemptsRemaining <= 0)
            LoseAndReturnToStart("As três tentativas da sala foram usadas.");
    }

    public bool CaptureGhost(GhostBehavior ghost)
    {
        if (ghost == null || ghost != looseGhost || ghostWasCaught)
            return false;

        looseGhost = null;
        ghostWasCaught = true;
        Debug.Log("Fantasma capturado. A sala pode ser concluída.", this);
        return true;
    }

    public void LeaveCurrentRoom()
    {
        if (transitioning || gameCompleted || currentRoom == null)
            return;

        if (isStartingRoom)
        {
            if (challengeRooms == null || challengeRooms.Length == 0)
            {
                Debug.LogError("Adicione ao menos uma sala de desafio ao GameController.", this);
                return;
            }

            LoadRoom(challengeRooms[0], false, 0);
            return;
        }

        if (currentRoom.IsHaunted && !ghostWasCaught)
        {
            LoseAndReturnToStart("A sala assombrada foi deixada sem capturar o fantasma.");
            return;
        }

        int nextRoomIndex = challengeRoomIndex + 1;
        if (challengeRooms == null || nextRoomIndex >= challengeRooms.Length)
        {
            gameCompleted = true;
            Debug.Log("Todas as salas de desafio foram concluídas.", this);
            GameCompleted?.Invoke();
            return;
        }

        LoadRoom(challengeRooms[nextRoomIndex], false, nextRoomIndex);
    }

    public void RestartGame()
    {
        if (startingRoom == null)
        {
            Debug.LogError("Não é possível reiniciar: a sala inicial não está configurada.", this);
            return;
        }

        gameCompleted = false;
        challengeRoomIndex = -1;
        LoadRoom(startingRoom, true, -1);
    }

    private void LoseAndReturnToStart(string reason)
    {
        Debug.LogWarning($"{reason} Retornando à sala inicial.", this);
        GameLost?.Invoke(reason);
        challengeRoomIndex = -1;
        LoadRoom(startingRoom, true, -1);
    }

    private bool LoadRoom(RoomData roomData, bool loadStartingRoom, int roomIndex)
    {
        if (roomData == null)
        {
            Debug.LogError("Uma sala da sequência está sem RoomData configurado.", this);
            return false;
        }

        if (roomData.RoomPrefab == null)
        {
            Debug.LogError($"A sala '{roomData.name}' não possui um prefab configurado.", roomData);
            return false;
        }

        transitioning = true;
        GameObject newRoomObject = Instantiate(roomData.RoomPrefab, Vector3.zero, Quaternion.identity);
        RoomInstance newRoomInstance = newRoomObject.GetComponent<RoomInstance>();

        if (newRoomInstance == null || !newRoomInstance.Initialize(roomData))
        {
            Debug.LogError($"O prefab da sala '{roomData.name}' precisa de um RoomInstance configurado corretamente.", roomData);
            Destroy(newRoomObject);
            transitioning = false;
            return false;
        }

        if (currentRoomObject != null)
            Destroy(currentRoomObject);
        if (looseGhost != null)
            Destroy(looseGhost.transform.root.gameObject);

        currentRoomObject = newRoomObject;
        currentRoom = roomData;
        isStartingRoom = loadStartingRoom;
        challengeRoomIndex = roomIndex;
        looseGhost = null;
        ghostWasReleased = false;
        ghostWasCaught = false;
        attemptsRemaining = attemptsPerRoom;

        player.TeleportTo(newRoomInstance.PlayerSpawnPoint);
        AttemptsChanged?.Invoke(attemptsRemaining, attemptsPerRoom);
        transitioning = false;
        return true;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}

public interface IInteractable
{
    void Interact();
}
