using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using Cinemachine;
using UnityEngine.SceneManagement;

public class ShipControl : MonoBehaviour
{
    //Player control related
    [Header("Player Control Related")]
    [SerializeField] private bool _playerControlled = false;
    [Space(5)]
    [SerializeField] private GameObject _playerShip;
    private Rigidbody _playerShipRigidBody;
    [Space(5)]
    [SerializeField] private float _xMouseAcceleration = 20f;
    [SerializeField] private float _yMouseAcceleration = 35f;
    [Space(5)]
    [SerializeField] private float _xInputValue;
    [SerializeField] private float _yInputValue;
    [SerializeField] private float _zInputValue;
    [Space(5)]
    [SerializeField] private Vector3 _absoluteMousePosition;
    [SerializeField] private Vector3 _relativeMousePosition;
    [Space(5)]
    [SerializeField] private int _screenPixelWidth;
    [SerializeField] private int _screenPixelHeight;
    [Space(5)]
    [SerializeField] private float _mousePercentOfDisplayWidth;
    [SerializeField] private float _mousePercentOfDisplayHeight;
    [Space(5)]
    [SerializeField] private float _defaultRollSpeed = 5000f;
    [SerializeField] private float _thrustSpeed = 5000f;
    [SerializeField] private float _thrusterBoostMultiplier = 1f;
    [Space(5)]
    [SerializeField] private bool _boostActive = false;
    [SerializeField] private bool _thrustActive = false;
    [SerializeField] private float _timeThrustLastActive = 0;
    [SerializeField] private float _currentTime;
    [SerializeField] private GameObject _thrustTrailGO;
    private Transform _thrustTrailTransform;
    private ParticleSystem _thrustTrailParticleSystem;

    //Camera related
    [Header("Camera Control Related")]
    [Space(5)]
    [SerializeField] private Camera _mainCamera;
    [Space(5)]
    [SerializeField] private LayerMask _followMask;
    [SerializeField] private LayerMask _cockpitMask;
    [SerializeField] private LayerMask _introCinematicMask;
    [Space(10)]
    [SerializeField] private string _currentVCam;
    [SerializeField] private string _lastVCamUsed = "CockpitCam";
    [SerializeField] private CinemachineVirtualCamera _followVCam;
    [SerializeField] private CinemachineVirtualCamera _cockpitVCam;
    [Space(5)]
    [SerializeField] private CinemachineVirtualCamera[] _cinematicVCams;
    [SerializeField] private float _lastCinematicVCamStartTime = 0;

    [SerializeField] private int _lastCinematicVCamUsed = -1;
    [SerializeField] private bool _cinematicVCamsActive = false;
    [SerializeField] private bool _cinematicPathInProgress = false;
    [SerializeField] private float _cinematicPathPosition = 0f;
    [SerializeField] private float _cinematicPathSpeed = 0.0015f;
    [Space(10)]
    [SerializeField] private GameObject _globalVolumeDefault;
    [Space(10)]
    [SerializeField] private GameObject _musicAudioSourceGO;
    [SerializeField] private bool _fadeOutMusicActive = false;
    [SerializeField] private bool _fadeInMusicActive = false;
    [SerializeField] private float _musicVolume = 0f;

    //Animation related
    [Header("Animation Related")]
    [Space(5)]
    [SerializeField] private Animator _starshipAnimator;
    [SerializeField] private Animator _wingsAnimator;
    [SerializeField] private Animator _minigunAnimator1;
    [SerializeField] private Animator _minigunAnimator2;
    
    //External scripts
    [Header("References")]
    [Space(5)]
    [SerializeField] private ThrusterControl _thrusterControlScript;

    private void OnEnable()
    {
        _screenPixelWidth = Screen.width;
        _screenPixelHeight = Screen.height;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (_playerShip == null)
        {
            _playerShip = this.gameObject;
        }
        _playerShipRigidBody = _playerShip.GetComponent<Rigidbody>();
        _thrustTrailTransform = _thrustTrailGO.transform;
        _thrustTrailParticleSystem = _thrustTrailTransform.GetComponent<ParticleSystem>();
        DoNullChecks();

        SwitchCamera("CockpitCam");
        _fadeOutMusicActive = false;
        _mainCamera.cullingMask = _introCinematicMask;

        _wingsAnimator.SetBool("isEngaged", false);

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        if (SceneManager.GetSceneByBuildIndex(0).isLoaded) {
            SceneManager.UnloadSceneAsync(0);
        }    
    }

