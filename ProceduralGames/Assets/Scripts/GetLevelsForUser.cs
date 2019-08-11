using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Storage;

public class GetLevelsForUser : MonoBehaviour {
    public GameObject authenticationCanvas;
    public GameObject levelsCanvas;

    public GameObject levelButtonPrefab;

    public FirebaseUser currentUser;

    public bool timeOut = false;
    bool levelObtained = false;     //This variable is used to wait till one level gets populated before attempting to download another level
    bool skipLevel = false;

    List<Button> levelButtons = new List<Button>();
    StorageReference packageStoragePath;

    PackagesInfo thinktacPackages;

    public static bool runningAProcess = false;

    private void Awake()
    {
        thinktacPackages = new PackagesInfo();
        Debug.Log("packages have been taken");
        //Debug.Log("package info: " + thinktacPackages.defaultPackageList[0]);
        //SceneManager.sceneLoaded += SceneLoaded;
        GetLevelsForUser[] glu = FindObjectsOfType<GetLevelsForUser>();
        foreach (GetLevelsForUser g in glu)
        {
            if (g.gameObject != this.gameObject)
                Destroy(this.gameObject);
        }
    }

    // Use this for initialization
    void Start () {
        authenticationCanvas.SetActive(true);
        levelsCanvas.SetActive(true);
        //CheckForAuthentication();   //Check for authentication
        UserManager.NoUserFoundEvent += NoUserFound;
        UserManager.UserDectectedEvent += UserFound;
        UserManager.LoadLevelsEvent += CheckAndGetLevelsFromServer;
    }

    void NoUserFound() {
        timeOut = true;
        Debug.Log("No user found....too bad....but this code is working, so yay");
        //StartCoroutine(LoginError());
        StartCoroutine(LoginError());
    }
    void UserFound() {
        timeOut = true;
        Debug.Log("User is found...yay");
        StartCoroutine(LoginSuccessful());
    }
    IEnumerator LoginSuccessful()
    {
        //yield return new WaitForSeconds(0.5f);
        authenticationCanvas.SetActive(true);
        Debug.Log("Login successful");
        authenticationCanvas.transform.GetChild(0).GetComponent<Text>().text = "Login Successful....Finding levels";

        //TODO: Get levels from the internet here

        yield return new WaitForSeconds(4);
        authenticationCanvas.SetActive(false);

        levelsCanvas.SetActive(true);

        //get the package info from the server
        FirebaseDatabase.DefaultInstance.RootReference.Child(UserManager.user.UserId).Child("package").ValueChanged += ValueChanged;
    }

    string package = ""; //The package user has subscribed to

    void ValueChanged(object sender, ValueChangedEventArgs args) {
        StopAllCoroutines();
        Debug.Log("args value:  " +args.Snapshot.Value);
        package = args.Snapshot.Value.ToString();
        UserManager.ErrorMessage("package obtained......");
    }
    
    IEnumerator CheckAndGetLevelsFromServer() {

        //wait for package name
        while (package == "")
        {
            Debug.Log("waiting for package");
            yield return new WaitForSeconds(0.5f);
        }

        //string name = user.Email.Split('@')[0];
        string child = UserManager.user.UserId;

        //Get the package name as per user id
        Debug.Log("user id is:" + child);
        

        if(package == "")
        {
            Debug.Log("package error.....");
        }
        else
        {
            Debug.Log("Instantiating all buttons....package is: " + package);
            InstantiateAllButtonsInThePackageAndCheckTheLocalStorage(package);
        }


        //UserManager.databaseRef.Child(child).Child("package").GetValueAsync().ContinueWith(task => {
        //    if (task.IsFaulted || task.IsCanceled) {
        //        Debug.Log("Unable to fetch the package name");
        //    }
        //    else
        //    {
        //        package = task.Result.Value.ToString();
        //        Debug.Log("package is: " + package);
        //
        //        InstantiateAllButtonsInThePackageAndCheckTheLocalStorage(package);
        //        //DownloadLevelsAccordingPackage(package);
        //    }
        //});
    }

    List<string> CheckAndReturnCurrentLevelsFromLocalStorage(out string localPath) {
        List<string> levels = new List<string>();
        
        string path = Path.Combine(Application.persistentDataPath, "ScenesBundles");
        localPath = path;
        //If the directory doesnt exist, create the directory
        if (!Directory.Exists(path))
        {
            Debug.Log("Directory doesnt exist....so added the directory");
            Directory.CreateDirectory(path);
        }
        else
        {
            Debug.Log("Directory exists already, so using it....");
        }
        //After the creation of the directory, continue to get the bundles in that directory
        string[] files = Directory.GetFiles(path);
        foreach (string f in files)
        {
            levels.Add(Path.GetFileNameWithoutExtension(f));
        }
        return levels;
    }

