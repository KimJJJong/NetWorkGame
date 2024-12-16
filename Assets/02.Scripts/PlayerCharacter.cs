using UnityEditor.PackageManager;
using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    public enum EAnimationType
    {
        Idle,
        Run,
        Fire,
        Die
    }

    public enum EState  // ĳ���� ����
    {
        Idle,
        Run,
        Fire,
        Die
    }

    private int _uid;
    private string _id;
    private ETeam _team;
    private bool _localPlayer;  // ���� �������� ĳ���� �ΰ�.

    private int _hp;
    private int _damage;
    private float _speed;
    private Vector3 _destPosition;  // ��ĳ���Ͱ� �ƴ�ĳ�����Ͻ� ��Ŷ�� �޾Ƽ� ���ߵ� ��ġ

    private Animator _animator;
    private EAnimationType _curAnimation;
    private EState _curState;       // ���� ĳ���� ����
    private bool _move;
    private float _fireTime;        // �߻��ϰ��� ���º������ �����ð�
    private float _curFireCoolTime; // ���� �߻� ���� �ɸ��� �ð�

    private Rigidbody _rigidbody;
    private Vector3 _moveDirection;

    public event System.Action<EAnimationType> onChangeAnimation;

    public int UID => _uid;
    public ETeam Team => _team;
    public bool IsLocalPlayer => _localPlayer;
    public int Damage => _damage;
    public bool IsAlive => _hp > 0;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Init(int uid, string id, ETeam team, Vector3 position)
    {
        _uid = uid; 
        _id = id; 
        _team = team;
        if (GameManager.Instance.UserUID == _uid)
            _localPlayer = true;

        _hp = 100;
        _damage = 5;
        _speed = 8f;

        _destPosition = position;
        transform.position = position;
    }

    private void Update()
    {
        if (!_localPlayer)
        {
            // ��ġ ����
            transform.position = Vector3.Lerp(transform.position, _destPosition, Time.deltaTime * _speed);
        }

        UpdateState();

        // �Է¹��� ���Ⱚ���� �̵��Ѵ�.
        if (_moveDirection != Vector3.zero)
        {
            _rigidbody.MovePosition(transform.position + _moveDirection.normalized * Time.deltaTime * _speed);
            _moveDirection = Vector3.zero;
        }

        // �߻� ��Ÿ��
        if(_curFireCoolTime > 0f)
        {
            _curFireCoolTime -= Time.deltaTime;
        }
    }

    public void UpdateState()
    {
        if (!_localPlayer)
            return;

        switch (_curState)
        {
            case EState.Idle:
                if (_move)
                    SetState(EState.Run);
                break;
            case EState.Run:
                if (!_move)
                    SetState(EState.Idle);
                break;
            case EState.Fire:
                _fireTime -= Time.deltaTime;
                if (_fireTime <= 0f)
                    SetState(EState.Idle);
                break;
            case EState.Die:
                break;
        }

        _move = false;
    }

    public void Move(Vector3 direction)
    {
        _moveDirection += direction; 
        _move = true;
    }

    public void SetPositionRotation(Vector3 position, float rotation)
    {
        _destPosition = position;
        transform.eulerAngles = new Vector3(0f, rotation, 0f);
    }

    public void FireBullet()
    {
        if (_curFireCoolTime > 0f)
            return;

        PacketPlayerFire packet = new PacketPlayerFire();
        packet.ownerUID = _uid;
        packet.position = transform.position + new Vector3(0f, 0.5f, 0f);
        packet.direction = transform.forward;
        GameManager.Instance.client.Send(packet);

        SetState(PlayerCharacter.EState.Fire);
        _curFireCoolTime =Define.FIRE_COOL_DOWN_TIME;
    }
    public void CreateBullet(Vector3 position, Vector3 direction, int ownerUID, int bulletUID)
    {
        GameObject bulletResource = null;
        if (_team == ETeam.Red)
        {
            bulletResource = Resources.Load("RedBullet") as GameObject;
        }
        else
        {
            bulletResource = Resources.Load("BlueBullet") as GameObject;
        }

        GameObject bullet = Instantiate(bulletResource);
        bullet.transform.position = position;
        bullet.transform.forward = direction.normalized;
        bullet.GetComponent<Bullet>().Init(ownerUID, bulletUID);
    }

    public void SetAnimation(EAnimationType type)
    {
        if (_curAnimation == type)
            return;

        // �ִϸ��̼� ��ȯ (��ȯ�� �����̸�, Ʈ�����ǽð�:�ξִϸ��̼��� ���ļ� ����)
        _animator.CrossFade(type.ToString(), 0.1f);
        _curAnimation = type;

        onChangeAnimation?.Invoke(type);
    }
    
    public void SetState(EState state)
    {
        switch(state)
        {
            case EState.Idle:
                SetAnimation(EAnimationType.Idle);
                break;
            case EState.Run:
                SetAnimation(EAnimationType.Run);
                break;
            case EState.Fire:
                SetAnimation(EAnimationType.Fire);
                _fireTime = 0.5f;
                break;
            case EState.Die:
                SetAnimation(EAnimationType.Die);
                break;
        }

        _curState = state;
    }

    public void ReceiveDamage(int damage)
    {
        _hp -=damage;

        if (!IsAlive)
        {
            SetState(EState.Die);
        }
    }
}