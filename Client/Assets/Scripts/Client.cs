using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Client : MonoBehaviour
{
    private string serverIP = "";

    public GameObject InputField;
    public GameObject StartScreen;
    public Transform playerCube;

    public void GetServerIP()
    {
        serverIP = InputField.GetComponent<Text>().text;
    }

    // Start is called before the first frame update
    void Start()
    {
        playerCube = GameObject.Find("").transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (serverIP != "")
        {
            Debug.Log("PASS");
        }
    }
}