    //This function gets the package information. It goes from there to buttons and level creation

    void InstantiateAllButtonsInThePackageAndCheckTheLocalStorage(string package)
    {
        string localStoragePath = Path.Combine(Application.persistentDataPath, "ScenesBundles");

        List<string> filesInStorage = new List<string>();
        //while instantiating the buttons, check the local storage and add the link to the scene if it exists
        if (!Directory.Exists(localStoragePath))
        {
            Directory.CreateDirectory(localStoragePath);
        }
        else
        {
            //After the creation of the directory, continue to get the bundles in that directory
            string[] files = Directory.GetFiles(localStoragePath);
            foreach (string f in files)
            {
                filesInStorage.Add(Path.GetFileNameWithoutExtension(f) + ".unity3d");
            }
        }

        List<string> scenesInPackage = GetLevelsFromPackage(package);

        //If the file is present in the local storage, link it to the scene, or else link it to the download function
        foreach(string scene in scenesInPackage)
        {
            //if the file storage consists of the file that we are currently reading
            if (filesInStorage.Contains(scene + ".unity3d"))
            {
                StartCoroutine(MakeAssetBundle(package, Path.Combine(localStoragePath, scene + ".unity3d"), null));
                //Instantiate button and link it to the scene

            }
            else
            {
                //Instantiate button and link it to the download function
                Debug.Log("Files not present in the storage");
                //create a button saying "download the level"
                Button b = Instantiate(levelButtonPrefab, levelsCanvas.transform.GetChild(0).GetChild(0)).GetComponent<Button>();
                b.name = Path.GetFileNameWithoutExtension(scene);
                b.transform.GetChild(0).GetComponent<Text>().text = "Download " + b.name;
                b.onClick.RemoveAllListeners();
                //Adding the listener that points to the function that downloads the level
                b.onClick.AddListener(() => {
                    if (!runningAProcess)
                    {
                        DownloadLevelFromServer(package, scene, localStoragePath, b);
                        b.enabled = false;
                    }
                });
                
            }
        }
    }

    List<string> GetLevelsFromPackage(string package)
    {
        if(package == "None")
        {
            return thinktacPackages.defaultPackageList;
        }
        if(package == "Lite")
        {
            return thinktacPackages.litePackageList;
        }
        if(package == "Exceed")
        {
            return thinktacPackages.exceedPackageList;
        }
        return null;
    }

