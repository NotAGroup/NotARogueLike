using UnityEngine;
using System;
using System.Text;

using TMPro;

public class UpgradeUI : MonoBehaviour
{
    [Header("Rendering")]
    public GameObject statUIPrefab;
    public Vector2 origin;
    public Vector2 offset;

    [Header("Stat information")]
    public TMP_Text statNameText;
    public TMP_Text statInfoText;

    private int selectedIndex;
    public BaseStatKey selectedStat { get => (BaseStatKey)stats.GetValue(selectedIndex); }

    private GameObject[] uiInstances;
    private UpgradeCostDefinitions defs;
    private StatScalingDefinitions scalingDefs;
    private GameObject player;

    // get array of stats
    private Array stats = Enum.GetValues(typeof(BaseStatKey));

    // 
    public void MoveSelection(Vector2 delta) {
        selectedIndex = (selectedIndex - (int)delta.y + stats.Length) % stats.Length;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        defs = GameObject.Find("Definitions").GetComponent<UpgradeCostDefinitions>();
        scalingDefs = GameObject.Find("Definitions").GetComponent<StatScalingDefinitions>();
        player = GameObject.Find("Player");

        // clear old ui objects
        if (uiInstances != null) {
            foreach (var i in uiInstances) 
                GameObject.Destroy(i);
        } else {
            uiInstances = new GameObject[stats.Length];
        }

        // 
        int index = 0;
        foreach(BaseStatKey key in stats) {
            var obj = Instantiate(statUIPrefab, transform);
            obj.transform.localPosition = origin + offset * index;

            uiInstances[index] = obj;
            index++;
        }
    }

    public void UpdateUpgrades() {
        int index = 0;
        foreach (GameObject i in uiInstances) {
            var disp = i.GetComponent<StatDisplay>();
            var stat = (BaseStatKey)stats.GetValue(index);
            int currentLevel = player.GetComponent<PlayerUpgrades>()[stat];
            int maxLevel = defs.MaxLevel(stat);
            int[] costs = defs[stat];
            int cost = (costs != null && currentLevel < costs.Length) ? costs[currentLevel] : -1;

            disp.SetStat(stat, currentLevel, maxLevel);
            disp.SetSelected(index == selectedIndex);
            disp.SetUpgradeable(currentLevel < maxLevel);
            disp.SetCost(cost);

            index++;
        }

        BaseStatKey selection = selectedStat;
        statNameText.text = Enum.GetName(typeof(BaseStatKey), selection);
        statInfoText.text = ComputeUpgradeDescription(selection);
    }
    
    private string ComputeUpgradeDescription(BaseStatKey baseStat) {
        StringBuilder builder = new();

        // iterate through different stats
        foreach (StatKey key in Enum.GetValues(typeof(StatKey))) {
            var upgrades = player.GetComponent<PlayerUpgrades>();

            var definition = scalingDefs[key];
            float current   = definition.ComputeFrom(upgrades.levels);
            float upgraded  = definition.ComputeWithOverride(upgrades.levels, baseStat, upgrades[baseStat] + 1);

            // skip if not changed by upgrade
            if (current == upgraded) continue;

            // show relative change if upgraded
            builder.AppendFormat("x{1:0.00} {0,-20}\n", key.ToString(), current / upgraded); 
        }

        return builder.ToString();
    }

    void Update() {
        // TODO: extract to player script
        UpdateUpgrades();
    }
}
