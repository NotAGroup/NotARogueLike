using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CorridorNode : Node
{
    private Node structure1;
    private Node structure2;
    private String room1;
    private String room2;
    private int corridorWidth;
    private int modifierDistanceFromWall=2;

    public CorridorNode(Node node1, Node node2, int corridorWidth) : base(null)
    {
        this.structure1 = node1;
        this.structure2 = node2;
        this.corridorWidth = corridorWidth;
        GenerateCorridor();
    }

    private void GenerateCorridor()
    {
        var relativePositionOfStructure2 = StructureHelper.CheckPositionStructure2AgainstStructure1(this.structure1, this.structure2);
        switch (relativePositionOfStructure2)
        {
            case RelativePosition.Up:
                ProcessRoomInRelationUpOrDown(this.structure1, this.structure2);
                break;
            case RelativePosition.Down:
                ProcessRoomInRelationUpOrDown(this.structure2, this.structure1);
                break;
            case RelativePosition.Right:
                ProcessRoomInRelationRightOrLeft(this.structure1, this.structure2);
                break;
            case RelativePosition.Left:
                ProcessRoomInRelationRightOrLeft(this.structure2, this.structure1);
                break;
            default:
                break;
        }
    }

    private void ProcessRoomInRelationRightOrLeft(Node structure1, Node structure2)
    {
        Node leftStructure = null;
        List<Node> leftStructureChildren = StructureHelper.TraverseGraphToExtractLowestLeafes(structure1);
        Node rightStructure = null;
        List<Node> rightStructureChildren = StructureHelper.TraverseGraphToExtractLowestLeafes(structure2);

        var sortedLeftStructure = leftStructureChildren.OrderByDescending(child => child.TopRightAreaCorner.x).ToList();
        if (sortedLeftStructure.Count == 1)
        {
            leftStructure = sortedLeftStructure[0];
        }
        else
        {
            int maxX = sortedLeftStructure[0].TopRightAreaCorner.x;
            sortedLeftStructure = sortedLeftStructure.Where(children => Math.Abs(maxX - children.TopRightAreaCorner.x) < 10).ToList();
            //int index = UnityEngine.Random.Range(0, sortedLeftStructure.Count);
            //leftStructure = sortedLeftStructure[index];
        }

        var sortedRightStructure = rightStructureChildren.OrderBy(child => child.TopRightAreaCorner.x).ToList();
        if (sortedRightStructure.Count == 1)
        {
            leftStructure = sortedRightStructure[0];
        }
        else
        {
            int minX = sortedRightStructure[0].BottomLeftAreaCorner.x;
            sortedRightStructure = sortedRightStructure.Where(children => Math.Abs(minX - children.BottomLeftAreaCorner.x) < 10).ToList();
        }
        float bestScale = 0;
        float scale = 0;
        Node rightResult = null;
        Node leftResult = null;
        foreach(var leftRoom in sortedLeftStructure)
        {
            foreach(var rightRoom in sortedRightStructure)
            {
                int topLeftBottomRight = Math.Abs(leftRoom.TopRightAreaCorner.y - rightRoom.BottomLeftAreaCorner.y);
                int topRightBottomLeft = Math.Abs(rightRoom.TopLeftAreaCorner.y - leftRoom.BottomRightAreaCorner.y);
                if (topLeftBottomRight <= topRightBottomLeft){
                    scale = (float)topLeftBottomRight/topRightBottomLeft;
                } 
                else
                {
                    scale = (float)topRightBottomLeft/topLeftBottomRight;
                }
                if (scale > bestScale){
                    bestScale = scale;
                    rightResult = rightRoom;
                    leftResult = leftRoom;
                }
            }
        }
        leftStructure = leftResult;
        rightStructure = rightResult;
        /*
        var possibleNeighboursInRightStructureList = rightStructureChildren.Where(
            child => GetValidYForNeighourLeftRight(
                leftStructure.TopRightAreaCorner,
                leftStructure.BottomRightAreaCorner,
                child.TopLeftAreaCorner,
                child.BottomLeftAreaCorner
                ) != -1
            ).OrderBy(child => child.BottomRightAreaCorner.x).ToList();

        if (possibleNeighboursInRightStructureList.Count <= 0)
        {
            rightStructure = structure2;
        }
        else
        {
            rightStructure = possibleNeighboursInRightStructureList[0];
        }
        */
        int y = GetValidYForNeighourLeftRight(
            leftStructure.TopLeftAreaCorner, 
            leftStructure.BottomRightAreaCorner,
            rightStructure.TopLeftAreaCorner,
            rightStructure.BottomLeftAreaCorner);
        int iterations = 0;
        while(iterations++ < 100 && y == -1 && sortedLeftStructure.Count > 1)
        {
            sortedLeftStructure = sortedLeftStructure.Where(
                child => child.TopLeftAreaCorner.y != leftStructure.TopLeftAreaCorner.y).ToList();
            leftStructure = sortedLeftStructure[0];
            y = GetValidYForNeighourLeftRight(leftStructure.TopLeftAreaCorner, leftStructure.BottomRightAreaCorner,
            rightStructure.TopLeftAreaCorner,
            rightStructure.BottomLeftAreaCorner);
        }
        room1 = leftStructure.name;
        room2 = rightStructure.name;
        BottomLeftAreaCorner = new Vector2Int(leftStructure.BottomRightAreaCorner.x, y - this.corridorWidth/2);
        TopRightAreaCorner = new Vector2Int(rightStructure.TopLeftAreaCorner.x, y + this.corridorWidth/2);
    }

    private int GetValidYForNeighourLeftRight(Vector2Int leftNodeUp, Vector2Int leftNodeDown, Vector2Int rightNodeUp, Vector2Int rightNodeDown)
    {
        if(rightNodeUp.y >= leftNodeUp.y && leftNodeDown.y >= rightNodeDown.y)
        {
            return StructureHelper.CalculateMiddlePoint(
                leftNodeDown + new Vector2Int(0, modifierDistanceFromWall),
                leftNodeUp - new Vector2Int(0, modifierDistanceFromWall) //+ this.corridorWidth)
                ).y;
        }
        if(rightNodeUp.y <= leftNodeUp.y && leftNodeDown.y <= rightNodeDown.y)
        {
            return StructureHelper.CalculateMiddlePoint(
                rightNodeDown + new Vector2Int(0,modifierDistanceFromWall),
                rightNodeUp - new Vector2Int(0, modifierDistanceFromWall)
                ).y;
        }
        if(leftNodeUp.y <= rightNodeUp.y && leftNodeUp.y >= rightNodeDown.y + this.corridorWidth + 2 * modifierDistanceFromWall) 
        {
            return StructureHelper.CalculateMiddlePoint(
                rightNodeDown + new Vector2Int(0,modifierDistanceFromWall),
                leftNodeUp - new Vector2Int(0,modifierDistanceFromWall)
                ).y;
        }
        if(leftNodeDown.y >= rightNodeDown.y && rightNodeUp.y >= leftNodeDown.y + this.corridorWidth + 2 * modifierDistanceFromWall)
        {
            return StructureHelper.CalculateMiddlePoint(
                leftNodeDown + new Vector2Int(0,modifierDistanceFromWall),
                rightNodeUp - new Vector2Int(0,modifierDistanceFromWall)
                ).y;
        }
        return- 1;
    }

    private void ProcessRoomInRelationUpOrDown(Node structure1, Node structure2)
    {
        Node bottomStructure = null;
        List<Node> structureBottmChildren = StructureHelper.TraverseGraphToExtractLowestLeafes(structure1);
        Node topStructure = null;
        List<Node> structureAboveChildren = StructureHelper.TraverseGraphToExtractLowestLeafes(structure2);

        var sortedBottomStructure = structureBottmChildren.OrderByDescending(child => child.TopRightAreaCorner.y).ToList();

        if (sortedBottomStructure.Count == 1)
        {
            bottomStructure = structureBottmChildren[0];
        }
        else
        {
            int maxY = sortedBottomStructure[0].TopLeftAreaCorner.y;
            sortedBottomStructure = sortedBottomStructure.Where(child => Mathf.Abs(maxY - child.TopLeftAreaCorner.y) < 10).ToList();
            //int index = UnityEngine.Random.Range(0, sortedBottomStructure.Count);
            //bottomStructure = sortedBottomStructure[index];
        }
        var sortedTopStructure = structureAboveChildren.OrderBy(child => child.BottomLeftAreaCorner.y).ToList();

        if (sortedTopStructure.Count == 1)
        {
            topStructure = structureAboveChildren[0];
        }
        else
        {
            int minY = sortedTopStructure[0].BottomLeftAreaCorner.y;
            sortedTopStructure = sortedTopStructure.Where(child => Mathf.Abs(minY - child.BottomLeftAreaCorner.y) < 10).ToList();
        }

        float bestScale = 0;
        float scale = 0;
        Node bottomResult = null;
        Node topResult = null;
        foreach(var bottomRoom in sortedBottomStructure)
        {
            foreach(var topRoom in sortedTopStructure)
            {
                int topLeftBottomRight = Math.Abs(bottomRoom.TopRightAreaCorner.x - topRoom.BottomLeftAreaCorner.x);
                int topRightBottomLeft = Math.Abs(topRoom.BottomRightAreaCorner.x - bottomRoom.TopLeftAreaCorner.x);
                scale = topLeftBottomRight <= topRightBottomLeft ?
                 (float)topLeftBottomRight/topRightBottomLeft :
                  (float)topRightBottomLeft/topLeftBottomRight;
                if (scale > bestScale){
                    bestScale = scale;
                    topResult = topRoom;
                    bottomResult = bottomRoom;
                }
            }
        }        bottomStructure = bottomResult;
        topStructure = topResult;
        /*
        var possibleNeighboursInTopStructure = structureAboveChildren.Where(
            child => GetValidXForNeighbourUpDown(
                bottomStructure.TopLeftAreaCorner,
                bottomStructure.TopRightAreaCorner,
                child.BottomLeftAreaCorner,
                child.BottomRightAreaCorner)
            != -1).OrderBy(child => child.BottomRightAreaCorner.y).ToList();
        if (possibleNeighboursInTopStructure.Count == 0)
        {
            topStructure = structure2;
        }
        else
        {
            topStructure = possibleNeighboursInTopStructure[0];
        }
        */
        int x = GetValidXForNeighbourUpDown(
                bottomStructure.TopLeftAreaCorner,
                bottomStructure.TopRightAreaCorner,
                topStructure.BottomLeftAreaCorner,
                topStructure.BottomRightAreaCorner);
        int iterations = 0;
        while(iterations++ < 100 && x==-1 && sortedBottomStructure.Count > 1)
        {
            sortedBottomStructure = sortedBottomStructure.Where(child => child.TopLeftAreaCorner.x != topStructure.TopLeftAreaCorner.x).ToList();
            bottomStructure = sortedBottomStructure[0];
            x = GetValidXForNeighbourUpDown(
                bottomStructure.TopLeftAreaCorner,
                bottomStructure.TopRightAreaCorner,
                topStructure.BottomLeftAreaCorner,
                topStructure.BottomRightAreaCorner);
        }
        room1 = bottomStructure.name;
        room2 = topStructure.name;
        BottomLeftAreaCorner = new Vector2Int(x - corridorWidth/2, bottomStructure.TopLeftAreaCorner.y);
        TopRightAreaCorner = new Vector2Int(x + corridorWidth/2, topStructure.BottomLeftAreaCorner.y);
    }

    private int GetValidXForNeighbourUpDown(Vector2Int bottomNodeLeft, 
        Vector2Int bottomNodeRight, Vector2Int topNodeLeft, Vector2Int topNodeRight)
    {
        /*if(topNodeLeft.x < bottomNodeLeft.x && bottomNodeRight.x < topNodeRight.x)
        {
            return StructureHelper.CalculateMiddlePoint(
                bottomNodeLeft + new Vector2Int(modifierDistanceFromWall, 0),
                bottomNodeRight - new Vector2Int(modifierDistanceFromWall, 0)
                ).x;
        }*/
        if(topNodeLeft.x >= bottomNodeLeft.x && bottomNodeRight.x >= topNodeRight.x)
        {
            return StructureHelper.CalculateMiddlePoint(
                topNodeLeft + new Vector2Int(modifierDistanceFromWall,0),
                topNodeRight - new Vector2Int(modifierDistanceFromWall,0)
                ).x;
        }
        if(bottomNodeLeft.x >= topNodeLeft.x && topNodeRight.x >= bottomNodeLeft.x + this.corridorWidth + 2*modifierDistanceFromWall) 
        {
            return bottomNodeRight.x <= topNodeRight.x ?
                StructureHelper.CalculateMiddlePoint(
                bottomNodeLeft + new Vector2Int(modifierDistanceFromWall,0),
                bottomNodeRight - new Vector2Int(modifierDistanceFromWall,0)
                ).x :
                StructureHelper.CalculateMiddlePoint(
                bottomNodeLeft + new Vector2Int(modifierDistanceFromWall,0),
                topNodeRight - new Vector2Int(modifierDistanceFromWall,0)
                ).x;
        }
        if(bottomNodeRight.x <= topNodeRight.x && bottomNodeRight.x >= topNodeLeft.x + this.corridorWidth + 2*modifierDistanceFromWall)
        {
            return topNodeLeft.x <= bottomNodeLeft.x ?
                StructureHelper.CalculateMiddlePoint(
                bottomNodeLeft + new Vector2Int(modifierDistanceFromWall, 0),
                bottomNodeRight - new Vector2Int(modifierDistanceFromWall, 0)
                ).x : 
                StructureHelper.CalculateMiddlePoint(
                topNodeLeft + new Vector2Int(modifierDistanceFromWall, 0),
                bottomNodeRight - new Vector2Int(modifierDistanceFromWall, 0)
                ).x;
        }
        return -1;
    }

    public String get_structre1()
    {
        return room1;
    }
    public String get_structre2()
    {
        return room2;
    }
}
