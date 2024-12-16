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

        // 총알이면 리턴.
        if (other.CompareTag("Bullet"))
        {
            return;
        }

        PlayerCharacter player = other.GetComponent<PlayerCharacter>();

        if (!player.IsAlive)
            return;

        // 플레이어가 아니라면 삭제만 한다.
        if (player == null)
        {
            RemoveBullet();
            return;
        }

        PlayerCharacter owner = GameManager.Instance.GetPlayer(_ownerUID);

        // 총알이 발사한 플레이어의 것이라면 처리 하지 않는다.
        if (_ownerUID == player.UID)
            return;

        // 같은 팀이면 처리하지 않는다.
        if (player.Team == owner.Team)
            return;

        // 피격 정보를 보낸다.
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

        // 삭제하라고 보낸다.
        PacketBulletDestroy packetBullet = new PacketBulletDestroy();
        packetBullet.bulletUID = _bulletUID;
        GameManager.Instance.client.Send(packetBullet);

        // 호스트는 먼저 삭제 한다.
        GameManager.Instance.RemoveBullet(_bulletUID);
    }

    public void Init(int ownerUID, int bulletUID)
    {
        _ownerUID = ownerUID;
        _bulletUID = bulletUID;
        GameManager.Instance.AddBullet(this);
    }
}