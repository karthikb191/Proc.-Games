using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Unity.Editor;
using Firebase.Storage;
using Firebase.Auth;
using Firebase.Database;

public class StudentDetails {
    public string displayName;
    public string email;
    public string standard;
    public string package;
    public string id;
    public StudentDetails(string id, string email, string name, string standard, string package) {
        this.id = id;
        this.email = email;
        this.displayName = name;
        this.standard = standard;
        this.package = package;
    }
    //public struct Games { }
}

enum Initialization {
    no,
    checksComplete,
    done
}

public class UserManager : MonoBehaviour {
    public GameObject MessageCanvas;

    public static GameObject messageCanvas;
    private static UserManager userManager;
    public static UserManager USERMANAGER {
        get {
            return userManager;
        }
        set {
            userManager = value;
        }
    }

    Initialization initialization = Initialization.no;

    public static string userId;
    public static string userName;

    public static bool userCanGoToNextLevel = false;

    //Authentication variables
    public static FirebaseAuth authentication;
    public static FirebaseUser user = null;

    //Database variables
    public static FirebaseDatabase database;
    public static DatabaseReference databaseRef;

    //Storage reference variables
    public static FirebaseStorage storage;
    public static StorageReference storageRef;

    //a few delegates
    public delegate void userDetected();
    public static event userDetected UserDectectedEvent;
    public delegate void noUserFound();
    public static event noUserFound NoUserFoundEvent;
    public delegate IEnumerator loadLevels();
    public static event loadLevels LoadLevelsEvent;

    //SignIn and SignOut delegates
    public delegate void signInComplete();
    public static event signInComplete SignInCompleteEvent;
    public delegate void signInFaulted(string errorMessage);
    public static event signInFaulted SignInFaultedEvent;

    public delegate void signUpComplete();
    public static event signUpComplete SignUpCompleteEvent;
    public delegate void signUpFaulted(string errorMessage);
    public static event signUpFaulted SignUpFaultedEvent;

    private static bool userCanLogin;

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            GUIStyle guistyle = new GUIStyle();
            guistyle.fontSize = 30;
            guistyle.normal.textColor = Color.black;
            
