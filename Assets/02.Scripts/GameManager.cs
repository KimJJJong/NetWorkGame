using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 게임에 대한 전반적인 관리
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject container = new GameObject("GameManager");
                _instance = container.AddComponent<GameManager>();
            }

            return _instance;
        }
    }

    private UIMain _ui;
    private Client _client;
    private Dictionary<int, PlayerCharacter> _playerDic = new Dictionary<int, PlayerCharacter>(); // UID, 플레이어캐릭터
    private PlayerCharacter _localPlayer;   // 내가 조작중인 캐릭터
    private Dictionary<int, Bullet> _bulletDic = new Dictionary<int, Bullet>(); // UID, 총알
    private bool _startGame;                // 게임이 진행 중인가?

    public int UserUID { get; set; }    // 클라이언트 자신의 UID
    public string UserID { get; set; }  // 클라이언트 자신의 ID
    public bool IsHost { get; set; }
    public bool IsGameStarted { get; set; }
    public Client client => _client;

    private void Start()
    {
        _ui = FindObjectOfType<UIMain>();
        _client = FindObjectOfType<Client>();
    }

    private void Update()
    {
        UpdateInput();
        UpdateCheckGameEnd();
    }

    private void UpdateInput()
    {
        if (_localPlayer == null||!_localPlayer.IsAlive)
            return;

        if (Input.GetKey(KeyCode.W))
        {
            _localPlayer.Move(Vector3.forward);
        }
        if (Input.GetKey(KeyCode.S))
        {
            _localPlayer.Move(Vector3.back);
        }
        if (Input.GetKey(KeyCode.A))
        {
            _localPlayer.Move(Vector3.left);
        }
        if (Input.GetKey(KeyCode.D))
        {
            _localPlayer.Move(Vector3.right);
        }

        // 마우스 방향으로 캐릭터를 회전
        Vector3 screenPos = Camera.main.WorldToScreenPoint(_localPlayer.transform.position);
        Vector3 dir = Input.mousePosition - screenPos;
        dir = new Vector3(dir.x, 0f, dir.y);
        _localPlayer.transform.forward = dir.normalized;

        // 총알발사
        if (Input.GetMouseButtonDown(0))
        {
            _localPlayer.FireBullet();

            PacketPlayerFire packet = new PacketPlayerFire();
            packet.ownerUID = UserUID;
            packet.position = _localPlayer.transform.position + new Vector3(0f, 0.5f, 0f);
            packet.direction = _localPlayer.transform.forward;
            _client.Send(packet);

            _localPlayer.SetState(PlayerCharacter.EState.Fire);
        }
    }

    private void UpdateCheckGameEnd()
    {
        if(!_startGame || !IsHost )
            return;

        if (_playerDic.Count <= 1)  // 혼자놀때 
            return;

        int blueAlive = 0;
        int redAlive = 0;
        foreach(var playerKeyValue in _playerDic)
        {
            if(playerKeyValue.Value.Team == ETeam.Blue)
            {
               if(playerKeyValue.Value.IsAlive)
                    blueAlive++;
            }
            else
            {
                if(playerKeyValue.Value.IsAlive)
                    redAlive++;
            }
        }

        if( blueAlive == 0 || redAlive == 0)
        {
            // 게임종료
            Host host = FindObjectOfType<Host>();
            if(host != null)
            {
                PacketGameEnd packet = new PacketGameEnd();
                if(blueAlive > 0 )
                    packet.winTeam=ETeam.Blue;
                else
                    packet.winTeam = ETeam.Red;
                host.SendAll(packet);
            }

            _startGame = false;
        }
    }

    public void GameReady()
    {
        // 게임 시작 준비.
        _ui.SetUIState(UIMain.EUIState.Game);

        PacketGameReadyOk packet = new PacketGameReadyOk();
        _client.Send(packet);
    }

    public void GameStart(PacketGameStart packet)
    {
        for (int i = 0; i < packet.userNum; i++)
        {
            // Resources 폴더에 있는것을 로드한다.
            var resource = Resources.Load("Player_0");
            // 로드된 리소스를 생성한다.
            var inst = Instantiate(resource) as GameObject;
            // GameObject에 있는 PlayerCharacter 컴포넌트를 가져온다.
            var player = inst.GetComponent<PlayerCharacter>();
            player.name = $"Player {packet.startInfos[i].uid}";

            player.Init(packet.startInfos[i].uid, packet.startInfos[i].id, packet.startInfos[i].team, packet.startInfos[i].position);
            _playerDic.Add(packet.startInfos[i].uid, player);

            if (UserUID == packet.startInfos[i].uid)
            {
                _localPlayer = player;
                _localPlayer.onChangeAnimation += OnChangeAnimation;
            }
        }

        _startGame = true;
        StartCoroutine(SendPlayerPosition());
    }

    private IEnumerator SendPlayerPosition()
    {
        float interval = 1f / 20f;
        while (_localPlayer != null)
        {
            PacketPlayerPosition packet = new PacketPlayerPosition();
            packet.uid = UserUID;
            packet.position = _localPlayer.transform.position;
            packet.rotation = _localPlayer.transform.eulerAngles.y;
            _client.Send(packet);

            yield return new WaitForSeconds(interval);
        }
    }

    public PlayerCharacter GetPlayer(int uid)
    {
        // 키값이 존재하는지 확인
        if (!_playerDic.ContainsKey(uid))
            return null;

        return _playerDic[uid];
    }

    private void OnChangeAnimation(PlayerCharacter.EAnimationType animationType)
    {
        PacketPlayerAnimation packet = new PacketPlayerAnimation();
        packet.uid = UserUID;
        packet.animationType = animationType;
        _client.Send(packet);
    }

    public void AddBullet(Bullet bullet)
    {
        _bulletDic.Add(bullet.BulletUID, bullet);
    }

    public void RemoveBullet(int uid)
    {
        if (!_bulletDic.ContainsKey(uid))
            return;

        Destroy(_bulletDic[uid].gameObject);
        _bulletDic.Remove(uid);
    }
}