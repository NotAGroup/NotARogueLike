using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;

struct DungeonSegment
{
    public GameObject area;

    public HashSet<Vector3Int> horizontalWallPositions;
    public HashSet<Vector3Int> verticalWallPositions;
}

[RequireComponent(typeof(NavMeshSurface))]
public class DungeonCreator : MonoBehaviour
{
    private GameObject dungeonLevel;
    private List<DungeonSegment> dungeonSegments;
    private NavMeshSurface navMeshSurface;

    public int dungeonWidth, dungeonLength;
    public int roomWidthMin, roomLengthMin;
    public int maxIterations;
    public int corridorWidth;
    public int enemyAmount;
    public float lootProb, shopProb;

    [Header("Materials")]
    public Material floorMaterial;
    public Material ceilingMaterial;

    [Range(0.0f, 0.3f)]
    public float roomBottomCornerModifier;
    [Range(0.7f, 1.0f)]
    public float roomTopCornerMidifier;
    [Range(0, 2)]
    public int roomOffset;
    
    [Header("Torch Placement")]
    public float pillarThickness;
    [Range(0, 6)]
    public float torchHeight;
    [Range(1, 20)]
    public float torchSpacing;
    [Range(0, 2)]
    public float torchWallOffset;

    public GameObject wallPrefab, pillarPrefab, playerPrefab, chestPrefab, shopPrefab, torchPrefab, trapDoorPrefab, navPointPrefab;

    private Dictionary<Vector3Int, DungeonSegment> horizontalWallOwners;
    private Dictionary<Vector3Int, DungeonSegment> verticalWallOwners;

    // definitions
    ItemDefinitions itemDefinitions;
    DungeonPropertyDefinitions dungeonPropertyDefinitions;
    OpponentDefinitions opponentDefinitions;

    // current level
    int level;
    DungeonProperties properties;

    void Awake()
    {
        GameObject defs = GameObject.Find("Definitions");
        itemDefinitions = defs.GetComponent<ItemDefinitions>();
        dungeonPropertyDefinitions = defs.GetComponent<DungeonPropertyDefinitions>();
        opponentDefinitions = defs.GetComponent<OpponentDefinitions>();

        navMeshSurface = GetComponent<NavMeshSurface>();
    }

    public void CreateDungeon()
    {
        level = RunData.Instance.level;
        if (level < 0) { 
            Debug.LogWarning("rundata has not been initialized yet, assuming level = 0");
            level = 0; 
        }

        properties = dungeonPropertyDefinitions.ComputeFrom(level);
        int size = (int) properties[DungeonPropertyKey.Size];
        Debug.Log("Generating dungeon with parameters: " + properties.ToString());

        DestroyAllChildren();

        DugeonGenerator generator = new DugeonGenerator(size * dungeonWidth, size * dungeonLength);
        var listOfRooms = generator.CalculateDungeon((int)Math.Clamp(Math.Log(size), 1, 5) * maxIterations,
            roomWidthMin,
            roomLengthMin,
            roomBottomCornerModifier,
            roomTopCornerMidifier,
            roomOffset,
            corridorWidth);

        dungeonLevel = new GameObject("DungeonLevel");
        dungeonLevel.transform.parent = transform;

        dungeonSegments = new List<DungeonSegment>();

        horizontalWallOwners = new Dictionary<Vector3Int, DungeonSegment>();
        verticalWallOwners = new Dictionary<Vector3Int, DungeonSegment>();

        for (int i = 0; i < listOfRooms.Count; i++)
        {
            Vector2Int bottomLeftAreaCorner = listOfRooms[i].BottomLeftAreaCorner;
            Vector2Int topRightAreaCorner = listOfRooms[i].TopRightAreaCorner;
            String type = listOfRooms[i].Type;

            Vector2Int areaCenter = (bottomLeftAreaCorner + topRightAreaCorner) / 2;

            DungeonSegment segment = new DungeonSegment();

            segment.area = new GameObject(char.ToUpper(type[0]) + type.Substring(1) + " " + areaCenter);
            segment.area.transform.parent = dungeonLevel.transform;
            segment.area.transform.position = new Vector3(areaCenter.x, 0, areaCenter.y);

            segment.horizontalWallPositions = new HashSet<Vector3Int>();
            segment.verticalWallPositions = new HashSet<Vector3Int>();

            dungeonSegments.Add(segment);

            CreateMesh(bottomLeftAreaCorner, topRightAreaCorner, segment, type);
        }

        CreatePlayer(listOfRooms);
        CreateTrapDoor(listOfRooms);
        CreateWalls();
        CreatePillars(listOfRooms);
        CreateLoot(listOfRooms);
        CreateEnemy(listOfRooms);
        CreateNavPoints(listOfRooms);
        CreateShop(listOfRooms);

        navMeshSurface.BuildNavMesh();
    }

