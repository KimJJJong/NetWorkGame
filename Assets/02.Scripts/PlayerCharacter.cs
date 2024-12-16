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

    public enum EState  // 캐릭터 상태
    {
        Idle,
        Run,
        Fire,
        Die
    }

    private int _uid;
    private string _id;
    private ETeam _team;
    private bool _localPlayer;  // 내가 조작중인 캐릭터 인가.

    private int _hp;
    private int _damage;
    private float _speed;
    private Vector3 _destPosition;  // 내캐릭터가 아닌캐릭터일시 패킷을 받아서 가야될 위치

    private Animator _animator;
    private EAnimationType _curAnimation;
    private EState _curState;       // 현재 캐릭터 상태
    private bool _move;
    private float _fireTime;        // 발사하고나서 상태변경까지 남은시간
    private float _curFireCoolTime; // 다음 발사 까지 걸리는 시간

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
            // 위치 보간
            transform.position = Vector3.Lerp(transform.position, _destPosition, Time.deltaTime * _speed);
        }

        UpdateState();

        // 입력받은 방향값으로 이동한다.
        if (_moveDirection != Vector3.zero)
        {
            _rigidbody.MovePosition(transform.position + _moveDirection.normalized * Time.deltaTime * _speed);
            _moveDirection = Vector3.zero;
        }

        // 발사 쿨타임
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

        // 애니메이션 전환 (전환할 상태이름, 트랜지션시간:두애니메이션을 합쳐서 보임)
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