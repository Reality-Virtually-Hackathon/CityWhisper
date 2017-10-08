using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.iOS;

using UnityEngine.UI;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Xml.Serialization;
using SendBird;

public class MessageManager : MonoBehaviour {


	public GameObject uiThemePrefab;



	SendBirdTheme uiTheme;

	GameObject uiPanel;

	SendBirdClient.ChannelHandler openChannelHandler;

	#region Menu

	Dictionary<string, OpenChannel> enteredChannels = new Dictionary<string, OpenChannel> ();


	GameObject menuPanel;
	Button btnConnect;
	Button btnOpenChannelList;
	Button btnStartGroupChannel;
	Button btnGroupChannelList;
	InputField inputUserName;

	#endregion

	#region OpenChannel

	private OpenChannelListQuery mChannelListQuery;
	GameObject openChannelPanel;

	enum TAB_MODE
	{
		CHANNEL,
		CLAN}

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

	string currentChannelUrl;

	#endregion

	#region OpenChannelList
	public GameObject cube;
	public GameObject channelListItemPrefab;
	ArrayList btnChannels = new ArrayList ();
	public Message1 xferMsg = new Message1 ();
	public bool flagNew3DMsg = false; 
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

	#endregion

	#region UserList

	GameObject userListPanel;

	private List<string> mUserList = new List<string> ();
	private UserListQuery mUserListQuery;

	public GameObject userListItemPrefab;
	List<UnityEngine.Object> btnUsers = new List<UnityEngine.Object> ();

	Button btnUserListClose;
	Button btnInvite;
	GameObject userListGridPanel;

	#endregion

	#region GroupChannelList

	private GroupChannelListQuery mGroupChannelListQuery;

	ArrayList btnGroupChannels = new ArrayList ();

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

	void FixedUpdate ()
	{
		if (autoScroll) {
			ScrollToBottom ();
		}
	}

	void Start ()
	{
		InitComponents ();

		SendBirdClient.ChannelHandler channelHandler = new SendBirdClient.ChannelHandler ();
		channelHandler.OnMessageReceived = (BaseChannel channel, BaseMessage message) => {
			// Draw new messages if user is on the channel.
			if(currentChannel.Url == channel.Url)
			{
				if (message is UserMessage) {
					string text;
					text = UserMessageRichText ((UserMessage)message);

					if(text.Contains("3dObject"))
					{
						string[] values = text.Split(',');
						Message1 obj = XMLSerializer.Deserialize(values[1]);
						Console.WriteLine("Text: " + values[1] + " Received obj World Position: x " + obj.transformX + " y: " + obj.transformY + " z: " + obj.transformZ);
						txtOpenChannelContent.text = txtOpenChannelContent.text + ("Text: " + values[1] + " Received obj Position: x " + obj.transformX + " y: " + obj.transformY + " z: " + obj.transformZ + "\n");
						Vector3 targetPos;
						targetPos.x = obj.transformX;
						targetPos.y = obj.transformY;
						targetPos.z = obj.transformZ;
						Vector3 recObjPos = LocationHelper.calculatePosition(locationManager.getOrigWorldPosition(), targetPos);
						Console.WriteLine("\n\nReceived obj calculated Position: x " + recObjPos.x+ " y: " + recObjPos.y + " z: " + recObjPos.z);
						GameObject clone = Instantiate(prefabObject, recObjPos, Quaternion.identity);
					}

					if(channel.IsOpenChannel())
						txtOpenChannelContent.text = txtOpenChannelContent.text + (UserMessageRichText ((UserMessage)message) + "\n");
					else {
						txtGroupChannelContent.text = txtGroupChannelContent.text + (UserMessageRichText ((UserMessage)message) + "\n");
						Console.WriteLine(message.ToString());
					}
				} else if (message is FileMessage) {

					if(channel.IsOpenChannel())
						txtOpenChannelContent.text = txtOpenChannelContent.text + (FileMessageRichText ((FileMessage)message) + "\n");
					else
						txtGroupChannelContent.text = txtGroupChannelContent.text + (FileMessageRichText ((FileMessage)message) + "\n");
				} else if (message is AdminMessage) {
					if(channel.IsOpenChannel())
						txtOpenChannelContent.text = txtOpenChannelContent.text + (AdminMessageRichText ((AdminMessage)message) + "\n");
					else
						txtGroupChannelContent.text = txtGroupChannelContent.text + (AdminMessageRichText ((AdminMessage)message) + "\n");
				}

				ScrollToBottom ();
			}
		};

		SendBirdClient.AddChannelHandler ("default", channelHandler);
	}