    private void CreatePlayer(List<Node> listOfRooms)
    {
        Node room = listOfRooms[UnityEngine.Random.Range(0, listOfRooms.Count)];

        int playerPosX = UnityEngine.Random.Range(room.BottomLeftAreaCorner.x + 2, room.TopRightAreaCorner.x - 1);
        int playerPosY = UnityEngine.Random.Range(room.BottomLeftAreaCorner.y + 2, room.TopRightAreaCorner.y - 1);
        Vector3 playerPos = new Vector3(playerPosX, 2, playerPosY);

        Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        player.transform.SetPositionAndRotation(playerPos, Quaternion.identity);
        player.name = "Player";
        player.StartGame();
    }

    // selects opponent class randomly
    private int SelectRandomOpponentClass()
    {
        float sample = UnityEngine.Random.Range(0,1f);
        int opponentClass = 0;
        for (int index = 0; index < opponentDefinitions.classes.Length; index++)
        {
            sample -= opponentDefinitions.classes[index].spawnProbability;
            if (sample <= 0f)
            {
                opponentClass = index;
                break;
            }
        }
        return opponentClass;
    }

    private void CreateEnemy(List<Node> listOfRooms)
    {
        int actualEnemyAmount = (int) (enemyAmount * properties[DungeonPropertyKey.EnemyCount]);

        bool isEnoughEnemies = false;
        int counter = 0;

        while (!isEnoughEnemies)
        {
            for (int i = 0; i < listOfRooms.Count; i++)
            {
                Node room = listOfRooms[i];

                if (room.Type == "room")
                {
                    if (UnityEngine.Random.Range(0.0f, 1.0f) > 0.5f)
                    {
                        int opponentClass = SelectRandomOpponentClass();

                        int enemyPosX = UnityEngine.Random.Range(room.BottomLeftAreaCorner.x + 2, room.BottomRightAreaCorner.x - 1);
                        int enemyPosY = UnityEngine.Random.Range(room.BottomLeftAreaCorner.y + 2, room.TopLeftAreaCorner.y - 1);
                        Vector3 enemyPos = new Vector3(enemyPosX, 1, enemyPosY);

                        GameObject foe = Instantiate(opponentDefinitions.classes[opponentClass].prefab, enemyPos, Quaternion.identity, dungeonSegments[i].area.transform);
                        foe.name = opponentDefinitions.classes[opponentClass].prefab.name;
                        Opponent opponent = foe.GetComponent<Opponent>();
                        opponent.spawnRoom = room;

                        OpponentStats stats = foe.GetComponent<OpponentStats>();
                        stats.ComputeFrom(opponentDefinitions.classes[opponentClass], (int)properties[DungeonPropertyKey.EnemyLevel]);

                        counter++;
                    }

                    if (counter >= actualEnemyAmount)
                    {
                        break;
                    }
                }
            }

            isEnoughEnemies = counter >= actualEnemyAmount;
        }
    }

    private void AddNavPoint(Node room, GameObject area, int x, int y)
    {
        Vector3 navPointPos = new Vector3(x, 0.5f, y);

        GameObject navPoint = Instantiate(navPointPrefab, navPointPos, Quaternion.identity, area.transform);
        navPoint.name = navPointPrefab.name;

        room.navPointList.Add(navPoint.GetComponent<NavPoint>());
    }

    private void CreateNavPoints(List<Node> listOfRooms)
    {
        for (int i = 0; i < listOfRooms.Count; i++)
        {
            Node room = listOfRooms[i];

            if (room.Type == "room")
            {
                GameObject area = dungeonSegments[i].area;

                AddNavPoint(room, area, room.BottomLeftAreaCorner.x + 5, room.BottomLeftAreaCorner.y + 5);
                AddNavPoint(room, area, room.TopLeftAreaCorner.x + 5, room.TopLeftAreaCorner.y - 5);
                AddNavPoint(room, area, room.TopRightAreaCorner.x - 5, room.TopRightAreaCorner.y - 5);
                AddNavPoint(room, area, room.BottomRightAreaCorner.x - 5, room.BottomRightAreaCorner.y + 5);
            }
        }
    }


