using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.UI;
using Helpers;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour {

	public float TimeLeft {get; private set;}
	public Text TimeLeftText;

	private float _currentStartingBudget = 50000.0f;
	public float Budget { get; private set;}
	public Text BudgetLeftText;

	private float _bestGardenBar = 10.0f;

	private bool _started = false;
	private bool _finished = false;
	private bool _countingDown = false;

	private float _countdownTime = 2.5f;

	private ObjectManager _objectManager;
	private AudioManager _audioManager;
	private UiManager _uiManager;

	public Text StartText;
	public Text FinishText;
	public GameObject UnlockContainer;

	public GameObject DiffHolder;
	public GameObject CountdownHolder;

	private Tile _selectedTile;
	public Image SelectedDisplay;
	public Sprite SelectedLocked;

	private GameObject[] _finishedTiles;

	private List<GameObject> _currentUnlocks = new List<GameObject>();
	public GameObject UnlockImage;
	public Transform UnlockHolder;

	public Vector3 DefaultCameraPosition;
	private bool _screenshakeActive = false;
	private float _screenshakeTime = 0.5f;
	public AnimationCurve IntensityCurve;

	//TODO: Add what the judge likes/dislikes.
	private int _judgeLikesOverlay = 0;
	private int _judgeHatesOverlay = 0;
	private bool _judgeHatesPavement = false;

	//Stats
	private int _gardensCompleted = 0;
	private int _bestGardens = 0;

	private int _selectedDifficulty = 0;

	private int _selectedAmount = 0; //0 = 1 flower, 1 = 2 flowers, 2 = 3 flowers.

	private bool _infinityMode = false;

	private int _unlockLevel = 1;

	//Input
	private bool _axisInUse = false;

	private bool _alreadyDeleted = false;

	//Extras
	private bool _angryTrowel = false;
	private int _tightness = 20;
	private int _elasticity = 8;

	public List<string> DidItFail;
	public List<string> DidItSuccess;
	public List<string> GardenTextPart;
	public List<string> KeepItUp;
	public List<string> LowBudget;
	public List<string> LikesHedges;
	public List<string> LikesFlowers;
	public List<string> HatesPavement;
	public List<string> HatesHedges;
	public List<string> HatesFlowers;

	private bool _runTutorial = true;

	// Use this for initialization
	void Start () {
		Random.InitState(System.DateTime.Now.GetHashCode());
		DefaultCameraPosition = Camera.main.transform.position;
		InitRefs ();
	}

	private void InitRefs(){
		_objectManager = GetComponent<ObjectManager> ();
		_audioManager = GetComponent<AudioManager>();
		_uiManager = GetComponent<UiManager> ();
	}

	// Update is called once per frame
	void Update () {
		UpdateUI ();

		if (_started && !_finished && !_infinityMode) {
			TimeCheck (() => {
				FinishGame();
			});
		}

		if(_started){
			CheckInput ();
		}

		if(!_started && !_finished && _countingDown){
			CountdownCheck ();
		}

		if (!_started && !_finished && !_countingDown)
		{
			CheckAngryTrowelMode();
		}
		CameraShake();
	}

	//Time Stuff
	private void TimeCheck(Action noTime){
		if (TimeLeft <= 0.0f)
			noTime.Invoke ();
		else
			TimeLeft -= Time.deltaTime;
	}

	//Level Stuff
	public void SetDifficulty(int diff){

		_selectedDifficulty = diff;
		if (_angryTrowel)
		{
			_tightness = 5;
			_elasticity = 5;
		}
		_objectManager.CheckUnlock (true, _unlockLevel);
		ReInitialise (diff,true);
		DiffHolder.SetActive (false);
		CountdownHolder.SetActive (true);
		StartCoroutine (LoadLevel ());
	}

	private void ReInitialise(int diff, bool initial)
	{
		switch(diff){
		case(0):
			//TODO: Easy
			TimeLeft = 60.0f;
			_bestGardenBar *= 1.25f;
			if (initial) {
				Budget = _currentStartingBudget;
			}
			break;
		case(1):
			//TODO: Medium
			TimeLeft = 45.0f;
			_bestGardenBar *= 2.0f;
			if (initial) {
				Budget = _currentStartingBudget / 2.0f;
			}
			break;
		case(2):
			//TODO: Hard
			TimeLeft = 30.0f;
			_bestGardenBar *= 3.0f;
			if (initial) {
				Budget = _currentStartingBudget / 4.0f;
			}
			break;
		case(3):
			//TODO: Infinity Mode
			TimeLeft = 60.0f;
			Budget = 1000000.0f;
			_infinityMode = true;
			break;
		default:
			//TODO: Default to Easy
			TimeLeft = 60.0f;
			if (initial) {
				Budget = _currentStartingBudget;
			}
			_selectedDifficulty = 0;
			break;
		}

		if (!_infinityMode)
			_countdownTime = 2.5f;
		else
			_countdownTime = 0.0f;
		SelectJudgeTastes ();
		_unlockLevel = PlayerPrefs.GetInt ("UnlockLevel");
		_objectManager.SetupUnlocks (_unlockLevel);
	}

	IEnumerator LoadLevel(){
		//TODO: Level loading stuff.
		var dirt = _objectManager.GetBaseByType ((int)BaseTypes.Dirt);
		for(var x = 0; x < 10; x++){
			for(var y = 0; y < 10; y++){
				GameObject go = GameObject.Instantiate (_objectManager.TilePrefab, new Vector3 (x, y, 0.0f), _objectManager.TilePrefab.transform.rotation) as GameObject;
				var tile = go.GetComponent<Tile> ();
				tile.Initialise (x, y,SelectBaseSprite(x,y,dirt[0].GetSprites()));
			}
		}
		_objectManager.ResetSelected ();
		SelectedDisplay.sprite = dirt [0].GetDisplaySprite(0);
		_countingDown = true;
		yield return null;
	}

	private void SelectJudgeTastes(){
		_judgeHatesPavement = Random.Range (0, 50) % 10 == 0;
		_judgeLikesOverlay = 1 - (Random.Range (10, 30)/10);
		var differentHate = false;
		var retries = 0;
		while(!differentHate){
			_judgeHatesOverlay = 1 - (Random.Range (10, 30) / 10);

			if(retries > 9){
				_judgeHatesOverlay = 0;
				_judgeLikesOverlay = 0;
			}

			if (_judgeHatesOverlay == 0 && _judgeLikesOverlay == 0 || _judgeHatesOverlay != _judgeLikesOverlay)
				differentHate = true;
			retries++;
		}
	}

	public void UpdateSelectedTile(Tile tile){
		_selectedTile = tile;
	}

	private void SetupNextLevel(){
		ResetTiles();
		_countdownTime = 3.0f;

		ReInitialise (_selectedDifficulty, false);
		StartCoroutine (LoadLevel ());
	}

	private void ResetTiles()
	{
		_started = false;
		foreach(var obj in _finishedTiles){
			Destroy (obj);
		}
		foreach(var obj in _currentUnlocks){
			Destroy (obj);
		}
		_finishedTiles = null;
		_finished = false;
	}

	//Finishing Stuff.
	private void FinishGame(){
		_finished = true;
		_uiManager.ShowFinish (() => {
			_audioManager.PlaySoundFx(SfxType.FinishRound);
			var oldBestGarden = _bestGardens;
			_finishedTiles = GameObject.FindGameObjectsWithTag("Tile");
			if (!_infinityMode)
			{
				_gardensCompleted++;
				GenerateFinishText();
				if (oldBestGarden < _bestGardens)
				{
					_unlockLevel += 2;
				}
				else
				{
					_unlockLevel++;
				}
				UnlockContainer.SetActive(true);
			}

			PlayerPrefs.SetInt("UnlockLevel",_unlockLevel);
			var unlockedTiles = _infinityMode ? null : _objectManager.CheckUnlock(false,_unlockLevel);

			if(unlockedTiles != null){
				foreach(var tile in unlockedTiles){
					GameObject go = GameObject.Instantiate(UnlockImage,UnlockHolder) as GameObject;
					go.GetComponent<Image>().sprite = tile.GetSprites()[0];
					_currentUnlocks.Add(go);
				}
			}
			else
			{
				UnlockContainer.SetActive(false);
			}
			_uiManager.HideHud(()=>{});
		});
	}

	private void GenerateFinishText(){
		var score = 0.0f;
		foreach(var obj in _finishedTiles){
			var currentTile = obj.GetComponent<Tile> ();
			if (_judgeHatesOverlay == (int)currentTile.CurrentOverlayType && _judgeHatesOverlay != 0)
				score -= currentTile.TileValue;
			else if (_judgeLikesOverlay == (int)currentTile.CurrentOverlayType && _judgeLikesOverlay != 0)
				score += currentTile.TileValue * 2.0f;
			else if (_judgeHatesPavement && (int)currentTile.CurrentBaseType == 2)
				score -= currentTile.TileValue * 2.0f;
			else
				score += currentTile.TileValue;
		}

		var didIt = DidItFail[Random.Range(0, DidItFail.Count)] + "\n\n";
		Debug.Log("Scored " + score + " out of " + _bestGardenBar);
		if(score > _bestGardenBar){
			didIt = DidItSuccess[Random.Range(0, DidItSuccess.Count)] + "\n\n";
			_bestGardens++;
		}

		var gardenText = GardenTextPart[Random.Range(0,GardenTextPart.Count)] + " " + _bestGardens + " out of " + _gardensCompleted + "\n\n";


		var keepItUp = KeepItUp[Random.Range(0,KeepItUp.Count)] + " \n\n";
		if(Budget < 500.0f){
			keepItUp = LowBudget[Random.Range(0,LowBudget.Count)] + " \n\n";
		}

		FinishText.text = didIt + gardenText + keepItUp;
	}

	//Control Stuff
	private void CheckInput(){
		CheckMousePosition ();
		CheckMouseInput ();
		CheckKeyboardInput ();
	}

	private void CheckMousePosition(){
		if (!_finished) {
			var ray = Physics2D.Raycast (Camera.main.ScreenToWorldPoint (Input.mousePosition), new Vector2 (0.0f, 0.0f));
			if (ray.transform == null) {
				if (_selectedTile != null) {
					_selectedTile.UpdateSelection (false);
					UpdateSelectedTile (null);
				}
				return;
			}

			if (ray.transform.gameObject.CompareTag("Tile"))
			{
				var tileXY = "";
				if (_selectedTile != null)
				{
					tileXY = _selectedTile.X.ToString() + _selectedTile.Y.ToString();
					_selectedTile.UpdateSelection(false);
				}
				UpdateSelectedTile (ray.transform.gameObject.GetComponent<Tile> ());
				_selectedTile.UpdateSelection (true);
				var tileXYNew = _selectedTile.X.ToString() + _selectedTile.Y.ToString();
				if (!tileXY.Equals(tileXYNew))
				{
					_alreadyDeleted = false;
				}

			}
		}
	}

	private void CheckMouseInput(){
		if (!_finished) {
			if (Input.GetMouseButton (0)) {
				if (_selectedTile != null && _objectManager.IsSelectedUnlocked()) {
					if(Budget <= 0.0f){
						FinishGame ();
					}
					var obj = _objectManager.GetSelected ();
					if (obj.IsOverlay && _selectedTile.CurrentOverlayType != (OverlayTypes)obj.Type) {
						_selectedTile.SetOverlay (obj.GetSprites()[_selectedAmount], (OverlayTypes)obj.Type, obj.Value,obj.ParticleMaterial);
						Budget -= obj.Cost;
						_audioManager.PlaySoundFx(SfxType.PlaceTile,false,true);
						ActivateCameraShake();
					} else if (!obj.IsOverlay && _selectedTile.CurrentBaseType != (BaseTypes)obj.Type) {
						_selectedTile.SetBase (SelectBaseSprite(_selectedTile,obj.GetSprites()), (BaseTypes)obj.Type, obj.Value,obj.ParticleMaterial);
						Budget -= obj.Cost;
						_audioManager.PlaySoundFx(SfxType.PlaceTile,false,true);
						ActivateCameraShake();
					}


				}
			} else if (Input.GetMouseButton (1)) {
				if (!_alreadyDeleted && _selectedTile != null) {
					var dirt = _objectManager.GetBaseByType ((int)BaseTypes.Dirt);
					_selectedTile.SetOverlay (null, OverlayTypes.None, 0.0f,dirt[0].ParticleMaterial,false);
					_selectedTile.SetBase (SelectBaseSprite(_selectedTile,dirt[0].GetSprites()), (BaseTypes)dirt [0].Type, dirt [0].Value,dirt[0].ParticleMaterial);
					Budget -= dirt [0].Cost;
					ActivateCameraShake();
					_audioManager.PlaySoundFx(SfxType.DeleteTile,false,true);
					_alreadyDeleted = true;
				}
			}
		}
	}

	private Sprite SelectBaseSprite(Tile tile, Sprite[] tileObject){
		if(tile.X == 0 && tile.Y == 0){
			return tileObject[3];
		}else if(tile.X == 0 && tile.Y == 9){
			return tileObject[1];
		}else if(tile.X == 9 && tile.Y == 0){
			return tileObject[4];
		}else if(tile.X == 9 && tile.Y == 9){
			return tileObject[2];
		}else{
			return tileObject[0];
		}
	}

	private Sprite SelectBaseSprite(int x, int y, Sprite[] tileObject){
		if(x == 0 && y == 0){
			return tileObject[3];
		}else if(x == 0 && y == 9){
			return tileObject[1];
		}else if(x == 9 && y == 0){
			return tileObject[4];
		}else if(x == 9 && y == 9){
			return tileObject[2];
		}else{
			return tileObject[0];
		}
	}

	private void CameraShake()
	{
		if (_screenshakeActive)
		{

			if (_screenshakeTime > 0.0f)
			{
				var val = IntensityCurve.Evaluate(MathHelper.ValueConversion(_screenshakeTime, 0.0f, 0.5f, 0.01f, 1.0f)) * _elasticity;
				Camera.main.transform.position = (Random.insideUnitSphere / val)/ _tightness + DefaultCameraPosition;
				_screenshakeTime -= Time.deltaTime;
			}
			else
			{
				_screenshakeTime = 0.5f;
				Camera.main.transform.position = DefaultCameraPosition;
				_screenshakeActive = false;
			}

		}
	}

	private void ActivateCameraShake()
	{
		_screenshakeActive = true;
		_screenshakeTime = 0.5f;
	}

	private void CheckKeyboardInput()
	{
		var vertAxis = Input.GetAxis("Vertical");
		var horizAxis = Input.GetAxis("Horizontal");
		if (!_finished) {

			if (horizAxis < -0.1f && !_axisInUse) {
				ChangeSelected(false);
			} else if (horizAxis > 0.1f && !_axisInUse) {
				ChangeSelected(true);
			}else if (vertAxis > 0.1f && !_axisInUse){
				ChangeAmount(true);
			}else if (vertAxis < -0.1f &&  !_axisInUse){
				ChangeAmount(false);
			}
			else if(Input.GetButtonDown("Select")){
				FinishGame ();
			}
			else if ((horizAxis > -0.1f && horizAxis < 0.1f && vertAxis > -0.1f && vertAxis < 0.1f) && _axisInUse)
			{
				_axisInUse = false;
			}
		}
		else{
			if(Input.GetKeyDown(KeyCode.Escape)) {
				Application.Quit ();
			}
			else if(Input.GetButtonDown("Select"))
			{
				NextGarden();
			}
		}
	}

	private void CheckAngryTrowelMode()
	{
		if (Input.GetKeyDown(KeyCode.LeftShift))
		{
			_angryTrowel = true;
		}
		else if (Input.GetKeyUp(KeyCode.LeftShift))
		{
			_angryTrowel = false;
		}
	}

	public void ChangeSelected(bool next)
	{
		_objectManager.ChangeSelected (next);
		_selectedAmount = 0;
		if (_objectManager.IsSelectedUnlocked ())
			SelectedDisplay.sprite = _objectManager.GetSelected ().GetDisplaySprite(0);
		var sfxType = next ? SfxType.NextSelect : SfxType.PreviousSelect;

		_audioManager.PlaySoundFx(sfxType,false,true);
		_axisInUse = true;
	}

	public void ChangeAmount(bool up)
	{
		if (_objectManager.GetSelected().IsOverlay && _objectManager.IsSelectedUnlocked())
		{
			ChangeFlowerAmount(up);
			SelectedDisplay.sprite = _objectManager.GetSelected().GetDisplaySprite(_selectedAmount);
			var sfxType = up ? SfxType.PlusAmount : SfxType.MinusAmount;

			_audioManager.PlaySoundFx(sfxType,false,true);
			_axisInUse = true;
		}
	}

	//UI Stuff
	private void UpdateUI(){
		if (!_infinityMode)
		{
			BudgetLeftText.text = "P " + Budget.ToString("##");
			TimeLeftText.text = TimeLeft.ToString("##");
		}
		else
		{
			BudgetLeftText.text = "P ----";
			TimeLeftText.text = "--";
		}
	}

	private void CountdownCheck(){
		if(_countdownTime > 0.0f){
			if (StartText.text.Equals("Set") && _countdownTime > 0.0f && _countdownTime < 0.5f) {
				StartText.text = "GO";
				_audioManager.PlaySoundFx(SfxType.CountdownEnd);
			} else if (StartText.text.Equals("Ready") && _countdownTime >= 1.4f && _countdownTime < 1.55f) {
				StartText.text = "Set";
				_audioManager.PlaySoundFx(SfxType.Countdown);
			} else if (_countdownTime >= 2.5f){
				StartText.text = "Ready";
				_audioManager.PlaySoundFx(SfxType.Countdown);
			}
			_countdownTime -= Time.deltaTime;
		}
		else{
			_uiManager.HideStart (() => {
				_uiManager.ShowHud(() => {
					_countingDown = false;
					_started = true;
				});
			});
		}
	}

	public void TitleScreen(bool quittingGame)
	{
		if (quittingGame)
			Application.Quit();

		_uiManager.HideTitle();
		_uiManager.ShowStart(() => { }, () =>
		{
			_infinityMode = false;
			DiffHolder.SetActive(true);
			CountdownHolder.SetActive(false);
		});
	}

	public void UpdateSlider(bool isTitle)
	{
		var audioLevel = 0.0f;
		if (isTitle)
		{
			audioLevel = _uiManager.TitleVolume.value;
			_uiManager.InGameVolume.value = audioLevel;
		}
		else
		{
			audioLevel = _uiManager.InGameVolume.value;
			_uiManager.TitleVolume.value = audioLevel;
		}

		_audioManager.UpdateAudioLevels(audioLevel);
		PlayerPrefs.SetFloat("AudioLevel",audioLevel);
	}

	public void ClickResetData()
	{
		_uiManager.HideTitle();
		_uiManager.ShowDataReset();
	}

	public void ResetData(bool clickedYes)
	{
		if(clickedYes)
			PlayerPrefs.DeleteAll();

		_uiManager.HideDataReset();
		_uiManager.ShowTitle();
	}

	public void NextGarden()
	{
		_uiManager.HideFinish(() =>
		{
			_uiManager.ShowStart(() =>
			{
				SetupNextLevel();
				//_audioManager.PlaySoundFx(SfxType.NextRound);
			}, () => { });
		});
	}

	public void ToTitle()
	{
		_uiManager.HideFinish(() => {_uiManager.ShowTitle();
			ResetTiles();
		});

	}

	public void ToggleTutorial(bool showing){
		if(showing){
			_uiManager.HideTitle ();
			_uiManager.ShowTutorial ();
		}
		else{
			_uiManager.HideTutorial ();
			_uiManager.ShowTitle ();
		}
	}

	//Other stuff
	private void ChangeFlowerAmount(bool up){
		if(up){
			if (_selectedAmount == 2)
				_selectedAmount = 0;
			else
				_selectedAmount++;
		}
		else{
			if (_selectedAmount == 0)
				_selectedAmount = 2;
			else
				_selectedAmount--;
		}
	}
}