	void ResetOpenChannelContent ()
	{
		txtOpenChannelContent.text = "";
		inputOpenChannel.text = "";
		lastTextPositionY = 0;
		autoScroll = true;
	}

	void OpenOpenChannelList ()
	{
		openChannelListPanel.SetActive (true);

		foreach (UnityEngine.Object btnChannel in btnChannels) {
			GameObject.Destroy (btnChannel);
		}
		btnChannels.Clear ();


		mChannelListQuery = OpenChannel.CreateOpenChannelListQuery ();
		mChannelListQuery.Limit = 50;
		LoadOpenChannels ();
	}

	void LoadOpenChannels()
	{
		mChannelListQuery.Next ((list, e) => {
			if (e != null) {
				Debug.Log (e.Code + ": " + e.Message);
				return;
			}	

			foreach (OpenChannel channel in list) {
				GameObject btnChannel = Instantiate (channelListItemPrefab) as GameObject;
				btnChannel.GetComponent<Image> ().sprite = uiTheme.channelButtonOff;

				if (channel.Url == selectedChannelUrl) {
					btnChannel.GetComponent<Image> ().overrideSprite = uiTheme.channelButtonOn;
					btnChannel.GetComponentInChildren<Text> ().color = uiTheme.channelButtonOnColor;
				} else {
					btnChannel.GetComponent<Image> ().overrideSprite = null;
					btnChannel.GetComponentInChildren<Text> ().color = uiTheme.channelButtonOffColor;
				}
				Text text = btnChannel.GetComponentInChildren<Text> ();
				text.text = "#" + channel.Name;
				btnChannel.transform.SetParent (channelGridPannel.transform);
				btnChannel.transform.localScale = Vector3.one;
				btnChannels.Add (btnChannel);

				OpenChannel final = channel;
				btnChannel.GetComponent<Button> ().onClick.AddListener (() => {
					foreach (KeyValuePair<string, OpenChannel> entry in enteredChannels) {
						entry.Value.Exit (null);
					}

					final.Enter ((e1) => {
						if (e1 != null) {
							Debug.Log (e1.Code + ": " + e1.Message);
							return;
						}

						currentChannel = final;
						ResetOpenChannelContent();
						txtOpenChannelTitle.text = "#" + final.Name;

						enteredChannels [final.Url] = final;

						openChannelListPanel.SetActive (false);
						openChannelPanel.SetActive(true);
					});
				});
			}
		});
	}


	void OpenUserList ()
	{
		foreach (UnityEngine.Object btnUser in btnUsers) {
			GameObject.Destroy (btnUser);
		}
		btnUsers.Clear ();

		userListPanel.SetActive (true);
		mUserListQuery = SendBirdClient.CreateUserListQuery ();
		mUserListQuery.Limit = 50;

		LoadUsers ();
	}

	public void LoadUsers ()
	{
		mUserListQuery.Next ((list, e) => {
			if (e != null) {
				Debug.Log (e.Code + ": " + e.Message);
				return;
			}

			foreach (User user in list) {
				GameObject userItem = Instantiate (userListItemPrefab) as GameObject;
				userItem.GetComponent<Image> ().sprite = uiTheme.channelButtonOff;

				Text text = userItem.GetComponentInChildren<Text> ();
				text.color = uiTheme.chatChannelButtonOffColor;
				text.text = user.Nickname;

				userItem.transform.SetParent (userListGridPanel.transform, false);
				userItem.transform.localScale = Vector3.one;
				btnUsers.Add (userItem);

				var userItemToggle = userItem.GetComponent<Toggle> ();

				User finalUser = user;
				userItemToggle.onValueChanged.AddListener ((isOn) => {
					if (isOn) {
						userItem.GetComponent<Image> ().overrideSprite = uiTheme.chatChannelButtonOn;
						userItem.GetComponentInChildren<Text> ().color = uiTheme.chatChannelButtonOnColor;
						mUserList.Add (finalUser.UserId);
					} else {
						userItem.GetComponent<Image> ().overrideSprite = uiTheme.chatChannelButtonOff;
						userItem.GetComponentInChildren<Text> ().color = uiTheme.chatChannelButtonOffColor;
						mUserList.Remove (finalUser.UserId);
					}
				});
			}

		});
	}

