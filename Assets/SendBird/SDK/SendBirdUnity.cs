using UnityEngine;
using UnityEngine.UI;

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using SendBird;
using SendBird.Model;
using SendBird.Query;
using System.Threading;

public class EventProcessor : MonoBehaviour {
	public void QueueEvent(Action action) {
		lock(m_queueLock) {
			m_queuedEvents.Add(action);
		}
	}

	void FixedUpdate() {
		MoveQueuedEventsToExecuting();

		while (m_executingEvents.Count > 0) {
			Action e = m_executingEvents[0];
			m_executingEvents.RemoveAt(0);
			e();
		}
	}

	private void MoveQueuedEventsToExecuting() {
		foreach (var action in m_executingEvents) {
			Debug.Log (action.ToString ());
		}
		lock(m_queueLock) {
			while (m_queuedEvents.Count > 0) {
				Action e = m_queuedEvents[0];
				m_executingEvents.Add(e);
				m_queuedEvents.RemoveAt(0);
			}
		}
	}

	private System.Object m_queueLock = new System.Object();
	private List<Action> m_queuedEvents = new List<Action>();
	private List<Action> m_executingEvents = new List<Action>();
}

public class SendBirdUnity : MonoBehaviour {
	internal static GameObject mGameObject;
	private EventProcessor mEventProcessor;

	public GameObject uiThemePrefab;

	SendBirdTheme uiTheme;

	GameObject uiPanel;

	#region Menu

	GameObject menuPanel;
	Button btnOpenChannel;
	Button btnStartMessaging;
	Button btnJoinMessaging;
	InputField inputUserName;

	#endregion

	#region OpenChat

	private ChannelListQuery mChannelListQuery;
	GameObject openChatPanel;

	enum TAB_MODE {CHANNEL, CLAN};
	TAB_MODE tabMode;

	string selectedChannelUrl = "";
	float lastTextPositionY;
	bool autoScroll = true;

	Text txtOpenChatTitle;
	Text txtOpenChatContent;
	InputField inputOpenChat;
	Button btnOpenChatSend;
	Button btnChannel;
	Button btnClan;
	Button btnOpenChatClose;

	string currentChannelUrl;

	#endregion

	#region ChannelList

	public GameObject channelListItemPrefab;
	ArrayList btnChannels = new ArrayList();

	GameObject channelPanel;
	Button btnChannelClose;
	GameObject channelGridPannel;

	#endregion

	#region Messaging

	GameObject messagingPanel;

	Text txtMessagingTitle;
	Text txtMessagingContent;
	InputField inputMessaging;
	Button btnMessagingSend;
	Button btnMessagingList;
	Button btnMessagingClose;
	MessagingChannel mMessagingChannel;

	internal long mMaxMessageTimestamp = long.MinValue;
	public long GetMaxMessageTimestamp()
	{
		return mMaxMessageTimestamp == long.MinValue ? long.MaxValue : mMaxMessageTimestamp;
	}
	internal long mMinMessageTimestamp = long.MaxValue;
	public long GetMinMessageTimestamp()
	{
		return mMinMessageTimestamp == long.MaxValue ? long.MinValue : mMinMessageTimestamp;
	}

	private void UpdateMessageTimestamp (MessageModel model)
	{
		mMaxMessageTimestamp = mMaxMessageTimestamp < model.messageTimestamp ? model.messageTimestamp : mMaxMessageTimestamp;
		mMinMessageTimestamp = mMinMessageTimestamp > model.messageTimestamp ? model.messageTimestamp : mMinMessageTimestamp;
	}

	public void Clear ()
	{
		mMaxMessageTimestamp = long.MinValue;
		mMinMessageTimestamp = long.MaxValue;
	}

	#endregion

	#region UserList

	GameObject userListPanel;

	private List<string> mUserList = new List<string>();
	private UserListQuery mUserListQuery;

	public GameObject userListItemPrefab;
	List<UnityEngine.Object> btnUsers = new List<UnityEngine.Object> ();

	Button btnUserListClose;
	Button btnInvite;
	GameObject userListGridPanel;

	#endregion

	#region MessagingChannelList

	private List<MessagingChannel> mMessagingChannelList = new List<MessagingChannel>();
	private MessagingChannelListQuery mMessagingChannelListQuery;

	ArrayList btnMessagingChannels = new ArrayList();

	GameObject messagingChannelListPanel;
	Button btnMessagingChannelListClose;
	GameObject messagingChannelListGridPanel;

	#endregion

	#region common

	Scrollbar openChatScrollbar;
	Scrollbar messagingScrollbar;
	string currentUserName;
	string userId;

	#endregion

	void Awake() {
	}

	void FixedUpdate() {
		if (autoScroll) {
			ScrollToBottom ();
		}
	}

