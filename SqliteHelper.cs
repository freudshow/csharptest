using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ==============================================================
// 辅助模型类 (Model Class)
// 这是一个代表数据库表中一行数据的 C# 模型。
// ==============================================================
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }

    public override string ToString()
    {
        return $"ID: {Id}, Username: {Username}, Email: {Email}, Age: {Age}";
    }
}

/// <summary>
/// SQLite数据库操作的辅助类，提供连接管理、创建表、增删改查等常用功能。
/// </summary>
public class SQLite3Helper
{
    // 数据库连接字符串。
    private readonly string _connectionString;

    /// <summary>
    /// 初始化辅助类，传入数据库文件路径。
    /// </summary>
    /// <param name="dbPath">数据库文件的物理路径 (e.g., "Data/myDatabase.sqlite")</param>
    public SQLite3Helper(string dbPath)
    {
        // 构造连接字符串
        _connectionString = $"Data Source={dbPath}";
    }

    /// <summary>
    /// 确保数据库连接和表结构是存在的。
    /// </summary>
    /// <param name="tableName">要操作的表名</param>
    /// <param name="createTableSql">创建表的SQL语句 (必须包含所有字段定义)</param>
    public void InitializeDatabase(string tableName, string createTableSql)
    {
        Console.WriteLine("--- 初始化数据库和表结构 ---");

        // 使用 using 确保连接在使用完毕后被正确释放
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();

            // 创建表，如果该表已经存在，则忽略 (IF NOT EXISTS)
            string createSql = $"CREATE TABLE IF NOT EXISTS {tableName} ({createTableSql});";
            using (var command = new SqliteCommand(createSql, connection))
            {
                command.ExecuteNonQuery();
                Console.WriteLine($"[成功] 表 {tableName} 检查/创建成功。");
            }
        }
    }

    // ==============================================================
    // 💡 核心执行方法 (ExecuteNonQuery)
    // 用于 INSERT, UPDATE, DELETE, CREATE 等不返回数据集的操作
    // ==============================================================
    /// <summary>
    /// 执行非查询操作（如插入、更新、删除）。
    /// </summary>
    /// <param name="sql">SQL语句。</param>
    /// <param name="parameters">参数数组，用于参数化查询，防止SQL注入。</param>
    /// <returns>影响的行数。</returns>
    public int ExecuteNonQuery(string sql, params (string name, object value)[] parameters)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = new SqliteCommand(sql, connection))
            {
                // 遍历参数数组，将参数安全地添加到命令对象
                foreach (var p in parameters)
                {
                    command.Parameters.AddWithValue(p.name, p.value);
                }

                try
                {
                    var rowsAffected = command.ExecuteNonQuery();
                    Console.WriteLine($"[SQL执行成功] 影响行数: {rowsAffected}");
                    return rowsAffected;
                }
                catch (SqliteException ex)
                {
                    Console.WriteLine($"[❌ SQL执行失败] SQL: {sql}\n错误: {ex.Message}");
                    return -1;
                }
            }
        }
    }

    // ==============================================================
    // 💡 读取方法 (ExecuteReader)
    // 最强大的方法，支持将结果集映射到任何复杂的C#对象 T
    // ==============================================================
    /// <typeparam name="T">目标类型，用于映射结果集。通常是DTO或Model类。</typeparam>
    /// <param name="sql">SQL查询语句。</param>
    /// <param name="mapper">一个委托函数，负责从 DataReader 中读取一行数据并映射到对象 T。</param>
    /// <param name="parameters">查询参数数组。</param>
    /// <returns>符合查询结果的列表。</returns>
    public IEnumerable<T> ExecuteReader<T>(string sql, Func<SqliteDataReader, T> mapper,
                                           params (string name, object value)[] parameters)
    {
        var results = new List<T>();

        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using (var command = new SqliteCommand(sql, connection))
            {
                // 添加参数
                foreach (var p in parameters)
                {
                    command.Parameters.AddWithValue(p.name, p.value);
                }

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // 使用提供的映射函数处理每行数据
                        results.Add(mapper(reader));
                    }
                }
            }
        }
        return results;
    }

    // ==============================================================
    // 📚 封装的业务方法 (CRUD示例)
    // ==============================================================

    /// <summary>
    /// 插入新用户记录。
    /// </summary>
    public int InsertUser(string username, string email, int age)
    {
        // SQL模板：注意使用 @paramName 占位符
        const string sql =
            "INSERT INTO Users (Username, Email, Age) VALUES (@username, @email, @age);";

        (string name, object value)[] parameters =
        {
            ("username", username),
            ("email", email),
            ("age", age)
        };

        return ExecuteNonQuery(sql, parameters);
    }

    /// <summary>
    /// 根据用户ID查询单个用户。
    /// </summary>
    public User GetUserById(int userId)
    {
        const string sql = "SELECT * FROM Users WHERE Id = @id LIMIT 1;";

        // 查询参数
        (string name, object value)[] parameters = { ("id", userId) };

        // 结果映射函数：当读取到数据时，将 SqliteDataReader 映射为 User 对象
        var mapper = (SqliteDataReader reader) => new User
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            Age = reader.GetInt32(reader.GetOrdinal("Age"))
        };

        var results = ExecuteReader<User>(sql, mapper, parameters);

        return results.FirstOrDefault();
    }

    /// <summary>
    /// 更新用户的年龄。
    /// </summary>
    public int UpdateUserAge(int userId, int newAge)
    {
        const string sql = "UPDATE Users SET Age = @age WHERE Id = @id;";
        (string name, object value)[] parameters =
        {
            ("age", newAge),
            ("id", userId)
        };
        return ExecuteNonQuery(sql, parameters);
    }

    /// <summary>
    /// 删除用户记录。
    /// </summary>
    public int DeleteUser(int userId)
    {
        const string sql = "DELETE FROM Users WHERE Id = @id;";
        (string name, object value)[] parameters =
        {
            ("id", userId)
        };
        return ExecuteNonQuery(sql, parameters);
    }

    public static void Main(string[] args)
    {
        // 1. 定义数据库路径
        string dbFilePath = "data_test.sqlite";

        // 2. 清理旧文件，确保每次运行都是全新的测试环境
        if (File.Exists(dbFilePath))
        {
            File.Delete(dbFilePath);
        }

        // 3. 实例化 Helper
        var helper = new SQLite3Helper(dbFilePath);

        // 4. 初始化数据库 (创建 Users 表)
        // 注意：表结构必须包含所有字段的定义。
        string createTableSql =
            "Id INTEGER PRIMARY KEY AUTOINCREMENT, " +
            "Username TEXT NOT NULL, " +
            "Email TEXT NOT NULL UNIQUE, " +
            "Age INTEGER";

        helper.InitializeDatabase("Users", createTableSql);

        // =========================================================
        // 🚀 流程演示：插入 -> 查询 -> 更新 -> 删除
        // =========================================================

        Console.WriteLine("\n==========================================");
        Console.WriteLine("  --- 1. 执行插入 (INSERT) ---");

        // 插入第一条数据 (成功)
        int result1 = helper.InsertUser("Alice", "alice@example.com", 30);
        Console.WriteLine($"插入 Alice 结果: {result1} 行影响。");

        // 插入第二条数据 (成功)
        int result2 = helper.InsertUser("Bob", "bob@example.com", 25);
        Console.WriteLine($"插入 Bob 结果: {result2} 行影响。");

        // 尝试插入重复的Email，会触发FOREIGN KEY或UNIQUE约束错误
        Console.WriteLine("\n>>> 尝试插入重复邮箱 (预期失败):");
        helper.InsertUser("Charlie", "alice@example.com", 40);

        Console.WriteLine("\n==========================================");
        Console.WriteLine("  --- 2. 执行查询 (READ) ---");

        // 通过ID查询数据 (这里假设Alice的ID是1，因为是AUTOINCREMENT)
        var user1 = helper.GetUserById(1);
        if (user1 != null)
        {
            Console.WriteLine("查询到的用户 (Alice): " + user1);
        }
        else
        {
            Console.WriteLine("用户未找到。");
        }

        Console.WriteLine("\n==========================================");
        Console.WriteLine("  --- 3. 执行更新 (UPDATE) ---");

        // 更新用户年龄
        int updateCount = helper.UpdateUserAge(1, 31);
        Console.WriteLine($"更新用户ID=1年龄成功: {updateCount} 行影响。");

        // 重新查询，验证更新
        var updatedUser = helper.GetUserById(1);
        if (updatedUser != null)
        {
            Console.WriteLine("更新后的用户 (Alice): " + updatedUser);
        }

        Console.WriteLine("\n==========================================");
        Console.WriteLine("  --- 4. 执行删除 (DELETE) ---");

        // 删除用户
        int deleteCount = helper.DeleteUser(2);
        Console.WriteLine($"删除用户ID=2成功: {deleteCount} 行影响。");

        // 验证删除
        var deletedUserCheck = helper.GetUserById(2);
        Console.WriteLine("删除后检查用户ID=2: " + (deletedUserCheck == null ? "未找到 (成功)" : "仍然存在 (失败)"));

        Console.WriteLine("\n==========================================");
        Console.WriteLine("数据库操作流程全部完成。");

        Console.ReadLine();
    }
}