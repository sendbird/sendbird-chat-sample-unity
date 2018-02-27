using UnityEngine;
using SendBird;

public class SendBirdUnity : MonoBehaviour
{
	void Awake ()
	{
		SendBirdClient.SetupUnityDispatcher (gameObject);
		StartCoroutine (SendBirdClient.StartUnityDispatcher);

		SendBirdClient.Init ("9DA1B1F4-0BE6-4DA8-82C5-2E81DAB56F23"); // SendBird Sample Application ID
		SendBirdClient.Log += (message) => {
			Debug.Log (message);
		};
	}
}
