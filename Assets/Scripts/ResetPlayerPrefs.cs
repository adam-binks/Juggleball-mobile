using UnityEngine;
using System.Collections;

public class ResetPlayerPrefs : MonoBehaviour {

	public bool resetBestScore;
	public bool resetHasCompletedARound;
	public bool resetRoundsPlayed;
	public bool resetAdCounter = true;
	
	void Start() {
		# if UNITY_EDITOR // don't do this in production! just in case I forget to disable the script
			if (resetBestScore) {
				PlayerPrefs.SetInt("BestScore", 0);
				print("resetting best score");
			}
			if (resetHasCompletedARound) {
				PlayerPrefs.SetInt("HasCompletedARound", 0);
				print("resetting has completed a round");
			}
			if (resetRoundsPlayed) {
				PlayerPrefs.SetInt("TotalGamesPlayed", 0);
				PlayerPrefs.SetInt("GamesPlayedTimed", 0);
				PlayerPrefs.SetInt("GamesPlayedLives", 0);
				print("resetting rounds played (3 prefs)");
			}
			if (resetAdCounter) {
				PlayerPrefs.SetInt("GamesSinceAdShown", 0);
				print("resetting ad counter");
			}
		#endif
	}
	
}