	void ResetGroupChannelContent ()
	{
		txtGroupChannelContent.text = "";
		inputGroupChannel.text = "";
		lastTextPositionY = 0;
		autoScroll = true;
	}

	void OpenGroupChannelList ()
	{
		foreach (UnityEngine.Object btnGroupChannel in btnGroupChannels) {
			GameObject.Destroy (btnGroupChannel);
		}
		btnGroupChannels.Clear ();

		groupChannelListPanel.SetActive (true);

		mGroupChannelListQuery = GroupChannel.CreateMyGroupChannelListQuery ();
		mGroupChannelListQuery.Limit = 50;
		LoadGroupChannels ();
	}

	void LoadGroupChannels()
	{
		mGroupChannelListQuery.Next ((list, e) => {
			if (e != null) {
				Debug.Log (e.Code + ": " + e.Message);
				return;
			}

			foreach (GroupChannel groupChannel in list) {
				GameObject btnGroupChannel = Instantiate (channelListItemPrefab) as GameObject;
				btnGroupChannel.GetComponent<Image> ().sprite = uiTheme.channelButtonOff;
				btnGroupChannel.GetComponent<Image> ().type = Image.Type.Sliced;

				if (groupChannel.Url == selectedChannelUrl) {
					btnGroupChannel.GetComponent<Image> ().overrideSprite = uiTheme.channelButtonOn;
					btnGroupChannel.GetComponentInChildren<Text> ().color = uiTheme.channelButtonOnColor;
				} else {
					btnGroupChannel.GetComponent<Image> ().overrideSprite = null;
					btnGroupChannel.GetComponentInChildren<Text> ().color = uiTheme.channelButtonOffColor;
				}

				Text text = btnGroupChannel.GetComponentInChildren<Text> ();
				text.text = string.Format ("{0} ({1})", GetDisplayMemberNames (groupChannel.Members), groupChannel.UnreadMessageCount);

				btnGroupChannel.transform.SetParent (groupChannelListGridPanel.transform);
				btnGroupChannel.transform.localScale = Vector3.one;
				btnGroupChannels.Add (btnGroupChannel);

				GroupChannel final = groupChannel;
				btnGroupChannel.GetComponent<Button> ().onClick.AddListener (() => {
					groupChannelListPanel.SetActive (false);
					groupChannelPanel.SetActive (true);


					currentChannel = final;
					ResetGroupChannelContent();
					txtGroupChannelTitle.text = GetDisplayMemberNames(final.Members);
				});
			}

		});
	}

