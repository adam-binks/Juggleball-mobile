using UnityEngine;
using System.Collections;
using DG.Tweening;

public class EyeControl : MonoBehaviour {
	
	public GameObject eyesGO;
	public GameObject[] irises;
	public GameObject[] pupils;
	[HeaderAttribute("Size and spacing")]
	[RangeAttribute(0, 1)]
	public float eyeSpacingMin;
	[RangeAttribute(0, 1)]
	public float eyeSpacingMax;
	[RangeAttribute(0, 2)]
	public float eyeSizeMin;
	[RangeAttribute(0, 2)]
	public float eyeSizeMax;
	[HeaderAttribute("Animation")]
	public float maxPupilMove;
	public float unfreezeOpenTime;
	public float deathShrinkTime;
	public float blinkTime;
	public float minBlinkInterval;
	public float maxBlinkInterval;
	
	private float eyeSpacing;
	private float eyeSize;
	private CircleControl circleControl;
	
	
	/// Slightly randomise eye size and spacing to make things a bit more interesting
	public void SetupSpacingAndSize() {
		circleControl = GetComponent<CircleControl>();
		
		eyeSpacing = Random.Range(eyeSpacingMin, eyeSpacingMax); // adjust position proportionally to ball size
		irises[0].transform.localPosition = new Vector2(-eyeSpacing * circleControl.actualRadius, 0);
		irises[1].transform.localPosition = new Vector2(eyeSpacing * circleControl.actualRadius, 0);
		
		eyeSize = Random.Range(eyeSizeMin, eyeSizeMax); // don't adjust size proportionally - small balls with big eyes are fun
		irises[0].transform.localScale = new Vector2(eyeSize, eyeSize);
		irises[1].transform.localScale = new Vector2(eyeSize, eyeSize);
	}
	
	
	void Update() {
		foreach (GameObject pupil in pupils) {
			float velocity = circleControl.thisRigidbody2D.velocity.y;
			if (velocity > 10) {
				velocity = 10;
			} else if (velocity < -10) {
				velocity = -10;
			}
			float oldY = pupil.transform.localPosition.y;
			float newY = oldY + ((velocity / 10f) - oldY) * 0.1f;
			pupil.transform.localPosition = new Vector2(pupil.transform.localPosition.x, newY);
		}
	}
	
	/// Animation on unfreeze
	public void WakeUp() {
		eyesGO.transform.DOScale(new Vector2(1, 1), unfreezeOpenTime);
		StartBlinkTimer(); // REALISM
	}
	
	/// Animation on ball drop
	public void RagdollEyes() {
		// shrink the eyes to nothingness
		eyesGO.transform.DOScale(new Vector2(1, 0), deathShrinkTime);
	}
	
	public void Blink(float time) {
		eyesGO.transform.DOScale(new Vector2(1, 0), time * 0.5f);
		StartCoroutine(UnblinkAfter(time * 0.5f));
	}
	
	IEnumerator UnblinkAfter(float seconds) {
		yield return new WaitForSeconds(seconds);
		eyesGO.transform.DOScale(new Vector2(1, 1), seconds);
	}
	
	/// Make the balls blink every now and then, with varying time between blinks
	IEnumerator BlinkAtRandom(float seconds) {
		yield return new WaitForSeconds(seconds);
		Blink(blinkTime);
		// start the next blink timer if the ball isn't dead
		if (circleControl.isActiveAndEnabled) {
			StartCoroutine(BlinkAtRandom(Random.Range(minBlinkInterval, maxBlinkInterval)));
		}
	}
	
	void StartBlinkTimer() {
		StartCoroutine(BlinkAtRandom(Random.Range(minBlinkInterval, maxBlinkInterval)));
	}
}