    private void SetRandomShopItems(ItemContainer items) {
        foreach (ItemSlot slot in items.slots) {
            slot.storedItem = null;

            // select random definition
            float random = UnityEngine.Random.Range(0f,1f);
            foreach (ItemDefinition def in itemDefinitions.definitions) {
                if (random <= def.shopProbability) {
                    slot.storedItem = def;
                    slot.count = 1;
                    break;
                }
                random -= def.shopProbability;
            }
        }
    }

    private void SetRandomDroppedItem(ItemSlot slot) {
        slot.storedItem = null;

        // select random definition
        var random = UnityEngine.Random.Range(0f,1f);
        foreach (ItemDefinition def in itemDefinitions.definitions) {
            if (random <= def.dropProbability) {
                slot.storedItem = def;
                slot.count = 1;
                break;
            }
            random -= def.dropProbability;
        }
    }

    private void CreateShop(List<Node> listOfRooms)
    {
        for (int i = 0; i < listOfRooms.Count(); i++)
        {
            Node room = listOfRooms[i];

            if (room.Type == "room")
            {
                // use center of room
                int shopX = (room.BottomLeftAreaCorner.x + 1 + room.BottomRightAreaCorner.x) / 2;
                int shopY = (room.BottomLeftAreaCorner.y + 1 + room.TopLeftAreaCorner.y) / 2;
                Vector3 shopPos = new Vector3(
                    shopX,
                    0f,
                    shopY);

                if (UnityEngine.Random.Range(0f,1f) < shopProb)
                {
                    GameObject shop = Instantiate(shopPrefab, shopPos, Quaternion.identity, dungeonSegments[i].area.transform);
                    shop.name = shopPrefab.name;

                    // compute random inventory of shop
                    ItemContainer items = new();
                    items.Resize(3);
                    SetRandomShopItems(items);

                    shop.GetComponent<ShopRenderer>().SetItems(items);
                }
            }
        }
    }

    // places the trapdoor in the room the furthest away from player
    private void CreateTrapDoor(List<Node> listOfRooms) {
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;

        float maxDist = 0f;
        Node selectedRoom = null;
        GameObject selectedAera = null;

        for (int i = 0; i < listOfRooms.Count(); i++) {
            Node room = listOfRooms[i];

            if (room.Type != "room") continue;

            Vector3 currentPos = new Vector3(
                (room.BottomLeftAreaCorner.x + room.BottomRightAreaCorner.x) / 2,
                0,
                (room.BottomLeftAreaCorner.y + room.TopLeftAreaCorner.y) / 2);
            float dist = (player.position - currentPos).magnitude;

            if (dist > maxDist) {
                maxDist = dist;
                selectedAera = dungeonSegments[i].area;
                selectedRoom = room;
            }
        }

        // place in center of the selected room
        Vector3 pos = new Vector3(
            (selectedRoom.BottomLeftAreaCorner.x + selectedRoom.BottomRightAreaCorner.x) / 2,
            0,
            (selectedRoom.BottomLeftAreaCorner.y + selectedRoom.TopLeftAreaCorner.y) / 2);
        GameObject trapdoor = Instantiate(trapDoorPrefab, pos, Quaternion.identity, selectedAera.transform);
        trapdoor.name = trapDoorPrefab.name;
    }


    private void CreateLoot(List<Node> listOfRooms)
    {
        for(int i = 0; i < listOfRooms.Count(); i++)
        {
            Node room = listOfRooms[i];

            if (room.Type == "room")
            {
                int chestX = UnityEngine.Random.Range(room.BottomLeftAreaCorner.x + 2, room.BottomRightAreaCorner.x - 1);
                int chestY = UnityEngine.Random.Range(room.BottomLeftAreaCorner.y + 2, room.TopLeftAreaCorner.y - 1);
                Vector3 chestPos = new Vector3(chestX, 0.35f, chestY);

                if (UnityEngine.Random.Range(0f,1f) > lootProb)
                {
                    GameObject chest = Instantiate(chestPrefab, chestPos, Quaternion.identity, dungeonSegments[i].area.transform);
                    chest.name = chestPrefab.name;

                    ItemSlot tmpSlot = new();
                    SetRandomDroppedItem(tmpSlot);
                    chest.GetComponent<DestroyableObject>().SetItem(tmpSlot.storedItem, tmpSlot.count);
                }
            }
        }
    }