	void InitComponents ()
	{
		uiPanel = GameObject.Find ("SendBirdUnity/UIPanel");
		(Instantiate (uiThemePrefab) as GameObject).transform.parent = uiPanel.transform;

		uiTheme = GameObject.FindObjectOfType (typeof(SendBirdTheme)) as SendBirdTheme;

		#region MenuPanel

		menuPanel = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel");
		menuPanel.GetComponent<Image> ().sprite = uiTheme.channelListFrameBG;

		var txtMenuTitle = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel/TxtTitle").GetComponent<Text> ();
		txtMenuTitle.color = uiTheme.titleColor;

		btnConnect = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel/BtnConnect").GetComponent<Button> ();
		btnConnect.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnConnect.GetComponent<Image> ().type = Image.Type.Sliced;
		btnConnect.onClick.AddListener (() => {
			nickname = inputUserName.text;
			userId = nickname; // Assign user's unique id.

			if (nickname == null || nickname.Length <= 0) {
				return;
			}

			SendBirdClient.Connect (userId, (user, e) => {
				if (e != null) {
					Debug.Log (e.Code + ": " + e.Message);
					return;
				}


				btnConnect.gameObject.SetActive (false);

				btnOpenChannelList.gameObject.SetActive (true);
				btnStartGroupChannel.gameObject.SetActive (true);
				btnGroupChannelList.gameObject.SetActive (true);

				SendBirdClient.UpdateCurrentUserInfo (nickname, null, (e1) => {
					if (e1 != null) {
						Debug.Log (e.Code + ": " + e.Message);
						return;
					}

				});
			});
		});


		btnOpenChannelList = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel/BtnOpenChannel").GetComponent<Button> ();
		btnOpenChannelList.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnOpenChannelList.GetComponent<Image> ().type = Image.Type.Sliced;
		btnOpenChannelList.onClick.AddListener (() => {
			menuPanel.SetActive (false);
			OpenOpenChannelList ();
		});

		btnStartGroupChannel = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel/BtnStartGroupChannel").GetComponent<Button> ();
		btnStartGroupChannel.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnStartGroupChannel.GetComponent<Image> ().type = Image.Type.Sliced;
		btnStartGroupChannel.onClick.AddListener (() => {
			menuPanel.SetActive (false);

			OpenUserList ();
		});

		btnGroupChannelList = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel/BtnGroupChannel").GetComponent<Button> ();
		btnGroupChannelList.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnGroupChannelList.GetComponent<Image> ().type = Image.Type.Sliced;
		btnGroupChannelList.onClick.AddListener (() => {
			menuPanel.SetActive (false);

			OpenGroupChannelList ();
		});

		inputUserName = GameObject.Find ("SendBirdUnity/UIPanel/MenuPanel/InputUserName").GetComponent<InputField> ();
		inputUserName.GetComponent<Image> ().sprite = uiTheme.inputTextBG;

		#endregion

		#region OpenChannel

		openChannelPanel = GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelPanel");
		openChannelPanel.GetComponent<Image> ().sprite = uiTheme.chatFrameBG;

		txtOpenChannelContent = GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelPanel/ScrollArea/TxtContent").GetComponent<Text> (); // (Text);
		txtOpenChannelContent.color = uiTheme.messageColor;

		txtOpenChannelTitle = GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelPanel/TxtTitle").GetComponent<Text> ();
		txtOpenChannelTitle.color = uiTheme.titleColor;

		openChannelScrollbar = GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelPanel/Scrollbar").GetComponent<Scrollbar> ();

		ColorBlock cb = openChannelScrollbar.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		openChannelScrollbar.colors = cb;
		openChannelScrollbar.onValueChanged.AddListener ((float value) => {
			if (value <= 0) {
				autoScroll = true;
				lastTextPositionY = txtOpenChannelContent.transform.position.y;
				return;
			}

			if (lastTextPositionY - txtOpenChannelContent.transform.position.y >= 100) {
				autoScroll = false;
			}

			lastTextPositionY = txtOpenChannelContent.transform.position.y;
		});

		inputOpenChannel = GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelPanel/InputOpenChannel").GetComponent<InputField> ();
		inputOpenChannel.GetComponent<Image> ().sprite = uiTheme.inputTextBG;
		inputOpenChannel.onEndEdit.AddListener ((string msg) => {
			SubmitOpenChannel ();
		});

		GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelPanel/InputOpenChannel/Placeholder").GetComponent<Text> ().color = uiTheme.inputTextPlaceholderColor;
		GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelPanel/InputOpenChannel/Text").GetComponent<Text> ().color = uiTheme.inputTextColor;

		btnOpenChannelSend = GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelPanel/BtnOpenChannelSend").GetComponent<Button> ();
		btnOpenChannelSend.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnOpenChannelSend.GetComponentInChildren<Text> ().color = uiTheme.sendButtonColor;
		btnOpenChannelSend.onClick.AddListener (() => {
			SubmitOpenChannel ();
		});

		btnOpenChannelClose = GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelPanel/BtnOpenChannelClose").GetComponent<Button> ();
		btnOpenChannelClose.GetComponent<Image> ().sprite = uiTheme.closeButton;
		btnOpenChannelClose.onClick.AddListener (() => {
			openChannelPanel.SetActive (false);
			menuPanel.SetActive (true);
		});

		#endregion

		#region ChannelList

		openChannelListPanel = GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelListPanel");
		openChannelListPanel.GetComponent<Image> ().sprite = uiTheme.channelListFrameBG;

		channelGridPannel = GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelListPanel/ScrollArea/GridPanel");

		GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelListPanel/TxtTitle").GetComponent<Text> ().color = uiTheme.titleColor;

		var channelScrollbar = GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelListPanel/Scrollbar").GetComponent<Scrollbar> ();
		cb = channelScrollbar.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		channelScrollbar.colors = cb;

		channelScrollbar.onValueChanged.AddListener ((float value) => {
			if (value <= 0) {
				LoadOpenChannels ();
			}
		});

		btnOpenChannelListClose = GameObject.Find ("SendBirdUnity/UIPanel/OpenChannelListPanel/BtnOpenChannelListClose").GetComponent<Button> ();
		btnOpenChannelListClose.GetComponent<Image> ().sprite = uiTheme.closeButton;
		btnOpenChannelListClose.onClick.AddListener (() => {
			openChannelListPanel.SetActive (false);
			menuPanel.SetActive(true);
		});

		#endregion

		#region GroupChannel

		groupChannelPanel = GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelPanel");
		groupChannelPanel.GetComponent<Image> ().sprite = uiTheme.chatFrameBG;

		txtGroupChannelTitle = GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelPanel/TxtTitle").GetComponent<Text> ();
		txtGroupChannelTitle.color = uiTheme.titleColor;

		btnGroupChannelClose = GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelPanel/BtnGroupChannelClose").GetComponent<Button> ();
		btnGroupChannelClose.GetComponent<Image> ().sprite = uiTheme.closeButton;
		btnGroupChannelClose.onClick.AddListener (() => {
			groupChannelPanel.SetActive (false);
			menuPanel.SetActive (true);
		});

		txtGroupChannelContent = GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelPanel/ScrollArea/TxtContent").GetComponent<Text> (); // (Text);
		txtGroupChannelContent.color = uiTheme.messageColor;

		txtGroupChannelTitle = GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelPanel/TxtTitle").GetComponent<Text> ();
		txtGroupChannelTitle.color = uiTheme.titleColor;

		groupChannelScrollbar = GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelPanel/Scrollbar").GetComponent<Scrollbar> ();
		ColorBlock cb_groupChannel = groupChannelScrollbar.colors;
		cb_groupChannel.normalColor = uiTheme.scrollBarColor;
		cb_groupChannel.pressedColor = uiTheme.scrollBarColor;
		cb_groupChannel.highlightedColor = uiTheme.scrollBarColor;
		groupChannelScrollbar.colors = cb_groupChannel;
		groupChannelScrollbar.onValueChanged.AddListener ((float value) => {
			if (value <= 0) {
				autoScroll = true;
				lastTextPositionY = txtGroupChannelContent.transform.position.y;
				return;
			}

			if (lastTextPositionY - txtGroupChannelContent.transform.position.y >= 100) {
				autoScroll = false;
			}

			lastTextPositionY = txtGroupChannelContent.transform.position.y;
		});

		inputGroupChannel = GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelPanel/InputGroupChannel").GetComponent<InputField> ();
		inputGroupChannel.GetComponent<Image> ().sprite = uiTheme.inputTextBG;
		inputGroupChannel.onEndEdit.AddListener ((string msg) => {
			SubmitGroupChannel ();
		});

		GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelPanel/InputGroupChannel/Placeholder").GetComponent<Text> ().color = uiTheme.inputTextPlaceholderColor;
		GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelPanel/InputGroupChannel/Text").GetComponent<Text> ().color = uiTheme.inputTextColor;

		btnGroupChannelSend = GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelPanel/BtnGroupChannelSend").GetComponent<Button> ();
		btnGroupChannelSend.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnGroupChannelSend.GetComponentInChildren<Text> ().color = uiTheme.sendButtonColor;
		btnGroupChannelSend.onClick.AddListener (() => {
			SubmitGroupChannel ();
		});

		#endregion

		#region UserList

		userListPanel = GameObject.Find ("SendBirdUnity/UIPanel/UserListPanel");
		userListPanel.GetComponent<Image> ().sprite = uiTheme.channelListFrameBG;

		userListGridPanel = GameObject.Find ("SendBirdUnity/UIPanel/UserListPanel/ScrollArea/GridPanel");

		GameObject.Find ("SendBirdUnity/UIPanel/UserListPanel/TxtTitle").GetComponent<Text> ().color = uiTheme.titleColor;

		var userListScrollBar = GameObject.Find ("SendBirdUnity/UIPanel/UserListPanel/Scrollbar").GetComponent<Scrollbar> ();
		cb = userListScrollBar.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		userListScrollBar.colors = cb;
		userListScrollBar.onValueChanged.AddListener ((float value) => {
			if (value <= 0) {
				LoadUsers ();
			}
		});

		btnUserListClose = GameObject.Find ("SendBirdUnity/UIPanel/UserListPanel/BtnUserListClose").GetComponent<Button> ();
		btnUserListClose.GetComponent<Image> ().sprite = uiTheme.closeButton;
		btnUserListClose.onClick.AddListener (() => {
			userListPanel.SetActive (false);
			menuPanel.SetActive (true);
		});

		btnInvite = GameObject.Find ("SendBirdUnity/UIPanel/UserListPanel/BtnInvite").GetComponent<Button> ();
		btnInvite.GetComponent<Image> ().sprite = uiTheme.sendButton;
		btnInvite.onClick.AddListener (() => {
			if(mUserList.Count <= 0)
			{
				return;
			}

			userListPanel.SetActive (false);
			groupChannelPanel.SetActive (true);

			GroupChannel.CreateChannelWithUserIds(mUserList, false, (channel, e) => {
				if(e != null)
				{
					Debug.Log(e.Code + ": " + e.Message);
					return;
				}

				currentChannel = channel;
				ResetGroupChannelContent();
				txtGroupChannelTitle.text = GetDisplayMemberNames(channel.Members);
			});
		});

		#endregion

		#region GroupChannelList

		groupChannelListPanel = GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelListPanel");
		groupChannelListPanel.GetComponent<Image> ().sprite = uiTheme.channelListFrameBG;

		groupChannelListGridPanel = GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelListPanel/ScrollArea/GridPanel");

		GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelListPanel/TxtTitle").GetComponent<Text> ().color = uiTheme.titleColor;

		var groupChannelListScrollbar = GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelListPanel/Scrollbar").GetComponent<Scrollbar> ();
		cb = groupChannelListScrollbar.colors;
		cb.normalColor = uiTheme.scrollBarColor;
		cb.pressedColor = uiTheme.scrollBarColor;
		cb.highlightedColor = uiTheme.scrollBarColor;
		groupChannelListScrollbar.colors = cb;
		groupChannelListScrollbar.onValueChanged.AddListener ((float value) => {
			if (value <= 0) {
				LoadGroupChannels();
			}
		});

		btnGroupChannelClose = GameObject.Find ("SendBirdUnity/UIPanel/GroupChannelListPanel/BtnGroupChannelListClose").GetComponent<Button> ();
		btnGroupChannelClose.GetComponent<Image> ().sprite = uiTheme.closeButton;
		btnGroupChannelClose.onClick.AddListener (() => {
			groupChannelListPanel.SetActive (false);
			if (!groupChannelListPanel.activeSelf) {
				menuPanel.SetActive (true);
			}
		});

		#endregion

		uiPanel.SetActive (true);
		menuPanel.SetActive (true);
		openChannelListPanel.SetActive (false);
		openChannelPanel.SetActive (false);
		groupChannelPanel.SetActive (false);
		userListPanel.SetActive (false);
		groupChannelListPanel.SetActive (false);
	}

