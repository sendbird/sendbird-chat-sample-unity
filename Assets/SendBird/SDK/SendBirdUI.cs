using UnityEngine;
using UnityEngine.UI;

using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using SendBird;
using SendBird.SBJson;

public class SendBirdUI : MonoBehaviour
{
	public GameObject uiThemePrefab;

	SendBirdTheme uiTheme;

	GameObject uiPanel;

	SendBirdClient.ChannelHandler openChannelHandler;

	#region Menu

	Dictionary<string, OpenChannel> enteredChannels = new Dictionary<string, OpenChannel>();
	public static string API_TOKEN = null;

	GameObject menuPanel;
	Button btnConnect;
	Button btnOpenChannelList;
	Button btnStartGroupChannel;
	Button btnGroupChannelList;
	InputField inputUserName;

	public GameObject messageListItemPrefab;
	List<UnityEngine.Object> btnMessage = new List<UnityEngine.Object>();
	[HideInInspector]
	public long messageId;
	[HideInInspector]
	public string editMessage;

	#endregion

	#region OpenChannel

	private OpenChannelListQuery mChannelListQuery;
	GameObject openChannelPanel;

	enum TAB_MODE
	{
		CHANNEL,
		CLAN
	}

	;

	TAB_MODE tabMode;

	string selectedChannelUrl = "";
	float lastTextPositionY;
	bool autoScroll = true;

	Text txtOpenChannelTitle;
	Text txtOpenChannelContent;
	InputField inputOpenChannel;
	Button btnOpenChannelSend;
	Button btnOpenChannelClose;

	#endregion

	#region OpenChannelList

	public GameObject channelListItemPrefab;
	ArrayList btnChannels = new ArrayList();

	GameObject openChannelListPanel;
	Button btnOpenChannelListClose;
	GameObject channelGridPannel;

	#endregion

	#region GroupChannel

	GameObject groupChannelPanel;

	Text txtGroupChannelTitle;
	Text txtGroupChannelContent;
	InputField inputGroupChannel;
	Button btnGroupChannelSend;
	Button btnGroupChannelClose;
	Button btnGroupChannelLeave;
	GameObject groupScrollArea;

	//sskim
	Text txtGroupChannelContent2;
	GameObject editableScrollArea;
	GameObject editableGridPannel;
	Scrollbar groupChannelScrollbar2;
	GameObject editPopup;

	#endregion

	#region UserList

	GameObject userListPanel;

	private List<string> mUserList = new List<string>();
	private UserListQuery mUserListQuery;

	public GameObject userListItemPrefab;
	List<UnityEngine.Object> btnUsers = new List<UnityEngine.Object>();

	Button btnUserListClose;
	Button btnInvite;
	GameObject userListGridPanel;

	#endregion

	#region GroupChannelList

	private GroupChannelListQuery mGroupChannelListQuery;

	ArrayList btnGroupChannels = new ArrayList();

	GameObject groupChannelListPanel;
	Button btnGroupChannelListClose;
	GameObject groupChannelListGridPanel;

	#endregion

	#region common

	BaseChannel currentChannel;

	Scrollbar openChannelScrollbar;
	Scrollbar groupChannelScrollbar;
	string nickname;
	string userId;

	#endregion

	void FixedUpdate()
	{
		if (autoScroll)
		{
			ScrollToBottom();
		}
	}

