using UnityEngine;
using System.Collections.Generic;

public class Host : MonoBehaviour
{
    private NetServer _server = new NetServer();
    private List<UserPeer> _userList = new List<UserPeer>();
    private int _curUID;

    public void StartHost()
    {
        MainThreadDispatcher.Instance.Init();
        PacketMessageDispatcher.Instance.Init();
        _server.onClientConnected += OnClientConnected;
        _server.Start(10);
        GameManager.Instance.IsHost = true;

        FindObjectOfType<Client>().StartClient("127.0.0.1");
    }
    
    private void OnClientConnected(UserToken token)
    {
        Debug.Log("유저 접속함");

        // 게임 진행중이라면 접속을 끊는다.
        if (GameManager.Instance.IsGameStarted)
        {
            token.Close();
            return;
        }

        UserPeer user = new UserPeer(token, _curUID, this);
        // 팀지정
        var redList = _userList.FindAll(item => item.Team == ETeam.Red);
        var blueList = _userList.FindAll(item => item.Team == ETeam.Blue);
        if (redList.Count > blueList.Count)
        {
            user.Team = ETeam.Blue;
        }
        else
        {
            user.Team = ETeam.Red;
        }

        _userList.Add(user);
        token.onSessionClosed += OnClosed;

        PacketReqUserInfo packet = new PacketReqUserInfo();
        packet.uid = _curUID;
        packet.team = user.Team;

        user.Send(packet);

        _curUID++;
    }

    private void OnClosed(UserToken token)
    {
        _userList.Remove(token.Peer as UserPeer);
    }

    public void SendAll(Packet packet)
    {
        foreach (UserPeer user in _userList)
        {
            user.Send(packet);
        }
    }

    public void SendAll(Packet packet, UserPeer except)
    {
        foreach (UserPeer user in _userList)
        {
            if (user == except)
                continue;
            user.Send(packet);
        }
    }

    public void SendUserList()
    {
        PacketAnsUserList sendPacket = new PacketAnsUserList();
        sendPacket.userNum = _userList.Count;
        for (int i = 0; i < _userList.Count; i++)
        {
            sendPacket.userInfos[i] = new UserInfo();
            sendPacket.userInfos[i].id = _userList[i].ID;
            sendPacket.userInfos[i].uid = _userList[i].UID;
            sendPacket.userInfos[i].team = _userList[i].Team;
            sendPacket.userInfos[i].host = _userList[i].IsHost;
        }
        SendAll(sendPacket);
    }

    public void CheckGameReady()
    {
        for (int i = 0; i < _userList.Count; i++)
        {
            if (!_userList[i].GameReady)
                return;
        }
        // 모두 GameReady가 true 일때
        GameManager.Instance.IsGameStarted = true;

        Vector3 redPosition = new Vector3(0f, 0f, -Define.START_POSITION_OFFSET);
        Vector3 bluePosition = new Vector3(0f, 0f, Define.START_POSITION_OFFSET);
        int redCount = 0;
        int blueCount = 0;

        // 게임 시작을 보낸다.
        PacketGameStart packet = new PacketGameStart();
        packet.userNum = _userList.Count;
        for (int i = 0; i < _userList.Count; i++)
        {
            packet.startInfos[i] = new GameStartInfo();
            packet.startInfos[i].uid = _userList[i].UID;
            packet.startInfos[i].id = _userList[i].ID;
            packet.startInfos[i].team = _userList[i].Team;
            if (_userList[i].Team == ETeam.Red)
            {
                packet.startInfos[i].position = redPosition;
                if (redCount % 2 == 0)
                {
                    redPosition = new Vector3(Mathf.Abs(redPosition.x) + Define.START_DISTANCE_OFFSET, redPosition.y, redPosition.z);
                }
                else
                {
                    redPosition = new Vector3(-redPosition.x, redPosition.y, redPosition.z);
                }
                redCount++;
            }
            else
            {
                packet.startInfos[i].position = bluePosition;
                if (blueCount % 2 == 0)
                {
                    bluePosition = new Vector3(Mathf.Abs(bluePosition.x) + 1f, bluePosition.y, bluePosition.z);
                }
                else
                {
                    bluePosition = new Vector3(-bluePosition.x, bluePosition.y, bluePosition.z);
                }
                blueCount++;
            }
        }

        SendAll(packet);
    }
}
