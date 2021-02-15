using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;
using MiniJSON;
using UnityEngine;
using UnityEngine.Audio;

public class ChainSoundPlayer : MonoBehaviour {
	[Header("Ex. 2020-01-01_23-59")] [SerializeField]
	private string startDateTime;

	[Header("Ex. 60(Sec)")] [SerializeField] [Range(1, 120)]
	public int duration;

	[SerializeField] private string _prefix;
	[SerializeField] private string _ip_addr;
	[SerializeField] private bool _clearTemporary = true;
	[SerializeField] private string _currentTrackName;
	[SerializeField] private float _currentPlayPosition = 0f;
	[SerializeField] private int _currentPlayingAudioSource = 0;
	[SerializeField] private STATE _state = STATE.STATE_START;
	[SerializeField] private AudioMixerGroup _audioMixerGroup;
	[SerializeField] private SpectrumUnitManager _spectrumUnitManager;
	[SerializeField] private int _index;

	public string StartDateTime {
		get => startDateTime;
		set => startDateTime = value;
	}

	public int Duration {
		get => duration;
		set => duration = value;
	}

	public string Prefix {
		get => _prefix;
		set => _prefix = value;
	}

	public string IPAddr {
		get => _ip_addr;
		set => _ip_addr = value;
	}

	public bool ClearTemporary {
		get => _clearTemporary;
		set => _clearTemporary = value;
	}

	public string CurrentTrackName {
		get => _currentTrackName;
		set => _currentTrackName = value;
	}

	public float CurrentPlayPosition {
		get => _currentPlayPosition;
		set => _currentPlayPosition = value;
	}

	public int CurrentPlayingAudioSource {
		get => _currentPlayingAudioSource;
		set => _currentPlayingAudioSource = value;
	}

	public STATE State {
		get => _state;
		set => _state = value;
	}

	public AudioMixerGroup AudioMixerGroup {
		get => _audioMixerGroup;
		set => _audioMixerGroup = value;
	}

	public SpectrumUnitManager SpectrumUnitManager {
		get => _spectrumUnitManager;
		set => _spectrumUnitManager = value;
	}

	public int Index {
		get => _index;
		set => _index = value;
	}

	private IList _clipList;
	private string _uuid;
	private const string URL_PREPARE_FILE = "/preparefiles";
	private const string URL_GET_AUDIO_FILE = "/getaudiofile";
	private const string URL_CLEAR_TEMPORARY_DIR = "/clear";

	private int _currentTrackNumber = 0;
	private AudioSource[] _audioSources;
	private bool _loadingDone = false;
	private int _frameCount = 0;
	private string _lastLoadedTrackFile = "";

	public enum STATE {
		// ReSharper disable once InconsistentNaming
		STATE_START,
		STATE_CLEAR_REQUEST,
		STATE_CLEAR_DONE,
		STATE_PREPARE_REQUEST,
		STATE_PREPARE_DONE,
		STATE_DOWNLOAD_REQUEST,
		STATE_DOWNLOAD_DONE,
		STATE_RUNNING,
		STATE_COMPLETE
	}

	// Use this for initialization
	void Start() {
		this._audioSources = GetComponents<AudioSource>();
		foreach (AudioSource src in this._audioSources) {
			src.outputAudioMixerGroup = this._audioMixerGroup;
		}
		if (this._clearTemporary == true) {
			StartCoroutine(ClearTemporaryFiles());
		}
		else {
			_state = STATE.STATE_CLEAR_DONE;
		}
	}

	private IEnumerator ClearTemporaryFiles() {
		_state = STATE.STATE_CLEAR_REQUEST;
		Debug.Log("Clear temp dir.");
		var url = this._ip_addr + URL_CLEAR_TEMPORARY_DIR;
		var request = new WWW(url);
		yield return request;
		_state = STATE.STATE_CLEAR_DONE;
	}

