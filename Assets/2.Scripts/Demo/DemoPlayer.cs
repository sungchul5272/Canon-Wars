using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class DemoPlayer : NetworkBehaviour
{
    public Camera playerCamera;
    public TextMeshPro nameText;
    public float moveSpeed;

    NetworkVariable<FixedString64Bytes> _networkPlayerName = new("Unknown", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        // 플레이어 아이디 부여
        if (FirebaseManager._instance != null)
        {
            if (IsOwner)
            {
                _networkPlayerName.Value = FirebaseManager._instance.userVO.UserID;
            }
     
            nameText.SetText(_networkPlayerName.Value.ToString());
            _networkPlayerName.OnValueChanged += (previousValue, newValue) =>
            {
                nameText.SetText(newValue.Value);
            };
        }
        else
        {
            Debug.LogWarning("No FirebaseManager.");
        }
    }

    void Update()
    {
        playerCamera.gameObject.SetActive(IsOwner);
        if (!IsOwner)
        {
            return;
        }

        Move();
    }

    void Move()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = moveSpeed * Time.deltaTime * new Vector3(moveX, 0, moveZ);
        transform.Translate(move);
    }
}
