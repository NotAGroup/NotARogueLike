using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;

struct DungeonSegment
{
    public GameObject area;

    public HashSet<Vector3Int> corridorOpenings;

    public HashSet<Vector3Int> horizontalWallPositions;
    public HashSet<Vector3Int> verticalWallPositions;
}

[RequireComponent(typeof(NavMeshSurface))]
public class DungeonCreator : MonoBehaviour
{
    private GameObject dungeonLevel;
    private DungeonSegment[] dungeonSegments;
    private NavMeshSurface navMeshSurface;

    public int dungeonWidth, dungeonLength;
    public int roomWidthMin, roomLengthMin;
    public int maxIterations;
    public int corridorWidth;
    public int enemyAmount;
    public float shopProb;

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

    public GameObject wallPrefab, pillarPrefab, playerPrefab, chestPrefab, largeChestPrefab, shopPrefab, torchPrefab, trapDoorPrefab, bossDoorPrefab, navPointPrefab;

    private Dictionary<Vector3Int, DungeonSegment> horizontalWallOwners;
    private Dictionary<Vector3Int, DungeonSegment> verticalWallOwners;

    // definitions
    ItemDefinitions itemDefinitions;
    DungeonPropertyDefinitions dungeonPropertyDefinitions;
    OpponentDefinitions opponentDefinitions;

    // current level
    int level;
    int opponents;
    DungeonProperties properties;
    bool hasBossRoom;

