using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Vec2 = UnityEngine.Vector2;
using Vec2I = UnityEngine.Vector2Int;

namespace BB
{
    public abstract class UITool
    {
        public static LinkedList<UITool> RegisterTools(GameController game)
        {
            LinkedList<UITool> tools = new LinkedList<UITool>();

            tools.AddLast(new ToolControlMinion(game));
            tools.AddLast(new ToolOrders(game));
            tools.AddLast(new ToolPlace(game));
            tools.AddLast(new ToolBuild(game));

            return tools;
        }

        protected readonly GameController game;

        protected UITool(GameController game) => this.game = game;

        public static RectInt RectInclusive(Vec2 start, Vec2 end)
        {
            Vec2 lower = Vec2.Min(start, end);
            Vec2 upper = Vec2.Max(start, end);

            Vec2I tileStart = lower.Floor();
            Vec2I tileEnd = upper.Ceil();

            return new RectInt(tileStart, tileEnd - tileStart);
        }

        // TODO: make this more general
        public virtual void OnTab() { }
        public virtual void OnKeyP() { }

        public virtual void OnClick(Vec2I pos) { }
        public virtual void OnDragStart(Vec2 start, Vec2 end)
            => OnDragStart(RectInclusive(start, end));

        public virtual void OnDragUpdate(Vec2 start, Vec2 end)
            => OnDragUpdate(RectInclusive(start, end));

        public virtual void OnDragEnd(Vec2 start, Vec2 end)
            => OnDragEnd(RectInclusive(start, end));

        protected virtual void OnDragStart(RectInt rect) => OnDragUpdate(rect);
        protected virtual void OnDragUpdate(RectInt rec) { }
        protected virtual void OnDragEnd(RectInt rect) { }
    }

    public class ToolControlMinion : UITool
    {
        public ToolControlMinion(GameController game) : base(game) { }

        public override void OnClick(Vec2I pos)
        {
            if (game.Tile(pos).passable)
                game.K_MoveMinion(pos);
        }
    }

    public class ToolOrders : UITool
    {
        private IOrdersGiver currentOrders;
        private Dictionary<Vec2I, Transform> activeOverlays
            = new Dictionary<Vec2I, Transform>();

        public ToolOrders(GameController game) : base(game) {
            // TODO: janky af
            currentOrders = game.registry.systems[1].orders;
        }

        public override void OnClick(Vec2I pos)
        {
            // TODO: handle items
            var tile = game.Tile(pos);
            if (currentOrders.ApplicableToBuilding(tile.building) && !currentOrders.HasOrder(tile))
                currentOrders.AddOrder(tile);
        }

        protected override void OnDragUpdate(RectInt rect)
        {
            List<Vec2I> toRemove = new List<Vec2I>();
            foreach (var kvp in activeOverlays)
            {
                if (!rect.Contains(kvp.Key))
                {
                    kvp.Value.Destroy();
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var v in toRemove)
                activeOverlays.Remove(v);

            foreach (var v in rect.allPositionsWithin)
            {
                if (!activeOverlays.ContainsKey(v))
                {
                    // TODO: handle items
                    var tile = game.Tile(v);
                    if (currentOrders.ApplicableToBuilding(tile.building) && !currentOrders.HasOrder(tile))
                    {
                        var o = currentOrders.CreateOverlay(tile);
                        activeOverlays.Add(v, o);
                    }
                }
            }
        }

        protected override void OnDragEnd(RectInt rect)
        {
            foreach (var v in rect.allPositionsWithin)
                OnClick(v);

            foreach (var kvp in activeOverlays)
                kvp.Value.Destroy();

            activeOverlays = new Dictionary<Vec2I, Transform>();
        }
    }

    public class ToolBuild : UITool
    {
        private readonly IBuildable[] builds;
        private int currentBuild;
        private Dir curDir;

        private IBuildable curProto => builds[currentBuild];

        public ToolBuild(GameController game) : base(game)
        {
            builds = new IBuildable[] {
                D_Proto<BldgWorkbenchDef>("BB:Woodcutter"),
                D_Proto<BldgWallDef>("BB:StoneBrick"),
                D_Proto<BldgFloorDef>("BB:StoneBrick")
            };

            currentBuild = builds.Length - 1;
            OnTab();
        }

        private IBuildable D_Proto<T>(string name) where T : BldgDef
            => (IBuildable)game.registry.D_GetProto<T>(name);

        public override void OnTab()
        {
            currentBuild = (currentBuild + 1) % builds.Length;
            curDir = curProto.AllowedOrientations().First();

            BB.LogInfo($"Active Build: {curProto.GetType().Name}:{curDir}");
        }

        public override void OnKeyP()
        {
             do {
                 curDir = curDir.NextCW();
             } while (!curProto.AllowedOrientations().Contains(curDir));

            BB.LogInfo($"Active Build: {curProto.GetType().Name}:{curDir}");
        }

        public override void OnClick(Vec2I pos)
        {
            // TODO: only work an valid tiles
            var tile = game.Tile(pos);
            if (game.CanPlaceBuilding(tile, curProto.Bounds(curDir)))
            {
                SystemBuild.K_instance.CreateBuild(curProto, tile, curDir);
                //game.AddBuilding(pos, curProto.CreateBuilding(curDir));
            }
        }

        protected override void OnDragEnd(RectInt rect)
        {
            foreach (var v in rect.allPositionsWithin)
                OnClick(v);
        }
    }

    public class ToolPlace : UITool
    {
        public ToolPlace(GameController game) : base(game) { }

        public override void OnClick(Vec2I pos)
        {
            //game.ModifyTerrain(pos, new Terrain(game, game.defs.Get<TerrainDef>("BB:Path")));
            // game.AddBuilding(pos, game.registry.walls.Get(game.defs.Get<BldgWallDef>("BB:StoneBrick"))
            //     .CreateBuilding());
        }
    }
}