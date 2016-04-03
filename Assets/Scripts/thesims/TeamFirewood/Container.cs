using UnityEngine;
using System.Collections.Generic;
using Infra.Utils;
using Ai.Goap;

namespace TeamFirewood {
	public enum Item {
	    None,
	    Logs,
	    Firewood,
	    Ore,
	    NewTool,
	    Branches,

		//New items:
		Treasure,
		Advice,
		Keys,
		Strength,
		Nirvana,
		Mushrooms, 
		Part2
	}

	/// <summary>
	/// Inventory that holds Agent's physical & mental belongings.
	/// </summary>
	public class Container : MonoBehaviour {
	    public GameObject tool;
	    // TODO: Add each tool as a specific item. Have the blacksmith craft each
	    //       tool separately. One goal per tool.
	    // TODO: Allow changing the priorities of the blacksmith's goals.
	    public string toolType = "ToolAxe";
	    public Dictionary<Item, int> items = new Dictionary<Item, int> {
	        {Item.Logs, 0},
	        {Item.Firewood, 0},
	        {Item.Ore, 0},
	        {Item.NewTool, 0},
	        {Item.Branches, 0},

			//New items:
			{Item.Treasure, 0},
			{Item.Advice, 0},
			{Item.Keys, 0},
			{Item.Strength, 0},
			{Item.Nirvana, 0},
			{Item.Mushrooms, 0},
			{Item.Part2, 0},
			
	    };

	    [SerializeField]
	    protected int newTools;
	    [SerializeField]
	    protected int logs;
	    [SerializeField]
	    protected int firewood;
	    [SerializeField]
	    protected int ore;
	    [SerializeField]
	    protected int branches;

		//New items:
		[SerializeField]
		protected int treasure;
		[SerializeField]
		protected int advice;
		[SerializeField]
		protected int keys;
		[SerializeField]
		protected int strength;
		[SerializeField]
		protected int nirvana;
		[SerializeField]
		protected int mushrooms;
		[SerializeField]
		protected int part2;


	    protected void Awake() {
	        // Make sure all new items are defined in the container.
	        foreach (var item in EnumUtils.EnumValues<Item>()) {
	            if (item == Item.None) continue;
	            items[item] = 0;
	        }
	        items[Item.NewTool] = newTools;
	        items[Item.Logs] = logs;
	        items[Item.Firewood] = firewood;
	        items[Item.Ore] = ore;
	        items[Item.Branches] = branches;

			//New items: 
			items[Item.Treasure] = treasure;
			items[Item.Advice] = advice;
			items[Item.Keys] = keys;
			items[Item.Strength] = strength;
			items[Item.Nirvana] = nirvana;
			items[Item.Mushrooms] = mushrooms;
			items[Item.Part2] = part2;
	    }

	#if DEBUG_CONTAINER
	    protected void Update() {
	        newTools = items[Item.NewTool];
	        logs = items[Item.Logs];
	        firewood = items[Item.Firewood];
	        ore = items[Item.Ore];
	        branches = items[Item.Branches];

			//New items: 
			items[Item.Treasure] = treasure;
			items[Item.Advice] = advice;
			items[Item.Keys] = keys;
			items[Item.Strength] = strength;
			items[Item.Nirvana] = nirvana;
			items[Item.Mushrooms] = mushrooms;
			items[Item.Part2] = part2;
	    }
	#endif
	}
}
