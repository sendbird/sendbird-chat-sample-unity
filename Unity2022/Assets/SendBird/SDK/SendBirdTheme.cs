using UnityEngine;

using System.Collections;

public class SendBirdTheme : MonoBehaviour {
	public Sprite chatFrameBG;
	public Sprite channelListFrameBG;

	public Sprite sendButton;
	public Color32 sendButtonColor = Color.white;

	public Sprite inputTextBG;
	public Color32 inputTextColor = Color.white;
	public Color32 inputTextPlaceholderColor = Color.gray;

	public Sprite closeButton;

	public Sprite chatChannelButtonOn;
	public Color32 chatChannelButtonOnColor =  ToColor (0xfff158);

	public Sprite chatChannelButtonOff;
	public Color32 chatChannelButtonOffColor = ToColor (0xd29828);

	public Sprite channelButtonOn;
	public Color32 channelButtonOnColor = Color.white;

	public Sprite channelButtonOff;
	public Color32 channelButtonOffColor = ToColor(0xd29828);

	public Color32 messageColor = ToColor (0xfff6d7);
	public Color32 systemMessageColor = ToColor (0xe7e530);
	public Color32 senderColor = ToColor (0xe19400);

	public Color32 titleColor = ToColor(0x54392a);

	public Color32 scrollBarColor = ToColor (0xffffff);

	public static string ToHex(Color32 color)
	{
		string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
		return hex;
	}

	public static Color32 ToColor(int HexVal)
	{
		byte R = (byte)((HexVal >> 16) & 0xFF);
		byte G = (byte)((HexVal >> 8) & 0xFF);
		byte B = (byte)((HexVal) & 0xFF);
		return new Color32(R, G, B, 255);
	}
}