    // Update is called once per frame
    void Update()
    {
        if (_playerControlled)
        {
            CheckInput();
            if (_cinematicVCamsActive)
            {
                MoveVCamDolly(_cinematicVCams[_lastCinematicVCamUsed]);
            }
        }

        if (_fadeOutMusicActive || _fadeInMusicActive)
        {
            FadeMusic();
        }
    }

    private void FixedUpdate()
    {
        if (_playerControlled)
        {
            CheckInputFixedUpdate();
        }
    }

    void CheckInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (_currentVCam == "FollowCam")
            {
                SwitchCamera("CockpitCam");
            }
            else if (_currentVCam == "CockpitCam")
            {
                SwitchCamera("FollowCam");
            }
            else if (_currentVCam == "CinematicCam")
            {
                SwitchCamera(_lastVCamUsed);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        _currentTime = Time.time;
        CheckPlayerIdle();
    }

    void CheckInputFixedUpdate()
    {
        ApplyKeyboardInput();
    }

    void CheckPlayerIdle()
    {
        //Determine if player has been idle for 10 seconds
        if (_currentTime - _timeThrustLastActive > 10f)
        {
            SwitchCamera("CinematicCam");
        }
    }

    private void ApplyKeyboardInput()
    {
        bool wActive;
        bool sActive;
        bool aActive;
        bool dActive;
        float zInput = 0;


        if (Input.GetKey(KeyCode.LeftShift))
        {
            _boostActive = true;
        } else
        {
            _boostActive = false;
        }

        if (_boostActive)
        {
            _thrusterBoostMultiplier = 4f;
            SetThrusterTrailAppearance(2);
        } else
        {
            _thrusterBoostMultiplier = 1f;
        }
        
        if (Input.GetKey(KeyCode.W))
        {
            AddThrust(transform.forward * _thrustSpeed * _thrusterBoostMultiplier);
            wActive = true;
            _thrustActive = true;
            _timeThrustLastActive = _currentTime;
            if (!_boostActive)
            {
                SetThrusterTrailAppearance(1);
            }
        }
        else 
        { 
            wActive = false;
            _thrustActive = false;
            AddSlowingThrust(transform.forward);
            SetThrusterTrailAppearance(0);
        }

        if (Input.GetKey(KeyCode.S))
        {
            //Back Thrust
            if (Time.time - _timeThrustLastActive < 5f)
            {
                AddThrust(-transform.forward * (_thrustSpeed * 0.1f));
            }
            
            sActive = true;
        }
        else { sActive = false; }

        if (wActive == false && sActive == false)
        {
            //No forward/back thrust input
            if (_wingsAnimator.GetBool("isEngaged") == true)
            {
                AnimateDisengageWings();
            }
        }
        else if (wActive == true && sActive == false) //forward not back thrust input
        {
            if (_wingsAnimator.GetBool("isEngaged") == false)
            {
                AnimateEngageWings();
            }
        }

        if (Input.GetKey(KeyCode.A))
        {
            //Roll Left
            zInput = _defaultRollSpeed * Time.deltaTime;
            aActive = true;
        }
        else
        {
            aActive = false;
        }

        if (Input.GetKey(KeyCode.D))
        {
            //Roll Right
            zInput = -_defaultRollSpeed * Time.deltaTime;
            dActive = true;
        }
        else
        { dActive = false; }

        if (aActive)
        {
            _thrusterControlScript.EnableThrusterSpotlight(0);
            _thrusterControlScript.EnableThrusterSpotlight(3);
        }
        
        if (dActive)
        {
            _thrusterControlScript.EnableThrusterSpotlight(1);
            _thrusterControlScript.EnableThrusterSpotlight(2);
        }
        
        if (!aActive)
        {
            _thrusterControlScript.DisableThrusterSpotlight(0);
            _thrusterControlScript.DisableThrusterSpotlight(3);
        }

        if (!dActive)
        {
            _thrusterControlScript.DisableThrusterSpotlight(1);
            _thrusterControlScript.DisableThrusterSpotlight(2);
        }

        if (wActive || sActive || aActive || dActive)
        {
            if (_cinematicVCamsActive == true)
            {
                SwitchCamera(_lastVCamUsed);
                _cinematicVCamsActive = false;
            }
        }

        if ((_thrustActive) || ((_currentTime - _timeThrustLastActive) < 9f) && _currentTime > 5f)
        {
            ApplyMouseInput(zInput);
        } else
        {
            ApplyMouseInput(zInput, true);
        }
    }

