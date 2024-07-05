using System;
using System.Collections;
using System.Collections.Generic;
using EzySlice;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEngine.GraphicsBuffer;

public class KnifeScript : MonoBehaviour
{
    //[TabGroup("GamePlay")]
    public GameObject cam;
    public float camSens = 1f;
    public GameObject woman;

    GameObject currentPanel;
    public GameObject startPanel;
    public GameObject finishPanel;
    public GameObject failPanel;
    public GameObject pausePanel;
    public GameObject defPanel;
    public GameObject hitCounter;
    public GameObject LevelTX;

    public GameObject knifeButton;
    public GameObject homeButton;
    public GameObject starButton;

    public GameObject sliceParicle0;
    public GameObject sliceParicle1;
    public GameObject sliceParicle2;
    public int sliceParticleSelector = 0;

    public int hitCount = 0;
     
    // Knife Movement
    public float yMovement = 5.0f;
    float yMovementTemp = 1f;
    public float zMovement = 5.0f;

    public float clickRotationSens = 5f;
    public float rotationSens = 1;
    public ForceMode rotationForceMode;

    public Rigidbody knifeMovement;

    public bool onFloor = false;
    public bool isTurning = false;
    public bool hit = false;

    private Vector3 cameraOffs;

    private Vector3 tempParentPos;

    bool clicked = false;
    bool started = false;
    bool failed = false;
    bool finished = false;
    bool isPen = false;
    bool womanSad = false;
    bool womanSmile = false;
    bool isSlicing = false;
    bool clickedButton = false;

    float knifeAngle = 0;
    float value = 0.5f;

    public LayerMask UILayerMask;

    //Knife Slice
    public Material[] materialsSlicedSide;
    Material materialSlicedSide;

