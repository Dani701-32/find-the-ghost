using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class GameController : MonoBehaviour
{
    [SerializeField] private RoomData startingRoom;
    [SerializeField] private RoomData[] challengeRooms;
    [SerializeField, Min(1)] private int attemptsPerRoom = 3;
    [SerializeField] private PlayerController player;

    [Header("Menu")]
    [SerializeField] private GameObject mainMenuPanel;

    [Header("UI Controllers")]
    [SerializeField] private GameObject roomTitleCard;
    [SerializeField] private TextMeshProUGUI roomTitleText;
    [SerializeField] private GameObject roomAttemptsCard;
    [SerializeField] private TextMeshProUGUI roomAttemptsText;
    [SerializeField, Min(0.01f)] private float roomTitleCardDuration = 3f;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private GameObject victoryScreen;

    private static GameController instance;
    private GameObject currentRoomObject;
    private RoomData currentRoom;
    private CanvasGroup roomTitleCanvasGroup;
    private CanvasGroup roomAttemptsCanvasGroup;
    private Coroutine roomTitleRoutine;
    private Coroutine roomAttemptsRoutine;
    private GhostBehavior looseGhost;
    private int attemptsRemaining;
    private int challengeRoomIndex = -1;
    private bool isStartingRoom;
    private bool ghostWasReleased;
    private bool ghostWasCaught;
    private bool transitioning;
    private bool gameCompleted;
    private bool gameOver;

    public static GameController Instance => instance;

    [Header("Game State")]
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

        if (mainMenuPanel == null)
            Debug.LogError("Configure o mainMenuPanel no GameController.", this);

        if (roomTitleCard == null)
            Debug.LogError("Configure o roomTitleCard no GameController.", this);
        else
        {
            roomTitleCanvasGroup = roomTitleCard.GetComponent<CanvasGroup>();
            if (roomTitleCanvasGroup == null)
                roomTitleCanvasGroup = roomTitleCard.AddComponent<CanvasGroup>();

            roomTitleCanvasGroup.alpha = 0f;
            roomTitleCard.SetActive(false);
        }

        if (roomTitleText == null)
            Debug.LogError("Configure o roomTitleText no GameController.", this);

        if (roomAttemptsCard == null)
            Debug.LogError("Configure o roomAttemptsCard no GameController.", this);
        else
        {
            roomAttemptsCanvasGroup = roomAttemptsCard.GetComponent<CanvasGroup>();
            if (roomAttemptsCanvasGroup == null)
                roomAttemptsCanvasGroup = roomAttemptsCard.AddComponent<CanvasGroup>();

            roomAttemptsCanvasGroup.alpha = 0f;
            roomAttemptsCard.SetActive(false);
        }

        if (roomAttemptsText == null)
            Debug.LogError("Configure o roomAttemptsText no GameController.", this);

        if (gameOverScreen == null)
            Debug.LogError("Configure a gameOverScreen no GameController.", this);
        else
            gameOverScreen.SetActive(false);

        if (victoryScreen == null)
            Debug.LogError("Configure a victoryScreen no GameController.", this);
        else
            victoryScreen.SetActive(false);
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
        ShowMainMenu();
    }

    // ===== MENU =====

    public void StartGame()
    {
        if (startingRoom == null)
        {
            Debug.LogError("Não é possível começar: a sala inicial não está configurada.", this);
            return;
        }

        gameCompleted = false;
        challengeRoomIndex = -1;

        if (!LoadRoom(startingRoom, true, -1))
            return;

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
    }

    public void ShowMainMenu()
    {
        ClearCurrentRoom();

        currentRoom = null;
        isStartingRoom = false;
        challengeRoomIndex = -1;
        gameCompleted = false;
        gameOver = false;
        transitioning = false;
        ghostWasReleased = false;
        ghostWasCaught = false;
        attemptsRemaining = attemptsPerRoom;

        HideRoomAlert();
        HideRoomTitle();

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);

        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        if (player != null)
            player.SetControlsEnabled(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Saindo do jogo.", this);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===== GAMEPLAY =====

    public bool CanInteract(IInteractable target)
    {
        if (target == null || transitioning || gameCompleted || gameOver || currentRoom == null)
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
            ShowGameOver("As tentativas da sala foram usadas.");
        else
            ShowAttemptFailedMessage();
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
            ShowVictory();
            GameCompleted?.Invoke();
            return;
        }

        LoadRoom(challengeRooms[nextRoomIndex], false, nextRoomIndex);
    }

    public void RestartGame()
    {
        StartGame();
    }

    private void LoseAndReturnToStart(string reason)
    {
        Debug.LogWarning($"{reason} Retornando à sala inicial.", this);
        GameLost?.Invoke(reason);
        challengeRoomIndex = -1;
        if (LoadRoom(startingRoom, true, -1))
            ShowRoomAlert($"{startingRoom.RoomName}\nYou returned to the starting room.");
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

        gameOver = false;
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

        HideRoomAlert();

        player.TeleportTo(newRoomInstance.PlayerSpawnPoint);
        AttemptsChanged?.Invoke(attemptsRemaining, attemptsPerRoom);
        transitioning = false;

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);

        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        player.SetControlsEnabled(true);
        ShowRoomTitle(roomData);
        return true;
    }

    private void ClearCurrentRoom()
    {
        if (currentRoomObject != null)
            Destroy(currentRoomObject);

        if (looseGhost != null)
            Destroy(looseGhost.transform.root.gameObject);

        currentRoomObject = null;
        looseGhost = null;
    }

    private void ShowGameOver(string reason)
    {
        if (gameOver)
            return;

        gameOver = true;
        Debug.LogWarning(reason, this);
        GameLost?.Invoke(reason);

        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);

        if (player != null)
            player.SetControlsEnabled(false);
    }

    private void ShowVictory()
    {
        if (player != null)
            player.SetControlsEnabled(false);

        if (victoryScreen != null)
            victoryScreen.SetActive(true);
    }

    private void ShowAttemptFailedMessage()
    {
        if (roomAttemptsCard == null || roomAttemptsText == null || currentRoom == null)
            return;

        string remainingAttempts = $"{attemptsRemaining} {(attemptsRemaining == 1 ? "attempt" : "attempts")} remaining.";
        ShowRoomAlert($"{currentRoom.RoomName}\nAttempt failed. {remainingAttempts}");
    }

    private void ShowRoomAlert(string message)
    {
        if (roomAttemptsCard == null || roomAttemptsText == null || roomAttemptsCanvasGroup == null)
            return;

        if (roomAttemptsRoutine != null)
            StopCoroutine(roomAttemptsRoutine);

        roomAttemptsText.text = message;
        roomAttemptsCanvasGroup.alpha = 0f;
        roomAttemptsCard.SetActive(true);
        roomAttemptsRoutine = StartCoroutine(ShowRoomAlertRoutine());
    }

    private IEnumerator ShowRoomAlertRoutine()
    {
        const float totalDuration = 2f;
        const float fadeDuration = totalDuration * 0.25f;
        const float visibleDuration = totalDuration - (fadeDuration * 2f);

        yield return FadeCanvasGroup(roomAttemptsCanvasGroup, 0f, 1f, fadeDuration);
        yield return new WaitForSecondsRealtime(visibleDuration);
        yield return FadeCanvasGroup(roomAttemptsCanvasGroup, 1f, 0f, fadeDuration);

        roomAttemptsCanvasGroup.alpha = 0f;
        roomAttemptsCard.SetActive(false);
        roomAttemptsRoutine = null;
    }

    private void HideRoomAlert()
    {
        if (roomAttemptsRoutine != null)
        {
            StopCoroutine(roomAttemptsRoutine);
            roomAttemptsRoutine = null;
        }

        if (roomAttemptsCanvasGroup != null)
            roomAttemptsCanvasGroup.alpha = 0f;

        if (roomAttemptsCard != null)
            roomAttemptsCard.SetActive(false);
    }

    private void ShowRoomTitle(RoomData roomData)
    {
        if (roomTitleCard == null || roomTitleText == null || roomTitleCanvasGroup == null)
            return;

        if (roomTitleRoutine != null)
            StopCoroutine(roomTitleRoutine);

        roomTitleText.text = roomData.RoomName;
        roomTitleRoutine = StartCoroutine(ShowRoomTitleRoutine());
    }

    private IEnumerator ShowRoomTitleRoutine()
    {
        roomTitleCanvasGroup.alpha = 0f;
        roomTitleCard.SetActive(true);

        float totalDuration = Mathf.Max(0.01f, roomTitleCardDuration);
        float fadeDuration = totalDuration * 0.25f;
        float visibleDuration = totalDuration - (fadeDuration * 2f);

        yield return FadeRoomTitle(0f, 1f, fadeDuration);
        yield return new WaitForSecondsRealtime(visibleDuration);
        yield return FadeRoomTitle(1f, 0f, fadeDuration);

        roomTitleCanvasGroup.alpha = 0f;
        roomTitleCard.SetActive(false);
        roomTitleRoutine = null;
    }

    private void HideRoomTitle()
    {
        if (roomTitleRoutine != null)
        {
            StopCoroutine(roomTitleRoutine);
            roomTitleRoutine = null;
        }

        if (roomTitleCanvasGroup != null)
            roomTitleCanvasGroup.alpha = 0f;

        if (roomTitleCard != null)
            roomTitleCard.SetActive(false);
    }

    private IEnumerator FadeRoomTitle(float startAlpha, float endAlpha, float duration)
    {
        yield return FadeCanvasGroup(roomTitleCanvasGroup, startAlpha, endAlpha, duration);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    private void OnDestroy()
    {
        if (roomTitleRoutine != null)
            StopCoroutine(roomTitleRoutine);
        if (roomAttemptsRoutine != null)
            StopCoroutine(roomAttemptsRoutine);

        if (instance == this)
            instance = null;
    }
}

public interface IInteractable
{
    void Interact();
}