	string UserMessageRichText (UserMessage message)
	{
		return "<color=#" + SendBirdTheme.ToHex (uiTheme.senderColor) + ">" + message.Sender.Nickname + ": </color>" + message.Message;
	}

	string FileMessageRichText (FileMessage message)
	{
		return "<color=#" + SendBirdTheme.ToHex (uiTheme.senderColor) + ">" + message.Sender.Nickname + ": </color>" + message.Name;
	}

	string AdminMessageRichText (AdminMessage message)
	{
		return "<color=#" + SendBirdTheme.ToHex (uiTheme.systemMessageColor) + ">" + message.Message + "</color>";
	}

	void ScrollToBottom ()
	{
		openChannelScrollbar.value = 0;
		groupChannelScrollbar.value = 0;
	}

	void SubmitOpenChannel ()
	{
		if (inputOpenChannel.text.Length > 0) {
			if (currentChannel != null && currentChannel.IsOpenChannel ()) {
				currentChannel.SendUserMessage (inputOpenChannel.text, (message, e) => {
					if(e != null)
					{
						Debug.Log(e.Code + ": " + e.Message);
						return;
					}

					txtOpenChannelContent.text = txtOpenChannelContent.text + (UserMessageRichText (message) + "\n");
					ScrollToBottom();
				});
				inputOpenChannel.text = "";
			}
		}
	}

