using UnityEngine;
using System.Collections;
using DG.Tweening;

public class CircleControl : MonoBehaviour {

	[HeaderAttribute("Gameplay")]
	public float upThrustOnTap;
	[HeaderAttribute("Juice")]
	public float scaleOnTap;
	public float tapScaleTime;
	public float blinkOnTapTime;
	public float animateInTime;
	[RangeAttribute(0, 1)]
	public float frozenMinAlpha;
	[RangeAttribute(0, 1)]
	public float frozenMaxAlpha;
	public float frozenPulseTime;
	public float deathQuiverStrength;
	public float deathQuiverTime;
	[HeaderAttribute("Ragdoll attributes")]
	public GameObject ragdollGO;
	[RangeAttribute(0, 1)]
	public float ragdollAlpha;
	public float ragdollAlphaFadeTime;
	public float ragdollMaxBounceX;
	public float ragdollMinBounceY;
	public float ragdollMaxBounceY;
	public CircleCollider2D deathCollider; // accurate collider to detect screen edge collision
	public CircleCollider2D tapCollider; // forgiving collider to detect touch input
	
	private GameMaster gameMaster;
	[HideInInspector]
	public float gravityScale;
	[HideInInspector]
	public int ballSlot;
	[HideInInspector]
	public float actualRadius;
	[HideInInspector]
	public Rigidbody2D thisRigidbody2D;
	[HideInInspector]
	public bool isFrozen = true;
	private SpriteRenderer thisSpriteRenderer;
	private Vector3 screenBottomLeft;
	private Vector3 screenTopRight;
	private Tweener frozenPulser;
	private EyeControl eyeControl;
	private float thisScale;
	private Sounds sfx;


	void Awake() {
		thisRigidbody2D = GetComponent<Rigidbody2D>();
		thisSpriteRenderer = GetComponent<SpriteRenderer>();
		eyeControl = GetComponent<EyeControl>();
		gameMaster = GameObject.Find("SCRIPTS").GetComponent<GameMaster>();
		sfx = gameMaster.gameObject.GetComponent<Sounds>();
		
		screenBottomLeft = Camera.main.ScreenToWorldPoint(Vector3.zero);
		screenTopRight = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight));
		
		gravityScale = thisRigidbody2D.gravityScale; // grab this so it can be reverted to when unfrozen
	}
	
	void Update () {
		// check if the circle has hit the top or bottom of the screen
		if (transform.position.y + actualRadius > screenTopRight.y ||
			transform.position.y - actualRadius < screenBottomLeft.y) {
			KillBall();
			return;
		}
		if (!gameMaster.roundInProgress) { // kill all balls when the timer runs out
			KillBall();
		}
	}
	
	/// scale everything by float scale and assign a gravityScale based on ball size
	public void SetupScale(float scale) {
		thisScale = scale;
		transform.localScale = new Vector3(scale, scale, 1);
		gravityScale = 1 + (scale * 0.5f); // bigger balls fall a bit faster
		actualRadius = deathCollider.radius * scale;
		
		// do a nice spawn animation
		transform.localScale = Vector3.zero;
		transform.DOScale(new Vector3(scale, scale, 1), animateInTime).SetEase(Ease.OutBounce);
		
		eyeControl.SetupSpacingAndSize();
	}

	public void OnTap () {
		// hurl the ball upwards
		thisRigidbody2D.velocity = new Vector2(0, upThrustOnTap);
		// if the ball is frozen, unfreeze it so it starts falling
		if (isFrozen) {
			Unfreeze();
		}
		// do some satisfying effects
		if (transform.localScale.x == thisScale) { // if not already inflated (prevent infinite inflation bug)
			transform.DOPunchScale(new Vector2(scaleOnTap * thisScale, scaleOnTap * thisScale), tapScaleTime);
		}
		eyeControl.Blink(blinkOnTapTime);
		sfx.Play(sfx.glug);
	}
	
	#if UNITY_EDITOR
	void OnMouseDown() {
		OnTap(); // replace touch input with mouse input when in editor for debugging
	}
	#endif
	
	/// Destroy the ball and remove all references
	void KillBall() {
		gameMaster.RemoveBall(this.gameObject, this); // allow future balls to be spawned near here etc
		GameObject.Destroy(this.gameObject);
		SpawnRagdoll();
	}
	
	/// Hold the ball in place until tapped, and visually indicate this
	public void Freeze() {
		thisRigidbody2D.gravityScale = 0;
		// reduce alpha while frozen
		thisSpriteRenderer.color = new Color(thisSpriteRenderer.color.r, 
												thisSpriteRenderer.color.g, 
												thisSpriteRenderer.color.b, 
												frozenMinAlpha);
		frozenPulser = thisSpriteRenderer.DOColor(new Color(thisSpriteRenderer.color.r, 
															thisSpriteRenderer.color.g, 
															thisSpriteRenderer.color.b, 
															frozenMaxAlpha),
															frozenPulseTime).SetLoops(-1, LoopType.Yoyo);
		eyeControl.eyesGO.transform.localScale = new Vector2(1, 0);
	}
	
	void Unfreeze() {
		isFrozen = false;
		gameMaster.activeUnfrozenBalls.Add(gameObject);
		thisRigidbody2D.gravityScale = gravityScale;
		frozenPulser.Kill();
		thisSpriteRenderer.DOColor(new Color(thisSpriteRenderer.color.r, 
											 thisSpriteRenderer.color.g, 
											 thisSpriteRenderer.color.b, 
											 1),
											 frozenPulseTime / 2f);
		eyeControl.WakeUp();
		
		// if this is the first ball, when it is unfrozen start the round timer
		if (gameMaster.roundInProgress && gameMaster.roundStartTime == -1) {
			gameMaster.StartTimer();
		}
	}
	
	/// A non-interactive ragdoll is spawned
	void SpawnRagdoll() {
		GameObject ragdoll = (GameObject)Instantiate(ragdollGO, transform.position, transform.rotation);
		// make the ragdoll identical to this ball
		SpriteRenderer ragdollSR = ragdoll.GetComponent<SpriteRenderer>();
		ragdollSR.transform.localScale = this.transform.localScale;
		ragdollSR.color = thisSpriteRenderer.color;
		// quickly fade the alpha to ragdollAlpha
		ragdollSR.DOColor(new Color(ragdollSR.color.r,
									ragdollSR.color.g,
									ragdollSR.color.b,
									ragdollAlpha),
									ragdollAlphaFadeTime);
		Rigidbody2D ragdollRB = ragdoll.GetComponent<Rigidbody2D>();
		float xForce = Random.Range(-ragdollMaxBounceX, ragdollMaxBounceX);
		ragdollRB.AddForce(new Vector2(xForce,
									   Random.Range(ragdollMinBounceY, ragdollMaxBounceY)),
									   ForceMode2D.Impulse);
		ragdollRB.AddTorque(xForce, ForceMode2D.Impulse);
		ragdoll.transform.DOShakeScale(deathQuiverTime, deathQuiverStrength);
		GameObject.Destroy(ragdoll, 5); // destroy ragdolls after 5 seconds
		
		// give the ragdoll this ball's eyes
		eyeControl.eyesGO.transform.SetParent(ragdoll.transform);
		eyeControl.RagdollEyes();
	}
}
