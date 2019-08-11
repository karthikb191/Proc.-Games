using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class CheckingUser : MonoBehaviour {
    string emailSignUp = "Email";
    string displayName = "Display Name";
    string passwordSignUp = "********";
    string passwordReenterSignUp = "";

    string emailSignIn = "email";
    string passwordSignIn = "password";

    GameObject loginCanvas;

    //signin variables
    GameObject signInPanel;
    GameObject signinEmailInputField;
    GameObject signinPasswordField;
    GameObject signinSubmitButton;
    GameObject signinSignUpButton;
    Animator signInAnimator;

    //signup variables
    GameObject signUpPanel;
    GameObject signupEmailInputField;
    GameObject signupUsername;
    GameObject signupPasswordField;
    GameObject reenterPasswordField;
    GameObject signupSubmitButton;
    GameObject signupBackButton;
    Animator signUpAnimator;

    GameObject errorPanel;
    Coroutine errorCoroutine;

    UserManager userManager;
    // Use this for initialization
    void Start () {
        userManager = FindObjectOfType<UserManager>();
        loginCanvas = GameObject.FindGameObjectWithTag("login canvas");

        //initializing sign parameters
        signInPanel = loginCanvas.transform.Find("SignIn Panel").gameObject;
        signinEmailInputField = signInPanel.transform.Find("E-Mail Input Field").gameObject;
        signinPasswordField = signInPanel.transform.Find("Password Input Field").gameObject;
        signinSubmitButton = signInPanel.transform.Find("Submit Button").gameObject;
        signinSignUpButton = signInPanel.transform.Find("SignUp Button").gameObject;

        signInAnimator = signInPanel.GetComponent<Animator>();

        //initializing signup parameters
        signUpPanel = loginCanvas.transform.Find("SignUp Panel").gameObject;
        
        signupEmailInputField = signUpPanel.transform.Find("E-Mail Input Field").gameObject;
        signupUsername = signUpPanel.transform.Find("Username").gameObject;
        signupPasswordField = signUpPanel.transform.Find("Password Input Field").gameObject;
        reenterPasswordField = signUpPanel.transform.Find("Re-enter password Field").gameObject;
        signupSubmitButton = signUpPanel.transform.Find("Submit Button").gameObject;
        signupBackButton = signUpPanel.transform.Find("Back Button").gameObject;

        signUpAnimator = signUpPanel.GetComponent<Animator>();

        //Adding the listeners to the respective buttons
        signinSubmitButton.GetComponent<Button>().onClick.AddListener(SignInSubmit);
        signinSignUpButton.GetComponent<Button>().onClick.AddListener(SignInSignUp);

        signupSubmitButton.GetComponent<Button>().onClick.AddListener(SignUpSubmit);
        signupBackButton.GetComponent<Button>().onClick.AddListener(SignUpBack);

        //initializing the error panel
        errorPanel = loginCanvas.transform.Find("Error Panel").gameObject;
        errorPanel.SetActive(false);
    }

    void SignInSubmit()
    {
        Debug.Log("clicked signin submit");
        //get the data in the email and password fields
        emailSignIn = signinEmailInputField.GetComponent<InputField>().text;
        passwordSignIn = signinPasswordField.GetComponent<InputField>().text;
        Debug.Log("email is: " + emailSignIn + " password: " + passwordSignIn);

        //passing on the valeus to the UserManager
        userManager.PerformSignIn(emailSignIn, passwordSignIn);

    }
    void SignInSignUp()
    {
        Debug.Log("clicked signup");
        signInAnimator.SetBool("In", false);
        signUpAnimator.SetBool("In", true);
    }

    void SignUpSubmit()
    {
        Debug.Log("clicked signup submit");

        emailSignUp = signupEmailInputField.GetComponent<InputField>().text;
        displayName = signupUsername.GetComponent<InputField>().text;
        passwordSignUp = signupPasswordField.GetComponent<InputField>().text;
        passwordReenterSignUp = reenterPasswordField.GetComponent<InputField>().text;

        if(passwordReenterSignUp == passwordSignUp)
        {
            userManager.PerformSignUp(emailSignUp, passwordSignUp, displayName);
        }
        else
        {
            if(errorCoroutine!=null)
                StopCoroutine(errorCoroutine);
            errorCoroutine = StartCoroutine(DisplayError("Check the password entered again"));
        }
    }
    void SignUpBack()
    {
        Debug.Log("clicked back");
        signInAnimator.SetBool("In", true);
        signUpAnimator.SetBool("In", false);
    }

    IEnumerator DisplayError(string errorMessage)
    {
        if(errorMessage == "")
        {
            errorMessage = "Unkown error has occurred";
        }
        errorPanel.SetActive(true);
        errorPanel.transform.GetChild(0).GetComponent<Text>().text = errorMessage;
        yield return new WaitForSeconds(3);

        errorPanel.SetActive(false);
    }

    //private void OnGUI()
    //{
    //    GUIStyle guistyle = new GUIStyle();
    //    
    //    GUI.skin.label.fontSize = 25;
    //    //guistyle.fontSize = 25;
    //    //guistyle.normal.textColor = Color.black;
    //    GUI.Label(new Rect(50, 50, 400, 50), "sign up");
    //    emailSignUp = GUI.TextField(new Rect(50, 100, 400, 50), emailSignUp, 50);
    //    displayName = GUI.TextField(new Rect(50, 170, 400, 50), displayName, 50);
    //    passwordSignUp = GUI.PasswordField(new Rect(50, 250, 400, 50), passwordSignUp, '*', 20);
    //
    //    GUI.Label(new Rect(550, 50, 400, 50), "sign in");
    //    emailSignIn = GUI.TextField(new Rect(550, 100, 400, 50), emailSignIn, 50);
    //    passwordSignIn = GUI.PasswordField(new Rect(550, 170, 400, 50), passwordSignIn, '*', 20);
    //
    //    if (GUI.Button(new Rect(50, 300, 100, 50), "Sign Up"))
    //    {
    //        UserManager.PerformSignUp(emailSignUp, passwordSignUp, displayName);
    //    }
    //    if (GUI.Button(new Rect(550, 250, 100, 50), "Sign In"))
    //    {
    //        UserManager.PerformSignIn(emailSignIn, passwordSignIn);
    //        //StartCoroutine(StartTheGame());
    //    }
    //}

    public static void StartTheGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1, LoadSceneMode.Single);
    }
}