	public void XferSend3DMsg()
	{
		print ("Entered Xfer Message\n");
		string text;
		text = "3dObject";
		text = text + "," + XMLSerializer.Serialize(xferMsg);
		print("Text: " + text + " Send obj Position X: " + xferMsg.transformX);
		if (currentChannel != null) {
			currentChannel.SendUserMessage (text, (message, e) => {
				if (e != null) {
					Debug.Log (e.Code + ": " + e.Message);
					return;
				}

				if(currentChannel.IsOpenChannel())
					txtOpenChannelContent.text = txtOpenChannelContent.text + (UserMessageRichText ((UserMessage)message) + "\n");
				else {
					txtGroupChannelContent.text = txtGroupChannelContent.text + (UserMessageRichText ((UserMessage)message) + "\n");
				}
				ScrollToBottom ();
			});
		}
	}

	public void Send3DButtonClick()
	{
		//SceneManager.LoadScene ("www", LoadSceneMode.Additive);
		//SceneManager.Load
		//SceneManager.Ac
		//SceneManager.UnloadSceneAsync();
		//Scene active = SceneManager.GetSceneAt (0);
		//SceneManager.SetActiveScene (active);
	}

	void SubmitGroupChannel ()
	{
		if (inputGroupChannel.text.Length > 0) {
			if (currentChannel != null && currentChannel.IsGroupChannel ()) {
				// GameObject cube;
				// string text = cube.transform.position.ToString();
				currentChannel.SendUserMessage (inputGroupChannel.text, (message, e) => {
					if (e != null) {
						Debug.Log (e.Code + ": " + e.Message);
						return;
					}

					txtGroupChannelContent.text = txtGroupChannelContent.text + (UserMessageRichText (message) + "\n");
					ScrollToBottom();
				});
				inputGroupChannel.text = "";
			}
		}
	}