	void Start() {
		string appId = "A7A2672C-AD11-11E4-8DAA-0A18B21C2D82"; // Release
		userId = SystemInfo.deviceUniqueIdentifier;
		SendBirdSDK.Init (appId);

		InitComponents ();
		mGameObject = gameObject;
		mEventProcessor = mGameObject.AddComponent<EventProcessor> ();
	}

	void Disconnect () {
		Debug.Log ("Disconnected from SendBird");
		SendBirdSDK.Disconnect ();
	}

	void ResetOpenChatContent() {
		mEventProcessor.QueueEvent(new Action (() => {
			txtOpenChatContent.text = "";
			lastTextPositionY = 0;
			autoScroll = true;
		}));
	}

	public void InitOpenChat() {
		currentUserName = inputUserName.text;
		SendBirdSDK.Login (userId, currentUserName);

		SendBirdEventHandler seh = new SendBirdEventHandler ();
		seh.OnConnect += (sender, e) => {
			mEventProcessor.QueueEvent(new Action (() => {
				txtOpenChatTitle.text = "#" +  e.Channel.GetUrlWithoutAppPrefix();
				selectedChannelUrl = e.Channel.url;
			}));
		};
		seh.OnError += (sender, e) => {
			if(e.Exception != null) {
				Debug.Log (e.Exception.StackTrace);
			}
		};
		seh.OnChannelLeft += (sender, e) => {
		};
		seh.OnMessageReceived += (sender, e) => {
			mEventProcessor.QueueEvent(new Action (() => {
				Message message = e.Message as Message;
				txtOpenChatContent.text = txtOpenChatContent.text + (MessageRichText(message) + "\n");
				ScrollToBottom();
			}));
		};
		seh.OnBroadcastMessageReceived += (sender, e) => {
			mEventProcessor.QueueEvent(new Action (() => {
				BroadcastMessage bm = e.Message as BroadcastMessage;
				txtOpenChatContent.text = txtOpenChatContent.text + (SystemMessageRichText(bm.message) + "\n");
			}));
		};
		seh.OnSystemMessageReceived += (sender, e) => {
			mEventProcessor.QueueEvent(new Action (() => {
				SystemMessage sm = e.Message as SystemMessage;
				txtOpenChatContent.text = txtOpenChatContent.text + (SystemMessageRichText(sm.message) + "\n");
			}));
		};
		seh.OnAllDataReceived += (sender, e) => {
		};
		seh.OnMessageDelivery += (sender, e) => {
		};

		SendBirdSDK.SetEventHandler (seh);
	}

	void OpenChannelList (int limit = 30) {
		mEventProcessor.QueueEvent(new Action (() => {
			channelPanel.SetActive (true);
		}));
		mChannelListQuery = SendBirdSDK.QueryChannelList ();
		mChannelListQuery.SetLimit(limit);
		mChannelListQuery.OnResult += (sender, e) =>  {
			if(e.Exception != null) {
				Debug.Log (e.Exception.StackTrace);
			} else {
			    OnQueryChannelList(e.Channels);
			}
		};
		mChannelListQuery.Next ();
	}

	public void OnQueryChannelList (List<Channel> channels) {
		mEventProcessor.QueueEvent(new Action (() => {
			foreach (UnityEngine.Object btnChannel in btnChannels) {
				GameObject.Destroy(btnChannel);
			}
			btnChannels.Clear ();

			foreach (Channel channel in channels) {
				GameObject btnChannel = Instantiate (channelListItemPrefab) as GameObject;
				btnChannel.GetComponent<Image>().sprite = uiTheme.channelButtonOff;

				if(channel.url == selectedChannelUrl) {
					btnChannel.GetComponent<Image>().overrideSprite = uiTheme.channelButtonOn;
					btnChannel.GetComponentInChildren<Text>().color = uiTheme.channelButtonOnColor;
				} else {
					btnChannel.GetComponent<Image>().overrideSprite = null;
					btnChannel.GetComponentInChildren<Text>().color = uiTheme.channelButtonOffColor;
				}
				Text text = btnChannel.GetComponentInChildren<Text> ();
				text.text = "#" + channel.GetUrlWithoutAppPrefix ();
				btnChannel.transform.SetParent(channelGridPannel.transform);
				btnChannel.transform.localScale = Vector3.one;
				btnChannels.Add (btnChannel);
				
				Channel channelFinal = channel;
				btnChannel.GetComponent<Button>().onClick.AddListener(() => {
					ResetOpenChatContent();
					
					SendBirdSDK.Join (channelFinal.url);
					SendBirdSDK.Connect (GetMaxMessageTimestamp());

					mEventProcessor.QueueEvent(new Action (() => {
						channelPanel.SetActive(false);
						SelectTab(TAB_MODE.CHANNEL);
					}));
				});
			}
		}));
	}

