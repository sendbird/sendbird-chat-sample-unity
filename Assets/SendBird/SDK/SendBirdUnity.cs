using UnityEngine;
using SendBird;

public class SendBirdUnity : MonoBehaviour
{
	void Awake ()
	{
		SendBirdClient.SetupUnityDispatcher (gameObject);
		StartCoroutine (SendBirdClient.StartUnityDispatcher);

		SendBirdClient.Init ("A7A2672C-AD11-11E4-8DAA-0A18B21C2D82"); // SendBird Sample Application ID
		SendBirdClient.Log += (message) => {
			Debug.Log (message);
		};
	}
}
