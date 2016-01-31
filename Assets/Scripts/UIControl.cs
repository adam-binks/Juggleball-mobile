using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class UIControl : MonoBehaviour {
	
	[HeaderAttribute("GameObjects")]
	public Text scoreText;
	public GameObject youScoredText;
	public GameObject replayButton;
	public GameObject selectDifferentGamemodeButton;
	public Text titleText;
	public Text subtitleText;
	public Text bestScoreText;
	public Image modeDisplay;
	public Sprite timerImage;
	public Sprite livesImage;
	public Text tutorialText;
	public GameObject livesHolder;
	public Image[] hearts;
	public GameObject selectModeScreen;
	public Text selectModeTimedBest;
	public Text selectModeLivesBest;
	public GameObject infoScreen;
	public GameObject buttonWidgets;
	public GameObject leaderboardButton;
	public GameObject achievementButton;
	[HeaderAttribute("Attribute tuning")]
	public float scoreAlpha;
	public bool fadeInScoreText;
	public float gameOverTransitionInTime;
	public float gameOverTransitionOutTime;
	public int gameOverScoreTextSize;
	public float bestScoreRotateTime;
	public float titleDisplayTime;
	public float titleAnimateTime;
	public float tutorialTransitionTime;
	public float tutorialDisplayTime;
	public Color liveHeartColour;
	public Color lostHeartColour;
	
	private GameMaster gameMaster;
	private int scoreTextNormalSize;
	private Vector2 scoreTextStartPos;
	private Vector2 bestTextStartPos;
	private Vector2 tutorialStartPos;
	private Vector2 replayButtonPos;
	private Vector2 buttonWidgetsPos;
	private Vector2 differentModeButtonPos;
	private Tweener bestRotateTween;
	private Sounds sfx;
	private Vector2 livesPos;
	
	
	
	void Awake() {
		gameMaster = GetComponent<GameMaster>();
		sfx = GetComponent<Sounds>();
		livesPos = livesHolder.GetComponent<RectTransform>().anchoredPosition;
	}
	
	
	/// Animate in the text JUGGLEBALL, then animate out after titleAnimateTime 
	public void ShowTitle() {
		sfx = GetComponent<Sounds>();
		
		titleText.gameObject.SetActive(true);
		titleText.transform.localScale = Vector3.zero;
		titleText.transform.DOScale(Vector3.one, titleAnimateTime).SetEase(Ease.OutBounce);
		Invoke("ShowSubtitle", titleAnimateTime * 0.6f);
		Invoke("HideTitles", titleDisplayTime);
		sfx.Play(sfx.tumble);
	}
	
	void ShowSubtitle() {
		subtitleText.gameObject.SetActive(true);
		subtitleText.transform.localScale = Vector3.zero;
		subtitleText.transform.DOScale(Vector3.one, titleAnimateTime * 0.8f).SetEase(Ease.OutQuint);
	}
	
	void HideTitles() {
		titleText.transform.DOScale(Vector3.zero, titleAnimateTime * 0.5f);
		subtitleText.transform.DOScale(Vector3.zero, titleAnimateTime * 0.25f);
		Invoke("DisableTitles", titleAnimateTime * 1.1f);
	}
	
	void DisableTitles() {
		titleText.gameObject.SetActive(false);
		subtitleText.gameObject.SetActive(false);
	}
	
	
	public void SetupScoreText() {
		scoreText.text = "0";
		if (fadeInScoreText){
			scoreText.color = new Color(scoreText.color.r,
										scoreText.color.g,
										scoreText.color.b,
										0);
		}
		scoreTextNormalSize = scoreText.fontSize;
		scoreTextStartPos = scoreText.transform.position;
		bestTextStartPos = bestScoreText.transform.position;
		replayButtonPos = replayButton.transform.position;
		buttonWidgetsPos = buttonWidgets.transform.position;
		tutorialStartPos = tutorialText.transform.position;
		differentModeButtonPos = selectDifferentGamemodeButton.transform.position;
	}

	public void FadeInScore() {
		scoreText.DOColor(new Color(scoreText.color.r,
									scoreText.color.g,
									scoreText.color.b,
									scoreAlpha), 2f);
	}
	
	public void DisplayLives() {
		if (gameMaster.gameMode == GameMaster.Mode.Timed) {
			Debug.LogError("Can't display lives in timed mode!");
			return;
		}
		livesHolder.SetActive(true);
		foreach (Image heart in hearts) {
			heart.color = liveHeartColour;
			heart.gameObject.SetActive(true);
		}
		RectTransform livesRT = livesHolder.GetComponent<RectTransform>();
		livesRT.anchoredPosition = new Vector2(livesPos.x, livesPos.y - 100f);
		livesRT.DOAnchorPos(livesPos, 0.1f);
		livesRT.localScale = new Vector3(0.6f, 0.6f, 1);
		livesRT.DOScale(Vector3.one, 0.1f);
	}
	
	public void LoseHeart() {
		if (gameMaster.gameMode == GameMaster.Mode.Timed) {
			Debug.LogError("Can't lose hearts in timed mode!");
			return;
		}
		if (gameMaster.lives < 0) {
			return;
		}
		int remainingLives = gameMaster.startLivesNum - gameMaster.lives;
		hearts[remainingLives - 1].DOColor(lostHeartColour, 0.1f);
	}
	
	public IEnumerator ShowTutorialTextAfterSeconds(string text, float delay) {
		yield return new WaitForSeconds(delay);
		ShowTutorialText(text);
	}
	
	public void ShowTutorialText(string text) {
		tutorialText.text = text;
		tutorialText.transform.position = new Vector2(Camera.main.pixelWidth * 0.5f, -Camera.main.pixelHeight * 2f);
		TransitionIn(tutorialText.gameObject, tutorialStartPos, tutorialTransitionTime);
	}
	
	public void HideTutorialText() {
		TransitionOut(tutorialText.gameObject, new Vector2(Camera.main.pixelWidth * 0.5f, -Camera.main.pixelHeight * 0.2f), tutorialTransitionTime);
	}
	
	/// This is a stupidly longwinded way of transitioning all the UI elements in from seperate places off the screen
	public void ShowGameOverScreen(bool bestScoreWasBeaten) {
		// move score text to the centre of the screen
		scoreText.alignment = TextAnchor.MiddleCenter;
		scoreText.transform.DOMove(new Vector2(Camera.main.pixelWidth * 0.5f, Camera.main.pixelHeight * 0.5f), gameOverTransitionInTime * 0.8f);
		DOTween.To(()=> scoreText.fontSize, x=> scoreText.fontSize = x, gameOverScoreTextSize, gameOverTransitionInTime);
		gameMaster.timerIndicator.transform.DOMove(new Vector3(-10, 0, 10), gameOverTransitionInTime);
		
		// move in the "YOU SCORED" text from the top
		youScoredText.transform.position = new Vector2(Camera.main.pixelWidth * 0.5f, Camera.main.pixelHeight * 1.2f);
		TransitionIn(youScoredText, new Vector2(Camera.main.pixelWidth * 0.5f, Camera.main.pixelHeight * 0.7f), gameOverTransitionInTime);
		
		// move in the "PLAY AGAIN?" and "DIFFERENT GAME MODE" buttons from the bottom
		replayButton.transform.position = new Vector2(Camera.main.pixelWidth * 0.5f, Camera.main.pixelHeight * -0.3f);
		TransitionIn(replayButton, replayButtonPos, gameOverTransitionInTime);
		
		selectDifferentGamemodeButton.transform.position = replayButton.transform.position;
		TransitionIn(selectDifferentGamemodeButton, differentModeButtonPos, gameOverTransitionInTime * 1.1f);
		
		// move in the button widgets from the top right
		buttonWidgets.transform.position = new Vector2(buttonWidgetsPos.x + 500, buttonWidgetsPos.y);
		TransitionIn(buttonWidgets, buttonWidgetsPos, gameOverTransitionInTime * 1.1f);
		
		// move in the "BEST" score from the top left and make it dance if best score was beaten
		bestScoreText.transform.position = new Vector2(Camera.main.pixelWidth * -0.5f, Camera.main.pixelHeight * 1.5f);
		TransitionIn(bestScoreText.gameObject, bestTextStartPos, gameOverTransitionInTime * 1.2f);
		if (bestScoreWasBeaten) {
			bestScoreText.transform.rotation = Quaternion.Euler(new Vector3(0, 0, -10));
			bestRotateTween = bestScoreText.transform.DORotate(new Vector3(0, 0, 10), bestScoreRotateTime)
													 .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutQuart);
		}
		if (gameMaster.gameMode == GameMaster.Mode.Timed) {
			modeDisplay.sprite = timerImage;
			bestScoreText.text = PlayerPrefs.GetInt("BestScoreTimed", 0).ToString();
		} else if (gameMaster.gameMode == GameMaster.Mode.Lives) {
			modeDisplay.sprite = livesImage;
			bestScoreText.text = PlayerPrefs.GetInt("BestScoreLives", 0).ToString();
		}
		
		// move out the lives sprites
		if (livesHolder.activeInHierarchy) {
			TransitionOut(livesHolder, new Vector2(livesHolder.transform.position.x, Camera.main.pixelHeight * 1.2f), gameOverTransitionInTime);
		}
		
		sfx.Play(sfx.whoosh);
	}
	
	
	public void HideGameOverScreen() {
		// revert the score text to its in game position in the top right
		scoreText.alignment = TextAnchor.UpperRight;
		scoreText.transform.DOMove(scoreTextStartPos, gameOverTransitionOutTime);
		DOTween.To(()=> scoreText.fontSize, x=> scoreText.fontSize = x, scoreTextNormalSize, gameOverTransitionOutTime);
		
		// transition out the "YOU SCORED" text
		TransitionOut(youScoredText, new Vector2(Camera.main.pixelWidth * 0.5f, Camera.main.pixelHeight * 1.2f), gameOverTransitionOutTime);
		
		// transition out the "PLAY AGAIN?" and "DIFFERENT GAMEMODE" buttons
		TransitionOut(replayButton, new Vector2(Camera.main.pixelWidth * 0.5f, Camera.main.pixelHeight * -0.3f), gameOverTransitionOutTime);
		TransitionOut(selectDifferentGamemodeButton, new Vector2(Camera.main.pixelWidth * 0.5f, Camera.main.pixelHeight * -0.3f),
					  gameOverTransitionOutTime * 0.8f);
					  
		// transition out the button widgets
		TransitionOut(buttonWidgets, new Vector2(Camera.main.pixelWidth * 1.3f, buttonWidgetsPos.y), gameOverTransitionOutTime * 0.9f);
		
		// transition out the "BEST" score
		TransitionOut(bestScoreText.gameObject, new Vector2(Camera.main.pixelWidth*-0.6f, Camera.main.pixelHeight*1.6f), 
					  gameOverTransitionOutTime*0.9f);
		if (bestRotateTween != null && bestRotateTween.IsActive()) { // if best score is wobbling, tween back to upright and stop wobbling
			bestRotateTween.Kill(false);
			bestScoreText.transform.DORotate(Vector3.zero, bestScoreRotateTime / 2f);
		}
		sfx.Play(sfx.whoosh);
	}
	
	public void HideGameOverAndShowModeSelect() {
		HideGameOverScreen();
		Invoke("ShowGameModeSelectScreen", gameOverTransitionOutTime * 0.8f);
	}
	
	/// Let the player choose between timed or lives modes
	public void ShowGameModeSelectScreen() {
		selectModeScreen.SetActive(true);
		selectModeScreen.transform.localScale = Vector3.zero;
		selectModeScreen.transform.DOScale(Vector3.one, gameOverTransitionInTime).SetEase(Ease.OutBounce);
		
		selectModeTimedBest.text = "BEST: " + PlayerPrefs.GetInt("BestScoreTimed", 0).ToString();
		selectModeLivesBest.text = "BEST: " + PlayerPrefs.GetInt("BestScoreLives", 0).ToString();
		
		sfx.Play(sfx.whoosh);
	}
	
	public void TimedModeClicked() {
		gameMaster.gameMode = GameMaster.Mode.Timed;
		TransitionOutGameModeSelectScreenAndStartRound();
	}
	
	public void HeartsModeClicked() {
		gameMaster.gameMode = GameMaster.Mode.Lives;
		TransitionOutGameModeSelectScreenAndStartRound();
	}
	
	void TransitionOutGameModeSelectScreenAndStartRound() {
		selectModeScreen.transform.DOScale(Vector3.zero, gameOverTransitionOutTime);
		Invoke("DisableModeSelect", gameOverTransitionOutTime);
		gameMaster.Invoke("StartRound", gameOverTransitionOutTime * 0.9f);
		sfx.Play(sfx.whoosh, 0.5f);
	}
	
	void DisableModeSelect() {
		selectModeScreen.SetActive(false);
	}
	
	/// Do a bunch of nice tweens to transition a UI element onto the screen
	void TransitionIn(GameObject GO, Vector2 endPoint, float time) {
		GO.SetActive(true);
		
		// move to point
		GO.transform.DOMove(endPoint, time);
		// scale up a little
		GO.transform.localScale = new Vector2(0.8f, 0.8f);
		GO.transform.DOScale(new Vector2(1, 1), time);
	}
	
	/// Do a bunch of nice tweens to transition a UI element onto the screen
	void TransitionOut(GameObject GO, Vector2 endPoint, float time) {
		GO.transform.DOMove(endPoint, time);
		// scale down a little
		GO.transform.localScale = new Vector2(1, 1);
		GO.transform.DOScale(new Vector2(0.8f, 0.8f), time);
		
		StartCoroutine(DisableAfterSeconds(time, GO));
	}
	
	IEnumerator DisableAfterSeconds(float seconds, GameObject GO) {
		yield return new WaitForSeconds(seconds);
		GO.SetActive(false);
	}
	
	public void HideGameOverAndShowInfoScreen() {
		HideGameOverScreen();
		Invoke("ShowInfoScreen", gameOverTransitionOutTime * 0.6f);
		scoreText.gameObject.SetActive(false);
	}
	
	public void HideInfoScreenAndShowGameOver() {
		infoScreen.transform.DOScale(Vector2.zero, gameOverTransitionInTime);
		infoScreen.transform.DOMove(buttonWidgetsPos, gameOverTransitionInTime);
		ShowGameOverScreen(false);
		scoreText.gameObject.SetActive(true);
	}
	
	void DisableInfoScreen() {
		infoScreen.SetActive(false);
	}
	
	void ShowInfoScreen() {
		infoScreen.SetActive(true);
		infoScreen.transform.position = buttonWidgetsPos;
		infoScreen.transform.DOMove(new Vector2(Camera.main.pixelWidth / 2f, Camera.main.pixelHeight / 2f), gameOverTransitionInTime);
		infoScreen.transform.localScale = Vector2.zero;
		infoScreen.transform.DOScale(Vector2.one, gameOverTransitionInTime);
	}
	
	public void TwitterButtonClicked() {
		Application.OpenURL("http://twitter.com/jellyberg");	
	}
	
	public void EmailButtonClicked() {
		Application.OpenURL("mailto:jellyberg+Android.Support@gmail.com");
	}
	
	public void MoreGamesButtonClicked() {
		#if UNITY_IPHONE
			Application.OpenURL("https://itunes.apple.com/gb/developer/adam-binks/id971852747");
		#endif
		#if UNITY_ANDROID
			Application.OpenURL("http://jellyberg.itch.io");
		#endif
	}
}