	void SelectTab(TAB_MODE tab) {
		tabMode = tab;
		if (tabMode == TAB_MODE.CHANNEL) {
			btnChannel.GetComponent<Image>().overrideSprite = uiTheme.chatChannelButtonOn;
			btnChannel.GetComponentInChildren<Text>().color = uiTheme.chatChannelButtonOnColor;

			btnClan.GetComponent<Image>().overrideSprite = null;
			btnClan.GetComponentInChildren<Text>().color = uiTheme.chatChannelButtonOffColor;
		} else {
			btnChannel.GetComponent<Image>().overrideSprite = null;
			btnChannel.GetComponentInChildren<Text>().color = uiTheme.chatChannelButtonOffColor;

			btnClan.GetComponent<Image>().overrideSprite = uiTheme.chatChannelButtonOn;
			btnClan.GetComponentInChildren<Text>().color = uiTheme.chatChannelButtonOnColor;
		}
	}

	void OpenUserList () {
		currentUserName = inputUserName.text;
		SendBirdSDK.Login (userId, currentUserName);

		mEventProcessor.QueueEvent(new Action (() => {
			userListPanel.SetActive (true);
		}));
		mUserListQuery = SendBirdSDK.QueryUserList ();
		mUserListQuery.OnResult += (sender, e) =>  {
			if(e.Exception != null) {
				Debug.Log (e.Exception.StackTrace);
			} else {
				OnQueryUserList(e.Users);
			}
		};
		mUserListQuery.Next ();
	}

	public void OnQueryUserList (List<User> users, bool loadMore = false) {
		mEventProcessor.QueueEvent(new Action (() => {	
			if (!loadMore) {
				foreach (UnityEngine.Object btnUser in btnUsers) {
					GameObject.Destroy(btnUser);
				}
				btnUsers.Clear ();
			}		
			foreach (User user in users) {
				GameObject userItem = Instantiate (userListItemPrefab) as GameObject;
				userItem.GetComponent<Image>().sprite = uiTheme.channelButtonOff;
				
				Text text = userItem.GetComponentInChildren<Text> ();
				text.color = uiTheme.chatChannelButtonOffColor;
				text.text = user.name;
				
				userItem.transform.SetParent(userListGridPanel.transform, false);
				userItem.transform.localScale = Vector3.one;
				btnUsers.Add (userItem);
				
				var userItemToggle = userItem.GetComponent<Toggle>();
				
				User finalUser = user;
				userItemToggle.onValueChanged.AddListener ((isOn) => {
					if(isOn) {
						userItem.GetComponent<Image>().overrideSprite = uiTheme.chatChannelButtonOn;
						userItem.GetComponentInChildren<Text>().color = uiTheme.chatChannelButtonOnColor;
						mUserList.Add (finalUser.GetId());
					} else {
						userItem.GetComponent<Image>().overrideSprite = uiTheme.chatChannelButtonOff;
						userItem.GetComponentInChildren<Text>().color = uiTheme.chatChannelButtonOffColor;
						mUserList.Remove(finalUser.GetId());
					}
				});
			}
		}));
	}

	public void LoadMoreUsers() {
		if(mUserListQuery != null && mUserListQuery.HasNext() && !mUserListQuery.IsLoading()) {
			mUserListQuery.OnResult += (sender, e) =>  {
				OnQueryUserList(e.Users, true);
			};
			mUserListQuery.Next ();
		}
	}

	void ResetMessagingContent() {
		mEventProcessor.QueueEvent(new Action (() => {
			txtMessagingContent.text = "";
			lastTextPositionY = 0;
			autoScroll = true;
		}));
	}

	public void UpdateMessagingChannel(MessagingChannel messagingChannel) {
		mEventProcessor.QueueEvent(new Action (() => {
			mMessagingChannel = messagingChannel;
			txtMessagingTitle.text = GetDisplayMemberNames (messagingChannel.GetMembers ());
		}));
	}

