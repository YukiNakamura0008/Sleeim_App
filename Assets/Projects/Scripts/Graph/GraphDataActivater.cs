using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Graph {
	/// <summary>
	/// 各グラフに設定するデータを切り替え可能にするためのクラス
	/// </summary>
	public class GraphDataActivater : MonoBehaviour {

		public Switcher graphDataTabSwitch;	//タブを切り替えるためのスイッチ
		public GraphDataSource InputData;	//データの変更通知を発行するクラス
		public IbikiGraph ibikiGraph;
		public BreathGraph breathGraph;
		IGraphDataSwitch activeGraph;		//グラフにアタッチするデータ。呼吸・いびき・睡眠のデータが入る

		void Awake () {
			InputData.OnGraphDataChange.Subscribe (_ => {
				//グラフに表示するデータが変更された際に実行される
				//データが更新されたら、選択中のデータの表示を更新する
				ChangeActiveGraph (activeGraph);
			});
		}

		void Start () {
			//前に開いていたタブを選択する
			switch (UserDataManager.Scene.GetGraphDataTabType ()) {
			case UserDataManager.Scene.GraphDataTabType.Breath:
				graphDataTabSwitch.Select (0);	//indexの数値はインスペクタの設定に依存
				break;
			case UserDataManager.Scene.GraphDataTabType.Ibiki:
				graphDataTabSwitch.Select (1);	//indexの数値はインスペクタの設定に依存
				break;
			case UserDataManager.Scene.GraphDataTabType.Sleep:
				graphDataTabSwitch.Select (2);	//indexの数値はインスペクタの設定に依存
				break;
			}
		}

		/// <summary>
		/// 呼吸のデータを選択した際に呼び出されます
		/// </summary>
		public void OnBreathSelected () {
			//タブの選択状態を記憶しておく
			UserDataManager.Scene.SaveGraphDataTabType (UserDataManager.Scene.GraphDataTabType.Breath);
			ChangeActiveGraph (breathGraph);
			activeGraph = breathGraph;
		}

		/// <summary>
		/// いびきのデータを選択した際に呼び出されます
		/// </summary>
		public void OnIbikiSelected () {
			//タブの選択状態を記憶しておく
			UserDataManager.Scene.SaveGraphDataTabType (UserDataManager.Scene.GraphDataTabType.Ibiki);
			ChangeActiveGraph (ibikiGraph);
			activeGraph = ibikiGraph;
		}

		//アクティブなグラフを変更します
		void ChangeActiveGraph (IGraphDataSwitch targetGraph) {
			Debug.Log ("mou GDA ChangeActiveGraph");
			//現在表示中のグラフを取り除く
			if (activeGraph != null) {
				activeGraph.SetDisActive ();
			}
			//指定のグラフを表示させる
			activeGraph = targetGraph;
			activeGraph.SetActive ();
		}
	}
}