	#region helpers

	private string GetDisplayMemberNames (List<User> members)
	{
		if (members.Count < 2) {
			return "No Members";
		} else if (members.Count == 2) {
			StringBuilder names = new StringBuilder ();
			foreach (var member in members) {
				if (member.UserId.Equals (SendBirdClient.CurrentUser.UserId)) {
					continue;
				}

				names.Append (", " + member.Nickname);
			}
			return (string)names.Remove (0, 2).ToString ();
			;
		} else {
			return "Group " + members.Count;
		}
	}

	#endregion




	public GameObject prefabObject;
	// Distance in Meters
	public int distanceFromCamera = 1;

	// Location Manager 
	private GameObject origLocation;
	private LocationManager locationManager;

	private HashSet<string> m_Clones;
	private Hashtable m_messages;

	private Message testMsg;
	private string testId;
	private bool recreateFlag = true;

	private float m_TimeUntilRemove = 5.0f;
	private float m_TimeUntilRecreate = 5.0f;

	void  Awake() {
		SendBirdClient.SetupUnityDispatcher (gameObject);
		StartCoroutine (SendBirdClient.StartUnityDispatcher);

		SendBirdClient.Init ("44153C8C-8CEB-468C-AAA3-1278F203B60F"); // SendBird Sample Application ID
		SendBirdClient.Log += (message) => {
			Debug.Log (message);
		};

		UnityARSessionNativeInterface.ARUserAnchorAddedEvent += AddAnchor;
		UnityARSessionNativeInterface.ARUserAnchorRemovedEvent += RemoveAnchor;
		m_Clones = new HashSet<string>();
		m_messages = new Hashtable();
		origLocation = GameObject.Find("OriginalLocation");
		locationManager = origLocation.GetComponent<LocationManager>();
	}

