﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Linq;

public class MessageDialog : DialogBase {

	//自動で禁則処理を適用するテキストフィールド
	[SerializeField] Text textField;
	[SerializeField] Button btnOK;
	[SerializeField] Button btnCancel;

	const string prehabPath = "Prehabs/Dialogs/MessageDialog";
	Action onOK;
	Action onCancel;

	/// <summary>
	/// メッセージダイアログを表示します。
	/// </summary>
	/// <param name="message">表示するメッセージ</param>
	/// <param name="useOK">OKボタンを使用するかどうか</param>
	/// <param name="useCancel">Cancelボタンを使用するかどうか</param>
	/// <param name="onOK">OKボタンを押した際に実行されるコールバック</param>
	/// <param name="onCancel">Cancelボタンを押した際に実行されるコールバック</param>
	/// <param name="positiveItemName">デフォルトでOKとなってるボタンの文言を任意の名前に設定</param>
	/// <param name="negativeItemName">デフォルトでCancelとなってるボタンの文言を任意の名前に設定</param>
	/// <param name="isUseHyphenation">禁則処理を適用するか</param></param></param>
	public static void Show (string message, bool useOK, bool useCancel, Action onOK = null, Action onCancel = null, string positiveItemName = "OK", string negativeItemName = "キャンセル") {
		GameObject prehab = CreateDialog (prehabPath);
		//コンテンツ設定
		MessageDialog dialog = prehab.GetComponent <MessageDialog> ();
		dialog.Init (message, useOK, useCancel, onOK, onCancel, positiveItemName, negativeItemName);
		dialogObj = prehab;
	}

	//初期化
	void Init (string message, bool useOK, bool useCancel, Action onOK, Action onCancel, string positiveItemName, string negativeItemName) {
		textField.text = message;
		btnOK.gameObject.SetActive (useOK);
		btnCancel.gameObject.SetActive (useCancel);
		this.onOK = onOK;
		this.onCancel = onCancel;
		//ボタンの文言設定
		Text okButtonText = this.btnOK.GetComponentInChildren<Text> ();			//OKボタンの表示テキスト(デフォルトOK)
		Text cancelButtonText = this.btnCancel.GetComponentInChildren<Text> ();	//Cancelボタンの表示テキスト(デフォルトCancel)
		okButtonText.text = positiveItemName;
		cancelButtonText.text = negativeItemName;
	}

	//「OK」ボタンが押されたときに呼び出される
	public void OnOKButtonTap () {
		Debug.Log ("OK");
		if (onOK != null)
			onOK ();
		Dismiss ();
	}

	//「Cancel」ボタンが押されたときに呼び出され鵜
	public void OnCancelButtonTap () {
		Debug.Log ("Cancel");
		if (onCancel != null)
			onCancel ();
		Dismiss ();
	}
}