            GUI.Label(new Rect(50, 50, 400, 50), "User: " + userName, guistyle);
        }

    }

    private void Awake()
    {
        userCanLogin = true;
        //SceneManager.sceneLoaded += SceneLoaded;
        UserManager[] ums = FindObjectsOfType<UserManager>();
        foreach (UserManager u in ums)
        {
            if (u.gameObject != this.gameObject)
                Destroy(this.gameObject);
        }
        
    }

    // Use this for initialization
    void Start () {
        Debug.Log("user manager executing");
        DontDestroyOnLoad(this.gameObject);


        //instatiate message canvas
        messageCanvas = Instantiate(MessageCanvas);
        messageCanvas = GameObject.FindGameObjectWithTag("message canvas");

        messageCanvas.SetActive(false);
        DontDestroyOnLoad(messageCanvas);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            
            if (dependencyStatus == DependencyStatus.Available)
            {
                StartCoroutine(ErrorMessage("Firebase initialized"));
                InitializeFirebase();
            }
            else {
                StartCoroutine(ErrorMessage("Problem initializing firebase......"));
                Debug.Log("Problem connecting to the google services");
            }
            
        });

        

        SceneManager.sceneLoaded += OnLevelChange;
        DontDestroyOnLoad(this.gameObject);
        
	}

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "StartScreen")
        {
            if (initialization == Initialization.checksComplete)
            {
                if (user == null)
                {
                    NoUserFoundEvent();
                }
                else
                {
                    UserDectectedEvent();
                    if (storage != null)
                        StartCoroutine(LoadLevelsEvent());  //After the user is detected, it's time to lead the levels
                    else
                        Debug.Log("storage reference is not established......nooooooooooooooo");
                }
                initialization = Initialization.done;
            }
        }
        
    }
    public void InitializeFirebase()
    {
        //Initialize the firebase Authentication
        authentication = FirebaseAuth.DefaultInstance;
        user = authentication.CurrentUser;

        FirebaseAuth.DefaultInstance.StateChanged += UserStateChanged;
        UserStateChanged(this, null);
        
        //user = null;

        //Initializing the firebase database details
        database = FirebaseDatabase.DefaultInstance;
        databaseRef = database.RootReference;
        if (database == null)
        {
            StartCoroutine(ErrorMessage("Database link error"));
            Debug.Log("Database connection failed....");
        }
        else
        {
            StartCoroutine(ErrorMessage("Database link obtained....."));
            FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://proceduralgames-bf245.firebaseio.com/");
            FirebaseApp.DefaultInstance.SetEditorServiceAccountEmail("proceduralgames-bf245@appspot.gserviceaccount.com");
            FirebaseApp.DefaultInstance.SetEditorP12FileName("ProceduralGames-81a568312783.p12");
            FirebaseApp.DefaultInstance.SetEditorP12Password("notasecret");
        }
        //Initializing the storage 
        storage = FirebaseStorage.DefaultInstance;
        storageRef = storage.GetReferenceFromUrl("gs://proceduralgames-bf245.appspot.com/");

        //add a functionality that reads the complete database to get the datashot
        FirebaseDatabase.DefaultInstance.RootReference.Child(user.UserId).ValueChanged += ValueChanged;
    }
    void ValueChanged(object sender, ValueChangedEventArgs args)
    {
        StopAllCoroutines();
        Debug.Log("Root reference value changed");
    }

    public void UserStateChanged(System.Object obj, System.EventArgs args)
    {
        if (authentication.CurrentUser != user)
        {
            Debug.Log("User state has changed....");
            user = authentication.CurrentUser;
            bool signedIn = user != authentication.CurrentUser && authentication.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("signed out: " + user.UserId);
            }
            if (signedIn)
            {
                user = authentication.CurrentUser;
                userId = user.UserId;
                userName = user.DisplayName;
                UserDectectedEvent();   //This event executes when no user is found
            }
        }
        initialization = Initialization.checksComplete; //setting this to checks complete, so that levels can be checked and loaded again
    }


    public void PerformSignUp(string email, string password, string dispName)
    {
        userCanLogin = false;
        user = null;
        authentication.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.Log("Something has gone wrong.....take care");
            }
            else
            {
                user = task.Result;
                if (user != null)
                {
                    //updating profile
                    Debug.Log("accessing profile right now");

                    Firebase.Auth.UserProfile profile = new Firebase.Auth.UserProfile
                    {
                        DisplayName = dispName
                        
                    };
                    
                    user.UpdateUserProfileAsync(profile).ContinueWith(t => {
                        if (t.IsCanceled || t.IsFaulted)
                        {
                            Debug.Log("Failed to update profile");
                        }
                        else if (task.IsCompleted)
                        {
                            Debug.Log("successfully created. Name: " + user.DisplayName);

                            Debug.Log("display name is: " + user.DisplayName);
                            Debug.Log("Sign up successful");

                            userName = dispName;
                            CheckAndAddStudentDataToDatabase(true);
                        }
                    });
                }

            }
            userCanLogin = true;
        });
        
    }
    public void CheckAndAddStudentDataToDatabase(bool signUp)
    {
        messageCanvas.SetActive(true);
        Debug.Log("display name of user: " + user.DisplayName);
        StudentDetails student = new StudentDetails(user.UserId, user.Email, user.DisplayName, "1", "None");
        string json = JsonUtility.ToJson(student);
        

        StopAllCoroutines();
        messageCanvas.SetActive(true);

        if(databaseCoroutine == null)
            databaseCoroutine = StartCoroutine(ReadFromDatabase(signUp, json));
        
    }
    Coroutine databaseCoroutine = null;

    IEnumerator ReadFromDatabase(bool signUp, string json)
    {
        int count = 0;
        bool valueObtained = false;
        //if value obtained or count greater than 10, break the loop
        while (!valueObtained && count < 100)
        {
            count++;
            
            yield return new WaitForSeconds(0.5f);

            messageCanvas.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Count:..." + count;

            valueObtained = true;
            databaseRef = FirebaseDatabase.DefaultInstance.RootReference;
            Debug.Log("User id: " + databaseRef.Child(user.UserId).Key);
            //If signing up, add the data to database
            if (signUp)
            {
                databaseRef.Child(user.UserId).SetRawJsonValueAsync(json);
            }
            if (databaseRef.Child(user.UserId) == null)
            {
                databaseRef.SetRawJsonValueAsync(json);
                Debug.Log("Data added to the database");
                if (!signUp)
                {
                    StartCoroutine(ErrorMessage("Starting the game"));
                    CheckingUser.StartTheGame();    //After the data is loaded, user can start the game
                }
            }
            else {
                Debug.Log("child has been found and it is not null");
                if (!signUp)
                {
                    StartCoroutine(ErrorMessage("Starting the game"));
                    CheckingUser.StartTheGame();    //After the data is loaded, user can start the game
                }
                else
                {
                    StartCoroutine(ErrorMessage("Unable to start the game"));
                    userCanLogin = true;
                    Debug.Log("sign up complete and user can login now");
                }
                messageCanvas.SetActive(false);
            }

            //databaseRef.GetValueAsync().ContinueWith(task =>
            //{
            //    if (!valueObtained)
            //    {
            //        valueObtained = true;
            //        messageCanvas.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Writing to Database...." + user.UserId;
            //
            //        if (task.IsCanceled || task.IsFaulted)
            //        {
            //
            //            StartCoroutine(ErrorMessage("Login faulted....try again"));
            //            Debug.Log("failed to take snapshot");
            //            shot = null;
            //        }
            //        else
            //        {
            //            messageCanvas.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Writing to Database....";
            //            Debug.Log("snapshot taken successfully");
            //            shot = task.Result;
            //            //string name = user.Email.Split('@')[0];
            //            string child = user.UserId;
            //            //string child = user.DisplayName + "-" + user.UserId;
            //
            //            Debug.Log("child: " + child);
            //            if (shot != null && !shot.HasChild(child))    //shot is not null and shot doesnt have the child
            //            {
            //                databaseRef.Child(user.UserId).SetRawJsonValueAsync(json);
            //                Debug.Log("Data added to the database");
            //            }
            //            else if (shot != null)
            //            {
            //                Debug.Log("Data is already present in the database");
            //            }
            //            else
            //                Debug.Log("shot is null");
            //            //userCanGoToNextLevel = true;
            //            if (!signUp)
            //            {
            //                StartCoroutine(ErrorMessage("Starting the game"));
            //                CheckingUser.StartTheGame();    //After the data is loaded, user can start the game
            //            }
            //            else
            //            {
            //                StartCoroutine(ErrorMessage("Unable to start the game"));
            //                userCanLogin = true;
            //                Debug.Log("sign up complete and user can login now");
            //            }
            //            messageCanvas.SetActive(false);
            //        }
            //    }
            //});
        }
    }
    public static IEnumerator ErrorMessage(string msg)
    {
        messageCanvas.SetActive(true);
        messageCanvas.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = msg;
        yield return new WaitForSeconds(2);
        messageCanvas.SetActive(false);
        
    }

    public void PerformSignIn(string email, string password)
    {
        if (userCanLogin)
        {
            if (FirebaseAuth.DefaultInstance.CurrentUser == null)
            {
                //activating the message canvas to display some message
                messageCanvas.SetActive(true);
                messageCanvas.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Logging IN.....Please wait";

                Debug.Log("current user is null, so performing login");
                userCanGoToNextLevel = false;
                authentication.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
                {
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        StartCoroutine(ErrorMessage("Login is faulted....try again"));
                        Debug.Log("Sign in problem....damn it");
                    }
                    else
                    {
                        StartCoroutine(ErrorMessage("Login Successful......."));
                        user = task.Result;
                        Debug.Log("SignIn Successful....Taking details and going to next level");
                        Debug.Log("user name is: " + user.DisplayName);
                        CheckAndAddStudentDataToDatabase(false);
                    }
                });
            }
            else
            {
                FirebaseAuth.DefaultInstance.SignOut();
                StartCoroutine(ErrorMessage("Please sign-in Again"));
                Debug.Log("current user is already logged in. So, performing next operations");
                //CheckAndAddStudentDataToDatabase(false);
            }
        }
        else
        {
            Debug.Log("User cant login yet");
        }
    }

    void SceneLoaded(Scene s, LoadSceneMode loadSceneMode)
    {
        //Check for multiple UseManagers
        UserManager[] ums = FindObjectsOfType<UserManager>();
        foreach (UserManager u in ums) {
            if (u.gameObject != this.gameObject)
                Destroy(u.gameObject);
        }

        if (s.buildIndex == 0)
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                var dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    InitializeFirebase();
                }
                else
                {
                    Debug.Log("Problem connecting to the google services");
                }
            });
        }
    }

    public static void SighOut() {
        Debug.Log("Signing out");
        authentication.SignOut();
        messageCanvas.SetActive(false);
        //After logout, go back to main screen
        SceneManager.LoadScene(0);
    }

    public static void BackToLevelSelect()
    {
        Debug.Log("Going back to Level Select");
        //After logout, go back to main screen
        SceneManager.LoadScene("StartScreen");
    }

    public static void UnloadAllAssetBundles()
    {
        AssetBundle[] bundles = Resources.FindObjectsOfTypeAll<AssetBundle>();
        Debug.Log("number of bundles " + bundles.Length);

        for (int i = 0; i < bundles.Length; i++)
        {
            Debug.Log("Bundle: " + bundles[i].name);
            bundles[i].Unload(true);
        }
    }

    void OnLevelChange(Scene scene, LoadSceneMode lsm)
    {
        messageCanvas.SetActive(false);
        if(scene.name == "StartScreen")
        {
            initialization = Initialization.checksComplete;
            //UnloadAllAssetBundles();
        }
    }
}