	void Start()
	{
		InitComponents();

		SendBirdClient.ChannelHandler channelHandler = new SendBirdClient.ChannelHandler();
		channelHandler.OnMessageReceived = (BaseChannel channel, BaseMessage message) => {

			// Draw new messages if user is on the channel.
			if (currentChannel.Url == channel.Url)
			{
				if (message is UserMessage)
				{
					if (channel.IsOpenChannel())
						txtOpenChannelContent.text = txtOpenChannelContent.text + (UserMessageRichText((UserMessage)message) + "\n");
					else
					{
						GameObject objMessage = Instantiate(messageListItemPrefab, editableGridPannel.transform) as GameObject;

						ChannelMessage msg = objMessage.GetComponent<ChannelMessage>();
						if (SendBirdClient.CurrentUser.UserId != msg.userId) objMessage.GetComponent<Button>().enabled = false;
						msg.message = (UserMessageRichText((UserMessage)message));
						msg.messageId = message.MessageId;
						msg.messageText.text = (UserMessageRichText((UserMessage)message));

						btnMessage.Add(objMessage);
					}
				}
				else if (message is FileMessage)
				{
					if (channel.IsOpenChannel())
						txtOpenChannelContent.text = txtOpenChannelContent.text + (FileMessageRichText((FileMessage)message) + "\n");
					else
						txtGroupChannelContent.text = txtGroupChannelContent.text + (FileMessageRichText((FileMessage)message) + "\n");
				}
				else if (message is AdminMessage)
				{
					GameObject objMessage = Instantiate(messageListItemPrefab, editableGridPannel.transform) as GameObject;

					ChannelMessage msg = objMessage.GetComponent<ChannelMessage>();
					if (SendBirdClient.CurrentUser.UserId != msg.userId) objMessage.GetComponent<Button>().enabled = false;
					msg.message = (AdminMessageRichText((AdminMessage)message));
					msg.messageId = message.MessageId;
					msg.messageText.text = (AdminMessageRichText((AdminMessage)message));

					btnMessage.Add(objMessage);
				}

				ScrollToBottom();
			}

		};

		channelHandler.OnUserReceivedInvitation = (GroupChannel channel, User inviter, List<User> invitees) =>
		{
			for (int i = 0; i < invitees.Count; i++)
			{
				if (invitees[i].UserId == SendBirdClient.CurrentUser.UserId)
				{
					// to do
				}
			}
		};

		channelHandler.OnMessageUpdated = (BaseChannel channel, BaseMessage message) =>
		{
			if (currentChannel.Url == channel.Url)
			{
				if (channel.IsGroupChannel())
				{
					for (int i = 0; i < btnMessage.Count; i++)
					{
						GameObject objMessage = btnMessage[i] as GameObject;
						ChannelMessage msg = objMessage.GetComponent<ChannelMessage>();
						if (message is UserMessage)
						{
							if (msg.messageId == message.MessageId)
							{
								if (SendBirdClient.CurrentUser.UserId != msg.userId) objMessage.GetComponent<Button>().enabled = false;
								msg.message = ((UserMessage)message).Message;
								msg.messageId = message.MessageId;
								msg.messageText.text = (UserMessageRichText((UserMessage)message));
							}
						}
						else if (message is AdminMessage)
						{
							if (msg.messageId == message.MessageId)
							{
								objMessage.GetComponent<Button>().enabled = false;
								msg.message = ((AdminMessage)message).Message;
								msg.messageId = message.MessageId;
								msg.messageText.text = (AdminMessageRichText((AdminMessage)message));
							}
						}
					}
				}
				else
				{
					LoadOpenChannelChatHistory();
				}
			}
		};

		SendBirdClient.AddChannelHandler("default", channelHandler);
	}

	void ResetOpenChannelContent()
	{
		txtOpenChannelContent.text = "";
		inputOpenChannel.text = "";
		lastTextPositionY = 0;
		autoScroll = true;
	}

	void LoadOpenChannelChatHistory()
	{
		PreviousMessageListQuery query = currentChannel.CreatePreviousMessageListQuery();

		ResetOpenChannelContent();
		query.Load(15, false, (List<BaseMessage> queryResult, SendBirdException e) => {
			if (e != null)
			{
				Debug.Log(e.Code + ": " + e.Message);
				return;
			}

			foreach (BaseMessage message in queryResult)
			{
				if (message is UserMessage)
				{
					txtOpenChannelContent.text = txtOpenChannelContent.text + (UserMessageRichText((UserMessage)message) + "\n");
				}
				else if (message is FileMessage)
				{
					txtOpenChannelContent.text = txtOpenChannelContent.text + (FileMessageRichText((FileMessage)message) + "\n");
				}
				else if (message is AdminMessage)
				{
					txtOpenChannelContent.text = txtOpenChannelContent.text + (AdminMessageRichText((AdminMessage)message) + "\n");
				}
			}
		});

	}

	void OpenOpenChannelList()
	{
		openChannelListPanel.SetActive(true);

		foreach (UnityEngine.Object btnChannel in btnChannels)
		{
			GameObject.Destroy(btnChannel);
		}
		btnChannels.Clear();


		mChannelListQuery = OpenChannel.CreateOpenChannelListQuery();
		mChannelListQuery.Limit = 50;
		LoadOpenChannels();
	}

	void LoadOpenChannels()
	{
		mChannelListQuery.Next((list, e) => {
			if (e != null)
			{
				Debug.Log(e.Code + ": " + e.Message);
				return;
			}

			foreach (OpenChannel channel in list)
			{
				GameObject btnChannel = Instantiate(channelListItemPrefab) as GameObject;
				btnChannel.GetComponent<Image>().sprite = uiTheme.channelButtonOff;

				if (channel.Url == selectedChannelUrl)
				{
					btnChannel.GetComponent<Image>().overrideSprite = uiTheme.channelButtonOn;
					btnChannel.GetComponentInChildren<Text>().color = uiTheme.channelButtonOnColor;
				}
				else
				{
					btnChannel.GetComponent<Image>().overrideSprite = null;
					btnChannel.GetComponentInChildren<Text>().color = uiTheme.channelButtonOffColor;
				}
				Text text = btnChannel.GetComponentInChildren<Text>();
				text.text = "#" + channel.Name;
				btnChannel.transform.SetParent(channelGridPannel.transform);
				btnChannel.transform.localScale = Vector3.one;
				btnChannels.Add(btnChannel);

				OpenChannel final = channel;
				btnChannel.GetComponent<Button>().onClick.AddListener(() => {
					foreach (KeyValuePair<string, OpenChannel> entry in enteredChannels)
					{
						entry.Value.Exit(null);
					}

					final.Enter((e1) => {
						if (e1 != null)
						{
							Debug.Log(e1.Code + ": " + e1.Message);
							return;
						}

						currentChannel = final;
						LoadOpenChannelChatHistory();
						txtOpenChannelTitle.text = "#" + final.Name;

						enteredChannels[final.Url] = final;

						openChannelListPanel.SetActive(false);
						openChannelPanel.SetActive(true);
					});
				});
			}
		});
	}

