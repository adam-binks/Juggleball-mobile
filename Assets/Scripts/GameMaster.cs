using UnityEngine;
using UnityEngine.Analytics;
using System.Collections.Generic;

public class GameMaster : MonoBehaviour {

	public enum Mode {Timed, Lives};
	
	[HeaderAttribute("Ball attributes")]
	public int maxBallsOnScreen;
	[TooltipAttribute("Applied at both edges of the screen")]
	public float ballScreenEdgeMargin;
	[TooltipAttribute("How far can new balls be placed from y=0?")]
	public float maxYDistFromHorizontal;
	[RangeAttribute(0, 2)]
	public float minScale;
	[RangeAttribute(0, 2)]
	public float maxScale;
	public Color[] ballColours;
	public GameObject ballPrefab;
	[HeaderAttribute("Difficulty")]
	[TooltipAttribute("In seconds")]
	public float roundTimeLimit;
	public GameObject timerIndicator;
	[TooltipAttribute("x = % of roundTimeLimit, y = time between ball spawns")]
	public AnimationCurve ballSpawnInterval;
	[TooltipAttribute("Score/second is multiplied by this (small scores are demotivating)")]
	public float scoreSizer;
	public float score;
	public int startLivesNum;
	[HeaderAttribute("Variants")]
	public Mode gameMode;
	public bool endOnBallsDropped;
	public float gameStartDelay;
	[HeaderAttribute("Adverts")]
	public int minScoreForAds;
	public int minGamesAdInterval;
	
	[HideInInspector]
	public float roundStartTime = -1; // default to -1 so the game knows the timer hasn't started yet
	[HideInInspector]
	public bool[] usedBallSlots;
	[HideInInspector]
	public List<GameObject> activeBalls;
	public List<Collider2D> activeBallColliders;
	[HideInInspector]
	public List<GameObject> activeUnfrozenBalls;
	[HideInInspector]
	public bool roundInProgress = false;
	private float slotWidth;
	private float leftmostSlotX;
	private Coroutine ballSpawnTimer;
	private UIControl UIController;  // << bad variable name :)
	private int ballsSpawned;
	private Sounds sfx;
	[HideInInspector]
	public int lives;
	private LeaderboardHandler LBHandler;
	
	
	/// Can't use Start() because splash screen means the game runs behind >:(
	public void StartAfterSplashscreen () {
		UIController = GetComponent<UIControl>();
		SetupSlots();
		UIController.SetupScoreText();
		
		UIController.ShowTitle();
		string nextMethod;
		if (PlayerPrefs.GetInt("HasCompletedARound", 0) == 0) {
			// if is first run go straight into a timed round
			gameMode = Mode.Timed;
			nextMethod = "StartRound";
		} else {
			// otherwise allow the player to select timed or lives modes
			nextMethod = "ShowModeSelectScreen";
		}
		Invoke(nextMethod, UIController.titleAnimateTime * 1.5f + UIController.titleDisplayTime + gameStartDelay);
		
		sfx = GetComponent<Sounds>();
		
		LBHandler = GetComponent<LeaderboardHandler>();
	}
	
