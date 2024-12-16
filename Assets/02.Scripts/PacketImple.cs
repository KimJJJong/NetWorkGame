// SC : 서버->클라, CS : 클라->서버, REL : 릴레이
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEditor.Build.Player;

public enum EProtocolID
{
    SC_REQ_USERINFO,
    CS_ANS_USERINFO,
    SC_ANS_USERLIST,
    CS_REQ_CHANGE_TEAM,
    REL_GAME_READY,
    CS_GAME_READY_OK,
    SC_GAME_START,
    REL_PLAYER_POSITION,
    REL_PLAYER_FIRE,
    REL_PLAYER_ANIMATION,
    REL_PLAYER_DAMAGE,
    REL_BULLET_DESTROY,
    SC_GAME_END,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketReqUserInfo : Packet
{
    public int uid;
    public ETeam team;

    public PacketReqUserInfo()
        : base((short)EProtocolID.SC_REQ_USERINFO   )
    {
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketAnsUserInfo : Packet
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
    public string id;
    public bool host;

    public PacketAnsUserInfo()
        : base((short)EProtocolID.CS_ANS_USERINFO)
    {
    }
}

// 마샬링으로 배열에 들어가는 요소는 struct로 해야 문제가 안생긴다. 
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UserInfo
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
    public int uid;
    public string id;
    public ETeam team;
    public bool host;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketAnsUserList : Packet
{
    public int userNum;
    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 20)]
    public UserInfo[] userInfos = new UserInfo[20];
    public PacketAnsUserList()
        : base ((short)EProtocolID.SC_ANS_USERLIST)
    {
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketReqChangeTeam : Packet
{
    public ETeam team;
    public PacketReqChangeTeam()
        : base ((short)EProtocolID.CS_REQ_CHANGE_TEAM)
    {
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketGameReady : Packet
{
    public PacketGameReady()
        : base ((short)EProtocolID.REL_GAME_READY)
    {
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketGameReadyOk : Packet
{
    public PacketGameReadyOk()
        : base((short)EProtocolID.CS_GAME_READY_OK)
    {
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GameStartInfo
{
    public int uid;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
    public string id;
    public ETeam team;
    public Vector3 position;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketGameStart : Packet
{
    public int userNum;
    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 20)]
    public GameStartInfo[] startInfos = new GameStartInfo[20];

    public PacketGameStart()
        : base((short)EProtocolID.SC_GAME_START)
    {
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketPlayerPosition : Packet
{
    public int uid;         // 서버에서 uid를 확인하고 대입한다.
    public Vector3 position;
    public float rotation;

    public PacketPlayerPosition()
        : base((short)EProtocolID.REL_PLAYER_POSITION)
    {
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketPlayerFire : Packet
{
    public int ownerUID;
    public int bulletUID;
    public Vector3 position;
    public Vector3 direction;

    public PacketPlayerFire()
        : base((short)EProtocolID.REL_PLAYER_FIRE)
    {
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketPlayerAnimation : Packet
{
    public int uid;
    public PlayerCharacter.EAnimationType animationType;

    public PacketPlayerAnimation()
        : base((short)EProtocolID.REL_PLAYER_ANIMATION)
    {
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketPlayerDamage : Packet
{
    public int attackUID;       // 때린 플레이어
    public int targetUID;       // 맞은 플레이어
    public PacketPlayerDamage()
        : base ((short)EProtocolID.REL_PLAYER_DAMAGE)
    {
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketBulletDestroy : Packet
{
    public int bulletUID;
    public PacketBulletDestroy()
        : base ((short)EProtocolID.REL_BULLET_DESTROY)
    {
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class PacketGameEnd :Packet
{
    public ETeam winTeam;

    public PacketGameEnd()
        : base((short)EProtocolID.SC_GAME_END)
    {

    }
}
