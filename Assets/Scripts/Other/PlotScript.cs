using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlotScript : MonoBehaviour
{
    [SerializeField] GameObject IntroCanvas;
    [SerializeField] GameObject EndingCanvas;
    [SerializeField] GameObject PlayerUI;
    [SerializeField] GameObject Player;

    private bool endingStarted = false;  

    void Start()
    {
        StartCoroutine(Intro());
    }

    private void Update()
    {
        if (Player.GetComponent<PlayerMovement>().ReturnEndCondition() == true)
        {
            if (endingStarted == false)
            {
                Debug.Log("Ending");
                StartCoroutine(Ending());
            }
        }
    }


    private IEnumerator Intro()
    {
        IntroCanvas.GetComponent<Canvas>().enabled = true;
        EndingCanvas.GetComponent<Canvas>().enabled = false;
        PlayerUI.GetComponent<Canvas>().enabled = false;
        yield return new WaitForSeconds(5f);
        IntroCanvas.GetComponent<Canvas>().enabled = false;
        EndingCanvas.GetComponent<Canvas>().enabled = false;
        PlayerUI.GetComponent<Canvas>().enabled = true;
        Time.timeScale = 1f;
    }

    private IEnumerator Ending()
    {
        endingStarted = true;
        EndingCanvas.GetComponent<Canvas>().enabled = true;
        PlayerUI.GetComponent<Canvas>().enabled = false;
        yield return new WaitForSeconds(5f);
        SceneManager.LoadScene(0);
    }
}
