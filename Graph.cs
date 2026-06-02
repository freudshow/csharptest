using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    #region 基础结构

    // 顶点颜色枚举（严格对应伪代码：WHITE / GRAY / BLACK）
    public enum Color
    {
        WHITE,  // 未发现
        GRAY,   // 已发现，正在处理
        BLACK   // 已处理完成
    }

    // 顶点
    public class Vertex<T>
    {
        public T Value { get; }
        public Color Color { get; set; }

        public Vertex(T value)
        {
            Value = value;
            Color = Color.WHITE;
        }
    }

    // 定义边
    public class Edge<T>
    {
        public T Source { get; }
        public T Target { get; }
        public double Weight { get; }

        public Edge(T source, T target, double weight = 1.0)
        {
            Source = source;
            Target = target;
            Weight = weight;
        }
    }

    // 定义图
    public class Graph<T> where T : notnull
    {
        public bool IsDirected { get; }

        // 邻接表：节点 -> 边列表
        public Dictionary<T, List<Edge<T>>> AdjacencyList { get; } = new();

        /// <summary>
        /// 创建无向图
        /// </summary>
        /// <param name="isDirected"></param>
        public Graph(bool isDirected = false)
        {
            IsDirected = isDirected;
        }

        /// <summary>
        /// 添加节点, 如果节点的邻接表不存在则创建
        /// </summary>
        /// <param name="vertex"></param>
        public void AddVertex(T vertex)
        {
            if (!AdjacencyList.ContainsKey(vertex))
                AdjacencyList[vertex] = new List<Edge<T>>();
        }

        /// <summary>
        /// 添一个节点的边
        /// 可以为有向或无向边，
        /// 无向边会自动互相添加邻接边
        /// </summary>
        /// <param name="source">源节点</param>
        /// <param name="target">目标节点</param>
        /// <param name="weight">边的权重</param>
        public void AddEdge(T source, T target, double weight = 1.0)
        {
            AddVertex(source);
            AddVertex(target);

            AdjacencyList[source].Add(new Edge<T>(source, target, weight));
            if (!IsDirected)
            {
                AdjacencyList[target].Add(new Edge<T>(target, source, weight));
            }
        }

        /// <summary>
        /// 获取所有节点
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> GetVertices() => AdjacencyList.Keys;

        /// <summary>
        /// 获取指定节点的邻接边
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public IEnumerable<Edge<T>> GetEdges(T vertex) => AdjacencyList[vertex];
    }

    #endregion 基础结构

    #region 算法实现

    public static class GraphAlgorithms
    {
        #region 遍历算法

        // 广度优先搜索 (BFS)
        public static void BFS<T>(Graph<T> graph, T startNode, Action<T> visitAction) where T : notnull
        {
            var visited = new HashSet<T>(); // 记录已访问的节点
            var queue = new Queue<T>(); // 记录未访问的节点
            var colors = new Dictionary<T, Color>();
            foreach (var vertex in graph.GetVertices())
            {
                colors[vertex] = Color.WHITE;
            }

            visited.Add(startNode);
            queue.Enqueue(startNode);
            colors[startNode] = Color.GRAY;

            while (queue.Count > 0)
            {
                Console.WriteLine($"Queue: {string.Join(", ", queue)}");
                Console.WriteLine($"Visited: {string.Join(", ", visited)}");
                T current = queue.Dequeue(); // 取出未访问的节点
                visitAction(current);

                var edges = graph.GetEdges(current); // 获取当前节点的邻接边

                foreach (var edge in edges)
                {
                    Console.WriteLine($"\t{edge.Source} -> {edge.Target} ({edge.Weight}). Visited {edge.Target}: {visited.Contains(edge.Target)}.");
                }

                // 遍历当前节点的邻接边
                foreach (var edge in edges)
                {
                    var target = edge.Target;
                    // 如果邻接边的目标节点未被访问过
                    if (colors[target] == Color.WHITE)
                    {
                        visited.Add(target); // 标记已访问
                        colors[target] = Color.GRAY;
                        queue.Enqueue(target); // 将邻接边的目标节点加入队列, 以便下次遍历
                    }
                }

                colors[current] = Color.BLACK;
            }
        }

        /// <summary>
        /// 标准 BFS 算法
        /// 输出：distance 距离、parent 前驱、color 颜色
        /// </summary>
        public static (Dictionary<T, int> Distance, Dictionary<T, T> Parent) BFSWithColor<T>(Graph<T> graph, T start, T defaultValue) where T : notnull
        {
            var vertices = graph.GetVertices().ToList();

            // 初始化
            var color = new Dictionary<T, Color>();
            var dist = new Dictionary<T, int>();
            var parent = new Dictionary<T, T>();

            foreach (var v in vertices)
            {
                color[v] = Color.WHITE;
                dist[v] = -1;
                parent[v] = defaultValue;
            }

            // 起点
            color[start] = Color.GRAY;
            dist[start] = 0;
            parent[start] = defaultValue;

            // 队列
            Queue<T> q = new Queue<T>();
            q.Enqueue(start);

            while (q.Count > 0)
            {
                T u = q.Dequeue();

                var edges = graph.GetEdges(u);

                // 遍历 u 的邻居
                foreach (var edge in edges)
                {
                    T v = edge.Target;

                    if (color[v] == Color.WHITE)
                    {
                        color[v] = Color.GRAY;
                        dist[v] = dist[u] + 1;
                        parent[v] = u;
                        q.Enqueue(v);
                    }
                }

                color[u] = Color.BLACK;
            }

            return (dist, parent);
        }

        /// <summary>
        /// 打印从起点到目标节点的路径
        /// </summary>
        public static void PrintPath<T>(T target, Dictionary<T, T> parent, T defaultValue) where T : notnull
        {
            Stack<T> stack = new Stack<T>();
            T current = target;

            while (!EqualityComparer<T>.Default.Equals(current, defaultValue))
            {
                stack.Push(current);
                parent.TryGetValue(current, out T p);
                current = p;
            }

            Console.Write("路径：");
            while (stack.Count > 0)
                Console.Write(stack.Pop() + " ");
            Console.WriteLine();
        }

        // 深度优先搜索 (DFS)
        public static void DFS<T>(Graph<T> graph, T startNode, Action<T> visitAction) where T : notnull
        {
            var visited = new HashSet<T>();
            Console.WriteLine($"--- DFS Recursive from {startNode} ---");
            DFSRecursive(graph, startNode, visited, visitAction);

            Console.WriteLine($"--- DFS Iterative from {startNode} ---");
            DFSIterative(graph, startNode, visitAction);
        }

        private static void DFSRecursive<T>(Graph<T> graph, T current, HashSet<T> visited, Action<T> visitAction) where T : notnull
        {
            visited.Add(current);
            visitAction(current);

            foreach (var edge in graph.GetEdges(current))
            {
                if (!visited.Contains(edge.Target))
                {
                    DFSRecursive(graph, edge.Target, visited, visitAction);
                }
            }
        }

        public static void DFSIterative<T>(Graph<T> graph, T startNode, Action<T> visitAction) where T : notnull
        {
            var visited = new HashSet<T>();
            var stack = new Stack<T>();
            stack.Push(startNode);

            while (stack.Count > 0)
            {
                T current = stack.Pop();
                if (!visited.Contains(current))
                {
                    visited.Add(current);
                    visitAction(current);
                    var reverseEdges = graph.GetEdges(current).Reverse();
                    foreach (var edge in reverseEdges)
                    {
                        stack.Push(edge.Target);
                    }
                }
            }
        }

        #endregion 遍历算法

        #region 最短路径 (Dijkstra)

        public static Dictionary<T, double> Dijkstra<T>(Graph<T> graph, T source) where T : notnull
        {
            var distances = new Dictionary<T, double>();
            // 使用 PriorityQueue (.NET 6+ 引入) 优化性能
            var priorityQueue = new PriorityQueue<T, double>();

            foreach (var vertex in graph.GetVertices())
            {
                distances[vertex] = double.MaxValue;
            }

            distances[source] = 0;
            priorityQueue.Enqueue(source, 0);

            while (priorityQueue.Count > 0)
            {
                T u = priorityQueue.Dequeue();

                foreach (var edge in graph.GetEdges(u))
                {
                    double alt = distances[u] + edge.Weight;
                    if (alt < distances[edge.Target])
                    {
                        distances[edge.Target] = alt;
                        priorityQueue.Enqueue(edge.Target, alt);
                    }
                }
            }

            return distances;
        }

        #endregion 最短路径 (Dijkstra)

        #region 最小生成树 (Kruskal)

        public static List<Edge<T>> Kruskal<T>(Graph<T> graph) where T : notnull
        {
            var mst = new List<Edge<T>>();
            var allEdges = new List<Edge<T>>();

            // 提取所有唯一的边
            foreach (var v in graph.GetVertices())
                foreach (var e in graph.GetEdges(v))
                    allEdges.Add(e);

            // 按权重排序
            var sortedEdges = allEdges.OrderBy(e => e.Weight).ToList();

            // 并查集 (Union-Find)
            var parent = new Dictionary<T, T>();
            foreach (var v in graph.GetVertices()) parent[v] = v;

            T Find(T i)
            {
                if (parent[i].Equals(i)) return i;
                return parent[i] = Find(parent[i]); // 路径压缩
            }

            void Union(T i, T j)
            {
                T rootI = Find(i);
                T rootJ = Find(j);
                if (!rootI.Equals(rootJ)) parent[rootI] = rootJ;
            }

            foreach (var edge in sortedEdges)
            {
                if (!Find(edge.Source).Equals(Find(edge.Target)))
                {
                    mst.Add(edge);
                    Union(edge.Source, edge.Target);
                }
            }

            return mst;
        }

        #endregion 最小生成树 (Kruskal)

        #region 拓扑排序 (Kahn's Algorithm)

        public static List<T> TopologicalSort<T>(Graph<T> graph) where T : notnull
        {
            if (!graph.IsDirected) throw new InvalidOperationException("Topological sort requires a directed graph.");

            var inDegree = new Dictionary<T, int>();
            foreach (var v in graph.GetVertices()) inDegree[v] = 0;

            foreach (var v in graph.GetVertices())
                foreach (var e in graph.GetEdges(v))
                    inDegree[e.Target]++;

            var queue = new Queue<T>();
            foreach (var v in inDegree)
                if (v.Value == 0) queue.Enqueue(v.Key);

            var result = new List<T>();
            while (queue.Count > 0)
            {
                T u = queue.Dequeue();
                result.Add(u);

                foreach (var edge in graph.GetEdges(u))
                {
                    inDegree[edge.Target]--;
                    if (inDegree[edge.Target] == 0) queue.Enqueue(edge.Target);
                }
            }

            if (result.Count != graph.AdjacencyList.Count)
                throw new Exception("Graph contains a cycle; topological sort impossible.");

            return result;
        }

        #endregion 拓扑排序 (Kahn's Algorithm)

        #region 无向图环检测

        public static bool HasCycleUndirected<T>(Graph<T> graph) where T : notnull
        {
            var visited = new HashSet<T>();

            foreach (var vertex in graph.GetVertices())
            {
                if (!visited.Contains(vertex))
                {
                    // 传入 vertex 作为起始点，且初始父节点为 default
                    if (IsUndirectedCycleRecursive(graph, vertex, default!, visited))
                        return true;
                }
            }
            return false;
        }

        private static bool IsUndirectedCycleRecursive<T>(Graph<T> graph, T current, T parent, HashSet<T> visited) where T : notnull
        {
            visited.Add(current);

            foreach (var edge in graph.GetEdges(current))
            {
                T neighbor = edge.Target;

                // 如果邻居没访问过，递归检查
                if (!visited.Contains(neighbor))
                {
                    if (IsUndirectedCycleRecursive(graph, neighbor, current, visited))
                        return true;
                }
                // 如果邻居访问过，且不是当前节点的父节点 -> 发现环！
                else if (!neighbor.Equals(parent))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion 无向图环检测

        #region 有向图环检测

        public static bool HasCycleDirected<T>(Graph<T> graph) where T : notnull
        {
            var visited = new HashSet<T>();
            var onStack = new HashSet<T>(); // 记录当前递归路径

            foreach (var vertex in graph.GetVertices())
            {
                if (IsDirectedCycleRecursive(graph, vertex, visited, onStack))
                    return true;
            }

            foreach (var vertex in graph.GetVertices())
            {
                if (IsDirectedCycleIterative(graph, vertex, visited, onStack))
                    return true;
            }

            return false;
        }

        private static bool IsDirectedCycleRecursive<T>(Graph<T> graph, T current, HashSet<T> visited, HashSet<T> onStack) where T : notnull
        {
            if (onStack.Contains(current)) return true; // 命中当前路径 -> 环
            if (visited.Contains(current)) return false; // 已经处理过该子树 -> 无环

            visited.Add(current);
            onStack.Add(current);

            foreach (var edge in graph.GetEdges(current))
            {
                if (IsDirectedCycleRecursive(graph, edge.Target, visited, onStack))
                    return true;
            }

            onStack.Remove(current); // 回溯：出栈
            return false;
        }

        private static bool IsDirectedCycleIterative<T>(Graph<T> graph, T current, HashSet<T> visited, HashSet<T> onStack) where T : notnull
        {
            // Preserve original short-circuit behavior
            if (onStack.Contains(current))
            {
                return true;
            }

            if (visited.Contains(current))
            {
                return false;
            }

            // Iterative DFS emulating recursion using an explicit stack of enumerators
            var stack = new Stack<(T node, IEnumerator<Edge<T>> enumerator)>();
            bool foundCycle = false;

            // Push start node
            visited.Add(current);
            onStack.Add(current);
            var startEnum = graph.GetEdges(current).GetEnumerator();
            stack.Push((current, startEnum));

            try
            {
                while (stack.Count > 0)
                {
                    var (node, enumerator) = stack.Peek();

                    if (enumerator.MoveNext())
                    {
                        var neighbor = enumerator.Current.Target;

                        if (onStack.Contains(neighbor))
                        {
                            foundCycle = true;
                            break;
                        }

                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            onStack.Add(neighbor);
                            var neighEnum = graph.GetEdges(neighbor).GetEnumerator();
                            stack.Push((neighbor, neighEnum));
                        }
                        // if visited and not onStack, ignore
                    }
                    else
                    {
                        // finished exploring node
                        stack.Pop();
                        enumerator.Dispose();
                        onStack.Remove(node);
                    }
                }
            }
            finally
            {
                // Ensure any remaining enumerators are disposed and onStack cleaned up
                while (stack.Count > 0)
                {
                    var frame = stack.Pop();
                    try { frame.enumerator.Dispose(); } catch { }
                    onStack.Remove(frame.node);
                }
            }

            return foundCycle;
        }

        // 辅助类：记录当前节点的访问进度
        private class NodeVisitState<T>
        {
            public T Node { get; }
            public IEnumerator<Edge<T>> EdgeEnumerator { get; }

            public NodeVisitState(T node, IEnumerator<Edge<T>> enumerator)
            {
                Node = node;
                EdgeEnumerator = enumerator;
            }
        }

        public static bool HasCycleDirectedIterative<T>(Graph<T> graph) where T : notnull
        {
            var visited = new HashSet<T>();
            var onStack = new HashSet<T>();

            foreach (var startNode in graph.GetVertices())
            {
                if (visited.Contains(startNode)) continue;

                // 模拟递归栈
                var stack = new Stack<NodeVisitState<T>>();

                // 初始进入
                visited.Add(startNode);
                onStack.Add(startNode);
                stack.Push(new NodeVisitState<T>(startNode, graph.GetEdges(startNode).GetEnumerator()));

                while (stack.Count > 0)
                {
                    var currentState = stack.Peek();
                    T u = currentState.Node;

                    // 尝试移动到下一个邻居
                    if (currentState.EdgeEnumerator.MoveNext())
                    {
                        T v = currentState.EdgeEnumerator.Current.Target;

                        if (onStack.Contains(v))
                        {
                            return true; // 发现后向边 -> 有环！
                        }

                        if (!visited.Contains(v))
                        {
                            visited.Add(v);
                            onStack.Add(v);
                            // 像递归调用一样，将新节点压栈，并为其创建邻居迭代器
                            stack.Push(new NodeVisitState<T>(v, graph.GetEdges(v).GetEnumerator()));
                        }
                    }
                    else
                    {
                        // 所有邻居都访问完了，执行回溯操作
                        stack.Pop();
                        onStack.Remove(u);
                    }
                }
            }
            return false;
        }

        #endregion 有向图环检测
    }

    #endregion 算法实现

    // --- 测试程序 ---
    internal class Program
    {
        public interface ITestInterface
        {
            int ID { get; set; }
            string Name { get; set; }
        }

        public class TestClass : ITestInterface
        {
            public int ID { get; set; }
            public string Name { get; set; }
        }

        private static void Main()
        {
            // 1. 创建一个加权无向图
            var graph = new Graph<string>(isDirected: false);
            graph.AddEdge("A", "B", 4);
            graph.AddEdge("A", "C", 2);
            graph.AddEdge("B", "C", 1);
            graph.AddEdge("B", "D", 5);
            graph.AddEdge("C", "D", 8);
            graph.AddEdge("C", "E", 10);
            graph.AddEdge("D", "E", 2);

            Console.WriteLine("--- BFS Traversal from A ---");
            GraphAlgorithms.BFS(graph, "A", node => Console.WriteLine("current Node: " + node + " "));
            Console.WriteLine("\n");

            Console.WriteLine("--- DFS Traversal from A ---");
            GraphAlgorithms.DFS(graph, "A", node => Console.WriteLine("current Node: " + node + " "));
            Console.WriteLine("\n");

            Console.WriteLine("--- Dijkstra Shortest Paths from A ---");
            var dists = GraphAlgorithms.Dijkstra(graph, "A");
            foreach (var kvp in dists) Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            Console.WriteLine();

            Console.WriteLine("--- Kruskal MST Edges ---");
            var mst = GraphAlgorithms.Kruskal(graph);
            foreach (var e in mst) Console.WriteLine($"{e.Source} - {e.Target} ({e.Weight})");
            Console.WriteLine();

            // 2. 测试拓扑排序 (有向无环图 DAG)
            var dag = new Graph<int>(isDirected: true);
            dag.AddEdge(5, 2);
            dag.AddEdge(5, 0);
            dag.AddEdge(4, 0);
            dag.AddEdge(4, 1);
            dag.AddEdge(2, 3);
            dag.AddEdge(3, 1);

            Console.WriteLine("--- Topological Sort ---");
            var sorted = GraphAlgorithms.TopologicalSort(dag);
            Console.WriteLine(string.Join(" -> ", sorted));

            // 1. 测试无向图
            var uGraph = new Graph<string>(isDirected: false);
            uGraph.AddEdge("A", "B");
            uGraph.AddEdge("B", "C");
            uGraph.AddEdge("C", "A"); // 形成环 A-B-C-A
            Console.WriteLine($"Undirected Graph has cycle: {GraphAlgorithms.HasCycleUndirected(uGraph)}"); // True

            // 2. 测试有向图 (无环)
            var dGraph1 = new Graph<string>(isDirected: true);
            dGraph1.AddEdge("A", "B");
            dGraph1.AddEdge("B", "C");
            Console.WriteLine($"Directed Graph 1 has cycle: {GraphAlgorithms.HasCycleDirected(dGraph1)}"); // False

            // 3. 测试有向图 (有环)
            var dGraph2 = new Graph<string>(isDirected: true);
            dGraph2.AddEdge("A", "B");
            dGraph2.AddEdge("B", "C");
            dGraph2.AddEdge("C", "A"); // 形成环
            Console.WriteLine($"Directed Graph 2 has cycle: {GraphAlgorithms.HasCycleDirected(dGraph2)}"); // True

            //-----------------------------------------------------------------------//
            Graph<char> graphNew = new Graph<char>(isDirected: false);

            // 2. 添加边（自带环，测试是否死循环）
            graphNew.AddEdge('s', 'a');
            graphNew.AddEdge('s', 'b');
            graphNew.AddEdge('a', 'd');
            graphNew.AddEdge('b', 'c');
            graphNew.AddEdge('c', 'd');
            graphNew.AddEdge('a', 'c'); // 构造环：s → a → c → b → s

            // 3. BFS
            var (distance, parent) = GraphAlgorithms.BFSWithColor(graphNew, 's', '\0');

            // 4. 输出结果
            Console.WriteLine("顶点\t距离\t路径");
            foreach (var v in graphNew.GetVertices())
            {
                Console.Write($"{v}\t{distance[v]}\t");
                GraphAlgorithms.PrintPath(v, parent, '\0');
            }

            Console.ReadLine();
        }
    }
}