    // temporary inventory for loot placement
    ItemContainer availableLootItems;
    CurrencyContainer availableLootCurrencies;

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
        try 
        {
            level = RunData.Instance.level;
            if (level < 0)
            {
                Debug.LogWarning("rundata has not been initialized yet, assuming level = 0");
                level = 0;
            }

            properties = dungeonPropertyDefinitions.ComputeFrom(level);
            int size = (int)properties[DungeonPropertyKey.Size];
            hasBossRoom = UnityEngine.Random.Range(0f,1f) <= properties[DungeonPropertyKey.HasBoss];
            Debug.Log("Generating dungeon with parameters: " + properties.ToString());

            DestroyAllChildren();

            DugeonGenerator generator = new DugeonGenerator(size * dungeonWidth, size * dungeonLength);
            var listOfRooms = generator.CalculateDungeon((int)Math.Clamp(Math.Log(size), 1, 5) * maxIterations,
                roomWidthMin,
                roomLengthMin,
                roomBottomCornerModifier,
                roomTopCornerMidifier,
                roomOffset,
                corridorWidth,
                hasBossRoom
                );

            dungeonLevel = new GameObject("DungeonLevel");
            dungeonLevel.transform.parent = transform;

            dungeonSegments = new DungeonSegment[listOfRooms.Count];

            horizontalWallOwners = new Dictionary<Vector3Int, DungeonSegment>();
            verticalWallOwners = new Dictionary<Vector3Int, DungeonSegment>();

            for (int i = listOfRooms.Count - 1; i >= 0; i--)
            {
                Vector2Int bottomLeftAreaCorner = listOfRooms[i].BottomLeftAreaCorner;
                Vector2Int topRightAreaCorner = listOfRooms[i].TopRightAreaCorner;
                string type = listOfRooms[i].Type;

                Vector2Int areaCenter = (bottomLeftAreaCorner + topRightAreaCorner) / 2;

                DungeonSegment segment = new DungeonSegment();
                string segmentName;

                if (type.Contains('_'))
                {
                    string[] name = type.Split('_');
                    segmentName = char.ToUpper(name[0][0]) + name[0].Substring(1) + " " +
                        char.ToUpper(name[1][0]) + name[1].Substring(1) + " " + areaCenter;
                }
                else
                {
                    segmentName = char.ToUpper(type[0]) + type.Substring(1) + " " + areaCenter;
                }

                segment.area = new GameObject(segmentName);
                segment.area.transform.parent = dungeonLevel.transform;
                segment.area.transform.position = new Vector3(areaCenter.x, 0, areaCenter.y);

                segment.corridorOpenings = new HashSet<Vector3Int>();

                segment.horizontalWallPositions = new HashSet<Vector3Int>();
                segment.verticalWallPositions = new HashSet<Vector3Int>();

                CreateMesh(bottomLeftAreaCorner, topRightAreaCorner, segment);
                StoreOnlyOpeningCentres(segment);

                dungeonSegments[i] = segment;
            }

            SampleLoot();
            CreatePlayer(listOfRooms);
            CreateTrapDoor(listOfRooms);
            CreateWalls();
            CreatePillars(listOfRooms);
            CreateNavPoints(listOfRooms);
            CreateEnemy(listOfRooms);

            if (hasBossRoom)
            {
                CreateBoss(listOfRooms);
                CreateBossDoor(listOfRooms);
            }

            CreateLoot(listOfRooms);
            CreateShop(listOfRooms);

            navMeshSurface.BuildNavMesh();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private void CreatePlayer(List<Node> listOfRooms)
    {
        Node room = listOfRooms.Find(r => r.Type == "starting_room");
        float playerPosX = (room.BottomLeftAreaCorner.x + room.TopRightAreaCorner.x) / 2f + UnityEngine.Random.Range(-2f, 2f);
        float playerPosY = (room.BottomLeftAreaCorner.y + room.TopRightAreaCorner.y) / 2f + UnityEngine.Random.Range(-2f, 2f);
        Vector3 playerPos = new Vector3(
            playerPosX,
            2,
            playerPosY
            );

        Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        player.transform.SetPositionAndRotation(playerPos, Quaternion.identity);
        player.name = "Player";
    }

    // selects opponent class randomly
    private int SelectRandomOpponentClass()
    {
        float sample = UnityEngine.Random.Range(0, 1f);
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

    private void CreateEncounter(List<Node> listOfRooms, int index, int enemyCount, int totalEnemyCount) 
    {
	    Node room = listOfRooms[index];
        int cornerCount = room.GetCorners().Count;

        int meleeEnemies = 0;
        int rangedEnemies = 0;

	    for (int i = 0; i < enemyCount; ) {
            int opponentClass = SelectRandomOpponentClass();

            // ensure ranged enemies constraint is fulfilled
            if (opponentDefinitions.classes[opponentClass].className == "Ranged Skeleton") 
            {
                rangedEnemies += 1;
                if (rangedEnemies > properties[DungeonPropertyKey.EnemyMaxRangedCount])
                    continue;
            }

            int enemyPosX = UnityEngine.Random.Range(room.BottomLeftAreaCorner.x + 2, room.BottomRightAreaCorner.x - 1);
            int enemyPosY = UnityEngine.Random.Range(room.BottomLeftAreaCorner.y + 2, room.TopLeftAreaCorner.y - 1);
            Vector3 enemyPos = new Vector3(enemyPosX, 1, enemyPosY);
    
            GameObject foe = Instantiate(opponentDefinitions.classes[opponentClass].prefab, enemyPos, Quaternion.identity, dungeonSegments[index].area.transform);
            foe.name = opponentDefinitions.classes[opponentClass].prefab.name;

            // ensures that not all skeletons want to go to the same NavPoint
            if (opponentDefinitions.classes[opponentClass].className == "Melee Skeleton")
            {
                foe.GetComponent<MeleeSkeleton>().SetNavPointID(meleeEnemies);
                meleeEnemies = (meleeEnemies + 1) % cornerCount;
            }

            // assign spawn room
            if (foe.TryGetComponent<Opponent>(out Opponent opponent))
            {
                opponent.spawnRoom = room;
            }

            // assign level
            if (foe.TryGetComponent<OpponentStats>(out OpponentStats stats))
            {
                int level = (int)(properties[DungeonPropertyKey.EnemyLevel]);
                stats.ComputeFrom(opponentDefinitions.classes[opponentClass], level);
            }

            // add loot
            if (foe.TryGetComponent<Rewards>(out Rewards enemyRewards)) {
                int type = UnityEngine.Random.Range(0, 3);
                switch (type) {
                case 0:
                    enemyRewards.AddItem(RandomItemFrom(availableLootItems));
                    break;
                case 1:
                    enemyRewards.xp = XpFrom(availableLootCurrencies, (int)(properties[DungeonPropertyKey.AvailableXP] / totalEnemyCount));
                    break;
                case 2:
                    enemyRewards.gold = GoldFrom(availableLootCurrencies, (int)(properties[DungeonPropertyKey.AvailableGold] / totalEnemyCount));
                    break;
                }
            }

            i++;
        }
    }

    private void CreateEnemy(List<Node> listOfRooms)
    {
        // expectation values
	    float encounters = properties[DungeonPropertyKey.EncounterCount] * properties[DungeonPropertyKey.Size] * properties[DungeonPropertyKey.Size];
        float enemiesPerEncounter = properties[DungeonPropertyKey.EnemyCount];
        opponents = enemyAmount * (int)(properties[DungeonPropertyKey.EnemyCount] * properties[DungeonPropertyKey.EncounterCount] *
            properties[DungeonPropertyKey.Size] * properties[DungeonPropertyKey.Size]); 

        if (hasBossRoom)
        {
            opponents++;
        }

        enemiesPerEncounter = Math.Clamp(enemiesPerEncounter, 1f, 3f);
        encounters = Math.Clamp(encounters, 2f, 10f);

        Debug.Log("Expecting " + encounters + " encounters of expected " + enemiesPerEncounter + "enemies each");

        // number of encounters follows poisson distribution
        int actualEncounters = (int)(Distributions.Poisson.Sample(8f) / 8f * encounters);
        for (int j = 0; j < actualEncounters; )
        {
            int i = UnityEngine.Random.Range(0, listOfRooms.Count());
            Node room = listOfRooms[i];

            if (room.Type == "room")
            {
	    	    int num = (int)(Distributions.Poisson.Sample(16f) / 16f * enemiesPerEncounter);

                Debug.Log("Creating encounter with " + num + " opponents in room " + i);
	    	    CreateEncounter(listOfRooms, i, num, opponents);

                j++;
            }

        }
    }

    private void AddNavPoint(Node room, GameObject area, string type, int x, int y)
    {
        Vector3 navPointPos = new Vector3(x, 0.5f, y);

        GameObject instance = Instantiate(navPointPrefab, navPointPos, Quaternion.identity, area.transform);
        instance.name = navPointPrefab.name;

        NavPoint navPoint = instance.GetComponent<NavPoint>();
        navPoint.type = type;

        room.navPointList.Add(navPoint);
    }

    private void CreateNavPoints(List<Node> listOfRooms)
    {
        for (int i = 0; i < listOfRooms.Count; i++)
        {
            Node room = listOfRooms[i];

            if (room.Type == "room" || room.Type == "starting_room" || room.Type == "boss_room")
            {
                DungeonSegment segment = dungeonSegments[i];

                AddNavPoint(room, segment.area, "corner", room.BottomLeftAreaCorner.x + 5, room.BottomLeftAreaCorner.y + 5);
                AddNavPoint(room, segment.area, "corner", room.TopLeftAreaCorner.x + 5, room.TopLeftAreaCorner.y - 5);
                AddNavPoint(room, segment.area, "corner", room.TopRightAreaCorner.x - 5, room.TopRightAreaCorner.y - 5);
                AddNavPoint(room, segment.area, "corner", room.BottomRightAreaCorner.x - 5, room.BottomRightAreaCorner.y + 5);

                foreach (var opening in segment.corridorOpenings)
                {
                    AddNavPoint(room, segment.area, "opening", opening.x, opening.z);
                }
            }
        }
    }

    private void SetRandomShopItems(ItemContainer items)
    {
        foreach (ItemSlot slot in items.slots)
        {
            slot.storedItem = null;

            // select random definition
            float random = UnityEngine.Random.Range(0f, 1f);
            foreach (ItemDefinition def in itemDefinitions.definitions)
            {
                if (random <= def.shopProbability)
                {
                    slot.storedItem = def;
                    slot.count = 1;
                    break;
                }
                random -= def.shopProbability;
            }
        }
    }

    private void CreateShop(List<Node> listOfRooms)
    {
        if (UnityEngine.Random.Range(0f, 1f) < shopProb)
        {
            int i = UnityEngine.Random.Range(0, listOfRooms.Count());
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

    // places the trapdoor in the room the furthest away from player
    private void CreateTrapDoor(List<Node> listOfRooms)
    {
        Node selectedRoom = null;
        GameObject selectedAera = null;

        if (hasBossRoom)
        {
            selectedRoom = listOfRooms.Find(r => r.Type == "boss_room");
            selectedAera = dungeonSegments[listOfRooms.IndexOf(selectedRoom)].area;
        }
        else
        {
            Transform player = GameObject.FindGameObjectWithTag("Player").transform;
            float maxDist = 0f;

            for (int i = 0; i < listOfRooms.Count(); i++)
            {
                Node room = listOfRooms[i];

                if (room.Type != "room") continue;

                Vector3 currentPos = new Vector3(
                    (room.BottomLeftAreaCorner.x + room.BottomRightAreaCorner.x) / 2,
                    0,
                    (room.BottomLeftAreaCorner.y + room.TopLeftAreaCorner.y) / 2);
                float dist = (player.position - currentPos).magnitude;

                if (dist > maxDist)
                {
                    maxDist = dist;
                    selectedAera = dungeonSegments[i].area;
                    selectedRoom = room;
                }
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

    // creates a budget of loot based on the dungeon properties
    private void SampleLoot() 
    {
        availableLootItems = new();
        availableLootCurrencies = new();

        // compute amount of arrows from enemies
        int numArrows = (int)properties[DungeonPropertyKey.AvailableAmmoPerEnemy];
        numArrows *= (int)properties[DungeonPropertyKey.EnemyCount];
        if (numArrows > 0)
        {
            availableLootItems.Resize(1);
            availableLootItems.slots[0].storedItem = itemDefinitions["BowAmmo"];
            availableLootItems.slots[0].count = numArrows;
        }

        // items
        int size = (int) properties[DungeonPropertyKey.Size];   
        int itemCount = (int)(properties[DungeonPropertyKey.AvailableItemsPerArea] * size * size);
        availableLootItems.Resize(itemCount + 1);

        // 
        FillWithRandomItems(availableLootItems);
        FillWithCurrency(availableLootCurrencies);

        // print available loot
        Debug.Log("DungeonCreator has a budget of items: " +  availableLootItems);
        Debug.Log("DungeonCreator has a budget of currencies: " + availableLootCurrencies[Currency.Gold] + " gold and " + availableLootCurrencies[Currency.XP] + " xp");
    }
    
    void FillWithCurrency(CurrencyContainer container)
    {
        // gold
        {
            float exp = Distributions.Bates.Sample(0.75f, 1.25f, 3) * properties[DungeonPropertyKey.AvailableGold];
            container[Currency.Gold] += (int)exp;
        }

        // xp
        {
            float exp = Distributions.Bates.Sample(0.75f, 1.25f, 3) * properties[DungeonPropertyKey.AvailableGold];
            container[Currency.XP] += (int)exp;
        }
    }

    private void FillWithRandomItems(ItemContainer items)
    {
        foreach (ItemSlot slot in items.slots)
        {
            // if slot contains something, keep it
            if(slot.storedItem != null && slot.count != 0) continue;

            // select random definition
            var random = UnityEngine.Random.Range(0f, 1f);
            foreach (ItemDefinition def in itemDefinitions.definitions)
            {
                if (random <= def.shopProbability)
                {
                    slot.storedItem = def;
                    slot.count = 1;
                    break;
                }
                random -= def.dropProbability;
            }
        }
    }

    private ItemSlot RandomItemFrom(ItemContainer budget) 
    {
        ItemSlot result = new ItemSlot();
        if (budget.Count < 1) return result;
        
        int slot = UnityEngine.Random.Range(0, budget.Count);
        result.storedItem = availableLootItems[slot].storedItem;
        result.count      = availableLootItems[slot].count;
        budget.ConsumeItem(slot);
        budget.Shrink();
        return result;
    }

    private int GoldFrom(CurrencyContainer budget, int expectedAmount) 
    {
        int amount = (int)(Distributions.Bates.Sample(0.6f, 1.4f, 3) * properties[DungeonPropertyKey.AvailableGold]);
        availableLootCurrencies[Currency.Gold] -= amount;
        return amount;
    }

    private int XpFrom(CurrencyContainer budget, int expectedAmount) 
    {
        int amount = (int)(Distributions.Bates.Sample(0.6f, 1.4f, 3) * properties[DungeonPropertyKey.AvailableXP]);
        availableLootCurrencies[Currency.XP] -= amount;
        return amount;
    }

    private GameObject PlaceChest(List<Node> listOfRooms, int index) 
    {
        Node room = listOfRooms[index];
        int chestX = UnityEngine.Random.Range(room.BottomLeftAreaCorner.x + 2, room.BottomRightAreaCorner.x - 1);
        int chestY = UnityEngine.Random.Range(room.BottomLeftAreaCorner.y + 2, room.TopLeftAreaCorner.y - 1);
        Vector3 chestPos = new Vector3(chestX, 0.35f, chestY);

        GameObject chest = Instantiate(chestPrefab, chestPos, Quaternion.identity, dungeonSegments[index].area.transform);
        chest.name = chestPrefab.name;
    
        return chest;
    }

    private GameObject PlaceLargeChest(List<Node> listOfRooms, int index) 
    {
        Node room = listOfRooms[index];
        int chestX = UnityEngine.Random.Range(room.BottomLeftAreaCorner.x + 2, room.BottomRightAreaCorner.x - 1);
        int chestY = UnityEngine.Random.Range(room.BottomLeftAreaCorner.y + 2, room.TopLeftAreaCorner.y - 1);
        Vector3 chestPos = new Vector3(chestX, 0.35f, chestY);

        GameObject chest = Instantiate(largeChestPrefab, chestPos, Quaternion.identity, dungeonSegments[index].area.transform);
        chest.name = largeChestPrefab.name;
    
        return chest;
    }

    private void CreateLoot(List<Node> listOfRooms)
    {
        // large chest, contains a larger portion of the available loot
        int i = UnityEngine.Random.Range(0, listOfRooms.Count());
        Node room = listOfRooms[i];
        if (room.Type == "room")
        {
            GameObject chest = PlaceLargeChest(listOfRooms, i);

            // add an item
            if (availableLootItems.Count > 0)
            {
                chest.GetComponent<Rewards>().AddItem(RandomItemFrom(availableLootItems));
            }

            // for currencies use an exponential distribution
            // add some xp
            if (properties[DungeonPropertyKey.AvailableXP] > 0)
            {
                int amount = (int)(Distributions.Exponential.Sample(1f) * properties[DungeonPropertyKey.AvailableXP]);
                availableLootCurrencies[Currency.XP] -= amount;
                chest.GetComponent<Rewards>().xp = amount;
            }

            // add some gold
            if (properties[DungeonPropertyKey.AvailableGold] > 0)
            {
                int amount = (int)(Distributions.Exponential.Sample(1f) * properties[DungeonPropertyKey.AvailableGold]);
                availableLootCurrencies[Currency.Gold] -= amount;
                chest.GetComponent<Rewards>().gold = amount;
            }
        }

        // 
        float size = properties[DungeonPropertyKey.Size];
        int numberOfChests = (int)(properties[DungeonPropertyKey.ChestsPerArea] * size * size);

        // items
        availableLootItems.Shrink();
        int itemChestCount = availableLootItems.Count; // lambda for poisson distribution
        for (float time = 0f; time < 1f && availableLootItems.Count > 0; time += Distributions.Exponential.Sample(itemChestCount))
        {
            i = UnityEngine.Random.Range(0, listOfRooms.Count());
            room = listOfRooms[i];
            if (room.Type == "room")
            {
                GameObject chest = PlaceChest(listOfRooms, i);
                chest.GetComponent<Rewards>().AddItem(RandomItemFrom(availableLootItems));
            }
        }

        // xp
        int xpChestCount = (int)(numberOfChests - itemChestCount) / 2;
        for (float time = Distributions.Exponential.Sample(xpChestCount); time < 1f && availableLootCurrencies[Currency.XP] > 0; time += Distributions.Exponential.Sample(xpChestCount))
        {
            i = UnityEngine.Random.Range(0, listOfRooms.Count());
            room = listOfRooms[i];
            if (room.Type == "room")
            {
                GameObject chest = PlaceChest(listOfRooms, i);
                chest.GetComponent<Rewards>().xp = GoldFrom(availableLootCurrencies, (int)(properties[DungeonPropertyKey.AvailableXP] / xpChestCount));
            }
        }

        // gold
        int goldChestCount = (int)(numberOfChests - itemChestCount) / 2;
        for (float time = Distributions.Exponential.Sample(goldChestCount); time < 1f && availableLootCurrencies[Currency.Gold] > 0; time += Distributions.Exponential.Sample(goldChestCount))
        {
            i = UnityEngine.Random.Range(0, listOfRooms.Count());
            room = listOfRooms[i];
            if (room.Type == "room")
            {
                GameObject chest = PlaceChest(listOfRooms, i);
                chest.GetComponent<Rewards>().gold = XpFrom(availableLootCurrencies, (int)(properties[DungeonPropertyKey.AvailableGold] / goldChestCount));
            }
        }

        // empty chests
        float fractionOfEmptyChests = 0.1f;
        float fractionOfEmptyLargeChests = 0.1f;
        int emptyChests = (int)(fractionOfEmptyChests * numberOfChests);
        for (float time = Distributions.Exponential.Sample(emptyChests); time < 1f; time += Distributions.Exponential.Sample(emptyChests))
        {
            i = UnityEngine.Random.Range(0, listOfRooms.Count());
            room = listOfRooms[i];
            if (room.Type == "room")
            {
                bool large = (UnityEngine.Random.Range(0f, 1f) < fractionOfEmptyLargeChests);
                GameObject chest = large ? PlaceLargeChest(listOfRooms, i) : PlaceChest(listOfRooms, i);
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

        if (hasBossRoom)
        {
            Node room = listOfRooms.Find(r => r.Type == "boss_room");
            GameObject area = dungeonSegments[listOfRooms.IndexOf(room)].area;

            float height = room.TopRightAreaCorner.y - room.BottomLeftAreaCorner.y;
            float width = room.TopRightAreaCorner.x - room.BottomLeftAreaCorner.x;

            Vector3 pillarOne = new Vector3(room.BottomLeftAreaCorner.x + width / 4, 0, room.BottomLeftAreaCorner.y + height / 4);
            PlaceTorchesBossRoom(CreatePillar(pillarOne, area), area);
            Vector3 pillarTwo = new Vector3(room.TopRightAreaCorner.x - width / 4, 0, room.BottomLeftAreaCorner.y + height / 4);
            PlaceTorchesBossRoom(CreatePillar(pillarTwo, area), area);
            Vector3 pillarThree = new Vector3(room.BottomLeftAreaCorner.x + width / 4, 0, room.TopRightAreaCorner.y - height / 4);
            PlaceTorchesBossRoom(CreatePillar(pillarThree, area), area);
            Vector3 pillarFour = new Vector3(room.TopRightAreaCorner.x - width / 4, 0, room.TopRightAreaCorner.y - height / 4);
            PlaceTorchesBossRoom(CreatePillar(pillarFour, area), area);
        }
    }

    private GameObject CreatePillar(Vector3 position, GameObject parent)
    {
        if (position != Vector3.zero)
        {
            Quaternion rotation = Quaternion.Euler(0.0f, UnityEngine.Random.Range(0, 4) * 90.0f, 0.0f);

            GameObject pillar = Instantiate(pillarPrefab, position, rotation, parent.transform);
            pillar.name = pillarPrefab.name;
            return pillar;
        } else
        {
            return null;
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

    private void CreateBoss(List<Node> listOfRooms)
    {
        Node room = listOfRooms.Find(r => r.Type == "boss_room");
        DungeonSegment segment = dungeonSegments[listOfRooms.IndexOf(room)];

        OpponentClassDefinition opponentClass = opponentDefinitions["Overlord"];

        if (opponentClass == null)
        {
            return;
        }

        if (opponentClass.bossEnemy)
        {
            int level = (int)(properties[DungeonPropertyKey.EnemyLevel]);
            
            int positionX = UnityEngine.Random.Range(room.BottomLeftAreaCorner.x + 2, room.BottomRightAreaCorner.x - 1);
            int positionY = UnityEngine.Random.Range(room.BottomLeftAreaCorner.y + 2, room.TopLeftAreaCorner.y - 1);
            Vector3 position = new Vector3(positionX, 1, positionY);

            GameObject instance = Instantiate(opponentClass.prefab, position, Quaternion.identity, segment.area.transform);
            instance.name = opponentClass.prefab.name;

            if (instance.TryGetComponent<Overlord>(out Overlord overlord))
            {
                overlord.SetArea(segment.area.transform);
                overlord.SetLevel(level);
            }

            // assign spawn room
            if (instance.TryGetComponent<Opponent>(out Opponent opponent))
            {
                opponent.spawnRoom = room;
            }

            // assign level
            if (instance.TryGetComponent<OpponentStats>(out OpponentStats stats))
            {
                stats.ComputeFrom(opponentClass, level);
            }

            // add loot
            if (instance.TryGetComponent<Rewards>(out Rewards enemyRewards))
            {
                int type = UnityEngine.Random.Range(0, 3);
                switch (type)
                {
                    case 0:
                        enemyRewards.AddItem(RandomItemFrom(availableLootItems));
                        break;
                    case 1:
                        enemyRewards.xp = XpFrom(availableLootCurrencies, (int)(properties[DungeonPropertyKey.AvailableXP] / opponents));
                        break;
                    case 2:
                        enemyRewards.gold = GoldFrom(availableLootCurrencies, (int)(properties[DungeonPropertyKey.AvailableGold] / opponents));
                        break;
                }
            }
        }
    }

    private void CreateBossDoor(List<Node> listOfRooms)
    {
        Node preBossRoom = listOfRooms.Find(r => r.name == "PreBossRoom");
        Node bossRoom = listOfRooms.Find(r => r.Type == "boss_room");
        RelativePosition relativePosition = StructureHelper.CheckPositionStructure2AgainstStructure1(bossRoom, preBossRoom);
        Node room = listOfRooms.Find(c => c.name == "BossCorridor");
        DungeonSegment segment = dungeonSegments[listOfRooms.IndexOf(room)];

        Vector3 doorPosition = new Vector3(
            (room.BottomLeftAreaCorner.x + room.TopRightAreaCorner.x) / 2f,
            0f,
            (room.BottomLeftAreaCorner.y + room.TopRightAreaCorner.y) / 2f
        );
        Vector3 leftTorchPos; 
        Vector3 rightTorchPos;
        Quaternion rotation;
        Quaternion torchRotation;
        float torchWallOffset = 1.049f;
        Debug.Log("Creating boss door at " + doorPosition + " with relative position " + relativePosition);
        if (relativePosition == RelativePosition.Up)
        {
            rotation = Quaternion.identity;
            leftTorchPos = new Vector3(room.BottomLeftAreaCorner.x, torchHeight, room.TopRightAreaCorner.y + torchWallOffset);
            rightTorchPos = new Vector3(room.TopRightAreaCorner.x, torchHeight, room.TopRightAreaCorner.y + torchWallOffset);
            torchRotation = Quaternion.Euler(0, 180, 0);
        }
        else if (relativePosition == RelativePosition.Down)
        {
            rotation = Quaternion.Euler(0, 180, 0);
            leftTorchPos = new Vector3(room.TopRightAreaCorner.x, torchHeight, room.BottomLeftAreaCorner.y - torchWallOffset);
            rightTorchPos = new Vector3(room.BottomLeftAreaCorner.x, torchHeight, room.BottomLeftAreaCorner.y - torchWallOffset);
            torchRotation = Quaternion.identity;
        }
        else if (relativePosition == RelativePosition.Right)
        {
            rotation = Quaternion.Euler(0, 90, 0);
            rightTorchPos = new Vector3(room.TopRightAreaCorner.x + torchWallOffset, torchHeight, room.TopRightAreaCorner.y);
            leftTorchPos = new Vector3(room.TopRightAreaCorner.x + torchWallOffset, torchHeight, room.BottomLeftAreaCorner.y);
            torchRotation = Quaternion.Euler(0, 270, 0);
        }
        else
        {
            rotation = Quaternion.Euler(0, 270, 0);
            rightTorchPos = new Vector3(room.BottomLeftAreaCorner.x - torchWallOffset, torchHeight, room.TopRightAreaCorner.y);
            leftTorchPos = new Vector3(room.BottomLeftAreaCorner.x - torchWallOffset, torchHeight, room.BottomLeftAreaCorner.y);
            torchRotation = Quaternion.Euler(0, 90, 0);
        }
        GameObject door = Instantiate(bossDoorPrefab, doorPosition, rotation, segment.area.transform);
        GameObject leftTorch = Instantiate(torchPrefab, leftTorchPos, torchRotation, segment.area.transform);
        GameObject rightTorch = Instantiate(torchPrefab, rightTorchPos, torchRotation, segment.area.transform);
        door.name = bossDoorPrefab.name;
        leftTorch.name = "left_" + torchPrefab.name;
        rightTorch.name = "right_" + torchPrefab.name;
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

    private void CreateMesh(Vector2 bottomLeftCorner, Vector2 topRightCorner, DungeonSegment segment)
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

            segment.corridorOpenings.Add(location);
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
        while (transform.childCount != 0)
        {
            foreach (Transform item in transform)
            {
                DestroyImmediate(item.gameObject);
            }
        }
    }

    private void PlaceTorches(Vector3 start, Vector3 end, GameObject parent)
    {
        float lenght = Vector3.Distance(start, end);
        float spacing = parent.name.Contains("Boss Room") ? torchSpacing * 0.5f : torchSpacing;
        float visibleLength = lenght - 2.0f * pillarThickness;

        float edgePadding = spacing * 0.5f;

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

        int torchCount = usableLength < 0 ? 1 : Mathf.FloorToInt(visibleLength / spacing) + 1;

        if (torchCount == 1)
        {
            Vector3 position = wallCenter;
            position += normal * torchWallOffset;
            position.y = torchHeight;

            GameObject torch = Instantiate(torchPrefab, position, Quaternion.LookRotation(-normal), parent.transform);
            torch.name = torchPrefab.name;

            return;
        }

        float placementLength = (torchCount - 1) * spacing;
        float startOffset = pillarThickness + edgePadding + (usableLength - placementLength) * 0.5f;

        for (int i = 0; i < torchCount; i++)
        {
            float offset = startOffset + i * spacing;

            Vector3 position = start + direction * offset;
            position += normal * torchWallOffset;
            position.y = torchHeight;

            GameObject torch = Instantiate(torchPrefab, position, Quaternion.LookRotation(-normal), parent.transform);
            torch.name = torchPrefab.name;
        }
    }

    private void PlaceTorchesBossRoom(GameObject Pillar, GameObject parent)
    {
        Vector3 position = new Vector3(Pillar.transform.position.x, torchHeight, Pillar.transform.position.z - 1.25f);
        Quaternion rotation = Quaternion.identity;
        GameObject torch = Instantiate(torchPrefab, position, rotation, parent.transform);
        torch.name = torchPrefab.name;
        position = new Vector3(Pillar.transform.position.x, torchHeight, Pillar.transform.position.z + 1.25f);
        rotation = Quaternion.Euler(0, 180, 0);
        GameObject torch2 = Instantiate(torchPrefab, position, rotation, parent.transform);
        torch2.name = torchPrefab.name;
        position = new Vector3(Pillar.transform.position.x - 1.25f, torchHeight, Pillar.transform.position.z);
        rotation = Quaternion.Euler(0, 90, 0);
        GameObject torch3 = Instantiate(torchPrefab, position, rotation, parent.transform);
        torch3.name = torchPrefab.name;
        position = new Vector3(Pillar.transform.position.x + 1.25f, torchHeight, Pillar.transform.position.z);
        rotation = Quaternion.Euler(0, 270, 0);
        GameObject torch4 = Instantiate(torchPrefab, position, rotation, parent.transform);
        torch4.name = torchPrefab.name;
    }

    void StoreOnlyOpeningCentres(DungeonSegment segment)
    {
        List<Vector3Int[]> openings = new List<Vector3Int[]>();
        int width = 6;
        int count = segment.corridorOpenings.Count / width;

        for (int i = 0; i < count; i++)
        {
            openings.Add(new Vector3Int[2] { segment.corridorOpenings.ElementAt(i * width), segment.corridorOpenings.ElementAt(i * width + (width - 1)) });
        }

        segment.corridorOpenings.Clear();

        foreach (var opening in openings)
        {
            segment.corridorOpenings.Add((opening[0] + opening[1]) / 2);
        }
    }
}