	void OpenUserList()
	{
		foreach (UnityEngine.Object btnUser in btnUsers)
		{
			GameObject.Destroy(btnUser);
		}
		btnUsers.Clear();

		userListPanel.SetActive(true);
		mUserListQuery = SendBirdClient.CreateUserListQuery();
		mUserListQuery.Limit = 50;

		LoadUsers();
	}

	public void LoadUsers()
	{
		mUserListQuery.Next((list, e) => {
			if (e != null)
			{
				Debug.Log(e.Code + ": " + e.Message);
				return;
			}

			mUserList.Clear();
			foreach (User user in list)
			{
				GameObject userItem = Instantiate(userListItemPrefab) as GameObject;
				userItem.GetComponent<Image>().sprite = uiTheme.channelButtonOff;

				Text text = userItem.GetComponentInChildren<Text>();
				text.color = uiTheme.chatChannelButtonOffColor;
				text.text = user.Nickname;

				userItem.transform.SetParent(userListGridPanel.transform, false);
				userItem.transform.localScale = Vector3.one;
				btnUsers.Add(userItem);

				var userItemToggle = userItem.GetComponent<Toggle>();

				User finalUser = user;
				userItemToggle.onValueChanged.AddListener((isOn) => {
					if (isOn)
					{
						userItem.GetComponent<Image>().overrideSprite = uiTheme.chatChannelButtonOn;
						userItem.GetComponentInChildren<Text>().color = uiTheme.chatChannelButtonOnColor;
						mUserList.Add(finalUser.UserId);
					}
					else
					{
						userItem.GetComponent<Image>().overrideSprite = uiTheme.chatChannelButtonOff;
						userItem.GetComponentInChildren<Text>().color = uiTheme.chatChannelButtonOffColor;
						mUserList.Remove(finalUser.UserId);
					}
				});
			}

		});
	}

	void ResetGroupChannelContent()
	{
		foreach (UnityEngine.Object obj in btnMessage)
		{
			GameObject.Destroy(obj);
		}
		btnMessage.Clear();

		txtGroupChannelContent.text = "";
		inputGroupChannel.text = "";
		lastTextPositionY = 0;
		autoScroll = true;
	}

	void LoadGroupChannelPreviousChatHistory()
	{
		PreviousMessageListQuery query = currentChannel.CreatePreviousMessageListQuery();
		ResetGroupChannelContent();
		query.Load(15, false, (List<BaseMessage> queryResult, SendBirdException e) => {
		if (e != null)
		{
			Debug.Log(e.Code + ": " + e.Message);
			return;
		}

			ChannelMessage msg = null;

			foreach (BaseMessage message in queryResult)
			{
				GameObject objMessage = Instantiate(messageListItemPrefab, editableGridPannel.transform) as GameObject;
				msg = objMessage.GetComponent<ChannelMessage>();

				if (message is UserMessage)
				{
					msg.message = ((UserMessage)message).Message;
					msg.userId = message.UserId;
					msg.messageId = message.MessageId;
					msg.messageText.text = (UserMessageRichText((UserMessage)message));
					if (SendBirdClient.CurrentUser.UserId == msg.userId) objMessage.GetComponent<Button>().enabled = true;

					btnMessage.Add(objMessage);
				}
				else if (message is FileMessage)
				{
					msg.message = ((FileMessage)message).Name;
					msg.messageId = message.MessageId;
					msg.messageText.text = (FileMessageRichText((FileMessage)message));
					if (SendBirdClient.CurrentUser.UserId == msg.userId) objMessage.GetComponent<Button>().enabled = true;

					btnMessage.Add(objMessage);
				}
				else if (message is AdminMessage)
				{
					msg.message = ((AdminMessage)message).Message;
					msg.messageId = message.MessageId;
					msg.messageText.text = (AdminMessageRichText((AdminMessage)message));
					if (SendBirdClient.CurrentUser.UserId == msg.userId) objMessage.GetComponent<Button>().enabled = true;

					btnMessage.Add(objMessage);
				}

				objMessage.GetComponent<Button>().onClick.AddListener(() =>
				{
					msg = objMessage.GetComponent<ChannelMessage>();
					UpdateMessage(msg);
				});
			}
		});

	}

