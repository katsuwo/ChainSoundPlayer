using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpectrumUnitManager : MonoBehaviour {
	private List<GameObject> spectrumList = new List<GameObject>();
	private List<GameObject> labelList = new  List<GameObject>();
	
	// Start is called before the first frame update
	void Start() {
		var canvas = GameObject.Find("SpectrumCanvas");
		if (canvas == null) {
			Debug.Log("Spectrum Canvas not found.");
			return;
		}

		for (var i = 0; i < canvas.transform.childCount; i++) {
			var child = canvas.transform.GetChild(i).gameObject;
			if (child.name.IndexOf("Spectrum") >= 0) {
				spectrumList.Add(child);
				var simpleSpectrum = child.GetComponent<SimpleSpectrum>();
				simpleSpectrum.isEnabled = false;
				continue;
			}
			if (child.name.IndexOf("Text") >= 0) {
				labelList.Add(child);
			}
		}
		Debug.Log(spectrumList.Count);
	}

	public void SetAudioSource(AudioSource src, int index) {
		if (index >= spectrumList.Count) return;
		var spectrum = spectrumList[index];
		if (spectrum == null) {
			Debug.Log("Specified spectrum is not found.");
			return;
		}

		var simpleSpectrum = spectrum.GetComponent<SimpleSpectrum>();
		simpleSpectrum.audioSource = src;
		simpleSpectrum.sourceType = SimpleSpectrum.SourceType.AudioSource;
		simpleSpectrum.isEnabled = true;
	}

	public void SetSpectrumLabel(string text, int index) {
		if (index >= labelList.Count) return;
		var label = labelList[index];
		if (label == null) {
			Debug.Log("Specified label is not found.");
			return;
		}

		var textMeshPro = label.GetComponent<TextMeshProUGUI>();
		textMeshPro.text = text;
	}
	
	// Update is called once per frame
	void Update() { }
}