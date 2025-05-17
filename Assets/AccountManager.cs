using System.Collections;
using System.Collections.Generic;
using Merkator.BitCoin;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using UnityEngine;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using System.Threading.Tasks;
using TMPro;

public class AccountManager : MonoBehaviour
{
    private const string PLAYER_PRIVATE_KEY = "PLAYER_PRIVATE_KEY";
    private const string PLAYER_PUBLIC_KEY = "PLAYER_PUBLIC_KEY";
    private const string PLAYER_PUBLIC_KEY_BYTES = "PLAYER_PUBLIC_KEY_BYTES";

    [SerializeField] private TMP_Text _scoreText;
    
    private Account _playerAccount;
    public Solana.Unity.Programs.Player PlayerData { get; private set; }
    public bool IsConnectedToBlockchain { get; private set; }
    
    // Global player score
    public ulong GlobalPlayerScore { get; private set; }
    
    private bool _isInitialStartup = true;
    
    private async void Start()
    {
        // Try to load existing player account on startup
        Debug.Log("Starting AccountManager, loading player account...");
        LoadPlayerAccount();
        
        // IMPORTANT: This is the ONLY place where InitializeWallet should be called
        // Initialize Web3 wallet if not already initialized
        if (Web3.Account == null)
        {
            Debug.Log("Web3 account not initialized, initializing wallet...");
            #if UNITY_EDITOR
            InitializeWallet();
            #else
            await Web3.Instance.LoginWalletAdapter();
            #endif
            
            // Wait a frame to make sure wallet initialization completes
            await Task.Yield();
            
            if (Web3.Account == null)
            {
                Debug.LogError("Wallet initialization failed. Web3.Account is still null.");
            }
            else
            {
                Debug.Log("Wallet initialization successful: " + Web3.Account.PublicKey);
            }
        }
        
        // Check connection status
        await CheckConnectionStatus();
        
        // Try to fetch player data after account is loaded
        if (_playerAccount != null && IsConnectedToBlockchain)
        {
            Debug.Log("Account loaded and connected, attempting to fetch player data...");
            await FetchPlayerData();
        }
        
        // Initial startup completed
        _isInitialStartup = false;
    }
    
    private void OnEnable()
    {
        // Register to Web3 events
        Web3.OnLogin += OnWeb3Login;
        Web3.OnLogout += OnWeb3Logout;
        Debug.Log("Registered Web3 events");
    }
    
    private void OnDisable()
    {
        // Unregister from Web3 events
        Web3.OnLogin -= OnWeb3Login;
        Web3.OnLogout -= OnWeb3Logout;
        Debug.Log("Unregistered Web3 events");
    }
    
    private async void OnWeb3Login(Account account)
    {
        Debug.Log($"Web3 login detected with account: {account.PublicKey}");
        IsConnectedToBlockchain = true;
        
        // Skip loading data during initial startup since Start() already handles it
        if (!_isInitialStartup)
        {
            Debug.Log("Web3 login - not initial startup, loading data...");
            await LoadDataOnConnection();
        }
        else
        {
            Debug.Log("Web3 login during initial startup - skipping redundant data loading");
        }
    }
    
    private void OnWeb3Logout()
    {
        Debug.Log("Web3 logout detected");
        IsConnectedToBlockchain = false;
    }
    
    private async Task CheckConnectionStatus()
    {
        Debug.Log("Checking blockchain connection status...");
        
        try
        {
            // Attempt to get a blockhash to check connection
            var blockHashResult = await Web3.Rpc.GetLatestBlockHashAsync();
            IsConnectedToBlockchain = blockHashResult?.Result?.Value != null;
            
            Debug.Log($"Blockchain connection status: {(IsConnectedToBlockchain ? "Connected" : "Disconnected")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Connection check failed: {ex.Message}");
            IsConnectedToBlockchain = false;
        }
    }
    
    public async Task LoadDataOnConnection()
    {
        Debug.Log("Loading data due to established connection...");
        
        // Make sure player account is loaded
        if (_playerAccount == null)
        {
            LoadPlayerAccount();
        }
        
        // Check if Web3 is initialized
        if (Web3.Account == null)
        {
            Debug.LogError("Cannot load data: Web3 account not initialized");
            return;
        }
        
        // Update connection status
        await CheckConnectionStatus();
        
        if (!IsConnectedToBlockchain)
        {
            Debug.LogWarning("Cannot load data - not connected to blockchain");
            return;
        }
        
        // Fetch latest player data from blockchain
        await FetchPlayerData();
        
        // Player is connected but not initialized on blockchain
        if (PlayerData == null && _playerAccount != null)
        {
            Debug.Log("Connection established but player not initialized on blockchain yet");
        }
    }
    
