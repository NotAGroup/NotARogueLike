using System;
using System.Collections.Generic;
using UnityEngine;
public abstract class Node
{
    private List<Node> childrenNodeList;

    public List<Node> ChildrenNodeList { get => childrenNodeList;}

    public bool Visted { get; set; }
    public Vector2Int BottomLeftAreaCorner { get; set; }
    public Vector2Int BottomRightAreaCorner { get; set; }
    public Vector2Int TopRightAreaCorner { get; set; }
    public Vector2Int TopLeftAreaCorner { get; set; }
    public String Type { get; set; }
    public String name;

    public Node Parent { get; set; }
    public int TreeLayerIndex { get; set; }

    public List<NavPoint> navPointList { get; set; }

    public Node(Node parentNode)
    {
        childrenNodeList = new List<Node>();
        navPointList = new List<NavPoint>();
        this.Parent = parentNode;
        if (parentNode != null)
        {
            parentNode.AddChild(this);
        }
    }

    public List<NavPoint> GetCorners()
    {
        List<NavPoint> corners = new List<NavPoint>();

        foreach (var navPoint in navPointList)
        {
            if (navPoint.type == "corner")
            {
                corners.Add(navPoint);
            }
        }

        return corners;
    }

    public List<NavPoint> GetCorridorOpenings()
    {
        List<NavPoint> corridorOpenings = new List<NavPoint>();

        foreach (var navPoint in navPointList)
        {
            if (navPoint.type == "opening")
            {
                corridorOpenings.Add(navPoint);
            }
        }

        return corridorOpenings;
    }

    public void AddChild(Node node)
    {
        childrenNodeList.Add(node);

    }

    public void RemoveChild(Node node)
    {
        childrenNodeList.Remove(node);
    }
}