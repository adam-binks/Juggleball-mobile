using UnityEngine;

public class Sounds : MonoBehaviour {
	
	public AudioClip click;
	public AudioClip pop;
	public AudioClip smash;
	public AudioClip sproing;
	public AudioClip tumble;
	public AudioClip whoosh;
	public AudioClip wooHoo;
	public AudioClip glug;
	
    private AudioSource audioSource;

	void Start () {
		audioSource = GetComponent<AudioSource>();
	}
	
	public void Play(AudioClip sound, float volume = 1, bool varyVolume = true) {
		if (audioSource == null) {
			audioSource = GetComponent<AudioSource>();
		}
		
		if (varyVolume) {
			audioSource.PlayOneShot(sound, Random.Range(volume - 0.1f, volume + 0.1f));
		} else {
			audioSource.PlayOneShot(sound, volume);
		}
	}
}