	public void InitMessaging() {
		currentUserName = inputUserName.text;
		SendBirdSDK.Login (userId, currentUserName);

		SendBirdEventHandler seh = new SendBirdEventHandler ();
		seh.OnConnect += (sender, e) => {
			mEventProcessor.QueueEvent(new Action (() => {
				selectedChannelUrl = e.Channel.url;
			}));
		};
		seh.OnError += (sender, e) => {
		};
		seh.OnChannelLeft += (sender, e) => {
		};
		seh.OnMessageReceived += (sender, e) => {
			mEventProcessor.QueueEvent(new Action (() => {
				Message message = e.Message as Message;
				txtMessagingContent.text = txtMessagingContent.text + (MessageRichText(message) + "\n");
				ScrollToBottom();
			}));
			// markasread
		};
		seh.OnBroadcastMessageReceived += (sender, e) => {
			mEventProcessor.QueueEvent(new Action (() => {
				BroadcastMessage bm = e.Message as BroadcastMessage;
				txtMessagingContent.text = txtMessagingContent.text + (SystemMessageRichText(bm.message) + "\n");
			}));
		};
		seh.OnSystemMessageReceived += (sender, e) => {
			mEventProcessor.QueueEvent(new Action (() => {
				SystemMessage sm = e.Message as SystemMessage;
				txtMessagingContent.text = txtMessagingContent.text + (SystemMessageRichText(sm.message) + "\n");
			}));
		};
		seh.OnAllDataReceived += (sender, e) => {
		};
		seh.OnMessageDelivery += (sender, e) => {
		};
		seh.OnReadReceived += (sender, e) => {
		};
		seh.OnTypeStartReceived += (sender, e) => {
		};
		seh.OnTypeEndReceived += (sender, e) => {
		};
		seh.OnMessagingStarted += (sender, e) => {
			UpdateMessagingChannel(e.MessagingChannel);

			var messagingChannelUrl = e.MessagingChannel.GetUrl();
			// message query
			MessageListQuery messageListQuery = SendBirdSDK.QueryMessageList(messagingChannelUrl);
			messageListQuery.OnResult += (sender_child, e_child) => {
				mEventProcessor.QueueEvent(new Action (() => {
					foreach (var messageModel in e_child.Messages) {
						if (messageModel is Message) {
							var message = messageModel as Message;
							if (message.IsPast ()) {
								txtMessagingContent.text = (MessageRichText(message) + "\n") + txtMessagingContent.text;
							} else {
								txtMessagingContent.text = txtMessagingContent.text + (MessageRichText(message) + "\n");
							}
							UpdateMessageTimestamp (message);
						} else if (messageModel is SystemMessage) {
							var message = messageModel as SystemMessage;
							if (message.IsPast ()) {
								txtMessagingContent.text = (SystemMessageRichText(message.message) + "\n") + txtMessagingContent.text;
							} else {
								txtMessagingContent.text = txtMessagingContent.text + (SystemMessageRichText(message.message) + "\n");
							}
							UpdateMessageTimestamp (message);
						} else if (messageModel is BroadcastMessage) {
							var message = messageModel as BroadcastMessage;
							if (message.IsPast ()) {
								txtMessagingContent.text = (SystemMessageRichText(message.message) + "\n") + txtMessagingContent.text;
							} else {
								txtMessagingContent.text = txtMessagingContent.text + (SystemMessageRichText(message.message) + "\n");
							}
							UpdateMessageTimestamp (message);
						}
					}
					
					SendBirdSDK.MarkAsRead(messagingChannelUrl);
					SendBirdSDK.Join (messagingChannelUrl);
					SendBirdSDK.Connect (GetMaxMessageTimestamp());
				}));
			};
			messageListQuery.Prev(long.MaxValue, 50);
		};
		seh.OnMessagingUpdated += (sender, e) => {
			UpdateMessagingChannel(e.MessagingChannel);
		};
		seh.OnMessagingChannelUpdated += (sender, e) => {
			if(mMessagingChannel != null && mMessagingChannel.GetId() == e.MessagingChannel.GetId()) {
				mEventProcessor.QueueEvent(new Action (() => {
					UpdateMessagingChannel(e.MessagingChannel);
				}));
			}
		};

		SendBirdSDK.SetEventHandler (seh);
	}

	public void InviteMessaging(List<string> userIds) {
		ResetMessagingContent ();
		InitMessaging ();
		SendBirdSDK.StartMessaging (userIds);
	}

	void OpenMessagingList () {
		currentUserName = inputUserName.text;
		SendBirdSDK.Login (userId, currentUserName);

		mEventProcessor.QueueEvent(new Action (() => {
			messagingChannelListPanel.SetActive (true);
		}));
		mMessagingChannelListQuery = SendBirdSDK.QueryMessagingList ();
		mMessagingChannelListQuery.OnResult += (sender, e) =>  {
			if(e.Exception != null) {
				Debug.Log (e.Exception.StackTrace);
			} else {
				mMessagingChannelList = e.MessagingChannels;
				OnQueryMessagingChannelList(mMessagingChannelList);
			}
		};
		mMessagingChannelListQuery.Next();
	}

	public void LoadMoreMessaging() {
		if(mUserListQuery != null && mUserListQuery.HasNext() && !mUserListQuery.IsLoading()) {
			mUserListQuery.OnResult += (sender, e) =>  {
				OnQueryUserList(e.Users, true);
			};
			mUserListQuery.Next ();
		}
	}

