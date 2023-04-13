using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditPopup : MonoBehaviour {

	public InputField prvMessage;
	public Text editMessage;
	public Button btnEditSend;
	public Button btnCancel;
	public SendBirdUI sendBirdUI;

	void Start()
	{
		btnEditSend.onClick.AddListener(() =>
		{
			sendBirdUI.UpdateMessageSend(editMessage.text);
			gameObject.SetActive(false);
		});

		btnCancel.onClick.AddListener(() =>
		{
			gameObject.SetActive(false);
		});
	}

	void OnEnable()
	{
		prvMessage.text = sendBirdUI.editMessage;
	}
}
