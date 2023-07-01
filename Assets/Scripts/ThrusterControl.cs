using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrusterControl : MonoBehaviour
{
    [SerializeField] private GameObject[] _thrustersSpotlightsGOs;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void EnableThrusterSpotlight(int thrusterID)
    {
        _thrustersSpotlightsGOs[thrusterID].gameObject.SetActive(true);
    }

    public void DisableThrusterSpotlight(int thrusterID)
    {
        _thrustersSpotlightsGOs[thrusterID].gameObject.SetActive(false);
    }
}
