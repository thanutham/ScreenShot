using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.IO;
using System.Text;

public class ReaderMainMenu : MonoBehaviour {
	private string pathConfig, pathUpdateImage;
	public GameObject ScreenShotImage;
	public GameObject BackgroudImage;

	RectTransform rectTranScreenShot, rectTranBackground;
	private String url;

	private float ScaleImage;

	GameObject[] gameObj;
	Texture2D[] textList;

	string[] files;
	private bool isLog;

	private void Start(){
		pathConfig = Application.persistentDataPath; 

		for (int i = pathConfig.Length - 1; i > 0; i--) {
			if (pathConfig [i] == '/') {
				pathConfig = pathConfig.Remove(i+1);
				break;
			}
		}

		pathUpdateImage = pathConfig;

		pathUpdateImage += "creen Saver/UpdateImage.txt";

		pathConfig += "Screen Saver/Config.txt";

		LoadFileImage ();

		if (File.Exists (pathUpdateImage)) 
			File.Delete (pathUpdateImage);

		ScaleImage = 1f;

		rectTranScreenShot = ScreenShotImage.GetComponent<Image> ().rectTransform;
	}

	private void Update()
	{
		if (File.Exists (pathUpdateImage)) {
			LoadFileImage ();
			File.Delete (pathUpdateImage);
		}
			
		if (Input.GetKeyDown (KeyCode.KeypadPlus)||Input.GetKeyDown (KeyCode.Plus)) {
			if (ScaleImage < 1f) {
				ScaleImage += 0.05f;
				rectTranScreenShot.localScale = new Vector2 (ScaleImage, ScaleImage);
			}
		}

		if (Input.GetKeyDown (KeyCode.KeypadMinus) || Input.GetKeyDown (KeyCode.Minus)) {
			if (ScaleImage > 0f) {
				ScaleImage -= 0.05f;
				rectTranScreenShot.localScale = new Vector2 (ScaleImage,ScaleImage);
			}
		}

		if (Input.GetKeyDown (KeyCode.Space)) {
			if (Screen.fullScreenMode == FullScreenMode.Windowed)
				Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
			else
				Screen.fullScreenMode = FullScreenMode.Windowed;
		}
	}

	private void LoadFileImage(){
		string readPathFromText = File.ReadAllText(pathConfig);

		url = "file://" + readPathFromText;

		StartCoroutine(LoadImage(url));
	}

	IEnumerator LoadImage(String LoadUrl)
	{
		using (WWW www = new WWW(LoadUrl))
		{
			Texture2D temp = new Texture2D(0,0);

			yield return www;
		
			temp = www.texture;
			Sprite sprite = Sprite.Create(temp, new Rect(0,0,temp.width, temp.height), new Vector2(0.5f,0.5f));
			ScreenShotImage.GetComponent<Image>().sprite = sprite;
		}
	}
		
}

