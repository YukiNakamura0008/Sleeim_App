﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Kaimin.Common;
using UnityEngine.UI;

/// <summary>
/// 睡眠履歴の表示に必要なデータを提供するクラス
/// </summary>
public class SleepListDataSource : MonoBehaviour
{

    string[] filePath = null;

    string[] FilePath
    {
        get
        {
            if (filePath == null)
                filePath = Kaimin.Common.Utility.GetAllFiles(Kaimin.Common.Utility.GsDataPath(), "*.csv");
            return filePath;
        }
    }

    /// <summary>
    /// 睡眠履歴をリスト表示するのに必要なデータを取得します
    /// fromにDateTime.MinValue・toにDateTime.MaxValueで期間を指定しない事が可能です
    /// </summary>
    public IEnumerator GetSleepListElementDataCoroutine(ScrollRect scrollRect, DateTime from, DateTime to, Action<SleepListElement.Data> onGetData, Action onComplete)
    {
		Debug.Log("mou fresh");
        int initLoadNum = 7;    //はじめにまとめてロードするアイテム数
        int multiLoadNum = 5;   //スクロールした際にまとめてロードするアイテム数
        int multiLoadCount = 0;
        bool popItem = false;

        scrollRect.verticalNormalizedPosition = 1f;
        //CSVから取得した睡眠データをSleepListElement.Dataに変換して返す
        //fromからtoまでの期間の睡眠データを取得する
        //取得したファイル一覧から指定した期間のファイルのみのリストを作成する
        for (int i = 0; i < PickFilePathInPeriod(FilePath, from, to).Count; i++)
        {
            string filePath = PickFilePathInPeriod(FilePath, from, to)[i];
            //一日ごとのデータを取得する
            List<SleepData> sleepDataList = ReadSleepDataFromCSV(filePath);         //睡眠データをCSVから取得する
            SleepHeaderData sleepHeaderData = ReadSleepHeaderDataFromCSV(filePath); //睡眠のヘッダーデータをCSVから取得する
                                                                                    //データを設定する
            DateTime bedTime = sleepHeaderData.DateTime;
            DateTime getUpTime = sleepDataList.Last().GetDateTime();
            List<DateTime> dateList = sleepDataList.Select(data => data.GetDateTime()).ToList();
            int longestApneaTime = sleepHeaderData.LongestApneaTime;
            int apneaCount = CulcApneaCount(sleepDataList);
            List<string> todayDataPathList = PickFilePathInPeriod(FilePath, from, to).Where(path => IsSameDay(bedTime, Utility.TransFilePathToDate(path))).ToList();
            int dateIndex = todayDataPathList
                .Select((path, index) => new { Path = path, Index = index })
                .Where(data => data.Path == filePath)
                .Select(data => data.Index)
                .First();                               //同一日の何個目のデータか(0はじまり)
            int crossSunCount = todayDataPathList
                .Take(dateIndex + 1)
                .Where(path => isCrossTheSun(bedTime, ReadSleepDataFromCSV(path).Last().GetDateTime()))
                .Count();                               //現在のデータまでの日マタギデータの個数
            int sameDataNum = todayDataPathList.Count;  //同一日のすべてのデータ個数
            int crossSunNum = todayDataPathList
                .Where(path => isCrossTheSun(bedTime, ReadSleepDataFromCSV(path).Last().GetDateTime()))
                .Count();                               //同一日の日マタギのみのデータ個数
            onGetData(new SleepListElement.Data(bedTime, dateList, longestApneaTime, apneaCount, dateIndex, crossSunCount, sameDataNum, crossSunNum));
            if (i + 1 < initLoadNum)
            {
                //初期アイテムロード
            }
            else if (popItem && (multiLoadCount < multiLoadNum))
            {
                //追加アイテムロード
                multiLoadCount++;
            }
            else
            {
                yield return new WaitUntil(() => scrollRect.verticalNormalizedPosition < 0.1f);
                popItem = true;
                multiLoadCount = 1; //既に一つは読み込み済み
            }
        }
        onComplete();
    }

    //日付をまたいでいるかどうか
    bool isCrossTheSun(DateTime start, DateTime end)
    {
        return start.Month != end.Month || start.Day != end.Day;
    }

    bool IsSameDay(DateTime date1, DateTime date2)
    {
        if (date1.Year != date2.Year)
            return false;
        if (date1.Month != date2.Month)
            return false;
        if (date1.Day != date2.Day)
            return false;
        return true;
    }

    /// <summary>
    /// 無呼吸検知数を求める
    /// </summary>
    /// <param name="sleepDataList"></param>
    /// <returns></returns>
    int CulcApneaCount(List<SleepData> sleepDataList)
    {
        int totalCount = 0;
        totalCount += sleepDataList.Where(sleepData => sleepData.BreathState1 == (int)SleepData.BreathState.Apnea).Count();
        totalCount += sleepDataList.Where(sleepData => sleepData.BreathState2 == (int)SleepData.BreathState.Apnea).Count();
        totalCount += sleepDataList.Where(sleepData => sleepData.BreathState3 == (int)SleepData.BreathState.Apnea).Count();
        return totalCount;
    }

    /// <summary>
    /// 最新の日付を取得します
    /// 一件もデータがなかった場合はDateTime.MinValueを返します
    /// </summary>
    public DateTime GetLatestDate()
    {
        return FilePath.Length >= 1 ? Utility.TransFilePathToDate(FilePath[FilePath.Length - 1]) : DateTime.MinValue;
    }

    /// <summary>
    /// 指定した期間にデータが存在するかどうかを返します
    /// </summary>
    public bool IsExistData(DateTime from, DateTime to)
    {
        return FilePath.Where(path => (from == DateTime.MinValue || Utility.TransFilePathToDate(path).CompareTo(from) >= 0) && (to == DateTime.MaxValue || Utility.TransFilePathToDate(path).CompareTo(to) <= 0)).Count() != 0;
    }

    //睡眠データのファイル一覧から指定した期間のもののみを取得
    List<string> PickFilePathInPeriod(string[] sleepFilePathList, DateTime from, DateTime to)
    {

		Debug.Log ("mou AAAAA");
		Debug.Log (sleepFilePathList);

		List<string> l = new List<string> ();
		string v = "Assets/Data/2019.csv";
		l.Add (v);
		return l;

        return sleepFilePathList.Where(path => (from == DateTime.MinValue || Utility.TransFilePathToDate(path).CompareTo(from) >= 0) && (to == DateTime.MaxValue || Utility.TransFilePathToDate(path).CompareTo(to) <= 0)).ToList();
    }



    //睡眠データをリソースのCSVファイルから取得します
    List<SleepData> ReadSleepDataFromCSV(string filepath)
    {
        return CSVSleepDataReader.GetSleepDatas(filepath);
    }

    //睡眠のヘッダーデータをCSVファイルから取得します
    SleepHeaderData ReadSleepHeaderDataFromCSV(string filepath)
    {
        return CSVSleepDataReader.GetSleepHeaderData(filepath);
    }
}
