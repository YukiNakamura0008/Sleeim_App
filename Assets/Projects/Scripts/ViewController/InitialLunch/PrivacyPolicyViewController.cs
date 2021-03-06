﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrivacyPolicyViewController : ViewControllerBase {

	[SerializeField]GameObject prehab;
	[SerializeField]string URL;

	protected override void Start () {
		base.Start ();
		prehab.GetComponent<PopUpWebView> ().Url = URL;
	}

	public override SceneTransitionManager.LoadScene SceneTag {
		get {
			return SceneTransitionManager.LoadScene.PrivacyPolicy;
		}
	}

	//戻るボタンをタップした際に呼び出される
	public void OnBackButtonTap () {
		SceneTransitionManager.LoadLevel (SceneTransitionManager.LoadScene.InitialLunch);
	}
}
