﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Extensions;

public class Expressions : MonoBehaviour
{
    CalculatorManager calcManager;
    
    Scroll paramScroll, vecFieldScroll, constantScroll;
    public static Expressions _instance;
    ExpressionSet selectedExpSet;
    [SerializeField]
    Transform selectedExpression;
    ExpressionBody selectedBody;
    List<Transform> expressions;
    public enum ExpressionType { CONSTANT, PARAMET, VECFIELD }

    //TODO:
    // 1 - speed up scroll
    // 2 - refactor adding expressions
    // 3 - more debugging
    //
    // nice to haves - 
    //  1 - slide variable shortcuts in and out 
    //  2 - enable underscore movement by raycast hit

    //BUGS:
    // * - creating empty game object everytime a letter is pressed
    // 

    //DONE:
    // 1 - flowline object added, works completely with parametric graphs
    // 2 - fixed all bugs with adding expressions

    void Awake()
    {
        _instance = this;
        calcManager = ParametricManager._instance;

        paramScroll = transform.parent.Find("ParametrizationPanel").GetComponentInChildren<Scroll>();
        vecFieldScroll = transform.parent.Find("VectorFieldPanel").GetComponentInChildren<Scroll>();
        constantScroll = transform.parent.Find("ConstantPanel").GetComponentInChildren<Scroll>();

        expressions = new List<Transform>();
    }

    public void setManager(CalculatorManager cm)
    {
        calcManager = cm;
    }

    public Scroll getScroll(ExpressionType type)
    {
        switch (type)
        {
            case ExpressionType.PARAMET:
                return paramScroll;
            case ExpressionType.VECFIELD:
                return vecFieldScroll;
            case ExpressionType.CONSTANT:
                return constantScroll;
        }

        print("<color=red>GET SCROLL RETURNED NULL</color>");
        return null;
    }

    public void addExpr(Transform exp)
    {
        expressions.Add(exp);
        exp.SetParent(transform);
    }

    public Transform getSelectedExpr()
    {
        return selectedExpression;
    }

    public ExpressionBody getSelectedBody()
    {
        return selectedBody;
    }

    public ExpressionSet getSelectedExprSet()
    {
        return selectedExpSet;
    }

    public void setSelectedExpr(Transform expr, ExpressionBody body)
    {
        if (expr == null)
        {
            selectedExpression = null;
            selectedBody = null;
            selectedExpSet = null;
            return;
        }

        selectedExpression = expr;
        selectedBody = body;
    }

    public bool selectedNotNull()
    {
        return (selectedExpression != null);
    }

    public void deleteExpression(Transform del)
    {
        calcManager.RemoveExpressionSet(del.gameObject.GetInterface<ExpressionTabInterface>().getExpSet());
        del.gameObject.GetInterface<ExpressionTabInterface>().deleteExpressionFromScroll();
        expressions.Remove(del);
    }

    void Update() { }
}