    private void ApplyMouseInput(float zInput, bool rollOnly = false)
    {
        Vector2 pitchYaw = GetMouseInput();
        float xInput = pitchYaw.x;
        float yInput = pitchYaw.y;

        RotatePlayerShip(xInput, yInput, zInput, rollOnly);
    }

    private Vector2 GetMouseInput()
    {
        Vector2 newInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        SetMousePercentOfScreen();

        if (_mousePercentOfDisplayWidth > 0.9f)
        {
            newInput.x += 1f;
        }
        else if (_mousePercentOfDisplayWidth < 0.1f)
        {
            newInput.x -= 1f;
        } else if (_mousePercentOfDisplayWidth < 0.5f)
        {
            newInput.x = -1f * _mousePercentOfDisplayWidth;
        } else if (_mousePercentOfDisplayWidth > 0.5f)
        {
            newInput.x = 1f * _mousePercentOfDisplayWidth;
        }

        if (_mousePercentOfDisplayHeight > 0.9f)
        {
            newInput.y += 1f;
        }
        else if (_mousePercentOfDisplayHeight < 0.1f)
        {
            newInput.y -= 1f;
        } else if (_mousePercentOfDisplayHeight < 0.5f)
        {
            newInput.y = -1f * _mousePercentOfDisplayHeight;
        } else if (_mousePercentOfDisplayHeight > 0.5f)
        {
            newInput.y = 1f * _mousePercentOfDisplayHeight;
        }

        if (_mousePercentOfDisplayWidth > 0.47f && _mousePercentOfDisplayWidth < 0.53f)
        {
            newInput.x = 0;
        }

        if (_mousePercentOfDisplayHeight > 0.47f && _mousePercentOfDisplayHeight < 0.53f)
        {
            newInput.y = 0;
        }

        //Round and clamp values to reduce AABB errors
        newInput.x = Mathf.Round(newInput.x * 100f) * 0.01f;
        newInput.y = Mathf.Round(newInput.y * 100f) * 0.01f;
        newInput.x = Mathf.Clamp(newInput.x, -1.0f, 1.0f);
        newInput.y = Mathf.Clamp(newInput.y, -1.0f, 1.0f);

        _xInputValue = newInput.x;
        _yInputValue = newInput.y;

        return newInput;
    }

    private void RotatePlayerShip(float xInput = 0, float yInput = 0, float zInput = 0, bool rollOnly = false)
    {
        //Round value to reduce AABB errors
        zInput = Mathf.Round(zInput * 100f) * 0.01f;

        _zInputValue = zInput;

        //roll
        transform.Rotate(Vector3.forward, zInput * Time.deltaTime);

        //yaw, pitch
        if (!rollOnly)
        {
            transform.Rotate(Vector3.up, xInput * _xMouseAcceleration * Time.deltaTime);
            transform.Rotate(Vector3.right, yInput * _yMouseAcceleration * Time.deltaTime);
        }
    }

    void  SetMousePercentOfScreen()
    {
        _absoluteMousePosition = Input.mousePosition;
        _mousePercentOfDisplayWidth = _absoluteMousePosition.x / _screenPixelWidth;
        _mousePercentOfDisplayHeight = _absoluteMousePosition.y / _screenPixelHeight; 
    }

    private void AddThrust(Vector3 newAddForce)
    {
        _playerShipRigidBody.AddForce(newAddForce, ForceMode.Force);
    }

    private void AddSlowingThrust(Vector3 newAddForce)
    {
        float timeSinceLastThrust = Time.time - _timeThrustLastActive;

        if (_timeThrustLastActive > 0f)
        {
            switch (timeSinceLastThrust)
            {
                case < 1f:
                    AddThrust(transform.forward * (_thrustSpeed * 0.6f));
                    break;
                case < 3f:
                    AddThrust(transform.forward * (_thrustSpeed * 0.4f));
                    break;
                case < 5f:
                    AddThrust(transform.forward * (_thrustSpeed * 0.2f));
                    break;
                case < 9f:
                    AddThrust(transform.forward * (_thrustSpeed * 0.1f));
                    break;
            }
        }
    }

    public void ReceiveForce(Vector3 newAddForce)
    {
        _playerShipRigidBody.AddForce(newAddForce, ForceMode.Impulse);
    }