	void UpdateMessage(ChannelMessage msg)
	{
		messageId = msg.messageId;
		editMessage = msg.message;
		editPopup.SetActive(true);
		
	}

	public void UpdateMessageSend(string sMessage)
	{
		if (currentChannel != null && currentChannel.IsGroupChannel())
		{
			currentChannel.UpdateUserMessage(messageId, sMessage, null, null, (message, e) =>
			{
				if (e != null)
				{
					Debug.Log(e.Code + ": " + e.Message);
					return;
				}
				ScrollToBottom();
			});
		}
	}

	void OpenGroupChannelList()
	{
		foreach (UnityEngine.Object btnGroupChannel in btnGroupChannels)
		{
			GameObject.Destroy(btnGroupChannel);
		}
		btnGroupChannels.Clear();

		groupChannelListPanel.SetActive(true);

		mGroupChannelListQuery = GroupChannel.CreateMyGroupChannelListQuery();
		if (mGroupChannelListQuery == null) return;
		mGroupChannelListQuery.IncludeEmpty = true;
		mGroupChannelListQuery.Limit = 50;
		LoadGroupChannels();
	}

	void LoadGroupChannels()
	{
		mGroupChannelListQuery.Next((list, e) => {
			if (e != null)
			{
				Debug.Log(e.Code + ": " + e.Message);
				return;
			}

			foreach (GroupChannel groupChannel in list)
			{
				GameObject btnGroupChannel = Instantiate(channelListItemPrefab) as GameObject;
				btnGroupChannel.GetComponent<Image>().sprite = uiTheme.channelButtonOff;
				btnGroupChannel.GetComponent<Image>().type = Image.Type.Sliced;

				if (groupChannel.Url == selectedChannelUrl)
				{
					btnGroupChannel.GetComponent<Image>().overrideSprite = uiTheme.channelButtonOn;
					btnGroupChannel.GetComponentInChildren<Text>().color = uiTheme.channelButtonOnColor;
				}
				else
				{
					btnGroupChannel.GetComponent<Image>().overrideSprite = null;
					btnGroupChannel.GetComponentInChildren<Text>().color = uiTheme.channelButtonOffColor;
				}

				btnGroupChannel.transform.SetParent(groupChannelListGridPanel.transform);
				btnGroupChannel.transform.localScale = Vector3.one;

				Text text = btnGroupChannel.GetComponentInChildren<Text>();
				text.text = string.Format("{0}:{1} ({2})", groupChannel.Name, GetDisplayMemberNames(groupChannel.Members), groupChannel.UnreadMessageCount);


				btnGroupChannels.Add(btnGroupChannel);

				GroupChannel final = groupChannel;
				btnGroupChannel.GetComponent<Button>().onClick.AddListener(() => {

					groupChannelListPanel.SetActive(false);
					groupChannelPanel.SetActive(true);

					currentChannel = final;

					editableScrollArea.SetActive(true);
					groupScrollArea.SetActive(false);

					LoadGroupChannelPreviousChatHistory();

				});
			}

		});
	}