    // Start is called before the first frame update
    void Start()
    {
        started = false;
        failed = false;
        finished = false;

        cameraOffs = transform.position - cam.transform.position;
        
        defPanel.SetActive(true);
        startPanel.SetActive(true);
        LevelTX.GetComponent<Text>().text = SceneManager.GetActiveScene().name;
        currentPanel = startPanel;

        knifeMovement.useGravity = false;
        knifeMovement.isKinematic = false;
        yMovementTemp = yMovement;

        if (woman != null)
        {
            StartCoroutine(Blink());
            SetWomanStart();
        }

        hitCount = PlayerPrefs.GetInt("hitCount", 0);
        hitCounter.GetComponent<Text>().text = hitCount.ToString();

        
    }
    /*public void OnPointerClick(PointerEventData eventData)
    {
        bool clickedButton = true;
    }*/
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, UILayerMask))
                {
                    Debug.Log("Clicked on " + hit.collider.gameObject.name);
                }
                else
                {
                    clickedButton = true;
                }
            }

            if (!clickedButton)
            {
                StartCoroutine(Clicked());
            }

            clickedButton = false;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }

    }
    void FixedUpdate()
    {
        //Camera Movement
        cam.transform.position = Vector3.Lerp(cam.transform.position, transform.position - cameraOffs, camSens);

        if (!clicked && !onFloor && !hit && !isSlicing)
        {
            RotateTheKnife();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("CanSlice"))
        {
            SliceObject(other.gameObject);
        }
        else if (other.gameObject.CompareTag("Floor") && !onFloor)
        {
            FindObjectOfType<AudioManager>().Play("StuckonFloor");

            knifeMovement.useGravity = false;
            knifeMovement.angularVelocity = Vector3.zero;
            knifeMovement.velocity = Vector3.zero;
            knifeMovement.isKinematic = true;
            onFloor = true;
            isTurning = false;

            yMovementTemp = yMovement;
            yMovement = yMovement * 1.5f;
        }
        else if (other.gameObject.CompareTag("Finish"))
        {
            Finish();
        }
        else if (other.gameObject.CompareTag("Obs"))
        {
            FindObjectOfType<AudioManager>().Play("Hit");
            GameOver();
        }
        else if (other.gameObject.CompareTag("woman"))
        {
            HitToWoman();
        }
        else if (other.gameObject.CompareTag("PrizeTarget"))
        {
            SliceObject(other.gameObject);
            SliceThePrize();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        StartCoroutine(Hit());

        if (collision.gameObject.CompareTag("CanSlice"))
        {
            knifeMovement.angularVelocity = Vector3.zero;
            knifeMovement.AddTorque(transform.up * clickRotationSens, ForceMode.Impulse);
            FindObjectOfType<AudioManager>().Play("Hit");
            //knifeMovement.AddForce(0, yMovement/5, -zMovement/5, ForceMode.Impulse);
        }
        else if (collision.gameObject.CompareTag("Finish"))
        {
            Finish();
        }

    }
    IEnumerator Clicked()
    {
        FindObjectOfType<AudioManager>().Play("Swipe");

        clicked = true;

        if (!started)
        {
            TapToFlip();
        }
        else if (failed)
        {
            Restart();
        }
        else if (finished)
        {
            NextLevel();
        }


        //Set the knife to not holding possition
        knifeMovement.isKinematic = false;
        knifeMovement.useGravity = true;

        //Knife Movement each tap
        knifeMovement.velocity = Vector3.zero;
        knifeMovement.AddForce(0, yMovement, zMovement, ForceMode.Impulse);
        yMovement = yMovementTemp;

        //Rotate Knife each tap
        knifeMovement.AddTorque(-transform.up * clickRotationSens, ForceMode.Impulse);

        if (onFloor)
        {
            StartCoroutine(SetNotOnFloor());
        }

        yield return new WaitForSeconds(0.2f);
        clicked = false;
    }

    void NextLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    void RotateTheKnife()
    {
        knifeAngle = transform.rotation.eulerAngles.x % 360;

        if (knifeAngle >= 0f && knifeAngle < 90f) //Between angles 0 - 90
        {
            value = Mathf.InverseLerp(-90f, 90f, knifeAngle);
        }
        else if (knifeAngle >= 90f && knifeAngle < 270f) //Between angles 90 - 270
        {
            value = Mathf.InverseLerp(270f, 90f, knifeAngle);
        }
        else if (knifeAngle >= 270f && knifeAngle <= 360f) //Between angles 270 - 0
        {
            value = Mathf.InverseLerp(270f, 450f, knifeAngle);
        }

        knifeMovement.AddTorque(-transform.up * rotationSens * value, rotationForceMode);

    }    

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GameOver()
    {
        knifeMovement.isKinematic = true;
        failed = true;
        currentPanel.SetActive(false);
        failPanel.SetActive(true);
        currentPanel = failPanel;

        print("Failed! :/");
    }

    public void Finish()
    {
        knifeMovement.isKinematic = true;
        finished = true;
        currentPanel.SetActive(false);
        finishPanel.SetActive(true);
        currentPanel = finishPanel;

        print("Finished.");
    }

    public void KnifeButton()
    {
        knifeButton.transform.GetChild(0).GetComponent<Animator>().SetTrigger("KnifeTrig");
    }
    public void HomeButton()
    {
        homeButton.transform.GetChild(0).GetComponent<Animator>().SetTrigger("HomeTrig");
    }
    public void StarButton()
    {
        starButton.transform.GetChild(0).GetComponent<Animator>().SetTrigger("StarTrig");
    }


    public void SliceThePrize()
    {
        if (!womanSad)
        {
            womanSmile = true;
            ShineWoman();
            StartCoroutine(SmileWoman());

            print("Slice the prize.");
        }
    }

    public void TapToFlip()
    {
        Time.timeScale = 1;
        started = true;
        currentPanel.SetActive(false);
        defPanel.SetActive(true);
        currentPanel = defPanel;

        print("Start.");
    }
    void HitToWoman()
    {
        womanSad = true;
        womanSmile = false;
        CryWoman();
        StartCoroutine(SadWoman());
        print("Hit to woman! :(");
    }
    void SetWomanStart()
    {
        woman.transform.parent.GetChild(2).GetChild(1).gameObject.SetActive(false);
        woman.transform.parent.GetChild(3).GetChild(1).gameObject.SetActive(false);

        woman.transform.parent.GetChild(2).GetChild(0).gameObject.SetActive(false);
        woman.transform.parent.GetChild(3).GetChild(0).gameObject.SetActive(false);

        womanSad = false;
        womanSmile = false;

        woman.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(10, 0f);
        woman.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(11, 0f);
    }
    public void CryWoman()
    {
        woman.transform.parent.GetChild(2).GetChild(0).gameObject.SetActive(true);
        woman.transform.parent.GetChild(3).GetChild(0).gameObject.SetActive(true);

        woman.transform.parent.GetChild(2).GetChild(1).gameObject.SetActive(false);
        woman.transform.parent.GetChild(3).GetChild(1).gameObject.SetActive(false);
    }

    public void ShineWoman()
    {
        woman.transform.parent.GetChild(2).GetChild(1).gameObject.SetActive(true);
        woman.transform.parent.GetChild(3).GetChild(1).gameObject.SetActive(true);
    }

    IEnumerator SetSlicing()
    {
        isSlicing = true;
        yield return new WaitForSeconds(0.15f);
        isSlicing = false;
    }

    IEnumerator SetNotOnFloor()
    {
        yield return new WaitForSeconds(0.5f);
        onFloor = false;
    }

    IEnumerator Hit()
    {
        knifeMovement.angularVelocity = Vector3.zero;
        hit = true;
        yield return new WaitForSeconds(0.4f);
        hit = false;
    }
    IEnumerator Blink()
    {
        for(int i=0; i<100; i++)
        {
            woman.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(12, i);
            yield return new WaitForSeconds(0.004f);
        }
        for (int i = 100; i > 0; i--)
        {
            woman.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(12, i);
            yield return new WaitForSeconds(0.004f);
        }
        yield return new WaitForSeconds(UnityEngine.Random.Range(0,15) * 0.1f);
        StartCoroutine(Blink());
    }
    IEnumerator SadWoman()
    {
        for (int i = 0; i < 100; i++)
        {
            woman.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(11, i);
            yield return new WaitForSeconds(0.004f);
        }
    }
    IEnumerator SmileWoman()
    {
        for (int i = 0; i < 100; i++)
        {
            woman.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(10, i);
            yield return new WaitForSeconds(0.004f);
        }
    }

    void SliceObject(GameObject other)
    {
        other.GetComponent<Collider>().enabled = false;
        StartCoroutine(SetSlicing());

        
        FindObjectOfType<AudioManager>().Play("Slice");

        if (other.gameObject.CompareTag("PrizeTarget"))

            hitCount = other.gameObject.CompareTag("PrizeTarget") ? hitCount + 10 : hitCount++;

        PlayerPrefs.SetInt("hitCount", hitCount);
        hitCounter.GetComponent<Text>().text = hitCount.ToString();

        isPen = other.transform.parent.name == "circle" ? true : false;

        if (other.transform.parent.name == "LegoS")
        {
            materialSlicedSide = other.GetComponent<MeshRenderer>().material;
        }
        else
        {
            int rnd = UnityEngine.Random.Range(0, materialsSlicedSide.Length);
            materialSlicedSide = materialsSlicedSide[rnd];
        }

        SlicedHull sliceobj = Slice(other, materialSlicedSide);
        GameObject SlicedObjLeft = sliceobj.CreateUpperHull(other, materialSlicedSide);
        GameObject SlicedObjRight = sliceobj.CreateLowerHull(other, materialSlicedSide);

        if(sliceParticleSelector == 0)
        {
            Instantiate(sliceParicle0, other.transform.position, Quaternion.Euler(-90, -180, 180));
        }
        else if (sliceParticleSelector == 1)
        {
            Instantiate(sliceParicle1, other.transform.position, Quaternion.Euler(-90, -180, 180));
        }
        else if (sliceParticleSelector == 2)
        {
            Instantiate(sliceParicle2, other.transform.position, Quaternion.Euler(-90, -180, 180));
        }

        Destroy(other);

        if (isPen)
        {
            SlicedObjLeft.transform.rotation = Quaternion.Euler(-180, 90, 0);
            SlicedObjRight.transform.rotation = Quaternion.Euler(-180, 90, 0);
        }

        //SlicedObjLeft.transform.position += tempParentPos;
        SlicedObjLeft.AddComponent<CapsuleCollider>();
        SlicedObjLeft.GetComponent<CapsuleCollider>().isTrigger = true;


        //SlicedObjRight.transform.position += tempParentPos;
        SlicedObjRight.AddComponent<CapsuleCollider>();
        SlicedObjRight.GetComponent<CapsuleCollider>().isTrigger = true;

        SlicedObjLeft.AddComponent<Rigidbody>();
        SlicedObjRight.AddComponent<Rigidbody>();


        SlicedObjLeft.GetComponent<Rigidbody>().AddForce(-2f, 2f, 0, ForceMode.Impulse);
        SlicedObjRight.GetComponent<Rigidbody>().AddForce(2f, 2f, 0, ForceMode.Impulse);

        SlicedObjLeft.GetComponent<Rigidbody>().AddTorque(-transform.right * clickRotationSens * 4f, ForceMode.Impulse);
        SlicedObjRight.GetComponent<Rigidbody>().AddTorque(transform.right * clickRotationSens * 4f, ForceMode.Impulse);
    }
    private SlicedHull Slice(GameObject obj, Material mat)
    {
        return obj.Slice(transform.position, transform.up, mat);
    }
}