	private IEnumerator PrepareAudioClip() {
		_state = STATE.STATE_PREPARE_REQUEST;
		var url = this._ip_addr + URL_PREPARE_FILE + "/" + this.startDateTime + "/" + this.duration + "/" + this._prefix;
		Debug.Log("Prepare audio clip : "+ url);

		var request = new WWW(url);
		yield return request;

		if (request.responseHeaders.ContainsKey("STATUS") && request.responseHeaders["STATUS"].Contains("200")) {
			_state = STATE.STATE_PREPARE_DONE;
			var text = request.text;
			var js = (IDictionary) Json.Deserialize(text);
			var temp = js["Items"];
			this._uuid = (string) js["uuid"];
			this._clipList = (IList) temp;
			var loadFile = (string) this._clipList[0];
			StartCoroutine(SetAudioClip(loadFile, 0));
		}
		else {
			yield return new WaitForSeconds(1);
			Debug.Log("retry...");
			PrepareAudioClip();
		}
	}

	private IEnumerator SetAudioClip(string fileName, int audioSourceNumber) {
		_lastLoadedTrackFile = fileName;
		Debug.Log(fileName);

		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		sw.Start();

		var url = this._ip_addr + URL_GET_AUDIO_FILE + "/" + this._uuid + "/" + fileName;
		var request = new WWW(url);
		yield return request;

		if (request.responseHeaders.ContainsKey("STATUS") && request.responseHeaders["STATUS"].Contains("200")) {
			var audioSource = (AudioSource) this._audioSources[audioSourceNumber];
			audioSource.clip = request.GetAudioClip(false, false);
			if (_currentTrackNumber == 0) {
				if (_spectrumUnitManager != null) {
					_spectrumUnitManager.SetAudioSource(audioSource, this._index);
					string[] delimiter = {"__"};
					var freq = fileName.Split(delimiter, StringSplitOptions.RemoveEmptyEntries)[0];
					var waveName = fileName.Split(delimiter, StringSplitOptions.RemoveEmptyEntries)[1];
					_spectrumUnitManager.SetSpectrumLabel($"{waveName}({freq})", this._index);
				}
				audioSource.Play();
				sw.Stop();
				Debug.Log(sw.ElapsedMilliseconds + "ms");
				_currentTrackName = _lastLoadedTrackFile;
				_state = STATE.STATE_RUNNING;
			}
			else {
				_state = STATE.STATE_DOWNLOAD_DONE;
			}

			_currentTrackNumber++;
		}
		else {
			yield return new WaitForSeconds(2);
			Debug.Log("retry...");
			SetAudioClip(fileName, audioSourceNumber);
		}
	}
  
	// Update is called once per frame
	void FixedUpdate() {
		if (_state == STATE.STATE_CLEAR_DONE) {
			StartCoroutine(PrepareAudioClip());
		}

		if (_state < STATE.STATE_DOWNLOAD_REQUEST) return;

		var currentAudioSource = this._audioSources[_currentPlayingAudioSource];
		if (currentAudioSource.isPlaying) {
			if (currentAudioSource.time >= 30f && _state == STATE.STATE_RUNNING) {
				_state = STATE.STATE_DOWNLOAD_REQUEST;
			}

			if (_state == STATE.STATE_DOWNLOAD_REQUEST) {
				if (_currentTrackNumber == this.duration+ _index*2) {
					_state = STATE.STATE_COMPLETE;
					return;
				}

				var fileName = (string)this._clipList[_currentTrackNumber];
				if (_lastLoadedTrackFile != fileName) {
					StartCoroutine(SetAudioClip(fileName, _currentPlayingAudioSource == 0 ? 1 : 0));
				}
			}
		}
		else {
			if (_state == STATE.STATE_DOWNLOAD_DONE) {
				_currentPlayingAudioSource = (_currentPlayingAudioSource == 0) ? 1 : 0;
				if (_spectrumUnitManager != null) {
					_spectrumUnitManager.SetAudioSource(this._audioSources[_currentPlayingAudioSource], this._index);
				}
				this._audioSources[_currentPlayingAudioSource].Play();
				_currentTrackName = _lastLoadedTrackFile;
				_state = STATE.STATE_RUNNING;
			}
		}

		if (_frameCount++ % 60 == 0) {
			_currentPlayPosition = currentAudioSource.time;
		}
	}
}