    private void CreatePillars(List<Node> listOfRooms)
    {
        for (int i = 0; i < listOfRooms.Count; i++)
        {
            Node room = listOfRooms[i];
            GameObject area = dungeonSegments[i].area;

            Vector3 bottomLeftAreaCorner = new Vector3(room.BottomLeftAreaCorner.x, 0, room.BottomLeftAreaCorner.y);
            CreatePillar(bottomLeftAreaCorner, area);
            Vector3 bottomRightAreaCorner = new Vector3(room.TopRightAreaCorner.x, 0, room.BottomLeftAreaCorner.y);
            CreatePillar(bottomRightAreaCorner, area);
            Vector3 topLeftCorner = new Vector3(room.BottomLeftAreaCorner.x, 0, room.TopRightAreaCorner.y);
            CreatePillar(topLeftCorner, area);
            Vector3 topRightCorner = new Vector3(room.TopRightAreaCorner.x, 0, room.TopRightAreaCorner.y);
            CreatePillar(topRightCorner, area);
        }
    }

    private void CreatePillar(Vector3 position, GameObject parent)
    {
        if (position != Vector3.zero)
        {
            Quaternion rotation = Quaternion.Euler(0.0f, UnityEngine.Random.Range(0, 4) * 90.0f, 0.0f);

            GameObject pillar = Instantiate(pillarPrefab, position, Quaternion.identity, parent.transform);
            pillar.name = pillarPrefab.name;
        }
    }

