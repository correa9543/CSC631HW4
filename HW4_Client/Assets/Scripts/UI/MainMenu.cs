using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
	private GameObject rootMenuPanel;
	private GameObject hotseatMenuPanel;
	private GameObject networkMenuPanel;

	private GameObject messageBox;
	private TMPro.TextMeshProUGUI messageBoxMsg;

	private TMPro.TextMeshProUGUI player1Name;
	private TMPro.TextMeshProUGUI player2Name;
	private TMPro.TextMeshProUGUI player3Name;

	private GameObject player1Input;
	private GameObject player2Input;
	private GameObject player3Input;

	private TMPro.TextMeshProUGUI playerName;
	private TMPro.TextMeshProUGUI opponent1Name;
	private TMPro.TextMeshProUGUI opponent2Name;

	private GameObject playerInput;
	private GameObject opponent1Input;
	private GameObject opponent2Input;

	private NetworkManager networkManager;
	private MessageQueue msgQueue;

	private string p1Name = "Player 1";
	private string p2Name = "Player 2";
	private string p3Name = "Player 3";

	private bool ready = false;
	private bool op1Ready = false;
	private bool op2Ready = false;

    // Start is called before the first frame update
    void Start()
    {
		rootMenuPanel = GameObject.Find("Root Menu");
		hotseatMenuPanel = GameObject.Find("Hotseat Menu");
		networkMenuPanel = GameObject.Find("Network Menu");

		messageBox = GameObject.Find("Message Box");
		messageBoxMsg = messageBox.transform.Find("Message").gameObject.GetComponent<TMPro.TextMeshProUGUI>();

		player1Name = GameObject.Find("Player1Name").GetComponent<TMPro.TextMeshProUGUI>();
		player2Name = GameObject.Find("Player2Name").GetComponent<TMPro.TextMeshProUGUI>();
		player3Name = GameObject.Find("Player3Name").GetComponent<TMPro.TextMeshProUGUI>();



		player1Input = GameObject.Find("NetPlayer1Input");
		player2Input = GameObject.Find("NetPlayer2Input");
		player3Input = GameObject.Find("NetPlayer3Input");

		networkManager = GameObject.Find("Network Manager").GetComponent<NetworkManager>();
		msgQueue = networkManager.GetComponent<MessageQueue>();

		msgQueue.AddCallback(Constants.SMSG_JOIN, OnResponseJoin);
		msgQueue.AddCallback(Constants.SMSG_LEAVE, OnResponseLeave);
		msgQueue.AddCallback(Constants.SMSG_SETNAME, OnResponseSetName);
		msgQueue.AddCallback(Constants.SMSG_READY, OnResponseReady);

		rootMenuPanel.SetActive(true);
		hotseatMenuPanel.SetActive(false);
		networkMenuPanel.SetActive(false);
		messageBox.SetActive(false);
	}

	#region RootMenu
	public void OnHotseatClick()
	{
		rootMenuPanel.SetActive(false);
		hotseatMenuPanel.SetActive(true);
	}

	public void OnNetworkClick()
	{
		Debug.Log("Send JoinReq");
		bool connected = networkManager.SendJoinRequest();
		if (!connected)
		{
			messageBoxMsg.text = "Unable to connect to server.";
			messageBox.SetActive(true);
		}
	}

	public void OnExitClick()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
	#endregion

	#region HotseatMenu
	public void OnStartClick()
	{
		StartHotseatGame();
	}

	public void OnBackClick()
	{
		rootMenuPanel.SetActive(true);
		hotseatMenuPanel.SetActive(false);
		networkMenuPanel.SetActive(false);
		messageBox.SetActive(false);
	}
	#endregion

	#region NetworkMenu
	public void OnResponseJoin(ExtendedEventArgs eventArgs)
	{
		ResponseJoinEventArgs args = eventArgs as ResponseJoinEventArgs;
		if (args.status == 0)
		{
			if (args.user_id == 1)
			{
				playerName = player1Name;
				playerInput = player1Input;
				opponent1Name = player2Name;
				opponent1Input = player2Input;
				opponent2Name = player3Name;
				opponent2Input = player3Input;
			}
			else if (args.user_id == 2)
			{
				playerName = player2Name;
				playerInput = player2Input;
				opponent1Name = player1Name;
				opponent1Input = player1Input;
				opponent2Input = player3Input;
				opponent2Name = player3Name;
			}
			else if(args.user_id == 3){
				playerName = player3Name;
				playerInput = player3Input;
				opponent1Name = player1Name;
				opponent2Name = player2Name;
				opponent1Input = player1Input;
				opponent2Input = player2Input;
			}
			else
			{
				Debug.Log("ERROR: Invalid user_id in ResponseJoin: " + args.user_id);
				messageBoxMsg.text = "Error joining game. Network returned invalid response.";
				messageBox.SetActive(true);
				return;
			}
			Constants.USER_ID = args.user_id;
			Constants.OP_ID = 3 - args.user_id;
			Constants.OP2_ID = 4 - args.user_id;

			if (args.op_id > 0)
			{
				if (args.op_id == Constants.OP_ID)
				{
					opponent1Name.text = args.op_name;
					op1Ready = args.op_ready;
				}
				else if(args.op_id == Constants.OP2_ID){
					opponent2Name.text = args.op_name;
					op2Ready = args.op_ready;
				}
				else
				{
					Debug.Log("ERROR: Invalid op_id in ResponseJoin: " + args.op_id);
					messageBoxMsg.text = "Error joining game. Network returned invalid response. " + Constants.OP2_ID + " " + args.op_id;
					messageBox.SetActive(true);
					return;
				}
			}
			else
			{
				opponent1Name.text = "Waiting for opponent";
				opponent2Name.text = "Waiting for opponent";
			}

			playerInput.SetActive(true);
			opponent1Name.gameObject.SetActive(true);
			opponent2Name.gameObject.SetActive(true);
			playerName.gameObject.SetActive(false);
			opponent1Input.SetActive(false); 
			opponent2Input.SetActive(false); 


			rootMenuPanel.SetActive(false);
			networkMenuPanel.SetActive(true);
		}
		else
		{
			messageBoxMsg.text = "Server is full.";
			messageBox.SetActive(true);
		}
	}

	public void OnLeave()
	{
		Debug.Log("Send LeaveReq");
		networkManager.SendLeaveRequest();
		rootMenuPanel.SetActive(true);
		networkMenuPanel.SetActive(false);
		ready = false;
	}

	public void OnResponseLeave(ExtendedEventArgs eventArgs)
	{
		ResponseLeaveEventArgs args = eventArgs as ResponseLeaveEventArgs;
		if (args.user_id != Constants.USER_ID)
		{
			if(args.user_id == Constants.OP_ID){
				opponent1Name.text = "Waiting for opponent";
				op1Ready = false;
			}
			else{
				opponent2Name.text = "Waiting for opponent";
				op2Ready = false;
			}
			
		}
	}

	public void OnPlayerNameSet(string name)
	{
		Debug.Log("Send SetNameReq: " + name);
		networkManager.SendSetNameRequest(name);
		if (Constants.USER_ID == 1)
		{
			p1Name = name;
		}
		else if (Constants.USER_ID == 2)
		{
			p2Name = name;
		}
		else{
			p3Name = name;
		}
	}

	public void OnResponseSetName(ExtendedEventArgs eventArgs)
	{
		ResponseSetNameEventArgs args = eventArgs as ResponseSetNameEventArgs;
		if (args.user_id != Constants.USER_ID)
		{
			if(args.user_id == Constants.OP_ID){
				opponent1Name.text = args.name;
			}
			else if(args.user_id == Constants.OP2_ID){
				opponent2Name.text = args.name;
			}
			
			if (args.user_id == 1)
			{
				p1Name = args.name;
			}
			else if (args.user_id == 2)
			{
				p2Name = args.name;
			}
			else{
				p3Name = args.name;
			}
		}
	}

	public void OnReadyClick()
	{
		Debug.Log("Send ReadyReq");
		networkManager.SendReadyRequest();
	}

	public void OnResponseReady(ExtendedEventArgs eventArgs)
	{
		ResponseReadyEventArgs args = eventArgs as ResponseReadyEventArgs;
		if (Constants.USER_ID == -1) // Haven't joined, but got ready message
		{
			if(args.user_id == Constants.OP_ID){
				op1Ready = true;
			}
			if(args.user_id == Constants.OP2_ID){
				op2Ready = true;
			}
		}
		else
		{
			if (args.user_id == Constants.OP_ID)
			{
				op1Ready = true;
			}
			else if(args.user_id == Constants.OP2_ID){
				op2Ready = true;
			}
			else if (args.user_id == Constants.USER_ID)
			{
				ready = true;
			}
			//Another condition checking if args.user_id == Constants.OP2_ID
				//op2Ready = true
			else
			{
				Debug.Log("ERROR: Invalid user_id in ResponseReady: " + args.user_id);
				messageBoxMsg.text = "Error starting game. Network returned invalid response.";
				messageBox.SetActive(true);
				return;
			}
		}

		//Add if the op2Read is true in the condition as well
		if (ready && op1Ready && op2Ready)
		{
			StartNetworkGame();
		}
	}
	#endregion

	public void OnOKClick()
	{
		messageBox.SetActive(false);
	}

	private void StartHotseatGame()
	{
		GameManager gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
		string p1Name = GameObject.Find("HotPlayer1Name").GetComponent<TMPro.TextMeshProUGUI>().text;
		if (p1Name.Length == 1)
		{
			p1Name = "Player 1";
		}
		string p2Name = GameObject.Find("HotPlayer2Name").GetComponent<TMPro.TextMeshProUGUI>().text;
		if (p2Name.Length == 1)
		{
			p2Name = "Player 2";
		}

		string p3Name = GameObject.Find("HotPlayer3Name").GetComponent<TMPro.TextMeshProUGUI>().text;
		if (p2Name.Length == 1)
		{
			p3Name = "Player 3";
		}
		//grab the p2Name like they did above and check with the same condtion.
			//give p3Name a value of "Player 3" if condition is true
		Player player1 = new Player(1, p1Name, new Color(0.9f, 0.1f, 0.1f), true);
		Player player2 = new Player(2, p2Name, new Color(0.2f, 0.2f, 1.0f), true);
		Player player3 = new Player(3, p3Name, new Color(0.0f, 1.0f, 0.0f), true);

		//Add a Player three here like they did for the 2 previous players. Pass in 2, p3Name, new Color(), true

		//pass in player three as a parameter as well
		gameManager.Init(player1, player2, player3);
		SceneManager.LoadScene("Game");
	}

	private void StartNetworkGame()
	{
		GameManager gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
		if (p1Name.Length == 0)
		{
			p1Name = "Player 1";
		}
		if (p2Name.Length == 0)
		{
			p2Name = "Player 2";
		}
		if(p3Name.Length == 0){
			p3Name = "Player 3";
		}
		//check if the p3Name length == 0 and if so set p3Name = "Player 3"


		Player player1 = new Player(1, p1Name, new Color(0.9f, 0.1f, 0.1f), Constants.USER_ID == 1);
		Player player2 = new Player(2, p2Name, new Color(0.2f, 0.2f, 1.0f), Constants.USER_ID == 2);
		Player player3 = new Player(3, p3Name, new Color(0.0f, 1.0f, 0.0f), Constants.USER_ID == 3);

		//create player 3 here as well passing in 3, p3Name, new Color(), Constants.USER_ID == 3);

		//Pass in player 3 as a parameter as well.

		//change init function to add 3 players
		gameManager.Init(player1, player2, player3);
		SceneManager.LoadScene("Game");
	}
}