    void SwitchCamera(string CameraName)
    {
        switch (CameraName)
        {
            case "FollowCam":
                _mainCamera.cullingMask = _followMask;
                _currentVCam = "FollowCam";
                _cockpitVCam.Priority = 10;

                if (_cinematicVCams != null)
                {
                    foreach (CinemachineVirtualCamera vCam in _cinematicVCams)
                    {
                        vCam.Priority = 10;
                    }
                }

                _followVCam.Priority = 100;
                _lastVCamUsed = "FollowCam";

                //Switch engine background audio
                _followVCam.GetComponent<AudioSource>().enabled = true;
                _cockpitVCam.GetComponent<AudioSource>().enabled = false;

                //turn off background music
                _fadeInMusicActive = false;
                _fadeOutMusicActive = true;
                break;
            case "CockpitCam":
                _mainCamera.cullingMask = _cockpitMask;
                _currentVCam = "CockpitCam";
                _followVCam.Priority = 10;

                if (_cinematicVCams != null)
                {
                    foreach (CinemachineVirtualCamera vCam in _cinematicVCams)
                    {
                        vCam.Priority = 10;
                    }
                }

                _cockpitVCam.Priority = 100;
                _lastVCamUsed = "CockpitCam";

                //Switch engine background audio
                _cockpitVCam.GetComponent<AudioSource>().enabled = true;
                _followVCam.GetComponent<AudioSource>().enabled = false;

                //turn off background music
                _fadeInMusicActive = false;
                _fadeOutMusicActive = true;
                break;
            case "CinematicCam":
                _mainCamera.cullingMask = _followMask;
                _currentVCam = "CinematicCam";
                _followVCam.Priority = 10;
                _cockpitVCam.Priority = 10;

                CycleThroughCinematicVCams();

                //Switch engine background audio
                _followVCam.GetComponent<AudioSource>().enabled = true;
                _cockpitVCam.GetComponent<AudioSource>().enabled = false;

                //Turn on background music
                _fadeInMusicActive = true;
                _fadeOutMusicActive = false;
                break;
        }
    }

    private void CycleThroughCinematicVCams()
    {
        if (_currentTime - _lastCinematicVCamStartTime > 9f)
        {
            foreach (CinemachineVirtualCamera vCam in _cinematicVCams)
            {
                vCam.Priority = 10;
            }

            switch (_lastCinematicVCamUsed)
            {
                case -1:
                    _cinematicVCams[2].Priority = 100;
                    _lastCinematicVCamUsed = 2;
                    _cinematicPathSpeed = 0.0015f;
                    break;
                case 0:
                    _cinematicVCams[2].Priority = 100;
                    _lastCinematicVCamUsed = 2;
                    _cinematicPathSpeed = 0.0015f;
                    ResetCinematicVCamPosition(_cinematicVCams[0]);
                    ResetCinematicVCamPosition(_cinematicVCams[1]);
                    ResetCinematicVCamPosition(_cinematicVCams[2]);
                    break;
                case 1:
                    _cinematicVCams[0].Priority = 100;
                    _lastCinematicVCamUsed = 0;
                    _cinematicPathSpeed = 0.0015f;
                    break;
                case 2:
                    _cinematicVCams[1].Priority = 100;
                    _lastCinematicVCamUsed = 1;
                    _cinematicPathSpeed = 0.0015f;
                    break;
            }

            _lastCinematicVCamStartTime = _currentTime;
            _cinematicVCamsActive = true;
        }
    }

    private void MoveVCamDolly(CinemachineVirtualCamera VCam)
    {
        _cinematicPathPosition = VCam.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition;

        if (_cinematicPathPosition < 1)
        {
            _cinematicPathPosition += _cinematicPathSpeed;
            VCam.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition = _cinematicPathPosition;
        }
    }

    private void ResetCinematicVCamPosition (CinemachineVirtualCamera VCam)
    {
        VCam.GetCinemachineComponent<CinemachineTrackedDolly>().m_PathPosition = 0f;
    }

    public void SetCameraMask(string newMask)
    {
        switch (newMask)
        {
            case "follow":
                _mainCamera.cullingMask = _followMask;
                break;
            case "cockpit":
                _mainCamera.cullingMask = _cockpitMask;
                break;
            case "introCinematic":
                _mainCamera.cullingMask = _introCinematicMask;
                break;
        }
    }

    private void FadeMusic()
    {
        if (_fadeInMusicActive)
        {
            if (!_musicAudioSourceGO.GetComponent<AudioSource>().enabled)
            {
                _musicAudioSourceGO.GetComponent<AudioSource>().enabled = true;
            }

            if (_musicVolume < 0.1f)
            {
                _musicVolume += 0.01f * Time.deltaTime;
            } else if (_musicVolume > 0.1f)
            {
                _musicVolume = 0.1f;
            }
        }

        if (_fadeOutMusicActive)
        {
            _musicVolume -= 0.02f * Time.deltaTime;
        }

        _musicAudioSourceGO.GetComponent<AudioSource>().volume = _musicVolume;

        if (_musicVolume <= 0f)
        {
            _fadeOutMusicActive = false;
            _musicAudioSourceGO.GetComponent<AudioSource>().enabled = false;
            _musicVolume = 0f;
        }
    }