    IEnumerator MakeAssetBundle(string package, string filePath, Button but)
    {
        UserManager.messageCanvas.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Extracting Asset Bundle for: " + package;

        string localPath = Path.GetDirectoryName(filePath);
        Debug.Log("directory name is: " + localPath);
        
        bool bundleLoadedAlready = false;
        //Get all the bundles that have already been loaded into unity
        AssetBundle[] bundles = Resources.FindObjectsOfTypeAll<AssetBundle>();
        Debug.Log("number of bundles already loaded are: " + bundles.Length);

        //If the bundle is already loaded, then instantiate button and link it to the scene
        foreach (AssetBundle b in bundles)
        {
            //If the bundle name and the file name are the same, then continue adding the button
            if(b.name == Path.GetFileNameWithoutExtension(filePath))
            {
                bundleLoadedAlready = true;
                if (b.isStreamedSceneAssetBundle)
                {
                    string[] s = b.GetAllScenePaths();
                    string sceneName = Path.GetFileNameWithoutExtension(s[0]);

                    //TODO: add the link to the button here
                    //yield return new WaitForSeconds(2);
                    Debug.Log("Scene loading: " + sceneName);

                    //Creating the Button
                    if(but == null)
                        but = Instantiate(levelButtonPrefab, levelsCanvas.transform.GetChild(0).GetChild(0)).GetComponent<Button>();

                    but.name = Path.GetFileNameWithoutExtension(filePath);
                    but.transform.GetChild(0).GetComponent<Text>().text = "Play " + b.name;
                    but.onClick.RemoveAllListeners();
                    but.onClick.AddListener(() =>
                    {
                        //Adding a delegate that loads respective scene when clicked
                        Debug.Log("Adding the listener to load the scene: " + sceneName);
                        Debug.Log("running a process? " + runningAProcess);
                        if(!runningAProcess)
                            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

                    });
                    //enabling the delete button and deleting the level from storage
                    Button deleteBut = but.transform.GetChild(1).GetComponent<Button>();
                    deleteBut.onClick.AddListener(() => {
                        //delete the level from the storage and add the link to the download here
                        if (!runningAProcess)
                        {
                            File.Delete(filePath);
                            Debug.Log("Files has been deleted");

                            //Now, change the text on the actual button and set the download function
                            but.transform.GetChild(0).GetComponent<Text>().text = "Download: " + b.name;
                            but.onClick.RemoveAllListeners();
                            but.onClick.AddListener(() =>
                            {
                                if (!runningAProcess)
                                {
                                    DownloadLevelFromServer(package, b.name, localPath, but);
                                    but.enabled = false;
                                }
                            });
                        }
                    });

                    levelObtained = true;
                    //UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                }
                else
                {
                    //If the asset bundle is not a streaming asset bundle, give out a failed message
                    skipLevel = true;
                    UserManager.messageCanvas.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "failed for: " + b.name;
                    Debug.Log("Not a scene bundle");
                }
            }
        }
        Debug.Log("Finished making file and making asset bundles");

        //If the asset bundle is not already loaded to the scene, load it 
        if (!bundleLoadedAlready)
        {
            AssetBundleCreateRequest ass = AssetBundle.LoadFromFileAsync(filePath);
            Debug.Log("loading file from local path");
            yield return ass;
            AssetBundle bun = ass.assetBundle;

            //text.text = "finished making asset bundle and loading scene";

            if (bun != null)
            {
                if (bun.isStreamedSceneAssetBundle)
                {
                    string[] s = bun.GetAllScenePaths();
                    string sceneName = Path.GetFileNameWithoutExtension(s[0]);

                    //TODO: add the link to the button here
                    //yield return new WaitForSeconds(2);
                    Debug.Log("Scene loading: " + sceneName);

                    //Creating the Button
                    if(but == null)
                        but = Instantiate(levelButtonPrefab, levelsCanvas.transform.GetChild(0).GetChild(0)).GetComponent<Button>();
                    but.name = Path.GetFileNameWithoutExtension(filePath);
                    but.transform.GetChild(0).GetComponent<Text>().text = "Play " + but.name;
                    but.onClick.RemoveAllListeners();
                    but.onClick.AddListener(() =>
                    {   //Adding a delegate that loads respective scene when clicked
                        if(!runningAProcess)
                            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                    });

                    //enable the delete button and add it's function
                    Button deleteBut = but.transform.GetChild(1).GetComponent<Button>();

                    deleteBut.onClick.AddListener(() => {
                        //delete the level from the storage and add the link to the download here
                        if (!runningAProcess)
                        {
                            File.Delete(filePath);
                            Debug.Log("Files has been deleted");

                            //Now, change the text on the actual button and set the download function
                            but.transform.GetChild(0).GetComponent<Text>().text = "Download " + but.name;
                            but.onClick.RemoveAllListeners();
                            but.onClick.AddListener(() =>
                            {
                                if (!runningAProcess)
                                {
                                    DownloadLevelFromServer(package, bun.name, localPath, but);
                                    but.enabled = false;
                                }
                            });

                        }
                        
                    });

                    levelObtained = true;
                }
                else
                {
                    skipLevel = true;
                    Debug.Log("Not a scene bundle");
                }
            }
            else
            {
                skipLevel = true;
                Debug.Log("Asset bundle is null.....Damn it");
            }
        }

        UserManager.messageCanvas.SetActive(false);


        levelObtained = true;
        //enable the button after finishing loading the bundle into the memory
        but.enabled = true;
        runningAProcess = false;
    }

    void DownloadLevelFromServer(string package, string fileName, string localPath, Button b)
    {
        string filePath = Path.Combine(localPath, fileName + ".unity3d");
        Debug.Log("File path is: " + filePath);
        Debug.Log("File name is: " + fileName);

        runningAProcess = true;
        
        Debug.Log("Downloading the level from the server");

        //For now, we ignore the package....

        //Checking if there exists a URL link. If it doesn't, the files doesnt exist in the storage
        Debug.Log(fileName + " is not present in local storage....attempting to download");
        //if the level is not present in the local storage, check the database to see if its there and download it

        packageStoragePath = UserManager.storageRef.Child(fileName);

        Debug.Log("package storage path: " + packageStoragePath.Path);

        packageStoragePath.GetDownloadUrlAsync().ContinueWith(t => {
            UserManager.messageCanvas.SetActive(true);
            UserManager.messageCanvas.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Downloading level: " + fileName;

            if (t.IsFaulted | t.IsCanceled)
            {
                Debug.Log("faulted for: " + fileName);
                //itemReceived = true;
                levelObtained = true;
                skipLevel = true;   //skip the level is it if faulted
                b.enabled = true;
                runningAProcess = false;
            }
            else
            {
                Debug.Log("succeeded for : " + fileName + " uri is: " + t.Result.ToString());
                if (packageStoragePath != null)
                {

                    Debug.Log("file found: " + packageStoragePath.Path);
                    //If the package storage path is not null, then download to local storage
                    const long maxAllowedSize = 300 * 1024 * 1024;
                    packageStoragePath.GetBytesAsync(maxAllowedSize).ContinueWith(task => {
                        if (task.IsFaulted || task.IsCanceled)
                        {
                            Debug.Log("faulted for file: " + fileName);
                            Debug.Log("task is faulted or cancelled");
                            runningAProcess = false;
                            skipLevel = true;   //If the download is faulted, skip level
                        }
                        else if (task.IsCompleted)
                        {

                            byte[] fileContents = task.Result;
                            Debug.Log("Finished downloading!");

                            //make a file of the byte stream

                            Debug.Log("directory is: " + localPath);
                            string tempPath = Path.Combine(localPath, fileName + ".unity3d");
                            Debug.Log("saving to the path: " + tempPath);
                            //levelsToRetain.Add(p);  //Add this level to levels to retain
                            MakeFile(fileContents, tempPath);
                            
                            //Now we have to make the asset bundle to incorporate it into unity and load scenes
                            //level obtained is set at the end of coroutine so that one level is completely obtained before attempting to download next
                            //StartCoroutine(MakeAssetBundle(directory + "/" + fileName + ".unity3d"));
                            
                            StartCoroutine(MakeAssetBundle(package, filePath, b));
                            //levelObtained = true;
                            
                        }

                        //set the active state of message canvas to false
                        UserManager.messageCanvas.SetActive(false);
                    });
                }
                else
                {
                    UserManager.messageCanvas.SetActive(false);
                    skipLevel = true;
                    Debug.Log("File is not found in the storage......Maybe it has to be added");
                }

                //continue with adding buttons after all the download is complete
                //AddButtonsAndCleanUp();

            }
        });

    }

    void MakeFile(byte[] fileContents, string tempPath)
    {
        //write contents to the file
        try
        {
            File.WriteAllBytes(tempPath, fileContents);
            Debug.Log("Saved Data to: " + tempPath.Replace("/", "\\"));
        }
        catch (Exception e)
        {
            Debug.Log("Failed To Save Data to: " + tempPath.Replace("/", "\\"));
            Debug.Log("Error: " + e.Message);
        }
    }

    IEnumerator MakeAssetBundle(string filePath)
    {
        yield return null;
    }

    void DownloadLevelsAccordingPackage(string package) {
        //Get the file names in the storage
        
        string localStoragePath;
        List<string> levelsPresent = CheckAndReturnCurrentLevelsFromLocalStorage(out localStoragePath); //levels that are currently present in local storage
        
        Debug.Log("levels present are: " + levelsPresent.Count);
        
        packageStoragePath = UserManager.storageRef;
        Debug.Log("package storage path is: " + packageStoragePath);
        if (package == "None")
        {
            Debug.Log("inside the none package");
            //packageStoragePath = packageStoragePath.Child("None");
            packageStoragePath = UserManager.storage.GetReference("None");

            List<string> defPackage = thinktacPackages.defaultPackageList;

            StartCoroutine(FindOrDownloadPackages(defPackage, levelsPresent, package, localStoragePath));

        }
        else if (package == "Lite") {
            packageStoragePath = packageStoragePath.Child("Lite");
            List<string> litePackage = thinktacPackages.litePackageList;

            StartCoroutine(FindOrDownloadPackages(litePackage, levelsPresent, package, localStoragePath));

        }
        else if(package == "Exceed")
        {
            packageStoragePath = packageStoragePath.Child("Exceed");
            List<string> exceedPackage = thinktacPackages.exceedPackageList;

            StartCoroutine(FindOrDownloadPackages(exceedPackage, levelsPresent, package, localStoragePath));
        }
        else
        {
            Debug.Log("Package name given is not correct....Check again");
            return;
        }
    }

    IEnumerator FindOrDownloadPackages(List<string> defPackage, List<string> levelsPresent, string package, string localStoragePath)
    {
        UserManager.messageCanvas.SetActive(true);
        UserManager.messageCanvas.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Populating levels.....Please wait";
        List<string> levelsToRetain = new List<string>();           //levels that must be shown right now
        foreach (string p in defPackage)
        {
            //for each package in the list inside class PackageInfo, either skip or attempt to download
            levelObtained = false;
            skipLevel = false;

            Debug.Log("changed name: " + p + ".unity3d");
            if (levelsPresent.Contains(p))
            {    //If the level is already present, then pass
                Debug.Log(p + " is present in storage");
                string filePath = localStoragePath + "/" + p + ".unity3d";
                Debug.Log("local storage path: " + filePath);
                StartCoroutine(MakeAssetBundle(filePath));
                levelsToRetain.Add(p);
                levelObtained = true;
                continue;
            }
            else
            {
                Debug.Log(p + " is not present in local storage....attempting to download");
                //if the level is not present in the local storage, check the database to see if its there and download it
                
                packageStoragePath = UserManager.storageRef.Child(package + "/" + p);

                Debug.Log("package storage path: " + packageStoragePath.Path);

                packageStoragePath.GetDownloadUrlAsync().ContinueWith(t => {
                    if (t.IsFaulted | t.IsCanceled)
                    {
                        Debug.Log("faulted for: " + p);
                        //itemReceived = true;
                        levelObtained = true;
                        skipLevel = true;   //skip the level is it if faulted
                    }
                    else
                    {
                        
                        Debug.Log("succeeded for : " + p + " uri is: " + t.Result.ToString());
                        if (packageStoragePath != null)
                        {
                            
                            Debug.Log("file found: " + packageStoragePath.Path);
                            //If the package storage path is not null, then download to local storage
                            const long maxAllowedSize = 300 * 1024 * 1024;
                            packageStoragePath.GetBytesAsync(maxAllowedSize).ContinueWith(task => {
                                if (task.IsFaulted || task.IsCanceled)
                                {
                                    Debug.Log("faulted for file: " + p);
                                    Debug.Log("task is faulted or cancelled");
                                    skipLevel = true;   //If the download is faulted, skip level
                                }
                                else if (task.IsCompleted)
                                {

                                    byte[] fileContents = task.Result;
                                    Debug.Log("Finished downloading!");

                                    //make a file of the byte stream
                                    string fileName = p;

                                    string tempPath = Path.Combine(localStoragePath, fileName + ".unity3d");
                                    Debug.Log("saving to the path: " + tempPath);
                                    levelsToRetain.Add(p);  //Add this level to levels to retain
                                    MakeFile(fileContents, tempPath);


                                    //Now we have to make the asset bundle to incorporate it into unity and load scenes
                                    //level obtained is set at the end of coroutine so that one level is completely obtained before attempting to download next
                                    StartCoroutine(MakeAssetBundle(localStoragePath + "/" + fileName + ".unity3d"));
                                    //levelObtained = true;
                                }

                            });
                        }
                        else
                        {
                            skipLevel = true;
                            Debug.Log("File is not found in the storage......Maybe it has to be added");
                        }

                        //continue with adding buttons after all the download is complete
                        //AddButtonsAndCleanUp();
                        
                    }
                });
                UserManager.messageCanvas.transform.GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Getting level: " + p;
                while (!levelObtained && !skipLevel) {
                    Debug.Log("waiting for......" + p);
                    yield return new WaitForFixedUpdate();
                }
                Debug.Log("level obtained: " + levelObtained + " skipped level" + skipLevel);
            }
            
        }
        UserManager.messageCanvas.SetActive(false);
    }





    IEnumerator TimeOutFunction() {
        float timer = 0;
        authenticationCanvas.SetActive(true);
        authenticationCanvas.transform.GetChild(0).GetComponent<Text>().text = "Connecting to network";
        while (true) {
            yield return new WaitForFixedUpdate();
            timer += Time.deltaTime;

            if (timer > 5.0f || timeOut) {
                timeOut = true;
                NoUserFound();
                break;
            }

        }
    }
    
    IEnumerator LoginError()
    {
        authenticationCanvas.SetActive(true);
        Debug.Log("Something is wrong with login....Go back and login");
        authenticationCanvas.transform.GetChild(0).GetComponent<Text>().text = "Something is wrong with login....Going back and login";
        yield return new WaitForSeconds(4);
        //TODO: Change this to exit application when deployed in the app
        Debug.Log("Loading login scene");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
    }




    private void OnDestroy()
    {
        UserManager.NoUserFoundEvent -= NoUserFound;
        UserManager.UserDectectedEvent -= UserFound;
        Debug.Log("destroying all coroutines");
        StopAllCoroutines();
        //Destroy all buttons:
        foreach (Button b in levelButtons)
            Destroy(b);
        //unloading the loaded asset bundles
        //UserManager.UnloadAllAssetBundles();
        
    }
    
}
