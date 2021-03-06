﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Kaimin.Managers;
using MiniJSON;
using System.Linq;
using Asyncoroutine;

public class BTConnectViewController : ViewControllerBase
{

    public ListAdapter Adapter;
    public GameObject ListElementPrehab;
    public GameObject IndicatorListElementPrehab;

    /// <summary>
    /// デバイス起動状態
    ///
    /// デバイス状態がブートかアプリかで接続方法が異なる
    /// </summary>
    private DeviceActivationStatusType DeviceActivationStatus;

    Coroutine searchDeviceCoroutine = null;

    protected override void Start()
    {
        base.Start();
        SearchDevice();
        DeviceActivationStatus = UserDataManager.DeviceActivationStatus.Load();
    }

    public override SceneTransitionManager.LoadScene SceneTag
    {
        get
        {
            return SceneTransitionManager.LoadScene.BTConnect;
        }
    }

    public override bool IsAllowSceneBack()
    {
        //初回起動時に表示される接続画面ではバックボタンを使えないようにする
        if (DialogBase.IsDisp())
        {
            //ダイアログ表示中であれば、戻れない
            return false;
        }
        else
        {
            return true;
        }
    }

    //「検索」ボタンが押されたときに実行される
    public void OnDetectButtonTap()
    {
        SearchDevice();
    }

    void SearchDevice()
    {
        //リストを初期化
        Adapter.ClearAllElement();
        //既にコルーチンが走ってたら終了処理をしてから新しく開始するようにする
        if (searchDeviceCoroutine != null)
        {
            //コルーチン停止
            StopCoroutine(searchDeviceCoroutine);
            //もし既にデバイスと接続済みであれば、検索結果が表示できるように切断を行ってから行う
            if (UserDataManager.State.isConnectingDevice())
                BluetoothManager.Instance.Disconnect();
            //初期化として検索停止しておく
            BluetoothManager.Instance.StopScanning();
        }
        searchDeviceCoroutine = StartCoroutine(SearchDeviceFlow());
    }