    private void AnimateEngageWings()
    {
        _wingsAnimator.SetTrigger("Engage");
        _wingsAnimator.SetBool("isEngaged", true);
    }

    private void AnimateDisengageWings()
    {
        _wingsAnimator.SetTrigger("Disengage");
        _wingsAnimator.SetBool("isEngaged", false);
    }

    private void AnimateCockpitControls(string newThrusterPosition, string newJoystickPosition)
    {
        //newThrusterPositions = "none" | "forward" | "back"
        //newJoystickPositions = "none" | "forward" | "back" | "left" | "right" | "forward-left" | "forward-right" | "back-left" | "back-right"
    }

    private void SetThrusterTrailAppearance(int thrusterMode)
    {
        //thruster modes - 0 no thrust | 1 regular thrust | 2 boost thrust
        Color noThrustColor = new Color(1.0f,1.0f,1.0f,0.098f);
        Color regularThrustColor = new Color(1.0f,1.0f,1.0f,0.376f);
        Color boostThrustColor = new Color(1.0f,0.675f,0.675f,1.0f);

        switch (thrusterMode) 
        {
            case 0:
                _thrustTrailTransform.localScale = new Vector3(0.7f, 0.5f, 0.5f);
                _thrustTrailParticleSystem.startColor = noThrustColor; //suggested main.startColor not working, Unity documentation shows it as assignable but visual studio says no
                //Debug.Log("ThrusterMode 0 fired!");
                break;
            case 1:
                _thrustTrailTransform.localScale = new Vector3(0.5f, 0.7f, 0.7f);
                _thrustTrailParticleSystem.startColor = regularThrustColor;
                //Debug.Log("ThrusterMode 1 fired!");
                break;
            case 2:
                _thrustTrailTransform.localScale = new Vector3(0.4f, 0.4f, 0.9f);
                _thrustTrailParticleSystem.startColor = boostThrustColor;
                //Debug.Log("ThrusterMode 2 fired!");
                break;
        }
    }

    public void SetTimeSinceLastThrust(float newValue)
    {
        _timeThrustLastActive = newValue;
    }

    public void EnablePlayerControl()
    {
        _playerControlled = true;
    }

    public void DisablePlayerControl()
    {
        _playerControlled = false;
    }

    public void EnableDefaultPPVolume ()
    {
        _globalVolumeDefault.SetActive(true);
    }

    void DoNullChecks()
    {
        if (_mainCamera == null)
        {
            Debug.Log("ShipControl::DoNullChecks()::_mainCamera is null!");
        }
        if (_followVCam == null)
        {
            Debug.Log("ShipControl::DoNullChecks()::_followVCam is null!");
        }
        if (_cockpitVCam == null)
        {
            Debug.Log("ShipControl::DoNullChecks()::_cockpitVCam is null!");
        }
        if (_cinematicVCams == null)
        {
            Debug.Log("ShipControl::DoNullChecks()::_cinematicVCams is null!");
        }
        if (_playerShip == null)
        {
            Debug.Log("ShipControl::DoNullChecks()::_playerShip is null!");
        }
        if (_wingsAnimator == null)
        {
            Debug.Log("ShipControl::DoNullChecks()::_wingsAnimator is null!");
        }
        if (_minigunAnimator1 == null)
        {
            Debug.Log("ShipControl::DoNullChecks()::_mingunAnimator1 is null!");
        }
        if (_minigunAnimator2 == null)
        {
            Debug.Log("ShipControl::DoNullChecks()::_minigunAnimator2 is null!");
        }
        //if (_starshipAnimator == null)
        //{
        //    Debug.Log("ShipControl::DoNullChecks()::_starshipAnimator is null!");
        //}
        if (_thrustTrailGO == null)
        {
            Debug.Log("ShipControl::DoNullChecks()::_thrustTrailGO is null!");
        }
        if (_thrustTrailTransform == null)
        {
            Debug.Log("ShipControl::DoNullChecks()::_thrustTrailTransform is null!");
        }
        if (_thrustTrailParticleSystem == null)
        {
            Debug.Log("ShipControl::DoNullChecks()::_thrustTrailParticleSystem is null!");
        }
    }
}
