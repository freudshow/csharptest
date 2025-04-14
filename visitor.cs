using System;

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
    public void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }

    public void PerformHardwareMaintenance()
    {
        Console.WriteLine("电脑正在进行硬件维护");
    }

    public void PerformSoftwareMaintenance()
    {
        Console.WriteLine("电脑正在进行软件维护");
    }
}

// 具体元素：打印机
internal class Printer : IElement
{
    public void Accept(IVisitor visitor)
    {
        visitor.Visit(this);
    }

    public void PerformHardwareMaintenance()
    {
        Console.WriteLine("打印机正在进行硬件维护");
    }

    public void PerformSoftwareMaintenance()
    {
        Console.WriteLine("打印机正在进行软件维护");
    }
}

// 具体访问者：硬件维护员
internal class HardwareMaintenanceVisitor : IVisitor
{
    public void Visit(Computer computer)
    {
        computer.PerformHardwareMaintenance();
    }

    public void Visit(Printer printer)
    {
        printer.PerformHardwareMaintenance();
    }
}

// 具体访问者：软件维护员
internal class SoftwareMaintenanceVisitor : IVisitor
{
    public void Visit(Computer computer)
    {
        computer.PerformSoftwareMaintenance();
    }

    public void Visit(Printer printer)
    {
        printer.PerformSoftwareMaintenance();
    }
}

internal class Program
{
    private static void VisitorMain()
    {
        IElement[] devices = { new Computer(), new Printer() };

        IVisitor hardwareVisitor = new HardwareMaintenanceVisitor();
        IVisitor softwareVisitor = new SoftwareMaintenanceVisitor();

        // 进行硬件维护
        foreach (var device in devices)
        {
            device.Accept(hardwareVisitor);
        }

        // 进行软件维护
        foreach (var device in devices)
        {
            device.Accept(softwareVisitor);
        }
    }
}