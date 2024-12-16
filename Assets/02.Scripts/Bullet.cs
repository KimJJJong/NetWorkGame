using UnityEngine;

public class Bullet : MonoBehaviour
{
    private int _bulletUID;
    private int _ownerUID;
    private Rigidbody _rigidbody;

    public int BulletUID => _bulletUID;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        _rigidbody.MovePosition(transform.position + transform.forward * Time.deltaTime * 20f);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!GameManager.Instance.IsHost)
            return;

        // �Ѿ��̸� ����.
        if (other.CompareTag("Bullet"))
        {
            return;
        }

        PlayerCharacter player = other.GetComponent<PlayerCharacter>();

        if (!player.IsAlive)
            return;

        // �÷��̾ �ƴ϶�� ������ �Ѵ�.
        if (player == null)
        {
            RemoveBullet();
            return;
        }

        PlayerCharacter owner = GameManager.Instance.GetPlayer(_ownerUID);

        // �Ѿ��� �߻��� �÷��̾��� ���̶�� ó�� ���� �ʴ´�.
        if (_ownerUID == player.UID)
            return;

        // ���� ���̸� ó������ �ʴ´�.
        if (player.Team == owner.Team)
            return;

        // �ǰ� ������ ������.
        PacketPlayerDamage packet = new PacketPlayerDamage();
        packet.attackUID = _ownerUID;
        packet.targetUID = player.UID;
        GameManager.Instance.client.Send(packet);

        RemoveBullet();
    }

    private void RemoveBullet()
    {
        if (!GameManager.Instance.IsHost)
            return;

        // �����϶�� ������.
        PacketBulletDestroy packetBullet = new PacketBulletDestroy();
        packetBullet.bulletUID = _bulletUID;
        GameManager.Instance.client.Send(packetBullet);

        // ȣ��Ʈ�� ���� ���� �Ѵ�.
        GameManager.Instance.RemoveBullet(_bulletUID);
    }

    public void Init(int ownerUID, int bulletUID)
    {
        _ownerUID = ownerUID;
        _bulletUID = bulletUID;
        GameManager.Instance.AddBullet(this);
    }
}