	/// Slots are the equally spaced positions balls can hold. Set them all to empty to start with.
	void SetupSlots() {
		float leftX = Camera.main.ScreenToWorldPoint(Vector3.zero).x + ballScreenEdgeMargin;
		float rightX = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, 0)).x - ballScreenEdgeMargin;
		
		slotWidth = (rightX - leftX) / (maxBallsOnScreen - 1);
		leftmostSlotX = leftX;
	}
	
	void ShowModeSelectScreen() {
		UIController.ShowGameModeSelectScreen();
	}
	
	public void HideGameOverAndStartRound() {
		UIController.HideGameOverScreen();
		StartCoroutine(StartRoundAfterSecs(UIController.gameOverTransitionOutTime));
	}
	
	IEnumerator<WaitForSeconds> StartRoundAfterSecs(float seconds) {
		yield return new WaitForSeconds(seconds);
		StartRound();
	}
	
	void StartRound() {
		roundInProgress = true;
		score = 0;
		ballsSpawned = 0;
		
		if (gameMode == Mode.Timed) {
			roundStartTime = -1; // don't start the round timer until the first ball has been tapped
			endOnBallsDropped = true; // not sure about this, maybe split this off into a survival mode?
		}
		if (gameMode == Mode.Lives) {
			lives = startLivesNum;
			endOnBallsDropped = false; // don't end the round if balls are dropped, only when all lives have been lost
			UIController.DisplayLives();
		}
		
		usedBallSlots = new bool[maxBallsOnScreen];
		activeBalls = new List<GameObject>();
		activeBallColliders = new List<Collider2D>();
		activeUnfrozenBalls = new List<GameObject>();
		
		SpawnBall(); // spawn the first ball - don't start a ball spawn timer yet
		
		UIController.ShowTutorialText("TAP TO JUGGLE");
	}
	
	void EndRound() {
		// if the player beats their best score, update it and activate celebration on game over screen
		string bestScoreString = "";
		if (gameMode == Mode.Timed) {
			bestScoreString = "BestScoreTimed";
		} else if (gameMode == Mode.Lives) {
			bestScoreString = "BestScoreLives";
		}
		
		bool bestScoreWasBeaten = false;
		if ((int)score > PlayerPrefs.GetInt(bestScoreString, 0)) {
			PlayerPrefs.SetInt(bestScoreString, (int)score);
			bestScoreWasBeaten = true;
			sfx.Play(sfx.wooHoo);
		}
		
		// increment total games played and games played of this mode
		PlayerPrefs.SetInt("TotalGamesPlayed", PlayerPrefs.GetInt("TotalGamesPlayed", 0) + 1);
		string mode = gameMode == Mode.Timed ? "Timed" : "Lives";
		PlayerPrefs.SetInt("GamesPlayed" + mode, PlayerPrefs.GetInt("GamesPlayed" + mode, 0) + 1);
		
		// track whether this is the user's first ever game
		PlayerPrefs.SetInt("HasCompletedARound", 1);
		
		// send analytics before modifying anything
		Analytics.CustomEvent("gameOver", new Dictionary<string, object>
		{
			{"ballsSpawned", ballsSpawned},
			{"roundLength", Time.time - roundStartTime},
			{"score", score},
			{"newHighScore", bestScoreWasBeaten},
			{"totalGamesPlayed", PlayerPrefs.GetInt("TotalGamesPlayed", 0)},
			{"gamesPlayedTimed", PlayerPrefs.GetInt("GamesPlayedTimed", 0)},
			{"gamesPlayedLives", PlayerPrefs.GetInt("GamesPlayedLives", 0)},
			{"adsShown", PlayerPrefs.GetInt("AdsShown", 0)}
		});
		
		// sort out social stuff
		UpdateLeaderboard();
		LBHandler.CheckForAchievements(true);
		
		// increment the ad countdown, but only if the player isn't doing particularly badly or well, to reduce frustration
		if ((int)score > minScoreForAds && !bestScoreWasBeaten) {
			PlayerPrefs.SetInt("GamesSinceAdShown", PlayerPrefs.GetInt("GamesSinceAdShown", 0) + 1);
			
			// if it's been long enough since the last once, show an ad
			if (PlayerPrefs.GetInt("GamesSinceAdShown", 0) >= minGamesAdInterval) {
				GetComponent<AdManager>().ShowAd();
				PlayerPrefs.SetInt("GamesSinceAdShown", 0);
				PlayerPrefs.SetInt("AdsShown", PlayerPrefs.GetInt("AdsShown", 0) + 1); // increment an ads shown counter for analytics
			}
		}
		
		roundInProgress = false;
		roundStartTime = -1;
		StopCoroutine(ballSpawnTimer);
		
		UIController.ShowGameOverScreen(bestScoreWasBeaten);
	}
	
	void UpdateLeaderboard() {
		LeaderboardHandler.Leaderboards LB = LeaderboardHandler.Leaderboards.Timed;
		if (gameMode == Mode.Timed) {
			LB = LeaderboardHandler.Leaderboards.Timed;
		} else if (gameMode == Mode.Lives) {
			LB = LeaderboardHandler.Leaderboards.Lives;
		}
		LBHandler.AddScoreToLeaderBoard((int)score, LB);
	}
	
	void FixedUpdate () {
		UpdateScore();
	}
	
	void Update() {
		if (gameMode == Mode.Timed && roundInProgress && roundStartTime != -1) {
			UpdateTimerIndicator();
		}
		if (roundInProgress) {
			HandleTouch();
		}
	}
	
	void SpawnBall() {
		if (activeBalls.Count >= maxBallsOnScreen) {
			return; // don't spawn a new ball if there are too many already
		}
		// Create and set up a new ball
		float scale = UnityEngine.Random.Range(minScale, maxScale);
		int ballSlot = GetNewBallSlot();
		
		Vector2 position = new Vector2(GetXPosByBallSlot(ballSlot), UnityEngine.Random.Range(-maxYDistFromHorizontal, maxYDistFromHorizontal));
		GameObject ball = (GameObject)Instantiate(ballPrefab, position, Quaternion.identity);
		
		CircleControl control = ball.GetComponent<CircleControl>();
		ball.GetComponent<SpriteRenderer>().color = ballColours[Random.Range(0, ballColours.Length)]; // assign a random colour
		control.SetupScale(scale); // assign a random scale
		control.Freeze();
		control.ballSlot = ballSlot;
		activeBalls.Add(ball);
		activeBallColliders.Add(control.tapCollider);
		ball.GetComponent<BallAccessories>().AddRandomAccessory(); // some balls recieve a random accessory
		
		ballsSpawned ++;
		
		if (ballsSpawned == 2) {
			if (gameMode == Mode.Timed) {
				UIController.ShowTutorialText("MORE BALLS = MORE POINTS");
				UIController.Invoke("HideTutorialText", UIController.tutorialDisplayTime);
			}
		}
		
		sfx.Play(sfx.pop);
	}
	
	/// Get an x position for a new ball which is not too close to any other balls or the screen edges
	int GetNewBallSlot() {
		while (true) {
			// if the randomly chosen slot is not in use, use it. Otherwise try another slot
			int randint = UnityEngine.Random.Range(0, maxBallsOnScreen);
			if (usedBallSlots[randint] != true) {
				usedBallSlots[randint] = true;
				return randint;
			}
		}
	}
	
	/// Work out the x position of a ballslot given the ID of that slot
	float GetXPosByBallSlot(int ballSlot) {
		return leftmostSlotX + slotWidth * ballSlot;
	}
	
	/// Opposite of above method
	int GetBallSlotByXPos(float xPos) {
		return (int)((xPos - leftmostSlotX) / slotWidth);
	}
	
	/// Handle touch input to make balls that have been tapped (or nearly tapped) jump
	void HandleTouch() {
		if (Input.touchCount > 0){
			foreach (Touch touch in Input.touches) { // check each touch
				if (touch.phase == TouchPhase.Began) { // no holding touch allowed
					Vector3 worldPos = Camera.main.ScreenToWorldPoint(touch.position);
					foreach(Collider2D col in activeBallColliders) { // check each ball collider
						if (col.OverlapPoint(worldPos)) {
							col.gameObject.transform.parent.GetComponent<CircleControl>().OnTap();
						}
					}
				}
			}
		}
	}
	
	/// Remove all evidence of a ball just like in men in black but with less will smith
	public void RemoveBall(GameObject ball, CircleControl ballControl) {
		if (!activeBalls.Contains(ball)) {
			Debug.LogError("Can't remove a removed ball!");
			return;
		}
		activeBalls.Remove(ball);
		activeBallColliders.Remove(ball.GetComponent<CircleControl>().tapCollider);
		activeUnfrozenBalls.Remove(ball);
		// allow new balls to be spawned in this slot
		usedBallSlots[ballControl.ballSlot] = false;
		
		if (gameMode == Mode.Lives) {
			LoseLife();
		}
		
		// end the round if no balls are being juggled and the first ball has been tapped
		if (activeUnfrozenBalls.Count == 0 && endOnBallsDropped && roundStartTime != -1) {
			EndRound();
		} else {
			sfx.Play(sfx.smash);  // don't play smash if timer is up
		}
	}
	
	void LoseLife() {
		lives --;
		if (lives <= 0) {
			EndRound();
		}
		UIController.LoseHeart();
	}
	
	/// Waits [seconds] seconds, then spawns a ball. This function then calls itself again based on the ballSpawnInterval curve
	IEnumerator<WaitForSeconds> SpawnBallAfterSeconds(float seconds) {
		yield return new WaitForSeconds(seconds);
		SpawnBall();
		
		// recursively call SpawnBallAfterSeconds()
		float progressThroughRound = (Time.time - roundStartTime) / roundTimeLimit;
		ballSpawnTimer = StartCoroutine(SpawnBallAfterSeconds(ballSpawnInterval.Evaluate(progressThroughRound)));
	}
	
	/// Update the player's score each frame - more balls = faster score increase
	void UpdateScore() {
		score += activeUnfrozenBalls.Count * scoreSizer * Time.deltaTime;
		UIController.scoreText.text = ((int)score).ToString();
	}
	
	/// Start ball spawn timer and hide tutorial text
	public void StartTimer() {
		roundStartTime = Time.time;
		
		// spawn the second ball after a delay
		float progressThroughRound = (Time.time - roundStartTime) / roundTimeLimit;
		ballSpawnTimer = StartCoroutine(SpawnBallAfterSeconds(ballSpawnInterval.Evaluate(progressThroughRound)));
		
		// show the score text
		UIController.FadeInScore();
		
		// show the next tutorial text
		UIController.HideTutorialText(); // hide TAP TO JUGGLE
	}
	
	/// Move the coloured background left to right based on time progress through the round
	void UpdateTimerIndicator() {
		float progressThroughRound = (Time.time - roundStartTime) / roundTimeLimit; // between 0 and 1
		timerIndicator.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(progressThroughRound * Camera.main.pixelWidth, 
																					   Camera.main.pixelHeight / 2f,
																					   10));
		if (Time.time - roundStartTime >= roundTimeLimit) {
			EndRound();
		}
	}
}