	void InitComponents()
	{
		uiPanel = GameObject.Find("SendBirdUnity/UIPanel");
		(Instantiate(uiThemePrefab) as GameObject).transform.parent = uiPanel.transform;

		editPopup = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/EditPopup");
		editPopup.gameObject.SetActive(false);

		uiTheme = GameObject.FindObjectOfType(typeof(SendBirdTheme)) as SendBirdTheme;

		#region MenuPanel

		menuPanel = GameObject.Find("SendBirdUnity/UIPanel/MenuPanel");
		menuPanel.GetComponent<Image>().sprite = uiTheme.channelListFrameBG;

		var txtMenuTitle = GameObject.Find("SendBirdUnity/UIPanel/MenuPanel/TxtTitle").GetComponent<Text>();
		txtMenuTitle.color = uiTheme.titleColor;

		btnConnect = GameObject.Find("SendBirdUnity/UIPanel/MenuPanel/BtnConnect").GetComponent<Button>();
		btnConnect.GetComponent<Image>().sprite = uiTheme.sendButton;
		btnConnect.GetComponent<Image>().type = Image.Type.Sliced;
		btnConnect.onClick.AddListener(() => {
			nickname = inputUserName.text;
			userId = nickname; // Please assign user's unique id.

			if (nickname == null || nickname.Length <= 0)
			{
				return;
			}

			btnConnect.gameObject.SetActive(false);

			btnOpenChannelList.gameObject.SetActive(true);
			btnStartGroupChannel.gameObject.SetActive(true);
			btnGroupChannelList.gameObject.SetActive(true);

			SendBirdClient.Connect(userId, (user, e) => {  // 
				if (e != null)
				{
					Debug.Log(e.Code + ": " + e.Message);
					return;
				}

				SendBirdClient.UpdateCurrentUserInfo(nickname, null, (e1) => {
					if (e1 != null)
					{
						Debug.Log(e.Code + ": " + e.Message);
						return;
					}

				});

			});

		});


		btnOpenChannelList = GameObject.Find("SendBirdUnity/UIPanel/MenuPanel/BtnOpenChannel").GetComponent<Button>();
		btnOpenChannelList.GetComponent<Image>().sprite = uiTheme.sendButton;
		btnOpenChannelList.GetComponent<Image>().type = Image.Type.Sliced;
		btnOpenChannelList.onClick.AddListener(() => {
			menuPanel.SetActive(false);
			OpenOpenChannelList();
		});

		btnStartGroupChannel = GameObject.Find("SendBirdUnity/UIPanel/MenuPanel/BtnStartGroupChannel").GetComponent<Button>();
		btnStartGroupChannel.GetComponent<Image>().sprite = uiTheme.sendButton;
		btnStartGroupChannel.GetComponent<Image>().type = Image.Type.Sliced;
		btnStartGroupChannel.onClick.AddListener(() => {
			menuPanel.SetActive(false);

			OpenUserList();
		});

		btnGroupChannelList = GameObject.Find("SendBirdUnity/UIPanel/MenuPanel/BtnGroupChannel").GetComponent<Button>();
		btnGroupChannelList.GetComponent<Image>().sprite = uiTheme.sendButton;
		btnGroupChannelList.GetComponent<Image>().type = Image.Type.Sliced;
		btnGroupChannelList.onClick.AddListener(() => {
			menuPanel.SetActive(false);

			OpenGroupChannelList();
		});

		inputUserName = GameObject.Find("SendBirdUnity/UIPanel/MenuPanel/InputUserName").GetComponent<InputField>();
		inputUserName.GetComponent<Image>().sprite = uiTheme.inputTextBG;

		#endregion

		#region OpenChannel

		openChannelPanel = GameObject.Find("SendBirdUnity/UIPanel/OpenChannelPanel");
		openChannelPanel.GetComponent<Image>().sprite = uiTheme.chatFrameBG;

		txtOpenChannelContent = GameObject.Find("SendBirdUnity/UIPanel/OpenChannelPanel/ScrollArea/TxtContent").GetComponent<Text>(); // (Text);
		txtOpenChannelContent.color = uiTheme.messageColor;

		txtOpenChannelTitle = GameObject.Find("SendBirdUnity/UIPanel/OpenChannelPanel/TxtTitle").GetComponent<Text>();
		txtOpenChannelTitle.color = uiTheme.titleColor;

		openChannelScrollbar = GameObject.Find("SendBirdUnity/UIPanel/OpenChannelPanel/Scrollbar").GetComponent<Scrollbar>();

		ColorBlock cb = openChannelScrollbar.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		openChannelScrollbar.colors = cb;
		openChannelScrollbar.onValueChanged.AddListener((float value) => {
			if (value <= 0)
			{
				autoScroll = true;
				lastTextPositionY = txtOpenChannelContent.transform.position.y;
				return;
			}

			if (lastTextPositionY - txtOpenChannelContent.transform.position.y >= 100)
			{
				autoScroll = false;
			}

			lastTextPositionY = txtOpenChannelContent.transform.position.y;
		});

		inputOpenChannel = GameObject.Find("SendBirdUnity/UIPanel/OpenChannelPanel/InputOpenChannel").GetComponent<InputField>();
		inputOpenChannel.GetComponent<Image>().sprite = uiTheme.inputTextBG;
		inputOpenChannel.onEndEdit.AddListener((string msg) => {
			SubmitOpenChannel();
		});

		GameObject.Find("SendBirdUnity/UIPanel/OpenChannelPanel/InputOpenChannel/Placeholder").GetComponent<Text>().color = uiTheme.inputTextPlaceholderColor;
		GameObject.Find("SendBirdUnity/UIPanel/OpenChannelPanel/InputOpenChannel/Text").GetComponent<Text>().color = uiTheme.inputTextColor;

		btnOpenChannelSend = GameObject.Find("SendBirdUnity/UIPanel/OpenChannelPanel/BtnOpenChannelSend").GetComponent<Button>();
		btnOpenChannelSend.GetComponent<Image>().sprite = uiTheme.sendButton;
		btnOpenChannelSend.GetComponentInChildren<Text>().color = uiTheme.sendButtonColor;
		btnOpenChannelSend.onClick.AddListener(() => {
			SubmitOpenChannel();
		});

		btnOpenChannelClose = GameObject.Find("SendBirdUnity/UIPanel/OpenChannelPanel/BtnOpenChannelClose").GetComponent<Button>();
		btnOpenChannelClose.GetComponent<Image>().sprite = uiTheme.closeButton;
		btnOpenChannelClose.onClick.AddListener(() => {
			openChannelPanel.SetActive(false);
			menuPanel.SetActive(true);

		});

		#endregion

		#region ChannelList

		openChannelListPanel = GameObject.Find("SendBirdUnity/UIPanel/OpenChannelListPanel");
		openChannelListPanel.GetComponent<Image>().sprite = uiTheme.channelListFrameBG;

		channelGridPannel = GameObject.Find("SendBirdUnity/UIPanel/OpenChannelListPanel/ScrollArea/GridPanel");

		GameObject.Find("SendBirdUnity/UIPanel/OpenChannelListPanel/TxtTitle").GetComponent<Text>().color = uiTheme.titleColor;

		var channelScrollbar = GameObject.Find("SendBirdUnity/UIPanel/OpenChannelListPanel/Scrollbar").GetComponent<Scrollbar>();
		cb = channelScrollbar.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		channelScrollbar.colors = cb;

		channelScrollbar.onValueChanged.AddListener((float value) => {
			if (value <= 0)
			{
				LoadOpenChannels();
			}
		});

		btnOpenChannelListClose = GameObject.Find("SendBirdUnity/UIPanel/OpenChannelListPanel/BtnOpenChannelListClose").GetComponent<Button>();
		btnOpenChannelListClose.GetComponent<Image>().sprite = uiTheme.closeButton;
		btnOpenChannelListClose.onClick.AddListener(() => {
			openChannelListPanel.SetActive(false);
			menuPanel.SetActive(true);

		});

		#endregion

		#region GroupChannel

		groupChannelPanel = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel");
		groupChannelPanel.GetComponent<Image>().sprite = uiTheme.chatFrameBG;

		txtGroupChannelTitle = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/TxtTitle").GetComponent<Text>();
		txtGroupChannelTitle.color = uiTheme.titleColor;

		groupScrollArea = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/ScrollArea");
		editableScrollArea = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/EditableScrollArea");
		editableGridPannel = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/EditableScrollArea/GridPanel");

		btnGroupChannelLeave = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/BtnGroupChannelLeave").GetComponent<Button>();
		btnGroupChannelLeave.GetComponent<Image>().sprite = uiTheme.channelButtonOff;
		btnGroupChannelLeave.onClick.AddListener(() => {
			groupChannelPanel.SetActive(false);
			menuPanel.SetActive(true);

			foreach (UnityEngine.Object obj in btnMessage)
			{
				GameObject.Destroy(obj);
			}
			btnMessage.Clear();

			string channelUrl = currentChannel.Url;
			GroupChannel.GetChannel(channelUrl, new GroupChannel.GroupChannelGetHandler((GroupChannel groupChannel1, SendBirdException e1) =>
			{
				try
				{
					if (e1 != null)
					{

					}

					groupChannel1.Leave(new GroupChannel.GroupChannelLeaveHandler((SendBirdException e2) =>
					{

					}));
					// When you delete a Group Channel you no longer use
					if (groupChannel1.Members.Count == 0)
						groupChannel1.DeleteChannel(channelUrl, new GroupChannel.GroupChannelLeaveHandler((SendBirdException e3) =>
						{

						}));

				}
				catch (Exception z)
				{
					Debug.Log(z);
				}
			}));
		});

		btnGroupChannelClose = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/BtnGroupChannelClose").GetComponent<Button>();
		btnGroupChannelClose.GetComponent<Image>().sprite = uiTheme.closeButton;
		btnGroupChannelClose.onClick.AddListener(() => {
			groupChannelPanel.SetActive(false);
			menuPanel.SetActive(true);

			// sskim add
			string channelUrl = currentChannel.Url;
			GroupChannel.GetChannel(channelUrl, new GroupChannel.GroupChannelGetHandler((GroupChannel groupChannel1, SendBirdException e1) =>
			{
				try
				{
					if (e1 != null)
					{
					}
					groupChannelListPanel.SetActive(false);

				}
				catch (Exception z)
				{
					Debug.Log(z);
				}
			}));
		});

		txtGroupChannelContent = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/ScrollArea/TxtContent").GetComponent<Text>(); // (Text);
		txtGroupChannelContent.color = uiTheme.messageColor;

		//txtGroupChannelContent2 = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/EditableScrollArea/GridPanel/PrevChatListItem/TxtContent").GetComponent<Text>(); // (Text);
		//txtGroupChannelContent2.color = uiTheme.messageColor;

		txtGroupChannelTitle = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/TxtTitle").GetComponent<Text>();
		txtGroupChannelTitle.color = uiTheme.titleColor;

		groupChannelScrollbar = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/ScrollArea/Scrollbar").GetComponent<Scrollbar>();
		ColorBlock cb_groupChannel = groupChannelScrollbar.colors;
		cb_groupChannel.normalColor = uiTheme.scrollBarColor;
		cb_groupChannel.pressedColor = uiTheme.scrollBarColor;
		cb_groupChannel.highlightedColor = uiTheme.scrollBarColor;
		groupChannelScrollbar.colors = cb_groupChannel;
		groupChannelScrollbar.onValueChanged.AddListener((float value) => {
			if (value <= 0)
			{
				autoScroll = true;
				lastTextPositionY = txtGroupChannelContent.transform.position.y;
				return;
			}

			if (lastTextPositionY - txtGroupChannelContent.transform.position.y >= 30)
			{
				autoScroll = false;
			}

			lastTextPositionY = txtGroupChannelContent.transform.position.y;
		});

		inputGroupChannel = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/InputGroupChannel").GetComponent<InputField>();
		inputGroupChannel.GetComponent<Image>().sprite = uiTheme.inputTextBG;
		inputGroupChannel.onEndEdit.AddListener((string msg) => {
			SubmitGroupChannel();
		});

		GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/InputGroupChannel/Placeholder").GetComponent<Text>().color = uiTheme.inputTextPlaceholderColor;
		GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/InputGroupChannel/Text").GetComponent<Text>().color = uiTheme.inputTextColor;

		btnGroupChannelSend = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/BtnGroupChannelSend").GetComponent<Button>();
		btnGroupChannelSend.GetComponent<Image>().sprite = uiTheme.sendButton;
		btnGroupChannelSend.GetComponentInChildren<Text>().color = uiTheme.sendButtonColor;
		btnGroupChannelSend.onClick.AddListener(() => {
			SubmitGroupChannel();
		});

		groupChannelScrollbar2 = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelPanel/EditableScrollArea/Scrollbar2").GetComponent<Scrollbar>();
		cb = groupChannelScrollbar2.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		groupChannelScrollbar2.colors = cb;
		groupChannelScrollbar2.onValueChanged.AddListener((float value) => {
			if (btnMessage.Count > 0)
			{
				if (value <= 0)
				{
					autoScroll = true; 
					lastTextPositionY = ((GameObject)btnMessage[btnMessage.Count-1]).transform.position.y;
					return;
				}

				if (lastTextPositionY - ((GameObject)btnMessage[btnMessage.Count - 1]).transform.position.y >= 30)
				{
					autoScroll = false;
				}

				lastTextPositionY = ((GameObject)btnMessage[btnMessage.Count - 1]).transform.position.y;
			}
		});

		#endregion

		#region UserList

		userListPanel = GameObject.Find("SendBirdUnity/UIPanel/UserListPanel");
		userListPanel.GetComponent<Image>().sprite = uiTheme.channelListFrameBG;

		userListGridPanel = GameObject.Find("SendBirdUnity/UIPanel/UserListPanel/ScrollArea/GridPanel");

		GameObject.Find("SendBirdUnity/UIPanel/UserListPanel/TxtTitle").GetComponent<Text>().color = uiTheme.titleColor;

		var userListScrollBar = GameObject.Find("SendBirdUnity/UIPanel/UserListPanel/Scrollbar").GetComponent<Scrollbar>();
		cb = userListScrollBar.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		userListScrollBar.colors = cb;
		userListScrollBar.onValueChanged.AddListener((float value) => {
			if (value <= 0)
			{
				LoadUsers();
			}
		});

		btnUserListClose = GameObject.Find("SendBirdUnity/UIPanel/UserListPanel/BtnUserListClose").GetComponent<Button>();
		btnUserListClose.GetComponent<Image>().sprite = uiTheme.closeButton;
		btnUserListClose.onClick.AddListener(() => {
			userListPanel.SetActive(false);
			menuPanel.SetActive(true);
		});

		btnInvite = GameObject.Find("SendBirdUnity/UIPanel/UserListPanel/BtnInvite").GetComponent<Button>();
		btnInvite.GetComponent<Image>().sprite = uiTheme.sendButton;
		btnInvite.onClick.AddListener(() => {
			if (mUserList.Count <= 0)
			{
				Debug.Log("Please select one or more.");
				return;
			}

			GroupChannel.CreateChannelWithUserIds(mUserList, false, (channel, e) => {
				if (e != null)
				{
					Debug.Log(e.Code + ": " + e.Message);
					return;
				}

				userListPanel.SetActive(false);

				editableScrollArea.SetActive(true);
				groupScrollArea.SetActive(false);

				groupChannelPanel.SetActive(true);
				currentChannel = channel;
				ResetGroupChannelContent();
				txtGroupChannelTitle.text = channel.Name + ":" + GetDisplayMemberNames(channel.Members);


			});
		});

		#endregion

		#region GroupChannelList

		groupChannelListPanel = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelListPanel");
		groupChannelListPanel.GetComponent<Image>().sprite = uiTheme.channelListFrameBG;

		groupChannelListGridPanel = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelListPanel/ScrollArea/GridPanel");

		GameObject.Find("SendBirdUnity/UIPanel/GroupChannelListPanel/TxtTitle").GetComponent<Text>().color = uiTheme.titleColor;

		var groupChannelListScrollbar = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelListPanel/Scrollbar").GetComponent<Scrollbar>();
		cb = groupChannelListScrollbar.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		groupChannelListScrollbar.colors = cb;
		groupChannelListScrollbar.onValueChanged.AddListener((float value) => {
			if (value <= 0)
			{
				LoadGroupChannels();
			}
		});

		btnGroupChannelClose = GameObject.Find("SendBirdUnity/UIPanel/GroupChannelListPanel/BtnGroupChannelListClose").GetComponent<Button>();
		btnGroupChannelClose.GetComponent<Image>().sprite = uiTheme.closeButton;
		btnGroupChannelClose.onClick.AddListener(() => {
			groupChannelListPanel.SetActive(false);
			if (!groupChannelListPanel.activeSelf)
			{
				menuPanel.SetActive(true);
			}
		});

		#endregion

		uiPanel.SetActive(true);
		menuPanel.SetActive(true);
		openChannelListPanel.SetActive(false);
		openChannelPanel.SetActive(false);
		groupChannelPanel.SetActive(false);
		userListPanel.SetActive(false);
		groupChannelListPanel.SetActive(false);

	}

