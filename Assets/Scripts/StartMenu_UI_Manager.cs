using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cinemachine;
using UnityEngine.Timeline;
using UnityEngine.SceneManagement;

public class StartMenu_UI_Manager : MonoBehaviour
{
    [SerializeField] private Canvas _canvas;
    [SerializeField] private GameObject _instructionsPanel;
    [SerializeField] private GameObject _spaceSimHeader;
    [SerializeField] private GameObject _backgroundDirector;
    private AsyncOperation asyncLoad;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void ShowInstructions()
    {
        if (_spaceSimHeader.activeInHierarchy)
        {
            _spaceSimHeader.SetActive(false);
        } else
        {
            _spaceSimHeader.SetActive(true);
        }

        if (!_instructionsPanel.activeInHierarchy)
        {
            _instructionsPanel.SetActive(true);
        } else
        {
            _instructionsPanel.SetActive(false);
        }
    }

    public void ExitApp()
    {
        Application.Quit();
    }

    public void LoadDemoScene()
    {
        if (_instructionsPanel.activeSelf)
        {
            _instructionsPanel.SetActive(false);
        }
        _backgroundDirector.SetActive(true);

        StartCoroutine(ASyncLoadScene());
    }

    private IEnumerator ASyncLoadScene()
    {
        asyncLoad = SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false;

        yield return new WaitForSeconds(9.68f);
        asyncLoad.allowSceneActivation = true;
    }
}
