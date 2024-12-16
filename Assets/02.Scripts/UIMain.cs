using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.UI;

public class UIMain : MonoBehaviour
{
    // ����Ƽ���� ����
    public GameObject startUI;
    public GameObject lobbyUI;

    public InputField inputID;
    public InputField inputIP;
    public Button buttonHost;
    public Button buttonClient;

    public Text textRed;
    public Text textBlue;
    public Button buttonRed;
    public Button buttonBlue;
    public Button buttonStart;
    //

    private Client _client;

    public enum EUIState
    {
        Start,
        Lobby,
        Game
    }

    // ���ӿ�����Ʈ�� �������ڸ��� �Ҹ���.
    private void Awake()
    {
        _client = FindObjectOfType<Client>();

        buttonHost.onClick.AddListener(() =>
        {
            string id = inputID.text;
            if (string.IsNullOrEmpty(id))
                return;

            GameManager.Instance.UserID = inputID.text;
            // Host ������Ʈ�� ã�Ƽ� �����Ѵ�.
            FindObjectOfType<Host>().StartHost();
        });

        buttonClient.onClick.AddListener(() =>
        {
            string id = inputID.text;
            if (string.IsNullOrEmpty(id))
                return;

            string ip = inputID.text;
            if (string.IsNullOrEmpty(ip))
                return;

            GameManager.Instance.UserID = inputID.text;
            // Client ������Ʈ�� ã�Ƽ� �����Ѵ�.
            FindObjectOfType<Client>().StartClient(inputIP.text);
        });

        buttonRed.onClick.AddListener(() =>
        {
            SendTeam(ETeam.Red);
        });

        buttonBlue.onClick.AddListener(() =>
        {
            SendTeam(ETeam.Blue);
        });

        buttonStart.onClick.AddListener(() =>
        {
            if (!GameManager.Instance.IsHost)
                return;

            PacketGameReady packet = new PacketGameReady();
            _client.Send(packet);
        });

        SetUIState(EUIState.Start);
    }

    public void SetUIState(EUIState state)
    {
        switch (state)
        {
            case EUIState.Start:
                startUI.SetActive(true);
                lobbyUI.SetActive(false);
                break;
            case EUIState.Lobby:
                startUI.SetActive(false);
                lobbyUI.SetActive(true);
                break;
            case EUIState.Game:
                startUI.SetActive(false);
                lobbyUI.SetActive(false);
                break;
        }
    }

    public void SetLobbyText(string red, string blue)
    {
        textRed.text = red;
        textBlue.text = blue;
    }

    public void SendTeam(ETeam team)
    {
        PacketReqChangeTeam packet = new PacketReqChangeTeam();
        packet.team = team;

        _client.Send(packet);
    }
}