	string UserMessageRichText(UserMessage message)
	{
		return "<color=#" + SendBirdTheme.ToHex(uiTheme.senderColor) + ">" + message.Sender.Nickname + ": </color>" + message.Message;
	}

	string FileMessageRichText(FileMessage message)
	{
		return "<color=#" + SendBirdTheme.ToHex(uiTheme.senderColor) + ">" + message.Sender.Nickname + ": </color>" + message.Name;
	}

	string AdminMessageRichText(AdminMessage message)
	{
		return "<color=#" + SendBirdTheme.ToHex(uiTheme.systemMessageColor) + ">" + message.Message + "</color>";
	}

	void ScrollToBottom()
	{
		openChannelScrollbar.value = 0;
		groupChannelScrollbar.value = 0;
		groupChannelScrollbar2.value = 0;
	}

	void SubmitOpenChannel()
	{
		if (inputOpenChannel.text.Length > 0)
		{
			if (currentChannel != null && currentChannel.IsOpenChannel())
			{
				currentChannel.SendUserMessage(inputOpenChannel.text, (message, e) => {
					if (e != null)
					{
						Debug.Log(e.Code + ": " + e.Message);
						return;
					}

					txtOpenChannelContent.text = txtOpenChannelContent.text + (UserMessageRichText(message) + "\n");

					ScrollToBottom();
				});
				inputOpenChannel.text = "";
			}
		}

	}

