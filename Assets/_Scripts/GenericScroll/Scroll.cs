﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NanoVRController;

[System.Serializable]
[RequireComponent(typeof(JoyStickReceiver))]
public class Scroll : MonoBehaviour
{
    //REQUIRED: some type of transparent shaders(not standard), Unlit/UnlitAlphaWithFade recommended

    List<Transform> objects;
    List<Transform> toAdd = new List<Transform>();

    Transform scrollBar;
    Vector3 toPos;
    JoyStickReceiver jsReceiver;

    public Transform objectParent;
    [SerializeField]
    public Material scrollerMaterial;
    [SerializeField]
    Vector3 margin;
    [SerializeField]
    Vector3 padding;

    [SerializeField]
    int fixedRowOrCol;
    [SerializeField]
    int numberOfVisibleThings;

    [SerializeField]
    float movementSpeed = 0.3f;
    [SerializeField]
    float fadeSpeed = 0.15f;

    public enum orientation { VERTICAL, HORIZONTAL }
    public enum placement { RIGHT, BOTTOM, LEFT, TOP }
    public enum direction { UP, DOWN, LEFT, RIGHT }

    [SerializeField]
    orientation currOrientation = orientation.VERTICAL;
    [SerializeField]
    placement scrollBarPlacement = placement.RIGHT;
    public direction currDirection = direction.UP;

    int numPages;
    int lowestVisIndex = 0;
    int highestVisIndex;
    int atIndexAdd;

    const float transparent = 0;
    const float opaque = 1;

    bool moving = false;
    bool fading = false;
    bool adding = false;
    bool setup = false;

    void Awake()
    {
        if (setup) return;
        SetUpMenu();
    }

