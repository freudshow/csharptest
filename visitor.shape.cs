using System;
using System.Collections.Generic;

namespace Visitor
{
    // ------------------------------
    // 1. 抽象元素（Element）：声明接受访问者的方法
    // ------------------------------
    internal interface IShapeElement
    {
        // 接受访问者的方法，参数为抽象访问者
        void Accept(IShapeVisitor visitor);
    }

    // ------------------------------
    // 2. 具体元素（ConcreteElement）：实现接受方法
    // ------------------------------
    // 具体元素1：圆
    internal class Circle : IShapeElement
    {
        public int Radius { get; } // 半径

        public Circle(int radius)
        {
            Radius = radius;
        }

        // 接受访问者：调用访问者针对“圆”的访问方法
        public void Accept(IShapeVisitor visitor)
        {
            visitor.Visit(this); // 将自身作为参数传入
        }
    }

    // 具体元素2：矩形
    internal class Rectangle : IShapeElement
    {
        public int Width { get; }  // 宽
        public int Height { get; } // 高

        public Rectangle(int width, int height)
        {
            Width = width;
            Height = height;
        }

        // 接受访问者：调用访问者针对“矩形”的访问方法
        public void Accept(IShapeVisitor visitor)
        {
            visitor.Visit(this); // 将自身作为参数传入
        }
    }

    // ------------------------------
    // 3. 抽象访问者（Visitor）：声明对所有具体元素的访问方法
    // ------------------------------
    internal interface IShapeVisitor
    {
        // 访问“圆”的方法
        void Visit(Circle circle);

        // 访问“矩形”的方法
        void Visit(Rectangle rectangle);
    }

    // ------------------------------
    // 4. 具体访问者（ConcreteVisitor）：实现具体操作
    // ------------------------------
    // 具体访问者1：计算面积
    internal class AreaVisitor : IShapeVisitor
    {
        // 计算圆的面积：π * r²
        public void Visit(Circle circle)
        {
            double area = Math.PI * circle.Radius * circle.Radius;
            Console.WriteLine($"圆的面积：{area:F2}");
        }

        // 计算矩形的面积：宽 * 高
        public void Visit(Rectangle rectangle)
        {
            int area = rectangle.Width * rectangle.Height;
            Console.WriteLine($"矩形的面积：{area}");
        }
    }

    // 具体访问者2：计算周长
    internal class PerimeterVisitor : IShapeVisitor
    {
        // 计算圆的周长：2 * π * r
        public void Visit(Circle circle)
        {
            double perimeter = 2 * Math.PI * circle.Radius;
            Console.WriteLine($"圆的周长：{perimeter:F2}");
        }

        // 计算矩形的周长：2 * (宽 + 高)
        public void Visit(Rectangle rectangle)
        {
            int perimeter = 2 * (rectangle.Width + rectangle.Height);
            Console.WriteLine($"矩形的周长：{perimeter}");
        }
    }

    // ------------------------------
    // 5. 对象结构（ObjectStructure）：管理元素集合，提供遍历接口
    // ------------------------------
    internal class ObjectStructure
    {
        private List<IShapeElement> _elements = new List<IShapeElement>();

        // 添加元素到集合
        public void AddElement(IShapeElement element)
        {
            _elements.Add(element);
        }

        // 让所有元素接受访问者的访问
        public void Accept(IShapeVisitor visitor)
        {
            foreach (var element in _elements)
            {
                element.Accept(visitor); // 每个元素调用自己的Accept方法
            }
        }
    }

    // ------------------------------
    // 测试代码
    // ------------------------------
    internal class shapeProgram
    {
        private static void Main(string[] args)
        {
            // 创建对象结构并添加元素
            ObjectStructure structure = new ObjectStructure();
            structure.AddElement(new Circle(5));       // 半径为5的圆
            structure.AddElement(new Rectangle(4, 6)); // 4x6的矩形

            // 创建访问者并执行操作
            IShapeVisitor areaVisitor = new AreaVisitor();
            Console.WriteLine("=== 计算面积 ===");
            structure.Accept(areaVisitor); // 所有元素接受面积访问者

            IShapeVisitor perimeterVisitor = new PerimeterVisitor();
            Console.WriteLine("\n=== 计算周长 ===");
            structure.Accept(perimeterVisitor); // 所有元素接受周长访问者

            Console.ReadLine();
        }
    }
}