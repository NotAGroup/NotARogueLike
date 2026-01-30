using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;

using TMPro;

public class UpgradeUI : MonoBehaviour
{
    [Header("Rendering")]
    public GameObject statUIPrefab;
    public bool upgradeEffectsRelative;

    // transforms that define slot positions
    public RectTransform statTransformStart;
    public RectTransform statTransformEnd;

    [Header("Stat information")]
    public TMP_Text statNameText;
    public TMP_Text statInfoText;

    private int selectedIndex;
    public BaseStatKey selectedStat { get => (BaseStatKey)stats.GetValue(selectedIndex); }

    private GameObject[] uiInstances;
    private UpgradeCostDefinitions defs;
    private StatScalingDefinitions scalingDefs;
    private Strings strings;
    private PlayerUpgrades upgrades;
    private GameObject player;

    // get array of stats
    private Array stats = Enum.GetValues(typeof(BaseStatKey));
    

    // 
    public void MoveSelection(Vector2 delta) 
    {
        selectedIndex = (selectedIndex - (int)delta.y + stats.Length) % stats.Length;
        UpdateUpgrades();
    }

    public void TryUpgrade() 
    {  
        upgrades.TryUpgrade(selectedStat);
        UpdateUpgrades();
    }

    public void OnClickUpgrade(int index)
    {
        selectedIndex = index;
        upgrades.TryUpgrade(selectedStat);
        UpdateUpgrades();
    }

    public void OnClick(int index)
    {
        selectedIndex = index;
        UpdateUpgrades();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        defs = GameObject.Find("Definitions").GetComponent<UpgradeCostDefinitions>();
        scalingDefs = GameObject.Find("Definitions").GetComponent<StatScalingDefinitions>();
        strings = GameObject.Find("Canvas").GetComponent<Strings>();
        player = GameObject.Find("Player");
        upgrades = player.GetComponent<PlayerUpgrades>();
    }

    void OnEnable() 
    {
        // clear old ui objects
        if (uiInstances == null) {
            uiInstances = new GameObject[stats.Length];
        }

        // create ui element for each stat
        int index = 0;
        foreach(BaseStatKey key in stats) {
            if (uiInstances[index] != null) continue;

            var obj = Instantiate(statUIPrefab, transform);
            RectTransform target = obj.GetComponent<RectTransform>();
            target.localPosition = Vector2.Lerp(statTransformStart.localPosition, statTransformEnd.localPosition, (float)index / (stats.Length - 1));
            target.localScale    = Vector2.Lerp(statTransformStart.localScale, statTransformEnd.localScale, (float)index / (stats.Length - 1));

            // onclick callback
            object boxedIndex = index;
            target.Find("UpgradeButton").GetComponent<Button>().onClick.AddListener(() => OnClickUpgrade((int)boxedIndex));

            target.GetComponent<Button>().onClick.AddListener(() => OnClick((int)boxedIndex));

            uiInstances[index] = obj;
            index++;
        }

        // disable preview gameobjects
        statTransformStart.gameObject.SetActive(false);
        statTransformEnd.gameObject.SetActive(false);

        selectedIndex = -1;

        UpdateUpgrades();
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

        if (selectedIndex >= 0)
        {
            statNameText.text = strings.baseStats[selectedStat];
            statInfoText.text = ComputeUpgradeDescription(selectedStat);
        } 
        else 
        {
            statNameText.text = "Nothing selected";
            statInfoText.text = "...";
        }
    }
    
    private string ComputeUpgradeDescription(BaseStatKey baseStat) {
        StringBuilder builder = new();

        // iterate through different stats
        foreach (StatKey key in Enum.GetValues(typeof(StatKey))) {
            var definition = scalingDefs[key];
            float current   = definition.ComputeFrom(upgrades.levels);
            float upgraded  = definition.ComputeWithOverride(upgrades.levels, baseStat, upgrades[baseStat] + 1);

            // skip if not changed by upgrade
            if (current == upgraded) continue;

            // show relative change if upgraded
            string name = strings.stats[key];
            if (upgradeEffectsRelative)
            {
                builder.AppendFormat("{0,-18}\t x{1:0.00}\n", name, upgraded / current); 
            }
            else
            {
                builder.AppendFormat("{0,-18}\t {1:0.00}->{2:0.00}\n", name, current, upgraded); 
            }
        }

        return builder.ToString();
    }
}