    private void CreateWalls()
    {
        foreach (var segment in dungeonSegments)
        {
            List<Vector3Int> horizontalWalls = segment.horizontalWallPositions.OrderBy(p => p.z).ThenBy(p => p.x).ToList();
            List<Vector3Int> verticalWalls = segment.verticalWallPositions.OrderBy(p => p.x).ThenBy(p => p.z).ToList();

            if (horizontalWalls.Count > 0)
            {
                horizontalWalls.Add(horizontalWalls[0]);
            }
            if (verticalWalls.Count > 0)
            {
                verticalWalls.Add(verticalWalls[0]);
            }

            int wallLength;
            float wallDetail;

            float wallPosX = 0;
            int wallPosY = 0;
            float wallPosZ = 0;

            int startX = -1;
            int startZ = -1;

            int temp = 0;
            int wallScaler = 4;

            foreach (var hWallPosition in horizontalWalls)
            {
                // Define starting point
                if (startX == -1)
                {
                    startX = hWallPosition.x;
                    wallPosZ = hWallPosition.z;
                    temp = hWallPosition.x;
                }
                else
                {
                    // check if coherent Wall, save end point as temp
                    if (hWallPosition.x == temp + 1)
                    {
                        temp = hWallPosition.x;
                    }
                    // (temp - start) = total length of wall
                    else
                    {
                        wallLength = temp - startX;
                        int segmentCount = (wallLength < wallScaler) ? 1 : wallLength / wallScaler;
                        float scale = 0;
                        if (segmentCount == 1)
                        {
                            if (wallLength < 4)
                            {
                                int wallOffset = wallLength % wallScaler;
                                scale = (float)wallOffset / wallScaler;
                            }
                            else
                            {
                                scale = (float)wallLength / wallScaler;
                            }
                            wallPosX = startX + (float)(wallLength + 1) / 2;
                            Vector3 wallPos = new Vector3(
                                wallPosX,
                                wallPosY,
                                wallPosZ
                            );
                            GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.identity, segment.area.transform);
                            wall.transform.localScale = new Vector3(scale, 1, 1);
                            wall.name = wallPrefab.name;
                        }
                        else
                        {
                            wallPosX = startX + 0.5f;
                            scale = (float)wallLength / segmentCount / wallScaler;
                            for (int i = 0; i < segmentCount; i++)
                            {
                                wallPosX += scale * wallScaler / 2;
                                wallDetail = UnityEngine.Random.Range(-0.1f, 0.1f);
                                Vector3 wallPos = new Vector3(
                                    wallPosX,
                                    wallPosY,
                                    wallPosZ + wallDetail
                                );
                                GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.identity, segment.area.transform);
                                wall.transform.localScale = new Vector3(scale, 1, 1);
                                wall.name = wallPrefab.name;
                                wallPosX += scale * wallScaler / 2;
                            }
                        }

                        PlaceTorches(new Vector3(startX, 0, wallPosZ), new Vector3(temp + 1, 0, wallPosZ), segment.area);

                        startX = hWallPosition.x;
                        wallPosZ = hWallPosition.z;
                        temp = hWallPosition.x;
                    }
                }
            }
            foreach (var vWallPosition in verticalWalls)
            {
                // Define starting point
                if (startZ == -1)
                {
                    startZ = vWallPosition.z;
                    wallPosX = vWallPosition.x;
                    temp = vWallPosition.z;
                }
                else
                {
                    // check if coherent Wall, save end point as temp
                    if (vWallPosition.z == temp + 1)
                    {
                        temp = vWallPosition.z;
                    }
                    // (temp - start) = total length of wall
                    else
                    {
                        wallLength = temp - startZ;
                        int segmentCount = (wallLength < wallScaler) ? 1 : wallLength / wallScaler;
                        float scale;

                        if (segmentCount == 1)
                        {
                            wallLength += 1;
                            if (wallLength < 4)
                            {
                                int wallOffset = wallLength % wallScaler;
                                scale = (float)wallOffset / wallScaler;
                            }
                            else
                            {
                                scale = (float)wallLength / wallScaler;
                            }
                            wallPosZ = startZ + (float)(wallLength + 1) / 2;
                            Vector3 wallPos = new Vector3(
                                wallPosX,
                                wallPosY,
                                wallPosZ - 0.5f
                            );
                            GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.Euler(0, 90, 0), segment.area.transform);
                            wall.transform.localScale = new Vector3(scale, 1, 1);
                            wall.name = wallPrefab.name;
                        }
                        else
                        {

                            wallPosZ = startZ + 0.5f;
                            scale = (float)wallLength / segmentCount / wallScaler;
                            for (int i = 0; i < segmentCount; i++)
                            {
                                wallPosZ += scale * wallScaler / 2;
                                wallDetail = UnityEngine.Random.Range(-0.1f, 0.1f);
                                Vector3 wallPos = new Vector3(
                                    wallPosX + wallDetail,
                                    wallPosY,
                                    wallPosZ
                                );
                                GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.Euler(0, 90, 0), segment.area.transform);
                                wall.transform.localScale = new Vector3(scale, 1, 1);
                                wall.name = wallPrefab.name;
                                wallPosZ += scale * wallScaler / 2;
                            }
                        }

                        PlaceTorches(new Vector3(wallPosX, 0, startZ), new Vector3(wallPosX, 0, temp + 1), segment.area);

                        startZ = vWallPosition.z;
                        wallPosX = vWallPosition.x;
                        temp = vWallPosition.z;
                    }
                }
            }
        }
    }

    private void CreatePlane(String name, Material material, GameObject parent, Vector3[] vertices)
    {
        GameObject plane = new GameObject(name);
        plane.transform.parent = parent.transform;

        MeshFilter meshFilter = plane.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = plane.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = plane.AddComponent<MeshCollider>();

        Mesh mesh = new Mesh();
        mesh.name = "Dungeon" + name + "Mesh";

        int[] triangles = new int[]
        {
            0, 1, 2,
            2, 1, 3
        };

        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x, vertices[i].z);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        meshRenderer.material = material;

        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;
    }

    private void CreateMesh(Vector2 bottomLeftCorner, Vector2 topRightCorner, DungeonSegment segment, String roomType)
    {
        Vector3 bottomLeftV = new Vector3(bottomLeftCorner.x, 0, bottomLeftCorner.y);
        Vector3 bottomRightV = new Vector3(topRightCorner.x, 0, bottomLeftCorner.y);
        Vector3 topLeftV = new Vector3(bottomLeftCorner.x, 0, topRightCorner.y);
        Vector3 topRightV = new Vector3(topRightCorner.x, 0, topRightCorner.y);

        Vector3[] floorVertices = new Vector3[]
        {
            topLeftV,
            topRightV,
            bottomLeftV,
            bottomRightV
        };

        Vector3[] ceilingVertices = new Vector3[]
        {
            new Vector3(bottomLeftV.x, 6, bottomLeftV.z),
            new Vector3(bottomRightV.x, 6, bottomRightV.z),
            new Vector3(topLeftV.x, 6, topLeftV.z),
            new Vector3(topRightV.x, 6, topRightV.z)
        };

        CreatePlane("Floor", floorMaterial, segment.area, floorVertices);
        CreatePlane("Ceiling", ceilingMaterial, segment.area, ceilingVertices);

        for (int row = (int)Math.Ceiling(bottomLeftV.x); row < (int)Math.Ceiling(bottomRightV.x); row++)
        {
            var position = new Vector3(row, 0, bottomLeftV.z);
            AddWallPosition(position, true, segment);
        }
        for (int row = (int)Math.Ceiling(topLeftV.x); row < (int)Math.Ceiling(topRightCorner.x); row++)
        {
            var position = new Vector3(row, 0, topRightV.z);
            AddWallPosition(position, true, segment);
        }
        for (int col = (int)Math.Ceiling(bottomLeftV.z); col < (int)Math.Ceiling(topLeftV.z); col++)
        {
            var position = new Vector3(bottomLeftV.x, 0, col);
            AddWallPosition(position, false, segment);
        }
        for (int col = (int)Math.Ceiling(bottomRightV.z); col < (int)Math.Ceiling(topRightV.z); col++)
        {
            var position = new Vector3(bottomRightV.x, 0, col);
            AddWallPosition(position, false, segment);
        }
    }

    private void AddWallPosition(Vector3 position, bool horizontally, DungeonSegment segment)
    {
        Vector3Int location = Vector3Int.CeilToInt(position);

        var wallOwners = horizontally ? horizontalWallOwners : verticalWallOwners;
        var wallPosition = horizontally ? segment.horizontalWallPositions : segment.verticalWallPositions;

        if (wallOwners.TryGetValue(location, out DungeonSegment owningSegment))
        {
            if (horizontally)
            {
                owningSegment.horizontalWallPositions.Remove(location);
            }
            else
            {
                owningSegment.verticalWallPositions.Remove(location);
            }

            wallOwners.Remove(location);
        }
        else
        {
            wallOwners.Add(location, segment);
            wallPosition.Add(location);
        }
    }

    private void DestroyAllChildren()
    {
        while(transform.childCount != 0)
        {
            foreach(Transform item in transform)
            {
                DestroyImmediate(item.gameObject);
            }
        }
    }

    private void PlaceTorches(Vector3 start, Vector3 end, GameObject parent)
    {
        float lenght = Vector3.Distance(start, end);
        float visibleLength = lenght - 2.0f * pillarThickness;

        float edgePadding = torchSpacing * 0.5f;

        if (visibleLength < edgePadding)
        {
            return;
        }

        float usableLength = visibleLength - 2.0f * edgePadding;

        Vector3 roomCenter = parent.transform.position;
        Vector3 wallCenter = (start + end) * 0.5f;

        Vector3 direction = (end - start).normalized;
        Vector3 toRoomCenter = (roomCenter - wallCenter).normalized;

        Vector3 normal = Vector3.Cross(Vector3.up, direction).normalized;

        if (Vector3.Dot(normal, toRoomCenter) < 0.0f)
        {
            normal = -normal;
        }

        int torchCount = usableLength < 0 ? 1 : Mathf.FloorToInt(visibleLength / torchSpacing) + 1;

        if (torchCount == 1)
        {
            Vector3 position = wallCenter;
            position += normal * torchWallOffset;
            position.y = torchHeight;

            GameObject torch = Instantiate(torchPrefab, position, Quaternion.LookRotation(-normal), parent.transform);
            torch.name = torchPrefab.name;

            return;
        }

        float placementLength = (torchCount - 1) * torchSpacing;
        float startOffset = pillarThickness + edgePadding + (usableLength - placementLength) * 0.5f;

        for (int i = 0; i < torchCount; i++)
        {
            float offset = startOffset + i * torchSpacing;

            Vector3 position = start + direction * offset;
            position += normal * torchWallOffset;
            position.y = torchHeight;

            GameObject torch = Instantiate(torchPrefab, position, Quaternion.LookRotation(-normal), parent.transform);
            torch.name = torchPrefab.name;
        }
    }
}
