// 访问者接口
internal interface IVisitor
{
    void Visit(Computer computer);

    void Visit(Printer printer);
}

// 元素接口
internal interface IElement
{
    void Accept(IVisitor visitor);
}

// 具体元素：电脑
internal class Computer : IElement
{
    public string Name { get; }
    public List<string> Parts { get; } = [];

    public Computer(string name, IEnumerable<string> initialParts)
    {
        Name = name;
        Parts.AddRange(initialParts);
    }

    public void Accept(IVisitor visitor) => visitor.Visit(this);
}

// 具体元素：打印机
internal class Printer : IElement
{
    public string Name { get; }
    public List<string> Parts { get; } = [];

    public Printer(string name, IEnumerable<string> initialParts)
    {
        Name = name;
        Parts.AddRange(initialParts);
    }

    public void Accept(IVisitor visitor) => visitor.Visit(this);
}

internal class HardwareMaintenanceVisitor : IVisitor
{
    public void Visit(Computer computer)
    {
        Console.WriteLine($"对电脑 {computer.Name} 做硬件检查，当前零件: {string.Join(",", computer.Parts)}");
    }

    public void Visit(Printer printer)
    {
        Console.WriteLine($"对打印机 {printer.Name} 做硬件检查，当前零件: {string.Join(",", printer.Parts)}");
    }
}

// 软件维护 Visitor（实现软件维护操作）
internal class SoftwareMaintenanceVisitor : IVisitor
{
    public void Visit(Computer computer)
    {
        Console.WriteLine($"对电脑 {computer.Name} 更新/检查软件");
    }

    public void Visit(Printer printer)
    {
        Console.WriteLine($"对打印机 {printer.Name} 更新/检查固件");
    }
}

// 新增操作：更换零件 Visitor —— 无需修改 Computer/Printer 类
internal class ReplacePartVisitor : IVisitor
{
    private readonly string _oldPart;
    private readonly string _newPart;

    public ReplacePartVisitor(string oldPart, string newPart)
    {
        _oldPart = oldPart;
        _newPart = newPart;
    }

    public void Visit(Computer computer)
    {
        Replace(computer.Parts, computer.Name);
    }

    public void Visit(Printer printer)
    {
        Replace(printer.Parts, printer.Name);
    }

    private void Replace(List<string> parts, string deviceName)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] == _oldPart)
            {
                parts[i] = _newPart;
                Console.WriteLine($"已在 {deviceName} 上将 {_oldPart} 替换为 {_newPart}");
                return;
            }
        }

        Console.WriteLine($"{deviceName} 上未发现 {_oldPart}，无法替换");
    }
}

public class Program
{
    private static void VisitorMain(string[] args)
    //private static void Main(string[] args)
    {
        IElement[] devices = [
            new Computer("PC-01", ["CPU", "RAM", "SSD"]),
            new Printer("PR-01", ["Head", "Roller"])
        ];

        IVisitor hwVisitor = new HardwareMaintenanceVisitor();
        IVisitor swVisitor = new SoftwareMaintenanceVisitor();
        IVisitor replaceVisitor = new ReplacePartVisitor("RAM", "RAM-16G");

        // 硬件检查
        foreach (var d in devices)
        {
            d.Accept(hwVisitor);
        }

        // 软件维护
        foreach (var d in devices)
        {
            d.Accept(swVisitor);
        }

        // 新增操作：更换零件（不修改元素类）
        foreach (var d in devices)
        {
            d.Accept(replaceVisitor);
        }

        Console.ReadLine();
    }
}