	public void OnQueryMessagingChannelList (List<MessagingChannel> messagingChannels, bool loadMore = false) {
		mEventProcessor.QueueEvent(new Action (() => {	
			if (!loadMore) {
				foreach (UnityEngine.Object btnMessagingChannel in btnMessagingChannels) {
					GameObject.Destroy(btnMessagingChannel);
				}
				btnMessagingChannels.Clear ();
			}

			foreach (MessagingChannel messagingChannel in messagingChannels) {
				GameObject btnMessagingChannel = Instantiate (channelListItemPrefab) as GameObject;
				btnMessagingChannel.GetComponent<Image>().sprite = uiTheme.channelButtonOff;
				btnMessagingChannel.GetComponent<Image>().type = Image.Type.Sliced;

				if(messagingChannel.GetUrl() == selectedChannelUrl) {
					btnMessagingChannel.GetComponent<Image>().overrideSprite = uiTheme.channelButtonOn;
					btnMessagingChannel.GetComponentInChildren<Text>().color = uiTheme.channelButtonOnColor;
				} else {
					btnMessagingChannel.GetComponent<Image>().overrideSprite = null;
					btnMessagingChannel.GetComponentInChildren<Text>().color = uiTheme.channelButtonOffColor;
				}

				Text text = btnMessagingChannel.GetComponentInChildren<Text> ();
				text.text = string.Format("{0} ({1})", GetDisplayMemberNames(messagingChannel.GetMembers()), messagingChannel.unreadMessageCount);

				btnMessagingChannel.transform.SetParent(messagingChannelListGridPanel.transform);
				btnMessagingChannel.transform.localScale = Vector3.one;
				btnMessagingChannels.Add (btnMessagingChannel);

				MessagingChannel finalMessagingChannel = messagingChannel;
				btnMessagingChannel.GetComponent<Button>().onClick.AddListener(() => {
					mEventProcessor.QueueEvent(new Action (() => {
						messagingChannelListPanel.SetActive(false);
						messagingPanel.SetActive(true);
						JoinMessaging(finalMessagingChannel.GetUrl());
					}));
				});
			}
		}));
	}

	public void JoinMessaging(string channelUrl) {
		ResetMessagingContent ();
		InitMessaging ();
		SendBirdSDK.JoinMessaging (channelUrl);
	}