	void OpenLiveUserList()
	{
		foreach (UnityEngine.Object btnUser in btnUsers)
		{
			GameObject.Destroy(btnUser);
		}
		btnUsers.Clear();

		userListPanel.SetActive(true);
		OpenChannel openChannel = (OpenChannel)currentChannel;

		mUserListQuery = openChannel.CreateParticipantListQuery();
		mUserListQuery.Limit = 50;

		LoadUsers();

	}
	void SubmitGroupChannel()
	{
		if (inputGroupChannel.text.Length > 0)
		{
			if (currentChannel != null && currentChannel.IsGroupChannel())
			{
				currentChannel.SendUserMessage(inputGroupChannel.text, (message, e) => {
					if (e != null)
					{
						Debug.Log(e.Code + ": " + e.Message);
						return;
					}
					GameObject objMessage = Instantiate(messageListItemPrefab, editableGridPannel.transform) as GameObject;

					ChannelMessage msg = objMessage.GetComponent<ChannelMessage>();
					msg.userId = SendBirdClient.CurrentUser.UserId;
					msg.message = ((UserMessage)message).Message;
					msg.messageId = message.MessageId;
					msg.messageText.text = UserMessageRichText(message);
					objMessage.GetComponent<Button>().enabled = true;
					btnMessage.Add(objMessage);

					objMessage.GetComponent<Button>().onClick.AddListener(() =>
					{
						msg = objMessage.GetComponent<ChannelMessage>();
						UpdateMessage(msg);
					});

					ScrollToBottom();
				});
				inputGroupChannel.text = "";
			}
		}
	}

	#region helpers

	private string GetDisplayMemberNames(List<Member> members)
	{
		if (members.Count < 2)
		{
			return "No Members";
		}
		else if (members.Count == 2)
		{
			StringBuilder names = new StringBuilder();
			foreach (var member in members)
			{
				if (member.UserId.Equals(SendBirdClient.CurrentUser.UserId))
				{
					continue;
				}

				names.Append(", " + member.Nickname);

			}

			return (string)names.Remove(0, 2).ToString();

		}
		else
		{
			return "Group " + members.Count;
		}
	}

	#endregion
}
