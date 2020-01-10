using System.Collections;
using UnityEngine;
using System;

public class MyDatabase {

	// 唯一のインスタンス
	private static MyDatabase sInstance;

	// データベースアクセスクラス
	private SqliteDatabase mDb;

	// データベースファイル名
	private static readonly string DB_NAME = "database.db";

	// バックアップデータベースファイル名
	private static readonly string BACKUP_DB_NAME = "backup.db";

	// データベースバージョンファイル名
	private static readonly string DB_VERSION_FILE_NAME = "database_ver.txt";

	/// <summary>
	/// インスタンス取得プロパティ
	/// </summary>
	public static MyDatabase Instance {
		get {
			if (sInstance == null) {
				sInstance = new MyDatabase();
			}
			return sInstance;
		}
	}

	/// <summary>
	/// 外部から実行されないコンストラクタ
	/// </summary>
	private MyDatabase() {
	}

	/// <summary>
	/// データベース初期化
	/// </summary>
	/// <param name="mono">呼び出し元インスタンス</param>
	public static IEnumerator Init(MonoBehaviour mono) {
		MyDatabase db = MyDatabase.Instance;
		yield return mono.StartCoroutine(GetNewDbVersion(DB_VERSION_FILE_NAME));
	}

	/// <summary>
	/// 最新のDBバージョンを取得する
	/// </summary>
	/// <param name="fileName">ファイル名</param>
	/// <returns>DBバージョン</returns>
	private static IEnumerator GetNewDbVersion(string fileName) {
		string ret;
		string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
		if (filePath.Contains("://")) {
			WWW www = new WWW(filePath);
			yield return www;
			ret = www.text;
		} else {
			ret = System.IO.File.ReadAllText(filePath);
		}
		MyDatabase.Instance.OnGetNewDbVersion(int.Parse(ret));
	}

	/// <summary>
	/// 最新のDBバージョン取得完了通知
	/// </summary>
	/// <param name="newDbVersion">最新のDBバージョン</param>
	private void OnGetNewDbVersion(int newDbVersion) {
		Debug.Log("MyDatabase start");
		// DB更新フラグ
		bool isDbUpdate = false;
		// DBバージョン更新フラグ
		bool isDbVersionUpdate = false;
		// DBバージョン
		int oldDbVersion = UserData.DatabaseVersion;
		// DB保存先パス
		string dbPath = System.IO.Path.Combine(Application.persistentDataPath, DB_NAME);

		// DB更新確認
		checkDbUpdate(dbPath, oldDbVersion, newDbVersion, ref isDbUpdate, ref isDbVersionUpdate);

		if (isDbUpdate) {
			Debug.Log("MyDatabase create backup data");
			// DBファイルの更新が必要な場合、古いDBファイルを別名で保存する
			string backupPath = System.IO.Path.Combine(Application.persistentDataPath, BACKUP_DB_NAME);
			System.IO.File.Copy(dbPath, backupPath, true);
		}

		// DBを読み込む
		mDb = new SqliteDatabase(DB_NAME, isDbUpdate || isDbVersionUpdate);

		// テーブルを生成する
		CreateDbTables();

		if (isDbUpdate) {
			Debug.Log("MyDatabase marge backup data");
			// 更新フラグが立っている時、古いDBから必要なデータを新しいDBに移行する
			SqliteDatabase backupDb = new SqliteDatabase(BACKUP_DB_NAME, false);
			// 各テーブルごとにマージ処理を行なう
			MargeData(ref backupDb);
		}

		if (isDbVersionUpdate) {
			Debug.Log("MyDatabase update db version " + oldDbVersion + " > " + newDbVersion);
			// DBバージョンを更新する
			UserData.DatabaseVersion = newDbVersion;
			UserData.Save();
		}
		Debug.Log("MyDatabase end");
	}

	/// <summary>
	/// DB更新確認
	/// </summary>
	/// <param name="dbPath">DBファイルパス</param>
	/// <param name="oldDbVersion">現在のバージョン</param>
	/// <param name="newDbVersion">新バージョン</param>
	/// <param name="isDbUpdate">DB更新フラグ</param>
	/// <param name="isDbVersionUpdate">DBバージョン更新フラグ</param>
	private static void checkDbUpdate(string dbPath, int oldDbVersion, int newDbVersion, ref bool isDbUpdate, ref bool isDbVersionUpdate) {
		Debug.Log("MyDatabase#checkDbUpdate start");
		if (System.IO.File.Exists(dbPath)) {
			Debug.Log("MyDatabase#checkDbUpdate file exists");
			// DBファイルが存在する場合(2回目以降の起動)
			// DBバージョンが更新されている場合と、DBファイルのタイムスタンプが更新されている場合、2つの可能性をチェックする

			if (oldDbVersion != newDbVersion) {
				Debug.Log("MyDatabase#checkDbUpdate update db version");
				// 定義してあるバージョンとユーザーデータに保持しているバージョンが異なれば更新フラグを立てる
				isDbUpdate = true;
				isDbVersionUpdate = true;
			}

			string srcDbPath = System.IO.Path.Combine(Application.streamingAssetsPath, DB_NAME);
			if (System.IO.File.GetLastWriteTimeUtc(srcDbPath) > System.IO.File.GetLastWriteTimeUtc(dbPath)) {
				Debug.Log("MyDatabase#checkDbUpdate update db timestamp");
				// DBファイルが存在し、かつタイムスタンプが更新されている場合、更新フラグを立てる
				isDbUpdate = true;
			}
		} else {
			Debug.Log("MyDatabase#checkDbUpdate file not exists");
			// DBファイルが存在しない場合(初回起動)
			isDbVersionUpdate = true;
		}
		Debug.Log("MyDatabase#checkDbUpdate isDbUpdate=" + isDbUpdate + ", isDbVersionUpdate=" + isDbVersionUpdate);
		Debug.Log("MyDatabase#checkDbUpdate end");
	}

	// ここから下がどんどん増えていく
	private SleepTable mSleepTable;

	private void CreateDbTables() {
		mSleepTable = new SleepTable (ref mDb);
	}

	private void MargeData(ref SqliteDatabase oldDb) {
		mSleepTable.MargeData (ref oldDb);
	}

	public SleepTable GetSleepTable () {
		Debug.Log ("fee");
		Debug.Log ("SleepTable:" + mSleepTable);
		Debug.Log ("DB:" + mDb);
		return mSleepTable;
	}
}
