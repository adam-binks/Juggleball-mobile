using UnityEngine;
using System.Collections;

public class BallAccessories : MonoBehaviour {
	public SpriteRenderer accessorySpriteRenderer;
	public Sprite[] accessories;
	[RangeAttribute(0, 1)]
	public float chanceOfAccessory;
	public GameObject eyes;
	
	/// Randomly apply a random accessory sprite to some balls
	public void AddRandomAccessory() {
		if (Random.Range(0f, 1f) < chanceOfAccessory) {
			accessorySpriteRenderer.sprite = accessories[Random.Range(0, accessories.Length)];
			eyes.SetActive(false);
		}
	}
}
