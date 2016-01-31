using UnityEngine;

public class CheckForSplashScreen : MonoBehaviour {
	
	private GameObject SCRIPTS;
	
	void Start() {
		SCRIPTS = GameObject.Find("SCRIPTS");
		SCRIPTS.SetActive(false);
	}
	
	/// The Unity Basic splashscreen doesn't prevent games running behind it. So now we wait...
	void Update () {
		if (!Application.isShowingSplashScreen) {
			SCRIPTS.SetActive(true);
			SCRIPTS.GetComponent<GameMaster>().StartAfterSplashscreen();
			Destroy(this.gameObject); // stop checking
		}
	}
}