	void InitComponents () {
		uiPanel = GameObject.Find ("SendBirdUnity/UIPanel");
		(Instantiate (uiThemePrefab) as GameObject).transform.parent = uiPanel.transform;

		uiTheme = GameObject.FindObjectOfType (typeof(SendBirdTheme)) as SendBirdTheme;

		#region MenuPanel

		menuPanel = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel");
		menuPanel.GetComponent<Image> ().sprite = uiTheme.channelListFrameBG;

		var txtMenuTitle = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel/TxtTitle").GetComponent<Text> ();
		txtMenuTitle.color = uiTheme.titleColor;

		btnOpenChannel = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel/BtnOpenChannel").GetComponent<Button> ();
		btnOpenChannel.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnOpenChannel.GetComponent<Image> ().type = Image.Type.Sliced;
		btnOpenChannel.onClick.AddListener (() => {
			mEventProcessor.QueueEvent(new Action (() => {
				menuPanel.SetActive (false);
				openChatPanel.SetActive (true);
				
				ResetOpenChatContent ();
				InitOpenChat();
				
				SendBirdSDK.Join ("jia_test.lobby");
				SendBirdSDK.Connect (GetMaxMessageTimestamp());
				
				SelectTab(TAB_MODE.CHANNEL);
			}));
		});

		btnStartMessaging = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel/BtnStartMessaging").GetComponent<Button> ();
		btnStartMessaging.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnStartMessaging.GetComponent<Image> ().type = Image.Type.Sliced;
		btnStartMessaging.onClick.AddListener (() => {
			mEventProcessor.QueueEvent(new Action (() => {
				menuPanel.SetActive (false);
				userListPanel.SetActive (true);
				OpenUserList();
			}));
		});

		btnJoinMessaging = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel/BtnJoinMessaging").GetComponent<Button> ();
		btnJoinMessaging.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnJoinMessaging.GetComponent<Image> ().type = Image.Type.Sliced;
		btnJoinMessaging.onClick.AddListener (() => {
			mEventProcessor.QueueEvent(new Action (() => {
				menuPanel.SetActive (false);
				messagingChannelListPanel.SetActive (true);
				OpenMessagingList();
			}));
		});

		inputUserName = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel/InputUserName").GetComponent<InputField> ();
		inputUserName.GetComponent<Image> ().sprite = uiTheme.inputTextBG;

		#endregion

		#region OpenChannel

		openChatPanel = GameObject.Find ("SendBirdUnity/UIPanel/OpenChatPanel");
		openChatPanel.GetComponent<Image> ().sprite = uiTheme.chatFrameBG;

		txtOpenChatContent = GameObject.Find("SendBirdUnity/UIPanel/OpenChatPanel/ScrollArea/TxtContent").GetComponent<Text>(); // (Text);
		txtOpenChatContent.color = uiTheme.messageColor;

		txtOpenChatTitle = GameObject.Find ("SendBirdUnity/UIPanel/OpenChatPanel/TxtTitle").GetComponent<Text> ();
		txtOpenChatTitle.color = uiTheme.titleColor;

		openChatScrollbar = GameObject.Find ("SendBirdUnity/UIPanel/OpenChatPanel/Scrollbar").GetComponent<Scrollbar>();

		ColorBlock cb = openChatScrollbar.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		openChatScrollbar.colors = cb;
		openChatScrollbar.onValueChanged.AddListener ((float value) => {
			if(value <= 0) {
				autoScroll = true;
				lastTextPositionY = txtOpenChatContent.transform.position.y;
				return;
			}

			if(lastTextPositionY - txtOpenChatContent.transform.position.y >= 100) {
				autoScroll = false;
			}

			lastTextPositionY = txtOpenChatContent.transform.position.y;
		});

		inputOpenChat = GameObject.Find ("SendBirdUnity/UIPanel/OpenChatPanel/InputOpenChat").GetComponent<InputField> ();
		inputOpenChat.GetComponent<Image> ().sprite = uiTheme.inputTextBG;
		inputOpenChat.onEndEdit.AddListener ((string msg) => {
			SubmitOpenChat();
		});

		GameObject.Find ("SendBirdUnity/UIPanel/OpenChatPanel/InputOpenChat/Placeholder").GetComponent<Text> ().color = uiTheme.inputTextPlaceholderColor;
		GameObject.Find ("SendBirdUnity/UIPanel/OpenChatPanel/InputOpenChat/Text").GetComponent<Text> ().color = uiTheme.inputTextColor;

		btnOpenChatSend = GameObject.Find ("SendBirdUnity/UIPanel/OpenChatPanel/BtnOpenChatSend").GetComponent<Button> ();
		btnOpenChatSend.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnOpenChatSend.GetComponentInChildren<Text> ().color = uiTheme.sendButtonColor;
		btnOpenChatSend.onClick.AddListener (() => {
			SubmitOpenChat();
		});

		btnClan = GameObject.Find ("SendBirdUnity/UIPanel/OpenChatPanel/BtnClan").GetComponent<Button> ();
		btnClan.GetComponent<Image> ().sprite = uiTheme.chatChannelButtonOff;
		btnClan.onClick.AddListener (() => {
			ResetOpenChatContent ();

			SendBirdSDK.Join ("jia_test.clan");
			SendBirdSDK.Connect (GetMaxMessageTimestamp());

			SelectTab(TAB_MODE.CLAN);
		});

		btnOpenChatClose = GameObject.Find ("SendBirdUnity/UIPanel/OpenChatPanel/BtnOpenChatClose").GetComponent<Button> ();
		btnOpenChatClose.GetComponent<Image> ().sprite = uiTheme.closeButton;
		btnOpenChatClose.onClick.AddListener (() => {
			Disconnect();
			mEventProcessor.QueueEvent(new Action (() => {
				openChatPanel.SetActive(false);
				menuPanel.SetActive(true);
			}));
		});

		btnChannel = GameObject.Find ("SendBirdUnity/UIPanel/OpenChatPanel/BtnChannel").GetComponent<Button> ();
		btnChannel.GetComponent<Image> ().sprite = uiTheme.chatChannelButtonOff;
		btnChannel.onClick.AddListener (() => {
			OpenChannelList();
		});

		#endregion

		#region ChannelList

		channelPanel = GameObject.Find ("SendBirdUnity/UIPanel/ChannelPanel");
		channelPanel.GetComponent<Image> ().sprite = uiTheme.channelListFrameBG;

		channelGridPannel = GameObject.Find ("SendBirdUnity/UIPanel/ChannelPanel/ScrollArea/GridPanel");

		GameObject.Find ("SendBirdUnity/UIPanel/ChannelPanel/TxtTitle").GetComponent<Text> ().color = uiTheme.titleColor;

		var channelScrollbar = GameObject.Find ("SendBirdUnity/UIPanel/ChannelPanel/Scrollbar").GetComponent<Scrollbar>();
		cb = channelScrollbar.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		channelScrollbar.colors = cb;

		btnChannelClose = GameObject.Find ("SendBirdUnity/UIPanel/ChannelPanel/BtnChannelClose").GetComponent<Button> ();
		btnChannelClose.GetComponent<Image> ().sprite = uiTheme.closeButton;
		btnChannelClose.onClick.AddListener (() => {
			mEventProcessor.QueueEvent(new Action (() => {
				channelPanel.SetActive(false);
			}));
		});

		#endregion

		#region Messaging

		messagingPanel = GameObject.Find ("SendBirdUnity/UIPanel/MessagingPanel");
		messagingPanel.GetComponent<Image> ().sprite = uiTheme.chatFrameBG;

		txtMessagingTitle = GameObject.Find ("SendBirdUnity/UIPanel/MessagingPanel/TxtTitle").GetComponent<Text> ();
		txtMessagingTitle.color = uiTheme.titleColor;

		btnMessagingClose = GameObject.Find ("SendBirdUnity/UIPanel/MessagingPanel/BtnMessagingClose").GetComponent<Button> ();
		btnMessagingClose.GetComponent<Image> ().sprite = uiTheme.closeButton;
		btnMessagingClose.onClick.AddListener (() => {
			mEventProcessor.QueueEvent(new Action (() => {
				messagingPanel.SetActive(false);
				menuPanel.SetActive(true);
			}));
		});

		txtMessagingContent = GameObject.Find("SendBirdUnity/UIPanel/MessagingPanel/ScrollArea/TxtContent").GetComponent<Text>(); // (Text);
		txtMessagingContent.color = uiTheme.messageColor;

		txtMessagingTitle = GameObject.Find ("SendBirdUnity/UIPanel/MessagingPanel/TxtTitle").GetComponent<Text> ();
		txtMessagingTitle.color = uiTheme.titleColor;

		messagingScrollbar = GameObject.Find ("SendBirdUnity/UIPanel/MessagingPanel/Scrollbar").GetComponent<Scrollbar>();
		ColorBlock cb_messaging = messagingScrollbar.colors;
		cb_messaging.normalColor = uiTheme.scrollBarColor;
		cb_messaging.pressedColor = uiTheme.scrollBarColor;
		cb_messaging.highlightedColor = uiTheme.scrollBarColor;
		messagingScrollbar.colors = cb_messaging;
		messagingScrollbar.onValueChanged.AddListener ((float value) => {
			if(value <= 0) {
				autoScroll = true;
				lastTextPositionY = txtMessagingContent.transform.position.y;
				return;
			}

			if(lastTextPositionY - txtMessagingContent.transform.position.y >= 100) {
				autoScroll = false;
			}

			lastTextPositionY = txtMessagingContent.transform.position.y;
		});

		inputMessaging = GameObject.Find ("SendBirdUnity/UIPanel/MessagingPanel/InputMessaging").GetComponent<InputField> ();
		inputMessaging.GetComponent<Image> ().sprite = uiTheme.inputTextBG;
		inputMessaging.onEndEdit.AddListener ((string msg) => {
			SubmitMessaging();
		});

		GameObject.Find ("SendBirdUnity/UIPanel/MessagingPanel/InputMessaging/Placeholder").GetComponent<Text> ().color = uiTheme.inputTextPlaceholderColor;
		GameObject.Find ("SendBirdUnity/UIPanel/MessagingPanel/InputMessaging/Text").GetComponent<Text> ().color = uiTheme.inputTextColor;

		btnMessagingSend = GameObject.Find ("SendBirdUnity/UIPanel/MessagingPanel/BtnMessagingSend").GetComponent<Button> ();
		btnMessagingSend.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnMessagingSend.GetComponentInChildren<Text> ().color = uiTheme.sendButtonColor;
		btnMessagingSend.onClick.AddListener (() => {
			SubmitMessaging();
		});

		btnMessagingList = GameObject.Find ("SendBirdUnity/UIPanel/MessagingPanel/BtnMessagingList").GetComponent<Button> ();
		btnMessagingList.GetComponent<Image> ().sprite = uiTheme.chatChannelButtonOff;
		btnMessagingList.GetComponent<Image> ().type = Image.Type.Sliced;
		btnMessagingList.GetComponentInChildren<Text>().color = uiTheme.chatChannelButtonOffColor;
		btnMessagingList.onClick.AddListener (() => {
			OpenMessagingList();
		});

		#endregion

		#region UserList

		userListPanel = GameObject.Find ("SendBirdUnity/UIPanel/UserListPanel");
		userListPanel.GetComponent<Image> ().sprite = uiTheme.channelListFrameBG;

		userListGridPanel = GameObject.Find ("SendBirdUnity/UIPanel/UserListPanel/ScrollArea/GridPanel");

		GameObject.Find ("SendBirdUnity/UIPanel/UserListPanel/TxtTitle").GetComponent<Text> ().color = uiTheme.titleColor;

		var userListScrollBar = GameObject.Find ("SendBirdUnity/UIPanel/UserListPanel/Scrollbar").GetComponent<Scrollbar>();
		cb = userListScrollBar.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		userListScrollBar.colors = cb;
		userListScrollBar.onValueChanged.AddListener ((float value) => {
			if(value <= 0) {
				LoadMoreUsers();
			}
		});

		btnUserListClose = GameObject.Find ("SendBirdUnity/UIPanel/UserListPanel/BtnUserListClose").GetComponent<Button> ();
		btnUserListClose.GetComponent<Image> ().sprite = uiTheme.closeButton;
		btnUserListClose.onClick.AddListener (() => {
			mEventProcessor.QueueEvent(new Action (() => {
				userListPanel.SetActive(false);
				menuPanel.SetActive(true);
			}));
		});

		btnInvite = GameObject.Find ("SendBirdUnity/UIPanel/UserListPanel/BtnInvite").GetComponent<Button> ();
		btnInvite.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnInvite.onClick.AddListener (() => {
			mEventProcessor.QueueEvent(new Action (() => {
				userListPanel.SetActive(false);
				messagingPanel.SetActive(true);
			}));
			InviteMessaging(mUserList);
		});

		#endregion

		#region MessagingList

		messagingChannelListPanel = GameObject.Find ("SendBirdUnity/UIPanel/MessagingChannelListPanel");
		messagingChannelListPanel.GetComponent<Image> ().sprite = uiTheme.channelListFrameBG;

		messagingChannelListGridPanel = GameObject.Find ("SendBirdUnity/UIPanel/MessagingChannelListPanel/ScrollArea/GridPanel");

		GameObject.Find ("SendBirdUnity/UIPanel/MessagingChannelListPanel/TxtTitle").GetComponent<Text> ().color = uiTheme.titleColor;

		var messagingChannelListScrollbar = GameObject.Find ("SendBirdUnity/UIPanel/MessagingChannelListPanel/Scrollbar").GetComponent<Scrollbar>();
		cb = messagingChannelListScrollbar.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		messagingChannelListScrollbar.colors = cb;
		messagingChannelListScrollbar.onValueChanged.AddListener ((float value) => {
			if(value <= 0) {
				
			}
		});

		btnMessagingClose = GameObject.Find ("SendBirdUnity/UIPanel/MessagingChannelListPanel/BtnMessagingChannelListClose").GetComponent<Button> ();
		btnMessagingClose.GetComponent<Image> ().sprite = uiTheme.closeButton;
		btnMessagingClose.onClick.AddListener (() => {
			mEventProcessor.QueueEvent(new Action (() => {
				messagingChannelListPanel.SetActive(false);
				if(!messagingPanel.activeSelf) {
					menuPanel.SetActive(true);
				}
			}));
		});

		#endregion

		uiPanel.SetActive (true);
		menuPanel.SetActive (true);
		openChatPanel.SetActive (false);
		channelPanel.SetActive (false);
		messagingPanel.SetActive (false);
		userListPanel.SetActive (false);
		messagingChannelListPanel.SetActive (false);
	}