    // IMPORTANT: This method should ONLY be called from Start()
    private void InitializeWallet()
    {
        Debug.Log("Initializing Web3 wallet...");
        
        #if UNITY_EDITOR
        // Try to load mnemonic from account.txt file in editor
        string mnemonic = LoadMnemonicFromFile();
        if (!string.IsNullOrEmpty(mnemonic))
        {
            Web3.Instance.CreateAccount(mnemonic, "testpwd");
            Debug.Log($"Web3 wallet initialized with account: {Web3.Account?.PublicKey}");
        }
        else
        {
            Debug.LogError("ERROR: account.txt file not found or invalid. You must manually create this file in your project root directory with your mnemonic. DO NOT commit this file to version control.");
            return;
        }
        #else
        // In non-editor builds, wallet adapter login is handled elsewhere
        Debug.Log("Non-editor build: wallet initialization handled by wallet adapter");
        #endif
    }
    
    private string LoadMnemonicFromFile()
    {
        try
        {
            // Get the path to the account.txt file in the project root
            string filePath = System.IO.Path.Combine(Application.dataPath, "account.txt");
            
            // Check if file exists
            if (System.IO.File.Exists(filePath))
            {
                // Read the entire file content as the mnemonic
                string mnemonic = System.IO.File.ReadAllText(filePath).Trim();
                
                // Validate it looks like a valid mnemonic (basic check)
                if (mnemonic.Split(' ').Length >= 12)
                {
                    Debug.Log($"Successfully loaded mnemonic from {filePath}");
                    return mnemonic;
                }
                else
                {
                    Debug.LogError($"Mnemonic in {filePath} doesn't look valid - should have at least 12 words");
                }
            }
            else
            {
                Debug.LogError($"account.txt file not found at {filePath}. Create this file manually with your mnemonic.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error reading account.txt file: {ex.Message}");
        }
        
        return null;
    }

    public void LoadPlayerAccount()
    {
        Debug.Log("LoadPlayerAccount called, checking for saved keys");
        try
        {
            if (PlayerPrefs.HasKey(PLAYER_PRIVATE_KEY) && PlayerPrefs.HasKey(PLAYER_PUBLIC_KEY_BYTES))
            {
                Debug.Log($"Found saved keys with key names: {PLAYER_PRIVATE_KEY} and {PLAYER_PUBLIC_KEY_BYTES}");
                try
                {
                    // Get stored private key
                    string privateKeyBase64 = PlayerPrefs.GetString(PLAYER_PRIVATE_KEY);
                    string publicKeyBase64 = PlayerPrefs.GetString(PLAYER_PUBLIC_KEY_BYTES);
                    
                    Debug.Log($"Retrieved privateKeyBase64, length: {(privateKeyBase64 != null ? privateKeyBase64.Length : 0)}");
                    Debug.Log($"Retrieved publicKeyBase64, length: {(publicKeyBase64 != null ? publicKeyBase64.Length : 0)}");
                    
                    if (string.IsNullOrEmpty(privateKeyBase64) || string.IsNullOrEmpty(publicKeyBase64))
                    {
                        Debug.LogWarning("Stored keys are empty, creating new account");
                        CreateNewAccount();
                        return;
                    }
                    
                    try {
                        // Convert Base64 strings back to byte arrays
                        byte[] privateKeyBytes = System.Convert.FromBase64String(privateKeyBase64);
                        byte[] publicKeyBytes = System.Convert.FromBase64String(publicKeyBase64);
                        
                        Debug.Log($"Converted privateKeyBase64 to byte array, length: {privateKeyBytes.Length}");
                        Debug.Log($"Converted publicKeyBase64 to byte array, length: {publicKeyBytes.Length}");
                        
                        // Check key validity
                        if (privateKeyBytes == null || privateKeyBytes.Length == 0 || 
                            publicKeyBytes == null || publicKeyBytes.Length == 0)
                        {
                            Debug.LogError("Private key or public key bytes are null or empty");
                            CreateNewAccount();
                            return;
                        }
                        
                        // Create account with both private and public keys
                        Debug.Log("About to create Account from key bytes...");
                        try {
                            _playerAccount = new Account(privateKeyBytes, publicKeyBytes);
                            Debug.Log($"Successfully loaded existing account with public key: {_playerAccount.PublicKey}");
                            return;
                        }
                        catch (System.Exception ex) {
                            Debug.LogError($"Error creating Account: {ex.GetType().Name}: {ex.Message}");
                            if (ex.InnerException != null) {
                                Debug.LogError($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                            }
                            
                            // If that fails, create a new account
                            Debug.LogWarning("Failed to reconstruct account, creating new one");
                            CreateNewAccount();
                            return;
                        }
                    }
                    catch (System.Exception ex) {
                        Debug.LogError($"Error converting Base64 to bytes: {ex.GetType().Name}: {ex.Message}");
                        CreateNewAccount();
                        return;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to load player account from PlayerPrefs: {ex.Message}");
                    Debug.LogError($"Exception type: {ex.GetType().Name}");
                    if (ex.InnerException != null) {
                        Debug.LogError($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                    }
                    Debug.LogError($"Stack trace: {ex.StackTrace}");
                    CreateNewAccount();
                }
            }
            else
            {
                Debug.Log($"Saved keys not found. Creating new account.");
                CreateNewAccount();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Unexpected error in LoadPlayerAccount: {ex.Message}");
            Debug.LogError($"Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null) {
                Debug.LogError($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            Debug.LogError($"Stack trace: {ex.StackTrace}");
            
            // Final fallback - create a new account without saving
            _playerAccount = new Account();
            Debug.Log($"Created fallback account: {_playerAccount.PublicKey}");
        }
    }
    
    private void CreateNewAccount()
    {
        Debug.Log("Creating new account...");
        try {
            // Create a completely new account
            _playerAccount = new Account();
            Debug.Log($"Account created successfully with public key: {_playerAccount.PublicKey}");
            SavePlayerAccount();
            Debug.Log($"Created new player account: {_playerAccount.PublicKey}");
        }
        catch (System.Exception ex) {
            Debug.LogError($"Error creating new account: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null) {
                Debug.LogError($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
    }
    
    private void SavePlayerAccount()
    {
        Debug.Log("Saving player account...");
        try
        {
            if (_playerAccount == null)
            {
                Debug.LogError("Cannot save null account");
                return;
            }
            
            if (_playerAccount.PrivateKey == null || _playerAccount.PublicKey == null)
            {
                Debug.LogError("Account's private key or public key is null");
                return;
            }
            
            // Save private key bytes as Base64 string
            byte[] privateKeyBytes = _playerAccount.PrivateKey.KeyBytes;
            string privateKeyBase64 = System.Convert.ToBase64String(privateKeyBytes);
            Debug.Log($"Private key bytes length: {privateKeyBytes.Length}");
            
            // Get the public key bytes
            byte[] publicKeyBytes = _playerAccount.PublicKey.KeyBytes;
            string publicKeyBase64 = System.Convert.ToBase64String(publicKeyBytes);
            Debug.Log($"Public key bytes length: {publicKeyBytes.Length}");
            
            // Save both private and public keys
            PlayerPrefs.SetString(PLAYER_PRIVATE_KEY, privateKeyBase64);
            PlayerPrefs.SetString(PLAYER_PUBLIC_KEY, _playerAccount.PublicKey.ToString());
            PlayerPrefs.SetString(PLAYER_PUBLIC_KEY_BYTES, publicKeyBase64);
            PlayerPrefs.Save();
            
            Debug.Log($"Player account saved successfully: {_playerAccount.PublicKey}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save player account: {ex.Message}");
            Debug.LogError($"Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null) {
                Debug.LogError($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
    }

    public async Task FetchPlayerData()
    {
        if (_playerAccount == null)
        {
            Debug.LogWarning("Cannot fetch player data: account is null");
            LoadPlayerAccount();
            
            if (_playerAccount == null)
            {
                Debug.LogError("Failed to load player account, cannot fetch player data");
                return;
            }
        }
        
        // Check if Web3 is initialized
        if (Web3.Account == null || Web3.Rpc == null)
        {
            Debug.LogError("Cannot fetch player data: Web3 not properly initialized");
            return;
        }
        
        // Check connection status
        if (!IsConnectedToBlockchain)
        {
            await CheckConnectionStatus();
            if (!IsConnectedToBlockchain)
            {
                Debug.LogWarning("Cannot fetch player data: not connected to blockchain");
                return;
            }
        }
        
        try
        {
            Debug.Log($"Fetching player data for account: {_playerAccount.PublicKey}");
            PlayerData = await Solana.Unity.Programs.Player.GetPlayerAsync(Web3.Rpc, _playerAccount.PublicKey);
            
            if (PlayerData != null)
            {
                // Update global score with blockchain data
                GlobalPlayerScore = PlayerData.Points;
                
                // Update UI
                UpdateScoreUI();
                
                Debug.Log($"Loaded player data successfully - Name: {PlayerData.Name}, Points: {PlayerData.Points}");
            }
            else
            {
                Debug.LogWarning("Loaded player data is null");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Could not load player data: {ex.Message}. Player may not be initialized on blockchain yet.");
            PlayerData = null;
            
            // Check if this is the "Account not found" error, which indicates the player needs to be initialized
            if (ex.Message.Contains("Account") && ex.Message.Contains("not found"))
            {
                Debug.Log("Account exists but player data not found. Attempting to initialize player...");
                await InitializePlayer("player");
                
                // Try to fetch player data again after initialization
                if (Web3.Account != null)
                {
                    // Wait a moment for blockchain to process the initialization
                    await Task.Delay(5000);
                    await FetchPlayerData();
                }
            }
            
            // If we get a network-related exception, update connection status
            if (ex is System.Net.WebException || 
                ex is System.Net.Http.HttpRequestException ||
                ex.Message.Contains("network") ||
                ex.Message.Contains("connection"))
            {
                Debug.LogWarning("Network-related error detected, marking as disconnected");
                IsConnectedToBlockchain = false;
            }
        }
    }

    public async Task InitializePlayer(string playerName = "Test Player")
    {
        // Check if Web3 is initialized - we can only continue if it is
        if (Web3.Account == null)
        {
            Debug.LogError("Cannot initialize player: Web3 account not initialized");
            return;
        }
        
        // Make sure we have a player account
        if (_playerAccount == null)
        {
            LoadPlayerAccount();
            
            if (_playerAccount == null)
            {
                Debug.LogError("Failed to load player account, cannot initialize player");
                return;
            }
        }
        
        // Check connection status
        if (!IsConnectedToBlockchain)
        {
            await CheckConnectionStatus();
            if (!IsConnectedToBlockchain)
            {
                Debug.LogWarning("Cannot initialize player: not connected to blockchain");
                return;
            }
        }

        Debug.Log($"Initializing player with account: {_playerAccount.PublicKey} and name: {playerName}");
        
        try
        {
            var blockHash = await Web3.Rpc.GetLatestBlockHashAsync();
            if (blockHash?.Result?.Value == null)
            {
                Debug.LogError("Failed to get recent blockhash");
                IsConnectedToBlockchain = false;
                return;
            }
            
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(Web3.Account)
                .AddInstruction(SolmateProgram.InitializePlayer(
                    _playerAccount.PublicKey,
                    Web3.Account.PublicKey,
                    playerName)
                );
                
            var tx = Transaction.Deserialize(transaction.Build(new List<Account> {Web3.Account, _playerAccount}));
            
            // Sign and Send the transaction
            var res = await Web3.Wallet.SignAndSendTransaction(tx);
            
            // Show Confirmation
            if (res?.Result != null)
            {
                await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
                Debug.Log("Player initialization succeeded, see transaction at https://explorer.solana.com/tx/" 
                          + res.Result + "?cluster=" + Web3.Wallet.RpcCluster.ToString().ToLower());
                
                // Fetch the player data after initialization with increased delay
                await Task.Delay(5000); // Wait 5 seconds for blockchain to process
                await FetchPlayerData();
            }
            else
            {
                Debug.LogError("Transaction failed: " + res?.ErrorData?.ToString());
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error initializing player: {ex.Message}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            Debug.LogError($"Stack trace: {ex.StackTrace}");
            IsConnectedToBlockchain = false;
        }
    }
    
    // Helper method to get the player public key (useful for other scripts)
    public PublicKey GetPlayerPublicKey()
    {
        if (_playerAccount == null)
        {
            LoadPlayerAccount();
        }
        return _playerAccount.PublicKey;
    }

    public async Task<bool> ReconnectAndLoadData()
    {
        Debug.Log("Attempting to reconnect and load data...");
        
        // Check if Web3 is initialized
        if (Web3.Account == null)
        {
            Debug.LogError("Cannot reconnect: Web3 account not initialized");
            return false;
        }
        
        try 
        {
            // Check connection status
            await CheckConnectionStatus();
            
            if (!IsConnectedToBlockchain)
            {
                Debug.LogError("Failed to connect to Solana blockchain");
                return false;
            }
            
            Debug.Log("Successfully connected to Solana blockchain");
            
            // Load data after successful connection
            await LoadDataOnConnection();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Reconnection failed: {ex.Message}");
            IsConnectedToBlockchain = false;
            return false;
        }
    }

    // This public method is for UI buttons to call
    public void InitializePlayerButton()
    {
        _ = InitializePlayer("Test Player");
    }

    // Add points from game achievements to the blockchain
    public async Task<bool> AddGamePoints(int points)
    {
        // Check if Web3 is initialized
        if (Web3.Account == null)
        {
            Debug.LogError("Cannot add game points: Web3 account not initialized");
            return false;
        }
        
        // Check connection status
        if (!IsConnectedToBlockchain)
        {
            await CheckConnectionStatus();
            if (!IsConnectedToBlockchain)
            {
                Debug.LogWarning("Cannot add game points: not connected to blockchain");
                return false;
            }
        }

        // Only try to add points if the player account is initialized
        if (PlayerData == null)
        {
            Debug.LogWarning("Player not initialized yet, can't add game points");
            return false;
        }
        
        try
        {
            Debug.Log($"Adding {points} points from game achievement...");
            Debug.Log($"Using authority account: {Web3.Account.PublicKey}");
            Debug.Log($"For player account: {_playerAccount.PublicKey}");
            
            var blockHash = await Web3.Rpc.GetLatestBlockHashAsync();
            if (blockHash?.Result?.Value == null)
            {
                Debug.LogError("Failed to get recent blockhash for AddPoints");
                IsConnectedToBlockchain = false;
                return false;
            }
            
            Debug.Log("Building AddPoints transaction...");
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(Web3.Account)
                .AddInstruction(SolmateProgram.AddPoints(
                    _playerAccount.PublicKey,
                    Web3.Account.PublicKey,
                    (ulong)points)); // Add game points
            
            Debug.Log("Building transaction with required signers...");
            byte[] txBytes = transaction.Build(new List<Account> { Web3.Account });
            var tx = Transaction.Deserialize(txBytes);
            
            Debug.Log("Signing and sending transaction...");
            // Sign and Send the transaction
            var res = await Web3.Wallet.SignAndSendTransaction(tx);
            
            if (res?.Result != null)
            {
                Debug.Log($"Transaction sent: {res.Result}");
                await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
                Debug.Log($"Successfully added {points} points to player account");
                
                // Update the global score immediately
                GlobalPlayerScore += (ulong)points;
                UpdateScoreUI();
                
                // Refresh player data to see the updated points
                await Task.Delay(2000); // Wait for blockchain to update
                await FetchPlayerData();
                return true;
            }
            else
            {
                Debug.LogError("Failed to add points: " + (res?.ErrorData != null ? res.ErrorData.ToString() : "null response"));
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error adding game points: {ex.Message}");
            if (ex.InnerException != null) {
                Debug.LogError($"Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
            Debug.LogError($"Stack trace: {ex.StackTrace}");
            IsConnectedToBlockchain = false;
            return false;
        }
    }
    
    // Called from UI to update score display
    public void UpdateScore()
    {
        UpdateScoreUI();
    }
    
    // Get current player score for other scripts to use
    public ulong GetPlayerScore()
    {
        return GlobalPlayerScore;
    }

    // Update the UI with the current score
    private void UpdateScoreUI()
    {
        if (_scoreText != null)
        {
            _scoreText.text = GlobalPlayerScore.ToString();
        }
    }
    
    // Create a singleton instance that can be accessed from other scripts
    private static AccountManager _instance;
    public static AccountManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AccountManager>();
                if (_instance == null)
                {
                    Debug.LogError("AccountManager instance not found in scene!");
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        // Ensure this component persists between scenes
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }
}