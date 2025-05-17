using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine.UI;
public class ConnectionSwitch : MonoBehaviour
{
    [SerializeField]
    private Button btnConnect;

    [SerializeField]
    private Button btnDisconnect;

    private void OnEnable() 
    {
        Web3.OnLogin += OnLogin;
        Web3.OnLogout += OnLogout;
    }

    private void OnDisable()
    {
        Web3.OnLogin -= OnLogin;
        Web3.OnLogout -= OnLogout;
    }

    private void OnLogin(Account account)
    {
        btnConnect.gameObject.SetActive(false);
        btnDisconnect.gameObject.SetActive(true);
    }

    private void OnLogout()
    {
        btnConnect.gameObject.SetActive(true);
        btnDisconnect.gameObject.SetActive(false);
    }
    
}