	string MessageRichText(Message message) {
		return "<color=#" + SendBirdTheme.ToHex(uiTheme.senderColor) + ">" + message.GetSenderName() + ": </color>" + message.message;
	}

	string SystemMessageRichText(string message) {
		return "<color=#" + SendBirdTheme.ToHex(uiTheme.systemMessageColor) + ">" + message + "</color>";
	}

	void ScrollToBottom() {
		openChatScrollbar.value = 0;
		messagingScrollbar.value = 0;
	}

	void SubmitOpenChat() {
		if (inputOpenChat.text.Length > 0) {
			SendBirdSDK.SendMessage(inputOpenChat.text);
			inputOpenChat.text = "";
		}
	}

	void SubmitMessaging() {
		if (inputMessaging.text.Length > 0) {
			SendBirdSDK.SendMessage(inputMessaging.text);
			inputMessaging.text = "";
		}
	}

	#region helpers

	private string GetDisplayMemberNames(List<MessagingChannel.Member> members) {
		if(members.Count < 2) {
			return "No Members";
		} else if(members.Count == 2) {
			StringBuilder names = new StringBuilder();
			foreach(var member in members) {
				if (member.GetId().Equals(SendBirdSDK.GetUserId())) {
					continue;
				}

				names.Append(", " + member.name);
			}
			return (string)names.Remove(0, 2).ToString();;
		} else {
			return "Group " + members.Count;
		}
	}

	#endregion
}
