using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
public class DugeonGenerator
{
    
    List<RoomNode> allNodesCollection = new List<RoomNode>();
    private int dungeonWidth;
    private int dungeonLength;

    public DugeonGenerator(int dungeonWidth, int dungeonLength)
    {
        this.dungeonWidth = dungeonWidth;
        this.dungeonLength = dungeonLength;
    }



    public List<Node> CalculateDungeon(int maxIterations, int roomWidthMin, int roomLengthMin, float roomBottomCornerModifier, float roomTopCornerMidifier, int roomOffset, int corridorWidth, bool hasBossRoom)
    {
        BinarySpacePartitioner bsp = new BinarySpacePartitioner(dungeonWidth, dungeonLength);
        allNodesCollection = bsp.PrepareNodesCollection(maxIterations, roomWidthMin, roomLengthMin);
        List<Node> roomSpaces = StructureHelper.TraverseGraphToExtractLowestLeafes(bsp.RootNode);

        RoomGenerator roomGenerator = new RoomGenerator(maxIterations, roomLengthMin, roomWidthMin);
        List<RoomNode> roomList = roomGenerator.GenerateRoomsInGivenSpaces(roomSpaces, roomBottomCornerModifier, roomTopCornerMidifier, roomOffset);
        
        // define a random room as starting room
        Node start_room = roomList[UnityEngine.Random.Range(0, roomList.Count)];
        start_room.Type = "starting_room";

        CorridorsGenerator corridorGenerator = new CorridorsGenerator();
        var corridorList = corridorGenerator.CreateCorridor(allNodesCollection, corridorWidth);
        
        if (hasBossRoom)
        {
            List<Node> bossRoomList = AddBossRoom(start_room, roomList);
            roomList.Add((RoomNode)bossRoomList[0]);
            corridorList.Add(bossRoomList[1]);
        }

        return new List<Node>(roomList).Concat(corridorList).ToList();
    }

    private List<Node> AddBossRoom(Node start_room, List<RoomNode> roomList)
    {
        float maxDist = 0f;
        Node preBossRoom = null;
        Vector2Int startPos = StructureHelper.CalculateMiddlePoint(start_room.BottomLeftAreaCorner, start_room.TopRightAreaCorner);
        for (int i = 0; i < roomList.Count(); i++)
        {
            if (roomList[i] == start_room) continue;
            Vector2Int roomPos = StructureHelper.CalculateMiddlePoint(roomList[i].BottomLeftAreaCorner, roomList[i].TopRightAreaCorner);
            float dist = Vector2Int.Distance(startPos, roomPos);

            if (dist > maxDist)
            {
                maxDist = dist;
                preBossRoom = roomList[i];
            }
        }
        preBossRoom.name = "PreBossRoom";
        RelativePosition relativePosition = StructureHelper.CheckPositionStructure2AgainstStructure1(start_room, preBossRoom);
        RoomNode bossRoom = null;
        Vector2Int bossRoomOpening;
        if (relativePosition == RelativePosition.Up)
        {
            bossRoomOpening = StructureHelper.CalculateMiddlePoint(preBossRoom.TopLeftAreaCorner, preBossRoom.TopRightAreaCorner);
            Vector2Int BossRoomBottomLeft = new Vector2Int(bossRoomOpening.x - 25, bossRoomOpening.y + 6);
            Vector2Int BossRoomTopRight = new Vector2Int(bossRoomOpening.x + 25, bossRoomOpening.y + 56);
            bossRoom = new RoomNode(BossRoomBottomLeft, BossRoomTopRight, bossRoom, preBossRoom.TreeLayerIndex);
        }
        else if (relativePosition == RelativePosition.Down)
        {
            bossRoomOpening = StructureHelper.CalculateMiddlePoint(preBossRoom.BottomLeftAreaCorner, preBossRoom.BottomRightAreaCorner);
            Vector2Int BossRoomBottomLeft = new Vector2Int(bossRoomOpening.x - 25, bossRoomOpening.y - 56);
            Vector2Int BossRoomTopRight = new Vector2Int(bossRoomOpening.x + 25, bossRoomOpening.y - 6);
            bossRoom = new RoomNode(BossRoomBottomLeft, BossRoomTopRight, bossRoom, preBossRoom.TreeLayerIndex);
        }
        else if (relativePosition == RelativePosition.Left)
        {
            bossRoomOpening = StructureHelper.CalculateMiddlePoint(preBossRoom.TopLeftAreaCorner, preBossRoom.BottomLeftAreaCorner);
            Vector2Int BossRoomBottomLeft = new Vector2Int(bossRoomOpening.x - 56, bossRoomOpening.y - 25);
            Vector2Int BossRoomTopRight = new Vector2Int(bossRoomOpening.x - 6, bossRoomOpening.y + 25);
            bossRoom = new RoomNode(BossRoomBottomLeft, BossRoomTopRight, bossRoom, preBossRoom.TreeLayerIndex);
        }
        else
        {
            bossRoomOpening = StructureHelper.CalculateMiddlePoint(preBossRoom.TopRightAreaCorner, preBossRoom.BottomRightAreaCorner);
            Vector2Int BossRoomBottomLeft = new Vector2Int(bossRoomOpening.x + 6, bossRoomOpening.y - 25);
            Vector2Int BossRoomTopRight = new Vector2Int(bossRoomOpening.x + 56, bossRoomOpening.y + 25);
            bossRoom = new RoomNode(BossRoomBottomLeft, BossRoomTopRight, bossRoom, preBossRoom.TreeLayerIndex);
        }
        bossRoom.Type = "boss_room";
        CorridorNode bossCorridor = new CorridorNode(preBossRoom, bossRoom, 6);
        bossCorridor.Type = "corridor";
        bossCorridor.name = "BossCorridor";
        List<Node> bossRoomRes = new List<Node>
        {
            bossRoom,
            bossCorridor
        };
        return bossRoomRes;
    }
}