    //戻るボタンが押されたときに実行される
    public void OnBackButtonTap()
    {
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Setting);
    }

    //「スキップ」ボタンが押されたときに実行される
    public void OnSkipButtonTap()
    {
        //ダイアログを表示してホーム画面へ遷移
        StartCoroutine(SkipParering());
    }

    //ペアリングをスキップする
    IEnumerator SkipParering(Action<bool> onResponse = null)
    {
        //本当にスキップするか確認
        bool isOK = false;
        bool isCancel = false;
        MessageDialog.Show(
            "本体機器とのペアリングが完了していません。接続をスキップしますか？",
            true,
            true,
            () =>
            {
                isOK = true;
            },
            () =>
            {
                isCancel = true;
            });
        yield return new WaitUntil(() => isOK || isCancel);
        if (isOK)
        {
            //スキップするなら
            //検索を停止
            BluetoothManager.Instance.StopScanning();
            SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Home);
        }
        else
        {
            //スキップしないなら
        }
        if (onResponse != null)
            onResponse(isOK);
        yield return null;
    }

    //Bluetoothが非アクティブの際にアクティブ化するためのダイアログを表示
    IEnumerator BluetoothActivateCheck(Action<bool> onResponse = null)
    {
        bool isUpdate = false;
        bool isSkip = false;
        MessageDialog.Show(
            "<size=30>Bluetoothがオフになっています。\nSleeimと接続できるようにするには、\nBluetoothをオンにしてください。</size>",
            true,
            true,
            () => isUpdate = true,
            () => isSkip = true,
            "更新",
            "スキップ");
        yield return new WaitUntil(() => isUpdate || isSkip);
        Debug.Log("isUpdate:" + isUpdate + "," + "isSkip:" + isSkip);
        if (isUpdate)
        {
            //Bluetooth有効化リクエストを発行する。有効にするかのダイアログが表示される
            NativeManager.Instance.BluetoothRequest();
            bool isActivate = false;
            //ユーザの入力待ち
#if UNITY_ANDROID
            yield return new WaitUntil(() => NativeManager.Instance.PermissionCode != -1);
            isActivate = NativeManager.Instance.PermissionCode == 1;    //Bluetoohが有効にされたか
#elif UNITY_IOS
            isActivate = false;	//iOSの場合、ユーザーの選択が受け取れなかったため、拒否された前提で進める
#endif
            if (isActivate)
            {
                //Bluetoothが有効になったら
            }
            else
            {
                //Bluetoothが有効にされなかったら
                //ダイアログを閉じるだけ
            }
            if (onResponse != null)
                onResponse(isActivate);
            yield return null;
        }
        else
        {
            //ペアリングをスキップする
            bool isSkipParering = false;
            yield return StartCoroutine(SkipParering((bool skip) => isSkipParering = skip));
            if (isSkipParering)
            {
                //スキップするため、Bluetoothは有効にされなかった
                if (onResponse != null)
                    onResponse(false);
            }
            else
            {
                //スキップしないなら、戻る
                yield return StartCoroutine(BluetoothActivateCheck(onResponse));
            }
        }
    }

    //Bluetoothが有効になっているかどうか確認
    IEnumerator CheckBluetoothIsActive(Action<bool> onResponse)
    {
        NativeManager.Instance.Initialize();
        bool isActive = NativeManager.Instance.BluetoothValidCheck();
        if (isActive)
        {
            //Bluetoothが有効なら
            onResponse(true);
            yield return null;
        }
        else
        {
            //無効の場合は無効の旨を表示し、システム設定の変更を促す
            yield return StartCoroutine(BluetoothActivateCheck(onResponse));
        }
    }

    IEnumerator SearchDeviceFlow()
    {
        //Bluetoothが有効か確認
        bool isBluetoothActive = false;
        yield return StartCoroutine(CheckBluetoothIsActive((bool isActive) => isBluetoothActive = isActive));
        if (!isBluetoothActive)
        {
            //Bluetooth無効時
            yield break;
        }
        //もし既にデバイスと接続済みであれば、検索結果が表示できるように切断を行ってから行う
        if (UserDataManager.State.isConnectingDevice())
            BluetoothManager.Instance.Disconnect();
        //初期化として検索停止しておく
        BluetoothManager.Instance.StopScanning();
        //インジケーターをリストに追加する
        var indicatorObj = Instantiate(IndicatorListElementPrehab);
        Adapter.SetElementToList(indicatorObj);

        /// <summary>
        /// デバイスを検索する
        ///
        /// TODO: 複数デバイス検索は、仕様ではなく、試験的に実施しているため、お客様からの指示があり次第処理を修正する
        /// NOTE: whileループをやめれば、1台だけの検索になります
        /// </summary>
        List<string> listDeviceAdress = new List<string>();	//リストに追加したデバイスのアドレスリスト
        bool? isContinue = null;
        string receiveData = "";
#if UNITY_ANDROID
        // iODはデバイスアドレスが取得できないため、複数検知しない
        while (true)
        {
#endif
            isContinue = null;       // BLEデバイス検索結果判定に使用する
            BluetoothManager.Instance.ScanBleDevice(
                (string data) =>
                {
                    isContinue = false;      // デバイスが存在しない
                    receiveData = data;
                },
                (string data) =>
                {
                    //デバイス発見時
                    //発見したデバイス情報読み出し
                    var json = Json.Deserialize(data) as Dictionary<string, object>;
                    string deviceName = (string)json["KEY1"];
                    string deviceAdress = (string)json["KEY2"];
                    int deviceIndex = 0;    //iOSのみで使用するデバイス識別番号
#if UNITY_IOS
                    //どのデバイスと接続するか決定するデバイス識別番号を取得(iOSのみでしか取得できない)
                    deviceIndex = Convert.ToInt32(json["KEY3"]);
#endif

                    //既にリストに追加されてるデバイスは弾く
                    bool isExistList = listDeviceAdress.Where(adress => adress == deviceAdress).Count() > 0;
                    //インジケーターがなければ、表示がおかしくなるため追加を行わない
                    bool isExistIndicator = indicatorObj != null;
                    if (!isExistList && indicatorObj)
                    {
                        //リストに追加する
                        listDeviceAdress.Add(deviceAdress);
                        // TODO: 試験的に、デバイス名とともにデバイスアドレスを表示する。仕様ではないため、お客様からの指示で下の一行を削除する
                        // iOSはデバイスアドレスが取得できないため、表示しない(できない)
                        if (deviceAdress != "")
                        {
                            deviceName += " [" + deviceAdress + "]";
                        }
                        var sleeimDevice = CreateListElement(
                            deviceName,
                            () =>
                            {
                                //接続ボタンを押した際のコールバック
                                //スキャンを停止する
                                BluetoothManager.Instance.StopScanning();
                                //インジケーターを削除
                                if (indicatorObj != null)
                                    DestroyImmediate(indicatorObj);
                                //ペアリング処理開始
                                StartCoroutine(Parering(deviceName, deviceAdress, deviceIndex));
                                isContinue = false;     // 検索処理終了フラグ
                            });
                        Adapter.SetElementToList(sleeimDevice);
                    }
                    if (isContinue == null) isContinue = true;
                });
            yield return new WaitUntil(() => isContinue != null);
#if UNITY_ANDROID
            // iODはデバイスアドレスが取得できないため、複数検知しない
            // 検索失敗or接続完了で終了
            if (isContinue == false) break;
        }
#endif

        //エラー時
        //インジケーター削除
        DestroyImmediate(indicatorObj);
        BluetoothManager.Instance.StopScanning();	//スキャン停止
        //エラー情報読み出し
        var j = Json.Deserialize(receiveData) as Dictionary<string, object>;
        int error1 = Convert.ToInt32(j["KEY1"]);	//不要？説明なし
        int error2 = Convert.ToInt32(j["KEY2"]);	//タイムアウトエラー(スキャンエラー含む)
        if (error2 == -4)
        {
            //接続切れのエラーであればリトライ
            yield return StartCoroutine(SearchDeviceFlow());
        }
        else
        {
            if (listDeviceAdress.Count() == 0)
            {
                //機器が見つからなかった旨のダイアログを表示する
                yield return StartCoroutine(TellNotFoundDevice());
            }
        }
    }

    //デバイスを検索した結果見つからなかった事をユーザーに伝える
    IEnumerator TellNotFoundDevice()
    {
        bool isOk = false;
        MessageDialog.Show("<size=32>Sleeimが見つかりませんでした。</size>", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk);
    }

    /// <summary>
    /// デバイスとペアリングする
    /// </summary>
    /// <param name="deviceName">デバイス名</param>
    /// <param name="deviceAdress">デバイスのBLEアドレス</param>
    /// <param name="index">iOSでのみ使用するデバイス識別番号</param>
    IEnumerator Parering(string deviceName, string deviceAdress, int index = 0)
    {
        if (DeviceActivationStatus == DeviceActivationStatusType.App)
        {
            BluetoothManager.Instance.ChangeServiceUUIDToNormal();
        }
        else
        {
            BluetoothManager.Instance.ChangeServiceUUIDToFirmwareUpdate();
        }

        //Bluetoothが有効か確認
        bool isBluetoothActive = false;
        yield return StartCoroutine(CheckBluetoothIsActive((bool isActive) => isBluetoothActive = isActive));
        if (!isBluetoothActive)
        {
            //Bluetooth無効時
            yield break;
        }
        //接続できてなければ、接続
        if (!UserDataManager.State.isConnectingDevice())
        {
            UpdateDialogAddButton.Show(deviceName + "に接続しています。",
                false,
                true,
                null,
                () =>
                {
                    //キャンセルボタン押下時
                    //デバイスとの接続を切る
                    BluetoothManager.Instance.Disconnect();
                },
                "OK",
                "キャンセル");
            bool isDeviceConnectSuccess = false;
            yield return StartCoroutine(DeviceConnect(deviceName, deviceAdress, (bool isSuccess) => isDeviceConnectSuccess = isSuccess, index));
            Debug.Log("Connecting_Result:" + isDeviceConnectSuccess);
            UpdateDialogAddButton.Dismiss();
            if (!isDeviceConnectSuccess)
            {
                //デバイス接続に失敗すれば	、接続失敗した旨を伝えるダイアログを表示する
                yield return StartCoroutine(TellFailedConnect(deviceName));
                yield break;
            }
        }
        UpdateDialog.Show("同期中");

        // デバイスがアプリ状態なら、バージョンを取得する
        if (DeviceActivationStatus == DeviceActivationStatusType.App)
        {
            //バージョン取得
            bool isGetVersionSuccess = false;
            yield return StartCoroutine(GetVersion((bool isSuccess) => isGetVersionSuccess = isSuccess));
            Debug.Log("GetVersion_Result:" + isGetVersionSuccess);
            if (!isGetVersionSuccess)
            {
                //バージョン取得に失敗すれば
                UpdateDialog.Dismiss();
                //同期に失敗した旨のダイアログを表示する
                yield return StartCoroutine(TellFailedSync());
                yield break;
            }
        }

        // デバイス状態がアプリならデバイス情報を取得する
        if (DeviceActivationStatus == DeviceActivationStatusType.App)
        {
            //デバイス情報取得
            bool isGetDeviceInfoSuccess = false;
            DateTime deviceTime = DateTime.MinValue;
            yield return StartCoroutine(GetDeviceInfo((bool isSuccess, DateTime _deviceTime) =>
            {
                isGetDeviceInfoSuccess = isSuccess;
                deviceTime = _deviceTime;
            }));
            Debug.Log("GetDeviceInfo_Result:" + isGetDeviceInfoSuccess);
            if (!isGetDeviceInfoSuccess)
            {
                //デバイス情報取得に失敗すれば
                UpdateDialog.Dismiss();
                //同期に失敗した旨のダイアログを表示する
                yield return StartCoroutine(TellFailedSync());
                yield break;
            }

            //デバイス時刻補正
            bool isCorrectDeviceTimeSuccess = false;
            yield return StartCoroutine(CorrectDeviceTime((bool isSuccess) => isCorrectDeviceTimeSuccess = isSuccess, deviceTime));
            Debug.Log("CorrectDeviceTime_Result:" + isCorrectDeviceTimeSuccess);
            if (!isCorrectDeviceTimeSuccess)
            {
                //デバイス時刻補正に失敗すれば
                UpdateDialog.Dismiss();
                //設定の変更に失敗した旨のダイアログを表示する
                yield return StartCoroutine(TellFailedChangeSetting());
                yield break;
            }
        }

        UpdateDialog.Dismiss();
        //ペアリングが完了した事を記録
        UserDataManager.State.SaveDoneDevicePareing();
        //接続したデバイスを記憶しておく
        UserDataManager.Device.SavePareringBLEAdress(deviceAdress);
        UserDataManager.Device.SavePareringDeviceName(deviceName);
        //完了ダイアログ表示
        yield return StartCoroutine(ShowPareringCompleteDialog(deviceName));
        //データの復元が必要であればデータを復元する
        if (UserDataManager.State.isNessesaryRestore())
        {
            bool isCompleteRestore = false;
            if (UserDataManager.State.GetRestoreDataCount() == 0)
            {
                //初回復元の場合
                FtpFunction.RestoreData(this, () => isCompleteRestore = true);
                yield return new WaitUntil(() => isCompleteRestore);
                //ファームウェアアップデートが必要か確認する
                yield return StartCoroutine(FarmwareVersionCheckFlow());
            }
            else
            {
                //復元再開の場合
                FtpFunction.ReRestoreData(this, () => isCompleteRestore = true);
                yield return new WaitUntil(() => isCompleteRestore);
            }
        }
        //ホーム画面に遷移
        SceneTransitionManager.LoadLevel(SceneTransitionManager.LoadScene.Home);
    }

    //デバイスのファームウェアが最新のものかどうか確認する処理の流れ
    //ファームウェアが最新でなくても更新までは行わない
    IEnumerator FarmwareVersionCheckFlow()
    {
        Debug.Log("FarmwareVersionCheck");
        UpdateDialog.Show("同期中");
        // TODO:G1Dのファームウェアの更新があるかどうか調べる
        // long h1dVersionInDevice = FarmwareVersionStringToLong (UserDataManager.Device.GetH1DAppVersion ());
        //Ftpサーバーから最新のファームウェアのファイル名を取得
        string ratestH1dFileName = "";
        yield return StartCoroutine(GetRatestFarmwareFileNameFromFtp("/Update/H1D", (string fileName) => ratestH1dFileName = fileName));
        if (ratestH1dFileName == null)
        {
            //FTPサーバーにファイルが存在しなかった、もしくはエラーが発生したら
            UpdateDialog.Dismiss();
            yield break;
        }
        Debug.Log("Ratest H1D Farmware is " + ratestH1dFileName);
        if (false)
            DeviceStateManager.Instance.OnFirmwareUpdateNecessary();
        else
            DeviceStateManager.Instance.OnFirmwareUpdateNonNecessary();
        UpdateDialog.Dismiss();
    }

    //「000.000.000.000」の形式のバージョンの文字列から整数値に変換する
    long FarmwareVersionStringToLong(string version)
    {
        //形式が違った場合は-1を返す
        if (version.Length != 15)
            return -1;
        //ドットを取り除く
        string versionString = version;
        versionString = versionString.Replace(".", "");
        return long.Parse(versionString);
    }

    /// <summary>
    /// FTPサーバーから最新のファームウェアのファイル名を取得する
    /// </summary>
    /// <param name="farmwareDirectory">G1D・H1Dのディレクトリパス</param>
    /// <param name="onGetFileName">目的のファイル名を受け取るコールバック</param>
    IEnumerator GetRatestFarmwareFileNameFromFtp(string farmwareDirectoryPath, Action<string> onGetFileName)
    {
        //FTPサーバー上にファームウェアのディレクトリが存在するか確認する
        int directoryExistResult = -1;
        bool isComplete = false;
        FtpManager.DirectoryExists(farmwareDirectoryPath, (int result) =>
        {
            directoryExistResult = result;
            isComplete = true;
        });
        yield return new WaitUntil(() => isComplete);
        Debug.Log(directoryExistResult == 1 ? "G1D directory is Exist!" : "G1D directory is NotExist...");
        if (directoryExistResult == 1)
        {
            //指定したファームウェアディレクトリの名のファイル名をすべて取得する
            var getAllFarmwareFileNameList = FtpManager.GetAllList(farmwareDirectoryPath);
            yield return getAllFarmwareFileNameList.AsCoroutine();
            List<string> farmwareFileNameList = new List<string>();
            if (getAllFarmwareFileNameList.Result != null && getAllFarmwareFileNameList.Result.Count > 0)
            {
                //取得したものには、ファイル、ディレクトリ、Linkが混在してるためファイルのみを取り出す
                farmwareFileNameList = getAllFarmwareFileNameList.Result
                    .Where(data => int.Parse(data[0]) == 0)	//ファイルのみ通す
                    .Select(data => data[1])		//ファイル名に変換
                    .ToList();
                //ファイルがあるか確認
                if (farmwareFileNameList.Count == 0)
                {
                    onGetFileName(null);
                    yield break;
                }
                //ファームウェア以外のファイルをはじく
                farmwareFileNameList = farmwareFileNameList
                    .Where(fileName => fileName.Contains(".mot"))
                    .ToList();
                //ファイルがあるか確認
                if (farmwareFileNameList.Count == 0)
                {
                    onGetFileName(null);
                    yield break;
                }
                //取得したディレクトリを確認
                foreach (var fileName in farmwareFileNameList)
                {
                    Debug.Log("GetFile:" + fileName);
                }
            }
            else
            {
                //なにかしらのエラーが発生した場合
                onGetFileName(null);
                yield break;
            }
            //ファイル名のリストが取得できれば、その中から最新のものを探す
            var ratestVersionFileIndex = farmwareFileNameList
                .Select((fileName, index) => new { FileName = fileName, Index = index })
                .Aggregate((max, current) => (FarmwareFileNameToVersionLong(max.FileName) > FarmwareFileNameToVersionLong(current.FileName) ? max : current))
                .Index;
            onGetFileName(farmwareFileNameList[ratestVersionFileIndex]);
            yield break;
        }
        onGetFileName(null);
    }

    //ファームウェアのアップデートファイルのフォルダ名からバージョン情報を抜き出して比較しやすいように整数型にして返す。
    //その値が大きいものほど新しいバージョン
    long FarmwareFileNameToVersionLong(string filePath)
    {
        string versionString = filePath;													//例：/Update/G1D/RD8001G1D_Ver000.000.000.004.mot
        versionString = versionString.Substring(0, versionString.LastIndexOf('.'));		//例：/Update/G1D/RD8001G1D_Ver000.000.000.004
        versionString = versionString.Substring(versionString.Length - 15);				//例：000.000.000.004
        versionString = versionString.Replace(".", "");									//例：000000000004
        return long.Parse(versionString);
    }


    /// <summary>
    /// デバイス接続の流れ
    /// </summary>
    /// <returns>The connect.</returns>
    /// <param name="deviceName">デバイス名</param>
    /// <param name="deviceAdress">デバイスアドレス</param>
    /// <param name="onResponse">結果を受け取るためのコールバック</param>
    /// <param name="index">iOSでのみ使用するデバイス識別番号</param>
    IEnumerator DeviceConnect(string deviceName, string deviceAdress, Action<bool> onResponse, int index = 0)
    {
        bool isSuccess = false;	//接続成功
        bool isFailed = false;	//接続失敗
        string receiveData = "";		//デバイス接続で成功・失敗時に受け取るデータ（JSONにパースして使用）
        BluetoothManager.Instance.Connect(
            deviceAdress,
            (string data) =>
            {
                //エラー時
                receiveData = data;
                isFailed = true;
            },
            (string data) =>
            {
                //接続完了時
                receiveData = data;
                isSuccess = true;
            },
            "",		//使用しない(UUID)
            index	//iOSでのデバイス接続に必要なデバイス識別番号
        );
        yield return new WaitUntil(() => isSuccess || isFailed);	//応答待ち
        if (isSuccess)
        {
            //接続成功時
        }
        else
        {
            //接続失敗時
            var json = Json.Deserialize(receiveData) as Dictionary<string, object>;
            int error1 = Convert.ToInt32(json["KEY1"]);
            int error2 = Convert.ToInt32(json["KEY2"]);
            if (error2 == -3)	//何らかの原因で接続できなかった場合(タイムアウト含む)
                Debug.Log("OccurAnyError");
            else if (error2 == -4)	//接続が切れた場合(GATTサーバには接続できたが、サービスまで全て接続できないと接続完了にはならない。)
                Debug.Log("DisConnectedError");
        }
        onResponse(isSuccess);
    }

    //デバイスと接続できなかった事をユーザーに伝える
    IEnumerator TellFailedConnect(string deviceName)
    {
        bool isOk = false;
        MessageDialog.Show("<size=32>" + deviceName + "と接続できませんでした。</size>", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk);
    }

    //ペアリング完了を通知するダイアログ表示
    IEnumerator ShowPareringCompleteDialog(string deviceName)
    {
        bool isOK = false;
        MessageDialog.Show(deviceName + "に接続しました。", true, false, () => isOK = true);
        yield return new WaitUntil(() => isOK);
    }

    //デバイスからバージョン取得
    IEnumerator GetVersion(Action<bool> onResponse)
    {
        Debug.Log("GetVersion");
        bool isSuccess = false;
        bool isFailed = false;
        string receiveData = "";
        BluetoothManager.Instance.SendCommandId(
            8,
            (string data) =>
            {
                //エラー時
                Debug.Log("GetVersion failed:" + data);
                isFailed = true;
            },
            (bool success) =>
            {
                Debug.Log("GetVersion commandWrite:" + success);
                if (!success)
                    isFailed = true;
            },
            (string data) =>
            {
                Debug.Log("GetVersion commandresponce:" + data);
                //falseのみ返ってくる
                isFailed = true;
            },
            (string data) =>
            {
                //Ver情報
                Debug.Log("GetVersion success:" + data);
                receiveData = data;
                isSuccess = true;
            });
        yield return new WaitUntil(() => isSuccess || isFailed);	//応答待ち
        if (isSuccess)
        {
            if (receiveData != "")
            {
                //データ取得成功時
                var json = Json.Deserialize(receiveData) as Dictionary<string, object>;
                int g1dAppVerMajor = Convert.ToInt32(json["KEY1"]);
                int g1dAppVerMiner = Convert.ToInt32(json["KEY2"]);
                int g1dAppVerRevision = Convert.ToInt32(json["KEY3"]);
                int g1dAppVerBuild = Convert.ToInt32(json["KEY4"]);
                //アプリ内保存
                string g1dAppVer = g1dAppVerMajor.ToString("000") + "." +
                                  g1dAppVerMiner.ToString("000") + "." +
                                  g1dAppVerRevision.ToString("000") + "." +
                                  g1dAppVerBuild.ToString("000");
                UserDataManager.Device.SaveG1dAppVersion(g1dAppVer);
            }
        }
        onResponse(isSuccess);
    }

    //同期に失敗した事をユーザーに伝える
    IEnumerator TellFailedSync()
    {
        bool isOk = false;
        MessageDialog.Show("同期に失敗しました。", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk);
    }

    //機器のアラーム設定を変更する
    IEnumerator ChangeDeviceAlermSetting(Action<bool> onResponse)
    {
        Debug.Log("ChangeDeviceAlermSetting");
        int alermSet = UserDataManager.Setting.Alerm.isEnable() ? 1 : 0;					//アラーム有効/無効
        int ibikiAlermSet = UserDataManager.Setting.Alerm.IbikiAlerm.isEnable() ? 1 : 0;	//いびきアラーム有効/無効
        int ibikiAlermSense = UserDataManager.Setting.Alerm.IbikiAlerm.GetDetectSense() == UserDataManager.Setting.Alerm.IbikiAlerm.DetectSense.Large ? 1 : 0;	//アラーム感度
        int lowBreathAlermSet = UserDataManager.Setting.Alerm.LowBreathAlerm.isEnable() ? 1 : 0;	//アラーム有効/無効
        int alermDelay = (int)UserDataManager.Setting.Alerm.GetDelayTime();				//アラーム遅延
        int stopMove = UserDataManager.Setting.Alerm.StopMoveAlerm.isEnable() ? 1 : 0;		//体動停止有効/無効
        int callTime = (int)UserDataManager.Setting.Alerm.GetCallTime();					//鳴動時間

        bool isSuccess = false;
        bool isFailed = false;
        BluetoothManager.Instance.SendCommandAlarm(
            alermSet,
            ibikiAlermSet,
            ibikiAlermSense,
            lowBreathAlermSet,
            alermDelay,
            stopMove,
            callTime,
            (string data) =>
            {
                //エラー時
                isFailed = true;
            },
            (bool success) =>
            {
                //コマンド書き込み結果
                Debug.Log("SendCommandAlerm_Success:" + success);
                if (!success)
                    isFailed = true;
            },
            (string data) =>
            {
                //応答結果
                Debug.Log("SendCommandAlerm_OnResponse:" + data);
                var json = Json.Deserialize(data) as Dictionary<string, object>;
                bool response = Convert.ToBoolean(json["KEY2"]);
                if (response)
                    isSuccess = true;
                else
                    isFailed = true;
            });
        yield return new WaitUntil(() => isSuccess || isFailed);
        if (isSuccess)
        {
            //アラーム設定が成功なら
            Debug.Log("SendCommandAlerm Success!!");
        }
        else
        {
            //アラーム設定が失敗なら
            Debug.Log("SendCommandAlerm Failed...");
        }
        onResponse(isSuccess);
    }

    //設定の変更に失敗した事をユーザーに伝える
    IEnumerator TellFailedChangeSetting()
    {
        bool isOk = false;
        MessageDialog.Show("設定変更に失敗しました。", true, false, () => isOk = true);
        yield return new WaitUntil(() => isOk);
    }

    //デバイス情報取得
    IEnumerator GetDeviceInfo(Action<bool, DateTime> onResponse)
    {
        Debug.Log("GetDeviceInfo");
        bool isSuccess = false;
        bool isFailed = false;
        string receiveData = "";
        DateTime _deviceTime = DateTime.MinValue;
        BluetoothManager.Instance.SendCommandId(
            18,
            (string data) =>
            {
                //エラー時
                Debug.Log("failed:" + data);
                isFailed = true;
            },
            (bool success) =>
            {
                Debug.Log("commandWrite:" + success);
                if (!success)
                    isFailed = true;
            },
            (string data) =>
            {
                Debug.Log("commandresponce:" + data);
                //falseのみ返ってくる
                isFailed = true;
            },
            (string data) =>
            {
                //Ver情報
                Debug.Log("success:" + data);
                receiveData = data;
                isSuccess = true;
            });
        yield return new WaitUntil(() => isSuccess || isFailed);	//応答待ち
        if (isSuccess)
        {
            if (receiveData != "")
            {
                //デバイス情報取得成功時
                var json = Json.Deserialize(receiveData) as Dictionary<string, object>;
                string deviceAdress = (string)json["KEY1"];		//デバイスアドレス(MACアドレス)
                int dataCount = Convert.ToInt32(json["KEY2"]);	//測定データ保持数
                int year = Convert.ToInt32(json["KEY3"]);		//年
                int month = Convert.ToInt32(json["KEY4"]);	//月
                int date = Convert.ToInt32(json["KEY5"]);		//曜日
                int day = Convert.ToInt32(json["KEY6"]);		//日
                int hour = Convert.ToInt32(json["KEY7"]);		//時
                int minute = Convert.ToInt32(json["KEY8"]);	//分
                int second = Convert.ToInt32(json["KEY9"]);	//秒
                //デバイスアドレスを保存
                UserDataManager.Device.SavePareringDeviceAdress(deviceAdress);
                //機器の時刻を保存
                //でたらめなデータでエラーにならないように処理
                string deviceTimeString = "20" + year.ToString("00") + "/" + month.ToString("00") + "/" + day.ToString("00") + " " + hour.ToString("00") + ":" + minute.ToString("00") + ":" + second.ToString("00");
                Debug.Log("deviceTime :" + deviceTimeString);
                if (DateTime.TryParse(deviceTimeString, out _deviceTime))
                {
                    Debug.Log("success DateTime parse");
                    UserDataManager.Device.SavePareringDeviceTime(_deviceTime);
                }
                else
                {
                    Debug.Log("failed DateTime parse");
                }
            }
        }
        else
        {
            //デバイス情報取得失敗時
        }
        onResponse(isSuccess, _deviceTime != null ? _deviceTime : DateTime.MinValue);
    }

    //機器時刻を補正する
    IEnumerator CorrectDeviceTime(Action<bool> onResponse, DateTime deviceTime)
    {
        //ＮＴＰサーバから時刻を取得してきて、
        DateTime correctTime = DateTime.MinValue;	//補正のための正しい時間
        float timeout = 5f;		//タイムアウト時間設定
        float timeCounter = 0;	//タイムアウト計測のためのカウンタ
        NTP.Instance.GetTimeStamp((DateTime? time) =>
        {
            if (time != null)
            {
                //NTP時刻が取得できれば
                correctTime = time.Value;
            }
        });
        yield return new WaitUntil(() =>
        {
            timeCounter += Time.deltaTime;
            //タイムアウトまたは、サーバから時刻が取得できれば抜ける
            return timeCounter > timeout || correctTime != DateTime.MinValue;
        });
        bool isTimeout = timeCounter > timeout;
        if (isTimeout)
        {
            //スマホ時刻を補正のための正しい時間として使用する
            correctTime = System.DateTime.Now;
        }
        //デバイスの時刻設定に成功すれば、機器の時刻と正しい時刻を比較
        if (deviceTime == DateTime.MinValue)
        {
            //デバイス時刻がでたらめな値だった場合、無条件に時刻設定を行う
            yield return StartCoroutine(SetDeviceTime(correctTime, onResponse));
        }
        else
        {
            //デバイス時刻が正常な値なら、正確な時間との比較を行う
            var timeDiff = deviceTime - correctTime;
            Debug.Log("DeviceTime:" + deviceTime + ",CorrectTime:" + correctTime + ",TimeDiffMinute:" + timeDiff.TotalMinutes);
            if (Mathf.Abs(Mathf.Abs((float)timeDiff.TotalMinutes)) >= 30f)
            {	//時間差が30分以上であれば
                Debug.Log("TimeDiff 30min over");
                yield return StartCoroutine(SetDeviceTime(correctTime, onResponse));
            }
            else
            {
                Debug.Log("TimeDiff 30min under");
                onResponse(true);
            }
        }
    }

    //デバイス時刻取得
    IEnumerator GetDeviceTime(Action<bool, DateTime> onResponse)
    {
        Debug.Log("GetDeviceTime");
        bool isSuccess = false;
        bool isFailed = false;
        string receiveData = "";
        BluetoothManager.Instance.SendCommandId(
            18,
            (string data) =>
            {
                //エラー時
                Debug.Log("failed:" + data);
                isFailed = true;
            },
            (bool success) =>
            {
                Debug.Log("commandWrite:" + success);
                if (!success)
                    isFailed = true;
            },
            (string data) =>
            {
                Debug.Log("commandresponce:" + data);
                //falseのみ返ってくる
                isFailed = true;
            },
            (string data) =>
            {
                //Ver情報
                Debug.Log("success:" + data);
                receiveData = data;
                isSuccess = true;
            });
        yield return new WaitUntil(() => isSuccess || isFailed);	//応答待ち
        if (isSuccess)
        {
            if (receiveData != "")
            {
                //デバイス情報取得成功時
                var json = Json.Deserialize(receiveData) as Dictionary<string, object>;
                int year = Convert.ToInt32(json["KEY3"]);		//年
                int month = Convert.ToInt32(json["KEY4"]);	//月
                int date = Convert.ToInt32(json["KEY5"]);		//曜日
                int day = Convert.ToInt32(json["KEY6"]);		//日
                int hour = Convert.ToInt32(json["KEY7"]);		//時
                int minute = Convert.ToInt32(json["KEY8"]);	//分
                int second = Convert.ToInt32(json["KEY9"]);	//秒
                //でたらめなデータでエラーにならないように処理
                string deviceTimeString = "20" + year.ToString("00") + "/" + month.ToString("00") + "/" + day.ToString("00") + " " + hour.ToString("00") + ":" + minute.ToString("00") + ":" + second.ToString("00");
                Debug.Log("deviceTime :" + deviceTimeString);
                DateTime deviceTime;
                if (DateTime.TryParse(deviceTimeString, out deviceTime))
                {
                    Debug.Log("success DateTime parse");
                    //デバイス時刻が正常に取得できたとき
                    onResponse(true, deviceTime);
                    yield break;
                }
                else
                {
                    Debug.Log("failed DateTime parse");
                }
            }
        }
        onResponse(false, DateTime.MinValue);
    }

    //デバイスの時刻を設定する
    IEnumerator SetDeviceTime(DateTime time, Action<bool> onResponse)
    {
        Debug.Log("SetDeviceTime");
        //機器の時刻補正を行う
        Debug.Log("Set Device Date " + time);
        string dateString = time.Year.ToString("0000") + "/" + time.Month.ToString("00") + "/" + time.Day.ToString("00") + " " +
            time.Hour.ToString("00") + ":" + time.Minute.ToString("00") + ":" + time.Second.ToString("00");
        bool isSuccess = false;
        bool isFailed = false;
        BluetoothManager.Instance.SendCommandDate(
            dateString,
            (string data) =>
            {
                //エラー時
                Debug.Log("SendCommandDate-OnError:" + data);
                isFailed = true;
            },
            (bool success) =>
            {
                //コマンド書き込み結果
                if (!success)
                    isFailed = true;
            },
            (string data) =>
            {
                //応答結果
                Debug.Log("SendCommandDate-OnResponse:" + data);
                var json = Json.Deserialize(data) as Dictionary<string, object>;
                bool success = Convert.ToBoolean(json["KEY2"]);
                if (success)
                    isSuccess = true;
                else
                    isFailed = true;
            });
        yield return new WaitUntil(() => isSuccess || isFailed);
        onResponse(isSuccess);
    }

    //再度ペアリングを要求する
    IEnumerator RequestPareringAgain()
    {
        bool isOK = false;
        MessageDialog.Show("再度ペアリングを行ってください。", true, false, () => isOK = true);
        yield return new WaitUntil(() => isOK);
    }

    //リストビューに追加する要素を作成
    GameObject CreateListElement(string deviceName, Action connectCallback)
    {
        var obj = Instantiate(ListElementPrehab);
        var deviceListElement = obj.GetComponent<DeviceListElement>();
        deviceListElement.Initialize(deviceName, connectCallback);
        return obj;
    }
}