    void Start()
    {
        if (transform.localEulerAngles != Vector3.zero)
        {
            Debug.LogError("Local rotation of object with Scroll script needs to be zero");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        if (objectParent.localScale != Vector3.one || objectParent.localEulerAngles != Vector3.zero)
        {
            Debug.LogError("Local scale and local rotation of Object Parent needs to be one and zero");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        jsReceiver = GetComponent<JoyStickReceiver>();
        if (jsReceiver != null) jsReceiver.JoyStickTouched += Scroll;
    }

    void Scroll(VRController c, ControllerComponentArgs e)
    {
        if (e.x == 0 && e.y == 0) return;

        switch (CalculateJoystickAngle(e))
        {
            case 0:
                if (currOrientation != orientation.HORIZONTAL) return;
                currDirection = direction.LEFT;
                break;
            case 90:
                if (currOrientation != orientation.VERTICAL) return;
                currDirection = direction.DOWN;
                break;
            case 180:
                if (currOrientation != orientation.HORIZONTAL) return;
                currDirection = direction.RIGHT;
                break;
            case 270:
                if (currOrientation != orientation.VERTICAL) return;
                currDirection = direction.UP;
                break;
        }

        TryMoveObjects();
    }

    int CalculateJoystickAngle(ControllerComponentArgs e)
    {
        float roundBy = 90;
        float temp = Mathf.Atan2(e.y, e.x);             //calculate angle in rad
        temp = temp * (180 / Mathf.PI);                 //rad to degree
        temp = Mathf.Round(temp / roundBy) * roundBy;   //round to nearest set degree 
        temp = (temp + 360) % 360;                      //positive
        return (int)temp;
    }

    public orientation GetOrientation()
    {
        return currOrientation;
    }

    public placement GetScrollBarPlacement()
    {
        return scrollBarPlacement;
    }

    public int GetLowestVisibleIndex()
    {
        return lowestVisIndex;
    }

    public int GetHighestVisibleIndex()
    {
        return highestVisIndex;
    }

    public void SetUpMenu()
    {
        if (setup) return;

        if (scrollBar == null) CreateScrollBar();

        Vector3 startPos = transform.localPosition;

        float xPos = startPos.x - (transform.localScale.x / 2) + margin.x;
        float yPos = startPos.y + (transform.localScale.y / 2) - margin.y;
        float zPos = startPos.z - (transform.localScale.z / 2) - margin.z;

        if (fixedRowOrCol == 1)
        {
            xPos = (currOrientation == orientation.VERTICAL) ? startPos.x : xPos;
            yPos = (currOrientation == orientation.VERTICAL) ? yPos : startPos.y;
        }

        startPos = new Vector3(xPos, yPos, zPos);
        objectParent.localPosition = startPos;
        setup = true;
    }

    void CreateScrollBar()
    {
        scrollBar = new GameObject().transform;
        scrollBar.name = "ScrollBar";
        scrollBar.SetParent(transform.parent);
        scrollBar.localScale = Vector3.one;
        scrollBar.localPosition = Vector3.zero;
        scrollBar.localEulerAngles = Vector3.zero;
        scrollBar.gameObject.AddComponent<ScrollBar>();
        scrollBar.GetComponent<ScrollBar>().moveSpeed = movementSpeed;
        scrollBar.GetComponent<ScrollBar>().initializeScrollBar();
    }

    public void InitializeObjects(List<Transform> objectList)
    {
        objects = objectList;

        SetNumPagesAndHighestVisibleIndex();

        for (int ind = 0; ind < objects.Count; ind++)
        {
            PlaceObject(objects[ind], ind, false, 0);
        }
    }

    void ExecuteAdd()
    {
        int temp = atIndexAdd;

        foreach (Transform o in toAdd)
        {
            objects.Insert(temp, o);
            ++temp;
        }

        SetNumPagesAndHighestVisibleIndex();

        temp = atIndexAdd;
        for (temp = atIndexAdd; temp < objects.Count; temp++)
        {
            PlaceObject(objects[temp], temp, false, 0);
            objects[temp].localScale = Vector3.one;
        }

        toAdd.Clear();
        adding = false;
    }

    void SetNumPagesAndHighestVisibleIndex()
    {
        numPages = (objects.Count <= numberOfVisibleThings) ? 1 :
            1 + (int)System.Math.Ceiling((double)(objects.Count - numberOfVisibleThings) / fixedRowOrCol);
        scrollBar.GetComponent<ScrollBar>().setNumPages(numPages);

        if (scrollBar.GetComponent<ScrollBar>().getCurrPage() == numPages)
            highestVisIndex = objects.Count - 1;

        if (scrollBar.GetComponent<ScrollBar>().getCurrPage() == 1 && numPages > 1)
            highestVisIndex = numberOfVisibleThings - 1;
    }

    public int GetScrollObjectCount()
    {
        if (objects == null) objects = new List<Transform>();
        return objects.Count;
    }

    public void AddToScroll(List<Transform> objs, Transform obj, int atIndex)
    {
        if (objects == null) objects = new List<Transform>();

        if (atIndex < 0 || atIndex > objects.Count)
        {
            Debug.Log("<color=red>INDEX " + atIndex + " IS OUT OF RANGE OF SCROLL OBJECTS</color>");
            return;
        }

        atIndexAdd = atIndex;

        if (objs != null && objs.Count > 0)
        {
            foreach (Transform o in objs)
            {
                o.SetParent(objectParent);
                o.localScale = Vector3.zero;
                toAdd.Add(o);
            }
        }
        else
        {
            obj.SetParent(objectParent);
            obj.localScale = Vector3.zero;
            toAdd.Add(obj);
        }

        adding = true;
    }

    public void AddObject(Transform newObj)
    {
        adding = true;

        if (newObj.localScale != Vector3.one)
        {
            Debug.Log("<color=red>MAKE THE SCALE OF THE TOP HIERARCHY OF THE SCROLL OBECT VECTOR3.ONE TO AVOID SCALING ISSUES</color>");
        }

        newObj.localScale = Vector3.zero;
        toAdd.Add(newObj);
    }

    public int GetIndex(Transform obj)
    {
        if (objects == null) objects = new List<Transform>();

        if (objects.Count == 0)
        {
            return 0;
        }
        else
        {
            return objects.IndexOf(obj);
        }
    }

    public Transform GetObj(int ind)
    {
        return objects[ind];
    }

    void PlaceObject(Transform obj, int ind, bool deleting, float delayTime)
    {
        obj.transform.localEulerAngles = Vector3.zero;

        Vector3 newPos = CalculateNewPos(ind, deleting);

        if (deleting)
        {
            StartCoroutine(MoveTo(obj, obj.localPosition, newPos, movementSpeed));
        }
        else
        {
            obj.localPosition = newPos;
        }

        if (ind < lowestVisIndex || ind > highestVisIndex)
        {
            obj.gameObject.SetActive(false);
        }
        else
        {
            if (deleting && !obj.gameObject.activeSelf) FadeButton(obj, true, delayTime);
        }
    }

    Vector3 CalculateNewPos(int ind, bool deleting)
    {
        float offset = (float)System.Math.Floor((double)ind / fixedRowOrCol);

        float xPos = (currOrientation == orientation.VERTICAL) ?
                      padding.x * (ind % fixedRowOrCol) : padding.x * offset;
        float yPos = (currOrientation == orientation.VERTICAL) ?
                      -padding.y * offset : -padding.y * (ind % fixedRowOrCol);

        float xOffset = (highestVisIndex == objects.Count - 1 && deleting) ?
                         xPos - (padding.x * (numPages - 1)) : xPos + objects[0].localPosition.x;
        float yOffset = (highestVisIndex == objects.Count - 1 && deleting) ?
                         yPos + (padding.y * (numPages - 1)) : yPos + objects[0].localPosition.y;

        Vector3 offsetToFirst = (currOrientation == orientation.VERTICAL) ?
                                 new Vector3(xPos, yOffset, objectParent.localPosition.z) :
                                 new Vector3(xOffset, yPos, objectParent.localPosition.z);

        return (lowestVisIndex == 0) ?
                new Vector3(xPos, yPos, objectParent.localPosition.z) : offsetToFirst;
    }

    public void Clear()
    {
        if (objects != null)
        {
            DeleteObjects(objects);
        }
    }

    public void DeleteObjects(List<Transform> objs)
    {
        bool removingLastObject = false;

        List<int> indecesToDelete = GetIndeces(objs);
        if (indecesToDelete[indecesToDelete.Count-1] == objects.Count-1) removingLastObject = true;

        indecesToDelete.Reverse(); //delete from end of list to beginning so that indeces don't get messed up

        for (int i = 0; i < indecesToDelete.Count; i++)
        {
            Transform d = objects[indecesToDelete[i]];
            objects.Remove(d);
            Destroy(d.gameObject);
        }

        ReassignVisibleIndeces();

        float scale = 0.04f;
        float baseTime = 0.05f;
        float delayTime = (removingLastObject) ? baseTime*7: baseTime;

        for (int i = 0; i < objects.Count; i++)
        {
            if (i >= lowestVisIndex && i <= highestVisIndex)
            {
                delayTime = (removingLastObject) ? 
                            delayTime : delayTime + scale;
            }

            PlaceObject(objects[i], i, true, delayTime);
        }
    }

    List<int> GetIndeces(List<Transform> objs)
    {
        List<int> indeces = new List<int>();

        for (int i = 0; i < objects.Count; i++)
        {
            Transform o = objects[i];
            foreach (Transform obj in objs)
            {
                if (o == obj)
                {
                    indeces.Add(i);
                    break;
                }
            }
        }

        return indeces;
    }

    void ReassignVisibleIndeces()
    {
        int prevNum = numPages;
        numPages = (objects.Count <= numberOfVisibleThings) ? 1 :
                    1 + (int)System.Math.Ceiling((double)(objects.Count - numberOfVisibleThings) / fixedRowOrCol);
        scrollBar.GetComponent<ScrollBar>().setNumPages(numPages);

        if (highestVisIndex > objects.Count - 1) //if now on the last page
        {
            highestVisIndex = objects.Count - 1;

            if (highestVisIndex - lowestVisIndex + 1 <= numberOfVisibleThings - fixedRowOrCol)
            {
                int offsetBy = (numberOfVisibleThings / fixedRowOrCol) -
                               (int)Mathf.Ceil(((float)(highestVisIndex - lowestVisIndex + 1) / (float)fixedRowOrCol));
                lowestVisIndex = lowestVisIndex - (fixedRowOrCol * offsetBy);
            }

            if (numPages == 1) lowestVisIndex = 0;
        }
    }

    void TryMoveObjects()
    {
        if (objects == null || objects.Count == 0) return;

        if (((currDirection == direction.DOWN || currDirection == direction.RIGHT) && lowestVisIndex == 0) ||
            ((currDirection == direction.UP || currDirection == direction.LEFT) && highestVisIndex == objects.Count - 1))
            return;

        if (!moving && !fading)
        {
            MoveObjects();
        }
    }

    void MoveObjects()
    {
        moving = true;
        fading = true;

        for (int i = 0; i < objects.Count; i++)
        {
            Transform obj = objects[i];
            Vector3 newPos = CalculateMovedPos(obj);

            if (i == 0) toPos = newPos;

            StartCoroutine(MoveTo(obj, obj.localPosition, newPos, movementSpeed));
            TryFadeObject(obj, i);
        }

        scrollBar.GetComponent<ScrollBar>().moveScroller(currDirection);
        SetLowAndHighIndeces();
    }

    Vector3 CalculateMovedPos(Transform obj)
    {
        float newX = (currDirection == direction.RIGHT) ?
                          (float)System.Math.Round((double)obj.localPosition.x + padding.x, 2) :
                          (float)System.Math.Round((double)obj.localPosition.x - padding.x, 2);

        float newY = (currDirection == direction.UP) ?
                     (float)System.Math.Round((double)obj.localPosition.y + padding.y, 2) :
                     (float)System.Math.Round((double)obj.localPosition.y - padding.y, 2);

        return (currOrientation == orientation.VERTICAL) ?
                new Vector3(obj.localPosition.x, newY, obj.localPosition.z) :
                new Vector3(newX, obj.localPosition.y, obj.localPosition.z);
    }

    void TryFadeObject(Transform obj, int i)
    {
        bool fadeIn = true;
        bool fadeOut = false;
        float fadeInDelay = 0.5f;

        if (currDirection == direction.UP || currDirection == direction.LEFT)
        {
            if (i >= lowestVisIndex && i < lowestVisIndex + fixedRowOrCol)
            {
                FadeButton(obj, fadeOut, 0);
            }
            else if (i > highestVisIndex && i <= highestVisIndex + fixedRowOrCol)
            {
                FadeButton(obj, fadeIn, fadeInDelay);
            }
        }
        else if (currDirection == direction.DOWN || currDirection == direction.RIGHT)
        {
            if (i <= highestVisIndex && i > highestVisIndex - fixedRowOrCol)
            {
                if (((highestVisIndex + 1) % fixedRowOrCol == 0) ||
                    (i > (highestVisIndex - ((highestVisIndex + 1) % fixedRowOrCol))))
                    FadeButton(obj, fadeOut, 0);
            }
            else if (i < lowestVisIndex && i >= lowestVisIndex - fixedRowOrCol)
            {
                FadeButton(obj, fadeIn, fadeInDelay);
            }
        }
    }

    void SetLowAndHighIndeces()
    {
        if (currDirection == direction.UP || currDirection == direction.LEFT)
        {
            lowestVisIndex += fixedRowOrCol;
            highestVisIndex += fixedRowOrCol;

            if (highestVisIndex > objects.Count - 1) highestVisIndex = objects.Count - 1;
        }
        else
        {
            lowestVisIndex -= fixedRowOrCol;
            highestVisIndex -= fixedRowOrCol;

            if (highestVisIndex - lowestVisIndex != numberOfVisibleThings - 1)
                highestVisIndex = lowestVisIndex + numberOfVisibleThings - 1;
        }
    }

    void FadeButton(Transform obj, bool fadeIn, float delayTime)
    {
        if (fadeIn) StartCoroutine(DelayFadeIn(obj, delayTime));

        float to = (fadeIn) ? opaque : transparent;
        Renderer[] childrenRenderer = obj.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in childrenRenderer)
        {
            float from = (r.gameObject.GetComponent<TMPro.TextMeshPro>()) ?
                          r.material.GetColor("_FaceColor").a :
                          r.material.GetColor("_Color").a;
            StartCoroutine(FadeObj(r, obj, from, to));
        }
    }

    #region Coroutines
    IEnumerator DelayFadeIn(Transform obj, float delayTime)
    {
        yield return new WaitForSecondsRealtime(delayTime);
        obj.gameObject.SetActive(true);
    }

    IEnumerator FadeObj(Renderer rend, Transform rootParent, float start, float end)
    {
        Material mat = rend.material;
        string colorName = "_Color";
        Color col = Color.white;

        if (mat.HasProperty("_FaceColor")) colorName = "_FaceColor";
        if (mat.HasProperty(colorName)) col = mat.GetColor(colorName);

        for (float i = 0.0f; i < 1.0f; i += Time.deltaTime / fadeSpeed)
        {
            col = mat.GetColor(colorName);
            col.a = Mathf.Lerp(start, end, i);
            rend.material.SetColor(colorName, col);
            yield return null;
        }

        col.a = end;
        rend.GetComponent<Renderer>().material.SetColor(colorName, col);
        fading = false;

        if (end == transparent) rootParent.gameObject.SetActive(false);
    }

    IEnumerator MoveTo(Transform obj, Vector3 start, Vector3 end, float overTime)
    {
        float startTime = Time.time;

        while (Time.time < startTime + overTime)
        {
            if (obj == null) break;
            obj.localPosition = Vector3.Lerp(start, end, (Time.time - startTime) / overTime);
            yield return null;
        }

        if (obj != null) obj.localPosition = end;
    }
    #endregion

    void Update()
    {
        if (moving && objects.Count > 0 && objects[0].localPosition == toPos) moving = false;

        if (adding && !moving) ExecuteAdd();
    }
}