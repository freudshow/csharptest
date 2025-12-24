using System;
using System.Collections.Generic;
using System.Linq;

namespace TreeAndQueueConsole
{
    /// <summary>
    /// 树形节点类（带深度限制，避免栈溢出）
    /// </summary>
    public class TreeNode
    {
        public int Value { get; set; }
        public List<TreeNode> Children { get; private set; }
        private int _depth;
        public const int MaxDepth = 3; // 最大递归深度，避免栈溢出
        public const int MinChildCount = 2;
        public const int MaxChildCount = 3;

        public TreeNode(int value, int depth = 0)
        {
            Value = value;
            _depth = depth;
            Children = new List<TreeNode>();

            if (_depth >= MaxDepth) return;

            Random random = new Random(Guid.NewGuid().GetHashCode());
            int childCount = random.Next(MinChildCount, MaxChildCount + 1);
            for (int i = 0; i < childCount; i++)
            {
                Children.Add(new TreeNode(random.Next(100, 999), _depth + 1));
            }
        }

        public bool AddChild(TreeNode childNode)
        {
            if (Children.Count >= MaxChildCount)
            {
                Console.WriteLine($"⚠️ 节点[{Value}]已达最大子节点数（{MaxChildCount}个），添加失败！");
                return false;
            }

            Children.Add(childNode);
            Console.WriteLine($"✅ 节点[{Value}]成功添加子节点[{childNode.Value}]");
            return true;
        }
    }

    internal class Program
    {
        private static readonly Random _random = new Random();

        private static void TreeMain(string[] args)
        {
            int step = 1;
            Console.WriteLine("===== 树形结构 + 队列 + 全节点遍历演示程序 =====");
            Console.WriteLine("操作流程：创建树 → 初始化队列 → BFS操作 → 添加节点 → 遍历整个树（DFS+BFS）\n");

            // -------------------------- 基础操作 --------------------------
            Console.WriteLine($"【操作{step}】创建根节点（自动生成{TreeNode.MinChildCount}~{TreeNode.MaxChildCount}个子节点，最大深度{TreeNode.MaxDepth + 1}层）");
            TreeNode root = new TreeNode(_random.Next(1, 100), 0);
            Console.WriteLine($"✅ 根节点创建成功");
            PrintTreeIterative(root);
            step++;

            // -------------------------- 新增：遍历整个树的所有节点 --------------------------
            Console.WriteLine($"【操作{step}】遍历整个树的所有节点（深度优先DFS）");
            List<TreeNode> dfsAllNodes = TraverseTreeDFS(root);
            PrintTraversalResult("DFS", dfsAllNodes);
            Console.WriteLine("----------------------------------------\n");
            step++;

            Console.WriteLine($"【操作{step}】遍历整个树的所有节点（广度优先BFS）");
            List<TreeNode> bfsAllNodes = TraverseTreeBFS(root);
            PrintTraversalResult("BFS", bfsAllNodes);
            Console.WriteLine("----------------------------------------\n");
            step++;

            // -------------------------- 验证：遍历结果统计 --------------------------
            Console.WriteLine($"【操作{step}】遍历结果统计");
            Console.WriteLine($"✅ DFS遍历总节点数：{dfsAllNodes.Count}");
            Console.WriteLine($"✅ BFS遍历总节点数：{bfsAllNodes.Count}");
            Console.WriteLine($"✅ 两次遍历节点数是否一致：{dfsAllNodes.Count == bfsAllNodes.Count}");
            Console.WriteLine("----------------------------------------\n");

            Console.WriteLine("===== 演示结束 =====");
            Console.ReadKey();
        }

        #region 核心工具方法

        /// <summary>
        /// 迭代打印树形结构（避免递归栈溢出）
        /// </summary>
        private static void PrintTreeIterative(TreeNode root)
        {
            if (root == null) return;

            Stack<(TreeNode Node, int Depth)> stack = new Stack<(TreeNode, int)>();
            stack.Push((root, 0));

            while (stack.Count > 0)
            {
                var (node, depth) = stack.Pop();
                string indent = new string(' ', depth * 4);
                string nodeSymbol = depth == 0 ? "" : "├─ ";
                Console.WriteLine($"{indent}{nodeSymbol}节点[{node.Value}]（子节点数：{node.Children.Count}）");

                // 反向入栈，保证打印顺序与递归一致
                for (int i = node.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push((node.Children[i], depth + 1));
                }
            }
        }

        /// <summary>
        /// 深度优先遍历（DFS）整个树，返回所有节点（迭代实现）
        /// </summary>
        private static List<TreeNode> TraverseTreeDFS(TreeNode root)
        {
            List<TreeNode> allNodes = new List<TreeNode>();
            if (root == null) return allNodes;

            Stack<TreeNode> stack = new Stack<TreeNode>();
            stack.Push(root);

            Console.WriteLine("🔍 DFS遍历过程（先根后子，深度优先）：");
            while (stack.Count > 0)
            {
                TreeNode current = stack.Pop();
                allNodes.Add(current);
                Console.Write($"{current.Value} → ");

                // 反向入栈子节点，保证遍历顺序从左到右
                for (int i = current.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(current.Children[i]);
                }
            }
            Console.WriteLine("遍历结束");
            return allNodes;
        }

        /// <summary>
        /// 广度优先遍历（BFS）整个树，返回所有节点（迭代实现）
        /// </summary>
        private static List<TreeNode> TraverseTreeBFS(TreeNode root)
        {
            List<TreeNode> allNodes = new List<TreeNode>();
            if (root == null) return allNodes;

            Queue<TreeNode> queue = new Queue<TreeNode>();
            queue.Enqueue(root);

            Console.WriteLine("🔍 BFS遍历过程（按层级遍历，广度优先）：");
            while (queue.Count > 0)
            {
                TreeNode current = queue.Dequeue();
                allNodes.Add(current);
                Console.Write($"{current.Value} → ");

                // 子节点入队，按层级遍历
                foreach (var child in current.Children)
                {
                    queue.Enqueue(child);
                }
            }
            Console.WriteLine("遍历结束");
            return allNodes;
        }

        /// <summary>
        /// 打印遍历结果（简化输出）
        /// </summary>
        private static void PrintTraversalResult(string traversalType, List<TreeNode> nodes)
        {
            Console.WriteLine($"\n📊 {traversalType}遍历结果（所有节点值）：");
            string nodeValues = string.Join(", ", nodes.Select(n => n.Value));
            Console.WriteLine($"总节点数：{nodes.Count} | 节点值列表：{nodeValues}");
        }

        #endregion 核心工具方法
    }
}