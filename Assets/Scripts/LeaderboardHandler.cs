using UnityEngine;
using System.Collections;
using GooglePlayGames;
using UnityEngine.SocialPlatforms;



public class LeaderboardHandler : MonoBehaviour
{
	public enum Leaderboards {Timed, Lives};
	
	[HeaderAttribute("Leaderboard IDs")]
    public string timedLeaderboardAndroid;
	public string livesLeaderboardAndroid;
	public string timedLeaderboardIOS;
	public string livesLeaderboardIOS;
	[HeaderAttribute("Achievement IDs (with hyphens)")]
	public string ach_knifeJuggler;
	public string ach_dynamiteJuggler;
	public string ach_stayinAlive;
	public string ach_bearGrylls;
	public string ach_getAJob;
	public string ach_wowYouLikeThis;
	[HideInInspector]
	public bool loggedIn; // to GameCenter/Google Play Games depending on the device OS


	void Start() {
		# if UNITY_ANDROID
			PlayGamesPlatform.DebugLogEnabled = true;
			// Activate the Google Play Games platform
			PlayGamesPlatform.Activate();
		#endif
		
		AttemptLogIn();
	}

	/// Login to the relevant games service
	public void AttemptLogIn()
	{
		Social.localUser.Authenticate((bool success) => {
			loggedIn = success;
		});
		// immediately check for achievements
		if (loggedIn) {
			CheckForAchievements(false);
		}
	}
	
	/// Called by the leaderboard button. Actually shows the root leaderboard UI not the current mode
	public void ShowCurrentModeLB() {
		if (!loggedIn) {
			return;
		}
		
		Social.ShowLeaderboardUI(); // Show the root leaderboard UI
	}
	
	/// Called by the achievements button. Bring up the platform specific achievement UI
	public void ShowAchievementUI() {
		if (!loggedIn) {
			return;
		}
		
		Social.ShowAchievementsUI();
	}

	/// Adds score to the given leaderboard. The score will be ignored by the games service if it is not the highest recorded
	public void AddScoreToLeaderBoard(int score, Leaderboards LB) {
		if (!loggedIn) {
			return;
		}	
		
		if (Social.localUser.authenticated) {
			Social.ReportScore(score, GetLBID(LB), null);
		}
	}
	
	/// Get the leaderboard ID for the given Leaderboards type, for the correct OS
	string GetLBID(Leaderboards type) {
		if (type == Leaderboards.Timed) {
				# if UNITY_ANDROID
					return timedLeaderboardAndroid;
				#endif
				# if UNITY_IPHONE
					return timedLeaderboardIOS;
				#endif
			} else if (type == Leaderboards.Lives) {
				# if UNITY_ANDROID
					return livesLeaderboardAndroid;
				#endif
				# if UNITY_IPHONE
					return livesLeaderboardIOS;
				#endif
			} else {
				Debug.LogError("Invalid leaderboard type");
				return "ERROR";
			}
	}
	
	/// Just ignore failures, next time the player completes a round when logged in they'll be uploaded
	public void CheckForAchievements(bool justFinishedRound) {
		if (!loggedIn) {
			return;
		}
		
		float completed = 100f;
		
		if (PlayerPrefs.GetInt("BestScoreTimed", 0) >= 250) {
			Social.ReportProgress(GetPlatformAchievementID(ach_knifeJuggler), completed, null);
		}
		if (PlayerPrefs.GetInt("BestScoreTimed", 0) >= 500) {
			Social.ReportProgress(GetPlatformAchievementID(ach_dynamiteJuggler), completed, null);
		}
		if (PlayerPrefs.GetInt("BestScoreLives", 0) >= 150) {
			Social.ReportProgress(GetPlatformAchievementID(ach_stayinAlive), completed, null);
		}
		if (PlayerPrefs.GetInt("BestScoreLives", 0) >= 400) {
			Social.ReportProgress(GetPlatformAchievementID(ach_bearGrylls), completed, null);
		}
		if (PlayerPrefs.GetInt("BestScoreTimed", 0) >= 250) {
			Social.ReportProgress(GetPlatformAchievementID(ach_knifeJuggler), completed, null);
		}
		
		ReportIncrementalAchievement(ach_getAJob, 100f, "TotalGamesPlayed", justFinishedRound ? 1:0);
		ReportIncrementalAchievement(ach_wowYouLikeThis, 1000f, "TotalGamesPlayed", justFinishedRound ? 1:0);
	}
	
	/// Things work differently for GameCenter and Play Games. GC wants % progress, Play wants amount incremented
	void ReportIncrementalAchievement(string ID, float target, string totalAchieved, int amountIncremented) {
		#if UNITY_IPHONE
			float progress = ((float)PlayerPrefs.GetInt(totalAchieved) / target) * 100f;  // % progress to achievement
			Social.ReportProgress(GetPlatformAchievementID(ach_getAJob), progress, null);
		#endif
		#if UNITY_ANDROID
			PlayGamesPlatform.Instance.IncrementAchievement(GetPlatformAchievementID(ID), amountIncremented, null);
		#endif
	}
	
	/// Because iTunes connect doesn't allow hypens in achievement IDs, I replaced them with underscores
	string GetPlatformAchievementID(string ID) {
		#if UNITY_IPHONE // replace hyphens with underscores
			return ID.Replace("-", "_");
		#endif
		
		#if UNITY_ANDROID // keep it as is - with hyphens
			return ID;
		#endif
	}
}