	public void AddMessage(GameObject obj, string anchorId) {
		Message message = new Message();

		message.m_object = obj;
		message.m_position = obj.transform.position;
		message.m_rotation = obj.transform.rotation;
		// calculate obj world position
		message.m_worldPosition = LocationHelper.calculateWorldPosition(locationManager.getOrigWorldPosition(), obj.transform.position);

		Console.WriteLine("Calculated Target Location: X: " + message.m_worldPosition.x + " Y: " + message.m_worldPosition.y + 
			" Z: " + message.m_worldPosition.z);


		print ("in add messge function\n");
		//GameObject manager = GameObject.Find ("SendbirdUnity");
		//SendBirdUI birdui = manager.GetComponent<SendBirdUI> ();
		//String testMsg = birdui.xferMsg
		xferMsg.transformX = message.m_worldPosition.x;
		xferMsg.transformY = message.m_worldPosition.y;
		xferMsg.transformZ = message.m_worldPosition.z;
		print ("in add messge function between initialization\n");
		xferMsg.rotationX = message.m_rotation.x;
		xferMsg.rotationY = message.m_rotation.y;
		xferMsg.rotationZ = message.m_rotation.z;
		xferMsg.rotationW = message.m_rotation.w;
		flagNew3DMsg = true;
		print ("in add messge function after initialization\n");
		XferSend3DMsg ();

		m_messages.Add(anchorId, message);
		m_Clones.Add(anchorId);
	}

	public void AddAnchor(ARUserAnchor anchor)
	{
		if (m_Clones.Contains(anchor.identifier))
		{
			Console.WriteLine("Our anchor was added!");
		}
	}

	public void RemoveAnchor(ARUserAnchor anchor)
	{
		if (m_Clones.Contains(anchor.identifier))
		{
			m_Clones.Remove(anchor.identifier);
			Console.WriteLine("AnchorRemovedExample: " + anchor.identifier);
		}
	}

	public void RemoveMessage(string anchorId) {
		if (m_Clones.Contains(anchorId))
		{
			Console.WriteLine("Removing anchor with id: " + anchorId);
			m_messages.Remove(anchorId);
			UnityARSessionNativeInterface.GetARSessionNativeInterface().RemoveUserAnchor(anchorId);
		}
	}
				

	// Update is called once per frame
	void Update () {
		if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
		{
			GameObject clone = Instantiate(prefabObject, Camera.main.transform.position + (this.distanceFromCamera * Camera.main.transform.forward), Quaternion.identity);
			UnityARUserAnchorComponent component = clone.GetComponent<UnityARUserAnchorComponent>();
			AddMessage(clone, component.AnchorId);
			// delete this if you don't want to remove object
			testId = component.AnchorId;
			testMsg = (Message)m_messages[testId];
			m_TimeUntilRemove = 5f;
		}

		// delete this also
		/*m_TimeUntilRemove -= Time.deltaTime;
		if (m_TimeUntilRemove <= 0.0f)
		{
			if (m_Clones.Contains(testId))
			{
				RemoveMessage(testId);
				m_TimeUntilRecreate = 5f;
			}

			m_TimeUntilRecreate -= Time.deltaTime;
			if (m_TimeUntilRecreate <= 0.0f && recreateFlag) {
				Vector3 targetPosition = LocationHelper.calculatePosition(locationManager.getOrigWorldPosition(), testMsg.m_worldPosition);
				Console.WriteLine("Recalculated Target Location: X: " + targetPosition.x + " Y: " + targetPosition.y + 
					" Z: " + targetPosition.z);
				GameObject recreateObj = Instantiate(prefabObject, targetPosition, testMsg.m_rotation);
				UnityARUserAnchorComponent component = recreateObj.GetComponent<UnityARUserAnchorComponent>();
				m_Clones.Add(component.AnchorId);
				recreateFlag = false;
			}
		}*/
	}
}

