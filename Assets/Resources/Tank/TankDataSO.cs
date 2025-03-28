using UnityEngine;

[CreateAssetMenu(fileName = "TankData", menuName = "Tank/Tank Data", order = 1)]
public class TankDataSO : ScriptableObject
{
    [Header("�⺻ ��ũ ����")]
    public string _tankName;         // ��ũ �̸�
    public int _hp;              // ü��
    public int _atk;              // ���ݷ�
    public float _speed;             // �̵� �ӵ�

    [Header("��ũ �̹���")]
    public Sprite _tankSprite;       // ��ũ ��ü �̹���

    [Header("�̻��� ���� ����")]
    public Sprite _missileSprite;    // �̻��� �̹���
    public float _missileSpeed;      // �̻��� �ӵ�
    public GameObject _missileEffect; // �̻��� ���� �� ȿ�� ������
}
