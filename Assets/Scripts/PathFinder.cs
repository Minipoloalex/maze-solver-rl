using System.Collections.Generic;
using UnityEngine;

public static class Pathfinder
{
    // Node class for A* algorithm
    private class Node
    {
        public Vector2Int Position { get; }
        public Node Parent { get; set; }
        public float GCost { get; set; } // Cost from start
        public float HCost { get; set; } // Heuristic cost to end
        public float FCost => GCost + HCost;

        public Node(Vector2Int position)
        {
            Position = position;
        }
    }

    // A* implementation
    public static List<Vector2Int> FindPath(MazeRuntimeGrid grid, Vector2Int startPos, Vector2Int endPos)
    {
        var toSearch = new List<Node>() { new Node(startPos) };
        var processed = new HashSet<Vector2Int>();

        while (toSearch.Count > 0)
        {
            Node current = toSearch[0];
            for (int i = 1; i < toSearch.Count; i++)
            {
                if (toSearch[i].FCost < current.FCost || (toSearch[i].FCost == current.FCost && toSearch[i].HCost < current.HCost))
                {
                    current = toSearch[i];
                }
            }

            processed.Add(current.Position);
            toSearch.Remove(current);

            if (current.Position == endPos)
            {
                // Path found, reconstruct and return
                var path = new List<Vector2Int>();
                while (current != null)
                {
                    path.Add(current.Position);
                    current = current.Parent;
                }
                path.Reverse();
                return path;
            }

            foreach (var direction in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var neighborPos = current.Position + direction;
                if (!grid.IsWall(neighborPos) && !processed.Contains(neighborPos))
                {
                    var neighborNode = new Node(neighborPos)
                    {
                        Parent = current,
                        GCost = current.GCost + 1,
                        HCost = Vector2Int.Distance(neighborPos, endPos)
                    };

                    // If neighbor is not in toSearch or we found a better path
                    if (!toSearch.Exists(n => n.Position == neighborPos) || neighborNode.GCost < toSearch.Find(n => n.Position == neighborPos).GCost)
                    {
                        toSearch.Add(neighborNode);
                    }
                }
            }
        }
        return null; // No path found
    }
}