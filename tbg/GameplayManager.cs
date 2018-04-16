using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    [SerializeField] private List<LevelData> _levels;
    private int _currentLevel = 0;

    private int _currentLevelButterflyCount = 0;

    private UiManager _ui;

	// Use this for initialization
	void Start () {
	    _ui = GetComponent<UiManager>();
        _ui.Initialise(_levels[_currentLevel].Title,_levels[_currentLevel].Subtitle,0);
	}

	// Update is called once per frame
	void Update () {

	}

    public void CapturedButterfly()
    {
        _currentLevelButterflyCount++;
        _ui.UpdateButterflyCount(_currentLevelButterflyCount);
        if(_currentLevelButterflyCount == _levels[_currentLevel].RequiredCapsForCompletion)
            EnableNextLevel();
    }

    #region Level Progression
    private void EnableNextLevel()
    {
        //TODO: Fire off the ending scene and credits or set the next level to active.
        _levels[_currentLevel].Completed = true;
        _levels[_currentLevel].FinishLevelTeleporter.UpdateCollectedEverything(true);
        if (_currentLevel >= _levels.Count - 1)
        {
            Debug.Log("We've hit the end of the final level. Start the ending stuff here.");
            return;
        }


        _currentLevel++;
        //TODO: Maybe we should have some kind of waypoint...
        _currentLevelButterflyCount = 0;
        _levels[_currentLevel].LevelParent.SetActive(true);

    }

    public void DisablePreviousLevel()
    {
        if(_currentLevel >= 0)
            _levels[_currentLevel - 1].LevelParent.SetActive(false);
    }

    public void DisplayLevelTitle()
    {
        _ui.Initialise(_levels[_currentLevel].Title,_levels[_currentLevel].Subtitle, 0);
    }
    #endregion
}
