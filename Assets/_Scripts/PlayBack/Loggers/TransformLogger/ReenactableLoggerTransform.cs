﻿using System.Collections;
using System.Collections.Generic;
using Extensions;
using UnityEngine;
public class ReenactableLoggerTransform : ReenactableLogger
{

    public static HashSet<ReenactableLoggerTransform> AllTransformLoggers = new HashSet<ReenactableLoggerTransform>();
    Vector3 lastLocalPos;
    Vector3 lastScale;
    Quaternion lastRotation;
    Transform lastParent;
    Transform[] lastChildren;

    bool added = false;

    long lastTime = -999;

    void Awake()
    {
        lastParent = transform.parent;
        AllTransformLoggers.Add(this);

        // if (Recorder.Recording)
        // {
        //     UnityEngine.Debug.Log("move first rec frame: " + PBT_FrameCounter.GetFrame());

        //     long currTime = PlaybackClock.GetTime(); ;
        //     LogMovement(currTime);
        // }
    }


    void RecordPosition(long currTime)
    {
        if (Recorder.Recording)
        {
            if (Changed())
            {
                LogMovement(currTime);
            }
        }
    }

    void FinalRecordChildren(long currTime)
    {
        if (Recorder.Recording)
        {
            RecordPosition(currTime);
            if (lastChildren != null)
            {
                foreach (Transform child in lastChildren)
                {
                    if (child != null)
                    {
                        child.GetComponent<ReenactableLoggerTransform>().FinalRecordChildren(currTime);
                    }
                }
            }

            foreach (Transform child in transform)
            {
                child.GetComponent<ReenactableLoggerTransform>().FinalRecordChildren(currTime);
            }
            lastChildren = GetChildren();
        }
    }
    public void RecursiveRecordPosition(long currTime)
    {
        if (Recorder.Recording)
        {
            RecordPosition(currTime);
            foreach (Transform child in transform)
            {
                child.GetComponent<ReenactableLoggerTransform>().RecursiveRecordPosition(currTime);
            }
            lastChildren = GetChildren();
        }
    }

    Transform[] GetChildren()
    {
        Transform[] children = new Transform[transform.childCount];
        int i = 0;

        foreach (Transform child in transform)
        {
            children[i] = child;
            i++;
        }
        return children;
    }

    public void LogMovement(long currTime)
    {
        long startTime;
        long endTime;
        long duration;

        GameObject nextParent = (transform.parent == null) ? null : transform.parent.gameObject;
        bool lerp = true;
        bool world = false;

        if (lastTime == -999)
        {
            Debug.Log("<color=orange>" + gameObject.name + " First" + "</color>");

            world = false;
            lerp = false;
        }

        if (ParentChanged())
        {

            string debugNextParent = (transform.parent == null) ? "null" : transform.parent.name;
            Debug.Log("<color=blue>" + gameObject.name + " -> " + debugNextParent + "</color>");

            world = false;
            lerp = false;
        }

        if (lerp)
        {
            startTime = lastTime;
            endTime = currTime;
            duration = endTime - startTime;
        }
        else
        {
            startTime = currTime;
            endTime = currTime;
            duration = 0;
        }

        CreateLogEntry(startTime, duration, nextParent, world);
        UpdateHistory(currTime);
    }

    private void CreateLogEntry(long startTime, long duration, GameObject nextParent, bool world)
    {
        if (world)
        {
            Recorder.RecordAction(PlaybackLogEntry.PlayBackActionFactory.CreateMovement(startTime, duration, gameObject, transform.position,
                                                                                     transform.rotation, transform.lossyScale, nextParent));
        }
        else
        {
            Recorder.RecordAction(PlaybackLogEntry.PlayBackActionFactory.CreateMovement(startTime, duration, gameObject, transform.localPosition,
                                                                                     transform.localRotation, transform.localScale, nextParent));
        }
    }

    private void UpdateHistory(long currTime)
    {
        lastParent = transform.parent;
        lastLocalPos = transform.localPosition;
        lastRotation = transform.localRotation;
        lastScale = transform.localScale;
        lastTime = currTime;
    }

    void OnDisable()
    {
        if (Recorder.Recording)
        {
            if (!gameObject.activeSelf)
            {
                long time = PlaybackClock.GetTime();
                Recorder.RecordAction(PlaybackLogEntry.PlayBackActionFactory.CreateDisable(time, gameObject));
                Debug.Log("logged Disable");
            }
        }
    }

    void OnEnable()
    {

        if (Recorder.Recording)
        {
            if (gameObject.activeSelf)
            {
                long time = PlaybackClock.GetTime();
                Recorder.RecordAction(PlaybackLogEntry.PlayBackActionFactory.CreateEnable(time, gameObject));
            }
        }
    }

    public override void OnDestroy()
    {
        AllTransformLoggers.Remove(this);
        if (Recorder.Recording)
        {
            long currTime = PlaybackClock.GetTime();
            FinalRecordChildren(currTime);


            long time = PlaybackClock.GetTime();
            Debug.Log("<color=purple>" + "Destroying " + gameObject.name + "</color>");


            Recorder.RecordAction(PlaybackLogEntry.PlayBackActionFactory.CreateDestroy(time, gameObject));
        }
        base.OnDestroy();
    }

    bool Changed()
    {
        //return true;
        bool changed = false;

        changed |= lastParent != transform.parent;
        changed |= lastLocalPos != transform.localPosition;
        changed |= lastRotation != transform.localRotation;
        changed |= lastScale != transform.localScale;


        return changed;
    }

    bool ParentChanged()
    {
        return lastParent != transform.parent;
    }
}