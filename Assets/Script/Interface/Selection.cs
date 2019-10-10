using System.Collections.Generic;

namespace BB
{
    public class Selection
    {
        public readonly List<Minion> minions
            = new List<Minion>();
        // TODO: other humans
        public readonly List<TileItem> items
            = new List<TileItem>();
        public readonly List<IBuilding> buildings
            = new List<IBuilding>();
        // TODO: animals

        public void SelectSingle()
        {
            TrimToSingle(minions);
            TrimToSingle(items);
            TrimToSingle(buildings);
        }

        private static void TrimToSingle<T>(List<T> list)
        {
            if (list.Count > 1)
                list.RemoveRange(1, list.Count - 1);
        }

        public void FilterType()
        {
            if (minions.Count > 0)
            {
                items.Clear();
                buildings.Clear();
            }
            else if (items.Count > 0)
            {
                buildings.Clear();
            }
        }

        public bool Empty()
        {
            return
                minions.Count == 0 &&
                items.Count == 0 &&
                buildings.Count == 0;
        }

        public void Add(Selection sel)
        {
            minions.AddRange(sel.minions);
            items.AddRange(sel.items);
            buildings.AddRange(sel.buildings);
        }
    }
}