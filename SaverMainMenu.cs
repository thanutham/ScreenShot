using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class SaverMainMenu : MonoBehaviour {
	//[DllImport("user32.dll")]
	//public static extern void OpenFileDialog ();
/*
	[DllImport("shell32.dll", ExactSpelling=true)]
	public static extern void ILFree(IntPtr pidlList);

	[DllImport("shell32.dll", CharSet=CharSet.Unicode, ExactSpelling=true)]
	public static extern IntPtr ILCreateFromPathW(string pszPath);

	[DllImport("shell32.dll", SetLastError = true)]
	public static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, uint cidl, [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl, uint dwFlags);

	[DllImport("shell32.dll", SetLastError = true)]
	public static extern void SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr bindingContext, [Out] out IntPtr pidl, uint sfgaoIn, [Out] out uint psfgaoOut);
*/
	private string pathConfig, pathSavePicture, pathUpdateImage;

	private float vBrightness, vSaturation, vContrast;

	enum AdjustType{ Brightness = 0, Saturation, Contrast }

	private AdjustType adjustType;

	[STAThread]

	private void Start(){
		pathConfig = Application.persistentDataPath + "/Config.txt"; 
		pathUpdateImage = Application.persistentDataPath + "/UpdateImage.txt";
		pathSavePicture = Application.dataPath; //default SaveFile

		adjustType = AdjustType.Brightness;
		vBrightness = 0f;// Brightness[-1 - 1]
		vSaturation = 0f;// Saturation[0 - 1]
		vContrast = 0f;// Contrast[-1 - 1]
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))
		{
			StartCoroutine(CaptureScreenShot ());
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			string path = pathSavePicture;


			#if UNITY_EDITOR
				path = EditorUtility.OpenFolderPanel("Selected Folder", pathSavePicture, "Screenshot.png");
			#endif

			#if UNITY_STANDALONE_WIN
				SelectInFileExplorer(pathSavePicture);
				//OpenFolderAndSelectItem(pathSavePicture,"Screenshot.png");
				//var p = StandaloneFileBrowser.OpenFilePanel("Open File", "", "", false);
				//ShowFolder (pathSavePicture);
				//OpenFileDialog ();
			#endif

			pathSavePicture = path;

		}

		if (Input.GetKeyDown (KeyCode.F1)) adjustType = AdjustType.Brightness;

		if (Input.GetKeyDown (KeyCode.F2)) adjustType = AdjustType.Saturation;

		if (Input.GetKeyDown (KeyCode.F3)) adjustType = AdjustType.Contrast;

		if (Input.GetKeyDown (KeyCode.Plus) || Input.GetKeyDown (KeyCode.KeypadPlus)) {
			if (adjustType == AdjustType.Brightness) {
				if (vBrightness < 1f)
					vBrightness += 0.2f;
			} else if (adjustType == AdjustType.Contrast) {
				if (vContrast < 1)
					vContrast += 0.2f;
			}else if (adjustType == AdjustType.Saturation) {
				if (vSaturation < 1)
					vSaturation += 0.2f;
			}
		}

		if (Input.GetKeyDown (KeyCode.Minus) || Input.GetKeyDown (KeyCode.KeypadMinus)) {
			if (adjustType == AdjustType.Brightness) {
				if (vBrightness > -1f)
					vBrightness -= 0.2f;
			} else if (adjustType == AdjustType.Contrast) {
				if (vContrast > 0)
					vContrast -= 0.2f;
			} else if (adjustType == AdjustType.Saturation) {
				if (vSaturation > 0)
					vSaturation -= 0.2f;
			}
		}
	}

	private static void ShowFolder(string path)
	{
		path = path.Replace("/",@"\");
		System.Diagnostics.Process.Start ("explorer.exe", "/select" + path);
	}

	private void SelectInFileExplorer(string fullPath)
	{
		if (string.IsNullOrEmpty(fullPath))
			throw new ArgumentNullException("fullPath");

		fullPath = Path.GetFullPath(fullPath);

		IntPtr pidlList = NativeMethods.ILCreateFromPathW(fullPath);
		if (pidlList != IntPtr.Zero)
			try
		{
			// Open parent folder and select item
			Marshal.ThrowExceptionForHR(NativeMethods.SHOpenFolderAndSelectItems(pidlList, 0, IntPtr.Zero, 0));
		}
		finally
		{
			NativeMethods.ILFree(pidlList);
		}
	}

	IEnumerator CaptureScreenShot(){
		yield return new WaitForEndOfFrame();

		print("Save ScreenShot");
		print (Application.dataPath);
	
		Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, true);
		screenshot.ReadPixels(new Rect(0,0,Screen.width, Screen.height), 0, 0);
		screenshot.Apply();

		Texture2D newScreenshot = ScaleTexture(screenshot, 2048,2048);

		//Brightness
		newScreenshot = AdjustBrightness(newScreenshot,vBrightness);
		//Saturation
		newScreenshot = AdjustSaturation (newScreenshot, vSaturation);
		//Contrast
		newScreenshot = AdjustContrast (newScreenshot, vContrast);


		var bytes = newScreenshot.EncodeToPNG();
		File.WriteAllBytes (pathSavePicture + "/Screenshot.png", bytes);

		string createText = pathSavePicture +"/Screenshot.png"/* + Environment.NewLine*/;
		File.WriteAllText(pathConfig, createText);
		File.WriteAllText (pathUpdateImage, "");
	}

	private Texture2D ScaleTexture(Texture2D source,int targetWidth,int targetHeight) {
		Texture2D result = new Texture2D(targetWidth,targetHeight,source.format,true);
		Color[] rpixels = result.GetPixels(0);

		float incX = ((float)1/source.width)*((float)source.width/targetWidth);
		float incY = ((float)1/source.height)*((float)source.height/targetHeight);

		for(int px = 0; px < rpixels.Length; px++) {
			rpixels[px] = source.GetPixelBilinear(incX*((float)px%targetWidth),
				incY*((float)Mathf.Floor(px/targetWidth)));
		}
		result.SetPixels(rpixels,0);
		result.Apply();
		return result;
	}

	private Texture2D AdjustBrightness(Texture2D source, float brightness)
	{
		print (brightness);
		Color[] rpixels = source.GetPixels(0);

		float bn = (brightness / 8) * 5;

		for(int px = 0; px < rpixels.Length; px++) {
			float npxR = rpixels [px].r + bn;
			float npxB = rpixels [px].b + bn;
			float npxG = rpixels [px].g + bn;

			if (npxR > 1f) npxR = 1f; if (npxR < 0f) npxR = 0f;
			if (npxB > 1f) npxB = 1f; if (npxB < 0f) npxB = 0f;
			if (npxG > 1f) npxG = 1f; if (npxB < 0f) npxB = 0f;

			rpixels [px].r =  npxR;
			rpixels [px].b =  npxB;
			rpixels [px].g =  npxG;
			
		}
		source.SetPixels(rpixels,0);
		source.Apply();
		return source;
	}

	private Texture2D AdjustContrast(Texture2D source, float contrast)
	{
		contrast *= 255;
		float factor = (259 * (contrast + 255)) / (255 * (259 - contrast));

		Color[] rpixels = source.GetPixels(0);

		for(int px = 0; px < rpixels.Length; px++) {
			//Red
			float npxR = rpixels [px].r * 255; 
			npxR -= 128;
			npxR *= factor;
			npxR += 128;
			npxR /= 255;
			//Green
			float npxG = rpixels [px].g * 255;
			npxG -= 128;
			npxG *= factor;
			npxG += 128;
			npxG /= 255;
			//Blue
			float npxB = rpixels [px].b * 255;
			npxB -= 128;
			npxB *= factor;
			npxB += 128;
			npxB /= 255;


			if (npxR > 1f) npxR = 1f; if (npxR < 0f) npxR = 0f;
			if (npxB > 1f) npxB = 1f; if (npxB < 0f) npxB = 0f;
			if (npxG > 1f) npxG = 1f; if (npxB < 0f) npxB = 0f;

			rpixels [px].r =  npxR;
			rpixels [px].b =  npxB;
			rpixels [px].g =  npxG;
		}
		source.SetPixels(rpixels,0);
		source.Apply();
		return source;
	}

	private Texture2D AdjustSaturation(Texture2D source, float saturation)
	{
		Color[] rpixels = source.GetPixels(0);

		for (int px = 0; px < rpixels.Length; px++) {
			float r = rpixels [px].r;
			float g = rpixels [px].g;
			float b = rpixels [px].b;

			float h, s, v;
			Color.RGBToHSV(new Color (r,g,b),out h, out s, out v);

			//Saturation [0-1]
			s += saturation;

			if (s > 1f)
				s = 1f;
			else if (s < 0)
				s = 0;

			Color newpixel = Color.HSVToRGB (h, s, v);
			rpixels [px] = newpixel;
		}
		return source;
	}

	/*
	public static void OpenFolderAndSelectItem(string folderPath, string file)
	{
		IntPtr nativeFolder;
		uint psfgaoOut;
		SHParseDisplayName(folderPath, IntPtr.Zero, out nativeFolder, 0, out psfgaoOut);

		if (nativeFolder == IntPtr.Zero)
		{
			// Log error, can't find folder
			return;
		}

		IntPtr nativeFile;
		SHParseDisplayName(Path.Combine(folderPath, file), IntPtr.Zero, out nativeFile, 0, out psfgaoOut);

		IntPtr[] fileArray;
		if (nativeFile == IntPtr.Zero)
		{
			// Open the folder without the file selected if we can't find the file
			fileArray = new IntPtr[0];
		}
		else
		{
			fileArray = new IntPtr[] { nativeFile };
		}

		SHOpenFolderAndSelectItems(nativeFolder, (uint)fileArray.Length, fileArray, 0);

		Marshal.FreeCoTaskMem(nativeFolder);
		if (nativeFile != IntPtr.Zero)
		{
			Marshal.FreeCoTaskMem(nativeFile);
		}
			
	}
*/

}

static class NativeMethods
{

	[DllImport("shell32.dll", ExactSpelling=true)]
	public static extern void ILFree(IntPtr pidlList);

	[DllImport("shell32.dll", CharSet=CharSet.Unicode, ExactSpelling=true)]
	public static extern IntPtr ILCreateFromPathW(string pszPath);

	[DllImport("shell32.dll", ExactSpelling=true)]
	public static extern int SHOpenFolderAndSelectItems(IntPtr pidlList, uint cild, IntPtr children, uint dwFlags);
}



