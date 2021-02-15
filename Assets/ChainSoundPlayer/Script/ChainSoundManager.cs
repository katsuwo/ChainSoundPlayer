using System;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public class ChainSoundManager : MonoBehaviour {
	private const string URL_FREQ_LIST = "/freqlist";
	[SerializeField] private string ip_addr;
	[SerializeField] private AudioMixerGroup _audioMixerGroup;
	[SerializeField] private AudioMixer _audioMixer;
	[SerializeField] private SpectrumUnitManager _spectrumUnitManager;
	[FormerlySerializedAs("recordedDate")] [Header("Ex. 2020-01-01_23-59")] [SerializeField]
	private string recordedDateTime;
	private IList _freqList;
	[SerializeField] public STATE _state = STATE.STATE_START;
	
	public enum STATE {
		STATE_START,
		STATE_FREQLIST_REQUEST,
		STATE_FREQLIST_DONE,
		STATE_RUNNING,
		STATE_COMPLETE
	}
	
	// Use this for initialization
	void Start () {
		var targetDate = recordedDateTime.Split('_')[0];
		StartCoroutine(GetFrequencyListAtTime(targetDate));
	}
	

	// Update is called once per frame
	void Update () {
		if (_state < STATE.STATE_FREQLIST_DONE) return;
		if (_state == STATE.STATE_FREQLIST_DONE) {
			var i = 0;
			foreach (var freq in this._freqList) {
				var soundPlayerPrefab = (GameObject) Resources.Load("ChainSoundPlayer");
				var soundPlayer = Instantiate(soundPlayerPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
				var player = (ChainSoundPlayer)soundPlayer.GetComponent<ChainSoundPlayer>();

				if (!_spectrumUnitManager.Equals(null)) {
					player._spectrumUnitManager = _spectrumUnitManager;
				}
				else {
					Debug.Log("Spectrum Manager not defined.");
				}

				if (!_audioMixer.Equals(null)) {
					var grp = _audioMixer.FindMatchingGroups($"wave{i+1}");
					player._audioMixerGroup = grp[0];
				}
				
				player.index = i;				
				player.ip_addr = this.ip_addr;
				player.prefix = (string)freq;
				player.startDateTime = this.recordedDateTime;
				player.duration = 60;
				player.clearTemporary = false;
				_state = STATE.STATE_RUNNING;
				i++;
			}
		}
	}
	
	private IEnumerator GetFrequencyListAtTime(string targetTime) {
		_state = STATE.STATE_FREQLIST_REQUEST;
		var url = this.ip_addr + URL_FREQ_LIST + "/" + targetTime;
		Debug.Log("Request frequency list: " + url);
		var request = new WWW(url);
		yield return request;

		if (request.responseHeaders.ContainsKey("STATUS") && request.responseHeaders["STATUS"].Contains("200")) {
			var text = request.text;
			var js = (IDictionary) Json.Deserialize(text);
			var temp = js["Items"];
			this._freqList = (IList) temp;
			_state = STATE.STATE_FREQLIST_DONE;
		}
	}

	private void SetupChainSoundPlayer() {
		
	}
}
