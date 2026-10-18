using UnityEngine;

public class RoomExit : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        GameController controller = GameController.Instance;
        if (controller == null)
        {
            Debug.LogError("RoomExit não encontrou um GameController ativo.", this);
            return;
        }

        controller.LeaveCurrentRoom();
    }
}
